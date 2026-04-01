
using GmailClone.Data;
using GmailClone.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
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
            await client.AuthenticateAsync("shakshi@zealousys.com", "hjih xbhd fagc lyyx");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _context.Emails.Add(email);
            await _context.SaveChangesAsync();
        }
        public async Task<List<EmailMessage>> ReceiveEmailsAsync()
        {
            var emails = new List<EmailMessage>();
            using var client = new ImapClient();

            // Use your IMAP settings (e.g., imap.gmail.com, 993)
            await client.ConnectAsync("imap.gmail.com", 993, true);
            await client.AuthenticateAsync("shakshi@zealousys.com", "hjih xbhd fagc lyyx");

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            // Fetch the last 10 emails
            for (int i = inbox.Count - 1; i >= Math.Max(0, inbox.Count - 10); i--)
            {
                var message = await inbox.GetMessageAsync(i);
                var email = new EmailMessage
                {
                    From = message.From.ToString(),
                    Subject = message.Subject,
                    Body = message.HtmlBody ?? message.TextBody,
                    DateSent = message.Date.DateTime,
                    IsRead = false // Default to Unseen
                };

                // FIX: Handle Incoming Attachments
                if (message.Attachments.Any())
                {
                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment is MimePart part)
                        {
                            var fileName = part.FileName;
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                            using (var stream = File.Create(path))
                            {
                                await part.Content.DecodeToAsync(stream);
                            }
                            email.AttachmentPath = "/uploads/" + fileName;
                        }
                    }
                }
                emails.Add(email);
            }

            await client.DisconnectAsync(true);
            return emails;
        }

    }

}

