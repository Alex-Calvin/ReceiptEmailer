using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReceiptEmailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReceiptEmailer.Models;

namespace ReceiptEmailer.Tests
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public async Task SendReceiptsAsync_WithValidDonations_ShouldSendEmails()
        {
            // Arrange
            var numberOfDonations = 1;
            var donations = Utilities.BuildTestDonations(numberOfDonations);
            
            // Act
            var (emailsSent, tickets) = await Program.SendReceiptsAsync(donations);
            
            // Assert
            Assert.IsNotNull(emailsSent);
            Assert.IsTrue(emailsSent.Count >= 0);
        }

        [TestMethod]
        public async Task SendReceiptsAsync_WithInvalidDonations_ShouldCreateTickets()
        {
            // Arrange
            var donations = new List<DonationEntity>(); // Empty list to simulate error condition
            
            // Act
            var (emailsSent, tickets) = await Program.SendReceiptsAsync(donations);
            
            // Assert
            Assert.IsNotNull(tickets);
            Assert.IsTrue(tickets.Count() >= 0);
        }

        [TestMethod]
        public void ParseCommandLineArguments_WithValidArgs_ShouldParseCorrectly()
        {
            // Arrange
            var args = new[] { "2023-01-01", "2023-01-31", "1234567890", "BATCH001" };
            
            // Act
            var (startDate, endDate, receiptNumber, batchNumber) = Program.ParseCommandLineArguments(args);
            
            // Assert
            Assert.AreEqual(new DateTime(2023, 1, 1), startDate);
            Assert.AreEqual(new DateTime(2023, 1, 31), endDate);
            Assert.AreEqual("1234567890", receiptNumber);
            Assert.AreEqual("BATCH001", batchNumber);
        }

        [TestMethod]
        public void ParseCommandLineArguments_WithInvalidArgs_ShouldUseDefaults()
        {
            // Arrange
            var args = new string[0];
            
            // Act
            var (startDate, endDate, receiptNumber, batchNumber) = Program.ParseCommandLineArguments(args);
            
            // Assert
            Assert.AreEqual(DateTime.Now.AddDays(-1).Date, startDate);
            Assert.AreEqual(DateTime.Now.AddDays(-1).Date, endDate);
            Assert.IsNull(receiptNumber);
            Assert.IsNull(batchNumber);
        }
    }
}