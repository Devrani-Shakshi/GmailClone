
using GmailClone.Data;
using GmailClone.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MimeKit;
using System.Net.Mail;

namespace GmailClone.Service
{
    public class EmailService
    {
        private readonly AppDbContext _context;

        public EmailService(AppDbContext context) => _context = context;

        public async Task SendEmailAsync(EmailMessage email, IFormFile? attachment)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Gmail Clone", email.From));
            message.To.Add(MailboxAddress.Parse(email.To));
            if (!string.IsNullOrEmpty(email.Cc))
                message.Cc.Add(MailboxAddress.Parse(email.Cc));

            message.Subject = email.Subject;
            var builder = new BodyBuilder { TextBody = email.Body };

            if (attachment != null)
            {
                using var ms = new MemoryStream();
                await attachment.CopyToAsync(ms);
                builder.Attachments.Add(attachment.FileName, ms.ToArray());
                email.AttachmentPath = "/uploads/" + attachment.FileName; // Logic for local storage
            }

            message.Body = builder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();

            await client.ConnectAsync("smtp.gmail.com", 587, false);
            await client.AuthenticateAsync("<your_Email", "<your_password>");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _context.Emails.Add(email);
            await _context.SaveChangesAsync();
        }
       

        public async Task<List<EmailMessage>> ReceiveEmailsAsync()
        {
            var emails = new List<EmailMessage>();
            using var client = new ImapClient();

            await client.ConnectAsync("imap.gmail.com", 993, true);
            await client.AuthenticateAsync("your_Email", "\"<your_password>");

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite); // Use ReadWrite so we can mark them as read later if needed

            // SEARCH for Unread messages specifically
            var results = await inbox.SearchAsync(SearchQuery.NotSeen);

            // Limit to the most recent 10 unread emails
            foreach (var uid in results.TakeLast(10))
            {
                var message = await inbox.GetMessageAsync(uid);

                var email = new EmailMessage
                {
                    From = message.From.ToString(),
                    To = "your_Email", // Your local user
                    Subject = message.Subject ?? "(No Subject)",
                    Body = message.HtmlBody ?? message.TextBody ?? "",
                    DateSent = message.Date.DateTime,
                    IsRead = false
                };

                // Attachment Logic (Ensure folder exists)
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                foreach (var attachment in message.Attachments)
                {
                    if (attachment is MimePart part)
                    {
                        var filePath = Path.Combine(uploadPath, part.FileName);
                        using (var stream = File.Create(filePath))
                        {
                            await part.Content.DecodeToAsync(stream);
                        }
                        email.AttachmentPath = "/uploads/" + part.FileName;
                    }
                }
                emails.Add(email);
            }

            await client.DisconnectAsync(true);
            return emails;
        }


    }

}

