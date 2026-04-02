using GmailClone.Data;
using GmailClone.Models;
using GmailClone.Service;
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


        public async Task<IActionResult> Zealous()
        {
            // Only show emails that have been categorized as Zealous
            var emails = await _db.Emails
                .Where(e => e.IsZealous && !e.IsTrash)
                .OrderByDescending(e => e.DateSent)
                .ToListAsync();

            return View(emails);
        }


        public async Task<IActionResult> Inbox(string search)
        {
            try
            {
                var newEmails = await _emailService.ReceiveEmailsAsync();
                foreach (var email in newEmails)
                {
                    // Check if we already have this exact email to avoid duplicates
                    bool exists = await _db.Emails.AnyAsync(e => e.Subject == email.Subject && e.DateSent == email.DateSent);
                    if (!exists)
                    {
                        _db.Emails.Add(email);
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch { /* Log error or ignore if offline */ }

            var query = _db.Emails.Where(e => !e.IsSpam && !e.IsTrash && !(e.IsZealous && e.IsRead));

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Subject.Contains(search) || e.Body.Contains(search) || e.From.Contains(search));
            }

            return View(await query.OrderByDescending(e => e.DateSent).ToListAsync());
        }


        public async Task<IActionResult> Details(int id)
        {
            var email = await _db.Emails.FindAsync(id);

            if (email != null)
            {
                // Mark as Seen
                email.IsRead = true;
                var content = (email.From + email.Subject + email.Body).ToLower();

                if (content.Contains("zealous"))
                {
                    email.IsZealous = true;
                }

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

        // 1. Move to Trash
        [HttpPost]
        public async Task<IActionResult> MoveToTrash(int id)
        {
            var email = await _db.Emails.FindAsync(id);
            if (email != null)
            {
                email.IsTrash = true;
                email.IsSpam = false; // Remove from spam if it was there
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Inbox");
        }

        // 2. Toggle Star (AJAX friendly or simple Redirect)
        public async Task<IActionResult> ToggleStar(int id)
        {
            var email = await _db.Emails.FindAsync(id);
            if (email != null)
            {
                email.IsStarred = !email.IsStarred; // Flip the boolean
                await _db.SaveChangesAsync();
            }
            // Redirect back to the page you came from
            return Redirect(Request.Headers["Referer"].ToString());
        }
        public async Task<IActionResult> Starred()
        {
            // Filter only starred items that are NOT in the trash
            var starredItems = await _db.Emails
                .Where(e => e.IsStarred && !e.IsTrash)
                .OrderByDescending(e => e.DateSent)
                .ToListAsync();

            return View(starredItems);
        }
        [HttpPost]
        public async Task<IActionResult> RestoreFromTrash(int id)
        {
            var email = await _db.Emails.FindAsync(id);
            if (email != null)
            {
                email.IsTrash = false; // Move back to Inbox
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Trash");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteForever(int id)
        {
            var email = await _db.Emails.FindAsync(id);
            if (email != null)
            {
                _db.Emails.Remove(email); // Completely delete from SQL
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Trash");
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
        //public async Task<IActionResult> Starred() => View(await _db.Emails.Where(e => e.IsStarred).ToListAsync());
        public async Task<IActionResult> Spam() => View(await _db.Emails.Where(e => e.IsSpam).ToListAsync());
        public async Task<IActionResult> Trash() => View(await _db.Emails.Where(e => e.IsTrash).ToListAsync());

    }
}

