using System;
using System.ComponentModel.DataAnnotations;

namespace ReceiptEmailer.Models
{
    public class DonationEntity
    {
        private decimal _totalAmount;
        private decimal _giftAmount;

        [Required]
        public string DonorId { get; set; }
        
        [Required]
        public string TransactionNumber { get; set; }
        
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }
        
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string Street3 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zipcode { get; set; }
        public string Country { get; set; }
        public string ForeignAddress { get; set; }
        
        [Required]
        public string DonorName { get; set; }
        
        [Required]
        public DateTime GiftReceivedDate { get; set; }
        
        [Required]
        public string ReceiptNumber { get; set; }
        
        public string Allocation1 { get; set; }
        public string Allocation2 { get; set; }
        public string Allocation3 { get; set; }
        public string Allocation4 { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? AllocationAmount1 { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? AllocationAmount2 { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? AllocationAmount3 { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? AllocationAmount4 { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? TotalGiftAmount { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? PremiumAmount { get; set; }
        
        public string SortOrder { get; set; }
        public string DisclosureRequired { get; set; }
        
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (string.IsNullOrEmpty(SortOrder) || SortOrder.Length < 2)
                {
                    _totalAmount = value;
                    return;
                }

                if (SortOrder.Substring(1, 1).Trim() == "3")
                {
                    var allocation1 = AllocationAmount1 ?? 0m;
                    var allocation2 = AllocationAmount2 ?? 0m;
                    var allocation3 = AllocationAmount3 ?? 0m;
                    var allocation4 = AllocationAmount4 ?? 0m;
                    var premium = PremiumAmount ?? 0m;

                    _totalAmount = allocation1 + allocation2 + allocation3 + allocation4 + premium;
                }
                else
                {
                    _totalAmount = value;
                }
            }
        }
        
        public decimal GiftAmount
        {
            get => _giftAmount;
            set
            {
                if (string.IsNullOrEmpty(SortOrder) || SortOrder.Length < 2)
                {
                    _giftAmount = value;
                    return;
                }

                if (SortOrder.Substring(1, 1).Trim() == "3")
                {
                    var allocation1 = AllocationAmount1 ?? 0m;
                    var allocation2 = AllocationAmount2 ?? 0m;
                    var allocation3 = AllocationAmount3 ?? 0m;
                    var allocation4 = AllocationAmount4 ?? 0m;

                    _giftAmount = allocation1 + allocation2 + allocation3 + allocation4;
                }
                else
                {
                    _giftAmount = value;
                }
            }
        }
    }
} 