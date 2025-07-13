using ReceiptEmailer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EmailManager;

namespace ReceiptEmailerTests
{
    internal static class Utilities
    {
        public static IList<DonationEntity> BuildTestDonations(int numberOfDonations)
        {
            if (numberOfDonations <= 0)
                return new List<DonationEntity>();

            var donations = new List<DonationEntity>();
            
            for (int i = 0; i < numberOfDonations; i++)
            {
                var donation = new DonationEntity
                {
                    DonorId = $"DONOR_{i:D6}",
                    TransactionNumber = $"TXN_{i:D8}",
                    EmailAddress = $"test.donor{i}@example.com",
                    Street1 = $"123 Test Street {i}",
                    Street2 = $"Apt {i}",
                    Street3 = string.Empty,
                    City = "Test City",
                    State = "TS",
                    Zipcode = "12345",
                    Country = "USA",
                    ForeignAddress = string.Empty,
                    DonorName = $"Test Donor {i}",
                    GiftReceivedDate = DateTime.Now.AddDays(-i),
                    ReceiptNumber = $"RCPT_{i:D8}",
                    Allocation1 = $"Test Fund {i}",
                    Allocation2 = string.Empty,
                    Allocation3 = string.Empty,
                    Allocation4 = string.Empty,
                    AllocationAmount1 = 100.00m + i,
                    AllocationAmount2 = null,
                    AllocationAmount3 = null,
                    AllocationAmount4 = null,
                    TotalGiftAmount = 100.00m + i,
                    PremiumAmount = 0.00m,
                    SortOrder = "1",
                    DisclosureRequired = "N"
                };

                donations.Add(donation);
            }

            return donations;
        }

        internal static ReconData BuildTestReconData(int emailsSent, int allReceipts, int eReceipts, int paperReceipts)
        {
            var allReceiptsData = BuildReceiptDataCollection(allReceipts);
            var eReceiptsData = BuildReceiptDataCollection(eReceipts);
            var paperReceiptsData = BuildReceiptDataCollection(paperReceipts);
            
            return new ReconData(allReceiptsData, eReceiptsData, paperReceiptsData, emailsSent);
        }

        private static DataRowCollection BuildReceiptDataCollection(int receipts)
        {
            if (receipts <= 0)
                return new DataTable().Rows;

            var table = CreateReceiptDataTable();
            
            for (int i = 0; i < receipts; i++)
            {
                var row = table.NewRow();
                foreach (DataColumn column in table.Columns)
                {
                    row[column.ColumnName] = $"{column.ColumnName}_{i:D3}";
                }
                table.Rows.Add(row);
            }
            
            return table.Rows;
        }

        private static DataTable CreateReceiptDataTable()
        {
            var table = new DataTable();
            var columns = new[]
            {
                "TRANS_REC_NUMBER", "DONOR_ID_NUMBER", "TAF_ID", "PREF_MAIL_NAME", "SALUTATION",
                "JOINT_DONOR_ID", "JOINT_DONOR_NAME", "JOINT_SALUATION", "MAILING_NAME", "MAILING_SALUATION",
                "JOINT_IND", "DECEASED_IND", "VIP", "STREET1", "STREET2", "STREET3", "CITY", "STATE",
                "ZIPCODE", "COUNTRY", "FOREIGN_ADDRESS_CITY", "EMAIL_ADDRESS", "TOTAL_AMT", "GIFT_TYPE",
                "PAYMENT_TYPE", "PAYMENT_DETAILS", "APPEAL_CODE", "DATE_OF_RECORD", "GIFT_PROCESSED_DATE",
                "TRIBUTE_GIFT", "DAF", "ALLOC_AMT1", "ALLOC_NAME1", "ALLOC_SCHOOL1", "ALLOC_AMT2",
                "ALLOC_NAME2", "ALLOC_SCHOOL2", "ALLOC_AMT3", "ALLOC_NAME3", "ALLOC_SCHOOL3", "ALLOC_AMT4",
                "ALLOC_NAME4", "ALLOC_SCHOOL4", "ADDITIONAL_ALLOCATIONS", "INCL_DISC_DISCLOSURE", "PREMIUM",
                "GIFT_AMT", "MODIFIED", "PAYMENT_TYPE_CODE", "GIK_TYPE_CODE", "ALLOCATION_SCHOOL_DESC",
                "ANIMAL_TYPE", "ANIMAL_NAME", "NUMBER_OF_SHARES", "STOCK_COMPANY", "LINE_1", "LINE_2",
                "LINE_3", "LINE_4", "LINE_5", "LINE_6", "PRIM_GIFT_BATCH_NUMBER", "PLEDGE_CONTINGENCY",
                "GIFT_TYPE_CODE", "GIFT_CONTINGENCY", "SORT_ORDER"
            };

            foreach (var columnName in columns)
            {
                table.Columns.Add(columnName, typeof(string));
            }

            return table;
        }

        internal static IList<string> BuildTestTickets(int numberOfTickets)
        {
            var tickets = new List<string>();
            
            for (int i = 0; i < numberOfTickets; i++)
            {
                var ticket = $"ISSUE-{i:D5}";
                tickets.Add(ticket);
            }

            return tickets;
        }

        internal static IList<IEmail> BuildTestReceiptEmails(int numberOfEmails)
        {
            var emails = new List<IEmail>();
            
            for (int i = 0; i < numberOfEmails; i++)
            {
                var email = new TestEmail(i);
                emails.Add(email);
            }

            return emails;
        }
    }
}
