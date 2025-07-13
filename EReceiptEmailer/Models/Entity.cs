using System;

namespace EReceiptEmailer.Models
{
    public class Entity
    {
        private decimal totalamt;
        private decimal giftamt;

        public string DonorID { get; set; }
        //public string JointDonorID { get; set; }
        public string TransactionNumber { get; set; }
        public string EmailAddress { get; set; }
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string Street3 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zipcode { get; set; }
        public string Country { get; set; }
        public string ForeignAddress { get; set; }
        public string Name { get; set; }
        public DateTime GiftReceived { get; set; }
        public string ReceiptNumber { get; set; }
        public string Allocation1 { get; set; }
        public string Allocation2 { get; set; }
        public string Allocation3 { get; set; }
        public string Allocation4 { get; set; }
        public decimal? AllocationAmt1 { get; set; }
        public decimal? AllocationAmt2 { get; set; }
        public decimal? AllocationAmt3 { get; set; }
        public decimal? AllocationAmt4 { get; set; }
        public decimal? TotalGift { get; set; }
        public decimal? Premium { get; set; }
        public string SortOrder { get; set; }
        public string Disclosure { get; set; }
        public decimal TotalAMT
        {
            get
            {
                return totalamt;
            }

            set
            {
                decimal a1 = Convert.ToDecimal(AllocationAmt1);
                decimal a2 = Convert.ToDecimal(AllocationAmt2);
                decimal a3 = Convert.ToDecimal(AllocationAmt3);
                decimal a4 = Convert.ToDecimal(AllocationAmt4);
                decimal prem = Convert.ToDecimal(Premium);


                if ((SortOrder.Trim() != null) && (SortOrder.Substring(1, 1).Trim() == "3"))
                {
                    totalamt = (a1 + a2 + a3 + a4 + prem);
                }

                else
                {
                    totalamt = value;
                }
            }
        }
        public decimal GiftAMT
        {
            get
            {
                return giftamt;
            }

            set
            {
                decimal a1 = Convert.ToDecimal(AllocationAmt1);
                decimal a2 = Convert.ToDecimal(AllocationAmt2);
                decimal a3 = Convert.ToDecimal(AllocationAmt3);
                decimal a4 = Convert.ToDecimal(AllocationAmt4);

                if ((SortOrder.Trim() != null) && (SortOrder.Substring(1, 1).Trim() == "3"))
                {
                    giftamt = (a1 + a2 + a3 + a4);
                }

                else
                {
                    giftamt = value;
                }
            }
        }
        

    }
}
