using ReceiptEmailer.Models;
using EmailManager;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReceiptEmailer
{
    public class Program
    {
        private const string DefaultAgency = "FN";
        private const string DefaultCampus = "F";
        private const string DefaultUsername = "ReceiptEmailer";
        private const string GiftTypeCodeFilter = "MG";

        static async Task Main(string[] args)
        {
            try
            {
                var (startDate, endDate, receiptNumber, batchNumber) = ParseCommandLineArguments(args);
                var donations = await GetDonationsFromDatabaseAsync(startDate, endDate, receiptNumber, batchNumber);
                
                if (donations?.Any() == true)
                {
                    var (emailsSent, tickets) = await SendReceiptsAsync(donations);
                    Console.WriteLine($"Successfully processed {donations.Count} donations. Emails sent: {emailsSent?.Count ?? 0}");
                }
                else
                {
                    Console.WriteLine("No donations found for the specified criteria.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing donations: {ex.Message}");
                await LogErrorAsync(ex);
            }
        }

        private static (DateTime startDate, DateTime endDate, string receiptNumber, string batchNumber) ParseCommandLineArguments(string[] args)
        {
            var startDate = DateTime.Now.AddDays(-1).Date;
            var endDate = DateTime.Now.AddDays(-1).Date;
            string receiptNumber = null;
            string batchNumber = null;

            if (args.Length > 1)
            {
                if (DateTime.TryParse(args[0], out var parsedStartDate))
                    startDate = parsedStartDate;
                
                if (DateTime.TryParse(args[1], out var parsedEndDate))
                    endDate = parsedEndDate;

                if (args.Length > 2 && !string.IsNullOrEmpty(args[2]) && args[2].Length == 10)
                    receiptNumber = args[2].Trim();

                if (args.Length > 3 && !string.IsNullOrEmpty(args[3]) && args[3].Length == 10)
                    batchNumber = args[3].Trim();
            }

            return (startDate, endDate, receiptNumber, batchNumber);
        }

        private static async Task<IList<DonationEntity>> GetDonationsFromDatabaseAsync(DateTime startDate, DateTime endDate, string receiptNumber, string batchNumber)
        {
            var donations = new List<DonationEntity>();
            var connectionString = ConfigurationManager.ConnectionStrings["donationdb"]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Database connection string not found in configuration.");

            using (var connection = new OracleConnection(connectionString))
            {
                await connection.OpenAsync();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "donation_system.gift_receipt_report.email_output";
                    command.CommandType = CommandType.StoredProcedure;
                    
                    command.Parameters.Add("i_agency", OracleDbType.Varchar2).Value = DefaultAgency;
                    command.Parameters.Add("i_campus", OracleDbType.Varchar2).Value = DefaultCampus;
                    command.Parameters.Add("i_start_date", OracleDbType.Date).Value = startDate.Date;
                    command.Parameters.Add("i_end_date", OracleDbType.Date).Value = endDate.Date;
                    command.Parameters.Add("i_receipt_num", OracleDbType.Varchar2).Value = receiptNumber ?? "";
                    command.Parameters.Add("i_username", OracleDbType.Varchar2).Value = DefaultUsername;
                    command.Parameters.Add("i_batch_number", OracleDbType.Varchar2).Value = batchNumber ?? "";
                    command.Parameters.Add("o_rc1", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader["GIFT_TYPE_CODE"]?.ToString() == GiftTypeCodeFilter)
                                continue;

                            donations.Add(MapReaderToDonationEntity(reader));
                        }
                    }
                }
            }

            return donations;
        }

        private static DonationEntity MapReaderToDonationEntity(IDataReader reader)
        {
            return new DonationEntity
            {
                DonorId = reader["DONOR_ID_NUMBER"]?.ToString() ?? "",
                TransactionNumber = reader["TRANS_REC_NUMBER"]?.ToString() ?? "",
                EmailAddress = reader["EMAIL_ADDRESS"]?.ToString() ?? "",
                Street1 = reader["LINE_1"]?.ToString() ?? "",
                Street2 = reader["LINE_2"]?.ToString() ?? "",
                Street3 = reader["LINE_3"]?.ToString() ?? "",
                DonorName = reader["MAILING_NAME"]?.ToString() ?? "",
                GiftReceivedDate = reader["DATE_OF_RECORD"] as DateTime? ?? DateTime.MinValue,
                ReceiptNumber = reader["TRANS_REC_NUMBER"]?.ToString() ?? "",
                Allocation1 = reader["ALLOC_NAME1"]?.ToString() ?? "",
                Allocation2 = reader["ALLOC_NAME2"]?.ToString() ?? "",
                Allocation3 = reader["ALLOC_NAME3"]?.ToString() ?? "",
                Allocation4 = reader["ALLOC_NAME4"]?.ToString() ?? "",
                AllocationAmount1 = reader["ALLOC_AMT1"] as decimal?,
                AllocationAmount2 = reader["ALLOC_AMT2"] as decimal?,
                AllocationAmount3 = reader["ALLOC_AMT3"] as decimal?,
                AllocationAmount4 = reader["ALLOC_AMT4"] as decimal?,
                TotalGiftAmount = reader["GIFT_AMT"] as decimal?,
                PremiumAmount = reader["PREMIUM"] as decimal?,
                SortOrder = reader["SORT_ORDER"]?.ToString() ?? "",
                DisclosureRequired = reader["INCL_DISC_DISCLOSURE"]?.ToString() ?? ""
            };
        }

        public static async Task<(IList<IEmail> emailsSent, IEnumerable<string> tickets)> SendReceiptsAsync(IList<DonationEntity> donations)
        {
            var emailsSent = new List<IEmail>();
            var tickets = new List<string>();

            try
            {
                var emailManager = CreateEmailManager();
                var emailTemplate = await LoadEmailTemplateAsync();

                foreach (var donation in donations)
                {
                    try
                    {
                        var emailBody = BuildEmailBody(donation, emailTemplate);
                        var emailResult = await SendDonationEmailAsync(donation, emailBody, emailManager);
                        
                        if (emailResult != null)
                            emailsSent.Add(emailResult);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email for donation {donation.ReceiptNumber}: {ex.Message}");
                        await LogErrorAsync(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                var ticket = await CreateErrorTicketAsync(ex);
                if (!string.IsNullOrEmpty(ticket))
                    tickets.Add(ticket);
            }

            return (emailsSent, tickets);
        }

        private static EmailManager CreateEmailManager()
        {
            var fromAddress = ReceiptEmailerSettings.Default.eReceiptEmailFromSetting;
            var emailManager = new EmailManager(fromAddress);
            
            emailManager.SmtpUsername = ReceiptEmailerSettings.Default.eReceiptEmailUndeliverableUsernameSetting;
            emailManager.SmtpPassword = ReceiptEmailerSettings.Default.eReceiptEmailUndeliverablePasswordSetting;
            emailManager.AggregateExceptions = true;
            
            return emailManager;
        }

        private static async Task<string> LoadEmailTemplateAsync()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ReceiptEmailer.ReceiptEmailBodyTemplate.html"))
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static string BuildEmailBody(DonationEntity donation, string emailTemplate)
        {
            var fundsHtml = BuildAllocationsHtml(donation);
            var disclosure = BuildDisclosureText(donation.DisclosureRequired);

            return string.Format(emailTemplate,
                disclosure,
                donation.DonorName,
                donation.GiftReceivedDate.ToShortDateString(),
                donation.ReceiptNumber,
                fundsHtml,
                donation.TotalAmount.ToString("C"),
                donation.PremiumAmount?.ToString("C") ?? "$0.00",
                donation.TotalGiftAmount?.ToString("C") ?? "$0.00");
        }

        private static string BuildAllocationsHtml(DonationEntity donation)
        {
            var funds = new StringBuilder();
            
            var allocations = new[]
            {
                (donation.Allocation1, donation.AllocationAmount1),
                (donation.Allocation2, donation.AllocationAmount2),
                (donation.Allocation3, donation.AllocationAmount3),
                (donation.Allocation4, donation.AllocationAmount4)
            };

            foreach (var (allocationName, allocationAmount) in allocations)
            {
                if (!string.IsNullOrEmpty(allocationName) && allocationAmount.HasValue)
                {
                    funds.Append($@"<tr>
                                        <td align=""left"" valign=""top"">
                                            <p>{allocationName}:</p>
                                        </td>
                                        <td align=""right"">
                                            <p>{allocationAmount.Value:C}</p>
                                        </td>
                                    </tr>");
                }
            }

            return funds.ToString();
        }

        private static string BuildDisclosureText(string disclosureRequired)
        {
            const string standardDisclosure = "Following are the details for your recent online gift. " +
                "Unless indicated below, no goods or services were provided in exchange for this gift.";

            const string extendedDisclosure = "Following are the details for your recent online gift. " +
                "Unless indicated below, no goods or services were provided in exchange for this gift, " +
                "but it does entitle you to a discount(s) on purchases of certain goods and/or services." +
                " If used, a portion of your gift may not be tax deductible; please consult your tax advisor.";

            return disclosureRequired?.Trim() == "Y" ? extendedDisclosure : standardDisclosure;
        }

        private static async Task<IEmail> SendDonationEmailAsync(DonationEntity donation, string emailBody, EmailManager emailManager)
        {
            var recipients = GetEmailRecipients(donation.EmailAddress);
            var bccRecipients = GetBccRecipients();
            var subject = ReceiptEmailerSettings.Default.eReceiptEmailSubjectSetting;
            var attachments = new Attachment[0];

            return await emailManager.SendEmailAsync(subject, emailBody, attachments, recipients, new List<string>(), bccRecipients);
        }

        private static List<string> GetEmailRecipients(string donorEmail)
        {
            var recipients = new List<string>();
            
            if (ReceiptEmailerSettings.Default.eReceiptEmailTestModeEnabledSetting)
            {
                var testAddresses = ReceiptEmailerSettings.Default.eReceiptEmailTestModeToAddressSetting?.Split(';') ?? new string[0];
                recipients.AddRange(testAddresses.Where(addr => !string.IsNullOrEmpty(addr)));
            }
            else
            {
                if (!string.IsNullOrEmpty(donorEmail))
                    recipients.Add(donorEmail);
            }

            return recipients;
        }

        private static List<string> GetBccRecipients()
        {
            var bccRecipients = new List<string>();
            var bccSetting = ReceiptEmailerSettings.Default.eReceiptEmailBCCSetting;
            
            if (!string.IsNullOrEmpty(bccSetting))
            {
                var bccAddresses = bccSetting.Split(';');
                bccRecipients.AddRange(bccAddresses.Where(addr => !string.IsNullOrEmpty(addr)));
            }

            return bccRecipients;
        }

        private static async Task<string> CreateErrorTicketAsync(Exception ex)
        {
            try
            {
                var issueTracker = new IssueTracker.IssueManager();
                return await Task.Run(() => issueTracker.CreateTicket(ex));
            }
            catch
            {
                return null;
            }
        }

        private static async Task LogErrorAsync(Exception ex)
        {
            try
            {
                var logFileName = ConfigurationManager.AppSettings["logFileName"] ?? "errors.txt";
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {ex.Message}\n{ex.StackTrace}\n\n";
                
                await File.AppendAllTextAsync(logFileName, logMessage);
            }
            catch
            {
                // Fallback to console if logging fails
                Console.WriteLine($"Error logging failed: {ex.Message}");
            }
        }
    }
}
