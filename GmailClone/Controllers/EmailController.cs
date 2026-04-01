using GmailClone.Data;
using GmailClone.Models;
using GmailClone.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GmailClone.Controllers
{

public class EmailController : Controller
    {
        private readonly AppDbContext _db;
        private readonly EmailService _emailService;

        public EmailController(AppDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Inbox(string search)
        {
            var emails = _db.Emails.Where(e => !e.IsSpam && !e.IsTrash);
            if (!string.IsNullOrEmpty(search))
                emails = emails.Where(e => e.Subject.Contains(search) || e.From.Contains(search));

            return View(await emails.OrderByDescending(e => e.DateSent).ToListAsync());
        }


        public async Task<IActionResult> Details(int id)
        {
            var email = await _db.Emails.FindAsync(id);
            if (email != null && !email.IsRead)
            {
                email.IsRead = true; // Mark as Seen
                await _db.SaveChangesAsync();
            }
            return View(email);
        }


        public async Task<IActionResult> SyncInbox()
        {
            var newEmails = await _emailService.ReceiveEmailsAsync();

            foreach (var email in newEmails)
            {
                // Only add if it doesn't exist (basic check by subject/date)
                if (!_db.Emails.Any(e => e.Subject == email.Subject && e.DateSent == email.DateSent))
                {
                    _db.Emails.Add(email);
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Inbox");
        }



        [HttpPost]
        public async Task<IActionResult> Send(EmailMessage model, IFormFile? Attachment)
        {
            if (ModelState.IsValid)
            {
                await _emailService.SendEmailAsync(model, Attachment);
                return RedirectToAction("Sent");
            }
            return View("Inbox", await _db.Emails.ToListAsync());
        }

        // Category Pages
        public async Task<IActionResult> Sent() => View(await _db.Emails.Where(e => e.From == "shakshi@zealousys.com").ToListAsync());
        public async Task<IActionResult> Starred() => View(await _db.Emails.Where(e => e.IsStarred).ToListAsync());
        public async Task<IActionResult> Spam() => View(await _db.Emails.Where(e => e.IsSpam).ToListAsync());
        public async Task<IActionResult> Trash() => View(await _db.Emails.Where(e => e.IsTrash).ToListAsync());

    }
}




 




