using System.ComponentModel.DataAnnotations;

namespace GmailClone.Models
{
    public class ComposeViewModel
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|zealousys\.com)$",
            ErrorMessage = "Only Gmail, Outlook, or Zealousys emails are allowed.")]
        public string To { get; set; }

        public string Cc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public IFormFile Attachment { get; set; } // For attachments
    }

}
