namespace GmailClone.Models
{
    using System.ComponentModel.DataAnnotations;

    public class EmailMessage
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|zealousys\.com)$",
            ErrorMessage = "Invalid domain. Use Gmail, Outlook, or Zealousys.")]
        public string To { get; set; }

        public string? Cc { get; set; }
        public string From { get; set; } = "shakshi@zealousys.com";
        public string Subject { get; set; } = "(No Subject)";
        public string Body { get; set; } = string.Empty;
        public DateTime DateSent { get; set; } = DateTime.Now;

        public bool IsRead { get; set; }     // false = Unseen, true = Seen
        public bool IsStarred { get; set; }
        public bool IsSpam { get; set; }
        public bool IsTrash { get; set; }
        public string? AttachmentPath { get; set; }
    }

}
