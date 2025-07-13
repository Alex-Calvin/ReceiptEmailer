using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReceiptEmailer
{
    public class ReconData
    {
        public DataRowCollection AllReceipts { get; }
        public DataRowCollection EReceipts { get; }
        public DataRowCollection PaperReceipts { get; }
        public int ErrorCount { get; }

        public ReconData(DataRowCollection allReceipts, DataRowCollection eReceipts, DataRowCollection paperReceipts, int emailsSentCount)
        {
            AllReceipts = allReceipts ?? new DataTable().Rows;
            EReceipts = eReceipts ?? new DataTable().Rows;
            PaperReceipts = paperReceipts ?? new DataTable().Rows;
            
            ErrorCount = CalculateErrorCount(eReceipts, emailsSentCount);
        }

        private static int CalculateErrorCount(DataRowCollection eReceipts, int emailsSentCount)
        {
            if (eReceipts?.Count == null)
                return emailsSentCount;

            return eReceipts.Count >= emailsSentCount 
                ? eReceipts.Count - emailsSentCount 
                : emailsSentCount - eReceipts.Count;
        }
    }
}
