using System;
using System.Collections.Generic;
using EmailManager;

namespace ReceiptEmailerTests
{
    public class TestEmail : IEmail
    {
        public TestEmail(int id)
        {
            Id = id;
            From = "noreply@example.com";
            To = new List<string>();
            to.Add("test@example.com");
            Subject = $"Test Email {id}";
            Body = $"This is test email body {id}";
            ReceivedDate = DateTime.Now;
            Attachments = new List<IAttachment>();
        }

        public int Id { get; set; }
        public string From { get; set; }
        public List<string> to { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime ReceivedDate { get; set; }
        public IList<IAttachment> Attachments { get; set; }
    }
}