using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailManager;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Configuration;

namespace ReceiptEmailer
{
    public class Reconciliation
    {
        private readonly IList<IEmail> _emailsSent;
        private readonly IList<string> _tickets;
        private const string DefaultAgency = "FN";
        private const string DefaultCampus = "F";
        private const string DefaultUsername = "ReceiptRecon";

        public Reconciliation(IEnumerable<IEmail> emailsSent, IEnumerable<string> tickets)
        {
            _emailsSent = emailsSent?.ToList() ?? new List<IEmail>();
            _tickets = tickets?.ToList() ?? new List<string>();
        }
        
        private IList<IAttachment> BuildAttachments(DataRowCollection allReceipts)
        {
            var attachments = new List<IAttachment>();
            
            if (allReceipts?.Count > 0)
            {
                var reportCsv = GetTableData(allReceipts);
                var attachmentData = Encoding.UTF8.GetBytes(reportCsv);
                attachments.Add(new Attachment(attachmentData, "All Gift Receipts.csv"));
            }

            return attachments;
        }

        private string BuildEmailBody(ReconData reconData)
        {
            var htmlTemplateContent = LoadEmailTemplate();
            var ticketInfo = BuildTicketInfo();
            
            return string.Format(htmlTemplateContent,
                reconData.AllReceipts.Count.ToString(),
                reconData.PaperReceipts.Count.ToString(),
                reconData.EReceipts.Count.ToString(),
                _emailsSent.Count.ToString(),
                reconData.ErrorCount.ToString(),
                ticketInfo);
        }

        private string LoadEmailTemplate()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ReceiptEmailer.ReconEmailBodyTemplate.html"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private string BuildTicketInfo()
        {
            if (_tickets?.Count == 0)
                return string.Empty;

            var ticketInfo = new StringBuilder(" - Issue Tickets: ");
            
            for (int i = 0; i < _tickets.Count; i++)
            {
                var ticketId = _tickets[i];
                ticketInfo.Append("<a href=\"https://issues.company.com/browse/");
                ticketInfo.Append(ticketId);
                ticketInfo.Append("\">");
                ticketInfo.Append(ticketId);
                ticketInfo.Append("</a>");
                
                if (i < _tickets.Count - 1)
                    ticketInfo.Append(", ");
            }
            
            return ticketInfo.ToString();
        }

        public static async Task<IList<string>> ProcessUndeliverablesAsync(DateTime fromDate)
        {
            var tickets = new List<string>();
            var emailManager = CreateEmailManager();
            var subject = "Undeliverable: " + ReceiptEmailerSettings.Default.eReceiptEmailSubjectSetting;
            var ereceiptEmails = new List<IEmail>();
            
            var targetDate = fromDate.Date;
            while (targetDate <= DateTime.Today)
            {
                var ndrs = await emailManager.GetEmailsAsync(subject, targetDate);
                ereceiptEmails.AddRange(ndrs);
                targetDate = targetDate.AddDays(1);
            }

            var processedAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var ereceiptEmail in ereceiptEmails)
            {
                if (!ereceiptEmail.From.ToUpper().Contains("MICROSOFT"))
                    continue;

                var undeliverableAddress = ExtractEmailAddress(ereceiptEmail.Body);
                if (string.IsNullOrEmpty(undeliverableAddress) || processedAddresses.Contains(undeliverableAddress))
                    continue;

                processedAddresses.Add(undeliverableAddress);
                var ticket = await CreateUndeliverableTicketAsync(undeliverableAddress, fromDate, ereceiptEmail);
                
                if (!string.IsNullOrEmpty(ticket))
                    tickets.Add(ticket);
            }

            return tickets;
        }

        private static EmailManager CreateEmailManager()
        {
            var emailManager = new EmailManager();
            emailManager.SmtpUsername = ReceiptEmailerSettings.Default.eReceiptEmailUndeliverableUsernameSetting;
            emailManager.SmtpPassword = ReceiptEmailerSettings.Default.eReceiptEmailUndeliverablePasswordSetting;
            return emailManager;
        }

        private static async Task<string> CreateUndeliverableTicketAsync(string undeliverableAddress, DateTime fromDate, IEmail ereceiptEmail)
        {
            try
            {
                var issueTracker = new IssueTracker.IssueManager();
                var attachments = ConvertToIssueAttachments(ereceiptEmail.Attachments);
                var eReceipts = await GetEmailsFromArchiveAsync(undeliverableAddress, fromDate);
                var receiptIds = ExtractReceiptIds(eReceipts);
                var body = BuildUndeliverableTicketBody(undeliverableAddress, receiptIds);
                
                var title = $"{ereceiptEmail.ReceivedDate.ToShortDateString()} - {undeliverableAddress} - Undeliverable Receipt";
                
                return await Task.Run(() => issueTracker.CreateTicket(title, body, attachments));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating undeliverable ticket: {ex.Message}");
                return null;
            }
        }

        private static IList<IssueTracker.IAttachment> ConvertToIssueAttachments(IEnumerable<IAttachment> emailAttachments)
        {
            var attachments = new List<IssueTracker.IAttachment>();
            
            foreach (var attachment in emailAttachments ?? Enumerable.Empty<IAttachment>())
            {
                var issueAttachment = new IssueTracker.IssueAttachment
                {
                    FileContent = attachment.Data,
                    FileName = attachment.Name
                };
                attachments.Add(issueAttachment);
            }
            
            return attachments;
        }

        private static List<string> ExtractReceiptIds(IList<IEmail> eReceipts)
        {
            var receiptIds = new List<string>();
            
            foreach (var eReceipt in eReceipts ?? Enumerable.Empty<IEmail>())
            {
                var receiptId = ExtractReceiptId(eReceipt);
                if (!string.IsNullOrEmpty(receiptId) && !receiptIds.Contains(receiptId))
                    receiptIds.Add(receiptId);
            }
            
            return receiptIds;
        }

        private static string BuildUndeliverableTicketBody(string undeliverableAddress, List<string> receiptIds)
        {
            var body = new StringBuilder();
            body.AppendLine($"Failed to email receipt(s) to {undeliverableAddress}");
            body.AppendLine("Receipt IDs:");
            
            foreach (var receiptId in receiptIds)
                body.AppendLine(receiptId);
            
            return body.ToString();
        }

        private static string ExtractReceiptId(IEmail eReceipt)
        {
            try
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(eReceipt.Body);
                
                const string targetText = "\r\nReceipt Number:\r\n";
                var receiptIdElement = doc.DocumentNode.Descendants()
                    .Where(td => td.NodeType == HtmlAgilityPack.HtmlNodeType.Element 
                                && td.Name == "td" 
                                && td.InnerText == targetText)
                    .Select(td => td.ParentNode.Elements("td").FirstOrDefault(e => e != td))
                    .FirstOrDefault();

                return receiptIdElement?.InnerText?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static async Task<IList<IEmail>> GetEmailsFromArchiveAsync(string undeliverableAddress, DateTime receivedDate)
        {
            if (string.IsNullOrEmpty(undeliverableAddress))
                return new List<IEmail>();

            try
            {
                var emailManager = new EmailManager();
                emailManager.SmtpUsername = ReceiptEmailerSettings.Default.eReceiptEmailArchiveUsernameSetting;
                emailManager.SmtpPassword = ReceiptEmailerSettings.Default.eReceiptEmailArchivePasswordSetting;
                
                var subject = ReceiptEmailerSettings.Default.eReceiptEmailSubjectSetting;
                var undeliverableEmails = await emailManager.GetEmailsAsync(subject, receivedDate);
                
                return undeliverableEmails
                    .Where(email => email.To.Any(t => string.Equals(t, undeliverableAddress, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving emails from archive: {ex.Message}");
                return new List<IEmail>();
            }
        }

        private static string ExtractEmailAddress(string body)
        {
            if (string.IsNullOrEmpty(body))
                return string.Empty;

            const string startPhrase = "Recipient Address:";
            var startIndex = body.IndexOf(startPhrase);
            
            if (startIndex == -1)
                return string.Empty;

            startIndex += startPhrase.Length;
            var endIndex = body.IndexOf('<', startIndex);
            
            if (endIndex == -1)
                endIndex = body.Length;

            return body.Substring(startIndex, endIndex - startIndex).Trim();
        }

        public async Task<ReconData> GetReconDataAsync(DateTime startDate, DateTime endDate)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["donationdb"]?.ConnectionString;
            
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Database connection string not found in configuration.");

            var receipts = new DataSet();
            receipts.Tables.Add("eReceipts");
            receipts.Tables.Add("paperReceipts");
            receipts.Tables.Add("allReceipts");
            receipts.Tables.Add("Errors");

            using (var connection = new OracleConnection(connectionString))
            using (var command = new OracleCommand("donation_system.gift_receipt_report.output", connection))
            using (var receiptsData = new OracleDataAdapter())
            {
                await connection.OpenAsync();
                
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("i_agency", OracleDbType.Varchar2).Value = DefaultAgency;
                command.Parameters.Add("i_campus", OracleDbType.Varchar2).Value = DefaultCampus;
                command.Parameters.Add("i_start_date", OracleDbType.Date).Value = startDate.Date;
                command.Parameters.Add("i_end_date", OracleDbType.Date).Value = endDate.Date;
                command.Parameters.Add("i_receipt_num", OracleDbType.Varchar2).Value = DBNull.Value;
                command.Parameters.Add("i_username", OracleDbType.Varchar2).Value = DefaultUsername;
                command.Parameters.Add("i_batch_number", OracleDbType.Varchar2).Value = DBNull.Value;
                command.Parameters.Add("i_is_email_program", OracleDbType.Decimal).Value = 1;
                command.Parameters.Add("o_rc1", OracleDbType.RefCursor, ParameterDirection.Output);
                
                receiptsData.SelectCommand = command;
                receiptsData.Fill(receipts.Tables["eReceipts"]);
                
                command.Parameters["i_is_email_program"].Value = 0;
                receiptsData.Fill(receipts.Tables["paperReceipts"]);
                
                command.CommandText = "donation_system.gift_receipt_report_AC.output";
                command.Parameters.RemoveAt("i_is_email_program");
                receiptsData.Fill(receipts.Tables["allReceipts"]);
            }

            return new ReconData(
                receipts.Tables["allReceipts"].Rows,
                receipts.Tables["eReceipts"].Rows,
                receipts.Tables["paperReceipts"].Rows,
                _emailsSent.Count);
        }

        private string GetTableData(DataRowCollection table)
        {
            if (table?.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var header = BuildHeaderRow(table[0].Table.Columns);
            sb.AppendLine(header);
            
            foreach (DataRow row in table)
            {
                var line = BuildDataLine(row);
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private string BuildDataLine(DataRow row)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                var value = Regex.Replace(row[i]?.ToString() ?? "", "\"", "\"\"");
                sb.Append($"\"{value}\"");
                
                if (i < row.Table.Columns.Count - 1)
                    sb.Append(',');
            }

            return sb.ToString();
        }

        private string BuildHeaderRow(DataColumnCollection columns)
        {
            var sb = new StringBuilder();
            
            for (int i = 0; i < columns.Count; i++)
            {
                sb.Append(columns[i].ColumnName);
                
                if (i < columns.Count - 1)
                    sb.Append(',');
            }

            return sb.ToString();
        }

        public async Task<IEmail> SendEmailAsync(ReconData reconData, DateTime date)
        {
            try
            {
                var emailBody = BuildEmailBody(reconData);
                var attachments = BuildAttachments(reconData.AllReceipts);
                var emailManager = new EmailManager { AggregateExceptions = false };
                
                var recipients = GetRecipients(ReceiptEmailerSettings.Default.ReconEmailToSetting);
                var ccRecipients = GetRecipients(ReceiptEmailerSettings.Default.ReconEmailCCSetting);
                var bccRecipients = GetRecipients(ReceiptEmailerSettings.Default.ReconEmailBCCSetting);
                
                var subject = $"Gift Receipt Reconciliation - {date.AddDays(1).ToShortDateString()}";
                
                return await emailManager.SendEmailAsync(subject, emailBody, attachments, recipients, ccRecipients, bccRecipients);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reconciliation email: {ex.Message}");
                return null;
            }
        }

        private static IEnumerable<string> GetRecipients(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Enumerable.Empty<string>();

            return value.Split(';').Where(addr => !string.IsNullOrEmpty(addr));
        }
    }
}
