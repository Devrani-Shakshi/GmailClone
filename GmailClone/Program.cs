

using Microsoft.EntityFrameworkCore;
using GmailClone.Data;
using GmailClone.Models;
using GmailClone.Service;

var builder = WebApplication.CreateBuilder(args);

string projectRoot = builder.Environment.ContentRootPath;
string appDataPath = Path.Combine(projectRoot, "App_Data");

// Ensure the App_Data folder exists physically
if (!Directory.Exists(appDataPath))
{
    Directory.CreateDirectory(appDataPath);
}

string dbFilePath = Path.Combine(appDataPath, "GmailCloneDB.mdf");
string connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={dbFilePath};Initial Catalog=GmailCloneDB_Internal;Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";

// 3. REGISTER SERVICES
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register your EmailService for sending emails
builder.Services.AddScoped<EmailService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. DATABASE INITIALIZATION & SEEDING
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // This line creates the .mdf file in your App_Data folder automatically
        context.Database.EnsureCreated();

        // Seed data so you can test all pages immediately
        if (!context.Emails.Any())
        {
            context.Emails.AddRange(
                new EmailMessage { To = "user@zealousys.com", From = "boss@gmail.com", Subject = "Urgent: Project Update", Body = "This is an unseen email.", IsRead = false, DateSent = DateTime.Now },
                new EmailMessage { To = "user@zealousys.com", From = "hr@outlook.com", Subject = "Interview Scheduled", Body = "This is a seen email.", IsRead = true, DateSent = DateTime.Now.AddDays(-1) },
                new EmailMessage { To = "client@zealousys.com", From = "user@zealousys.com", Subject = "Proposal Sent", Body = "This will show in your Sent folder.", DateSent = DateTime.Now.AddDays(-2) },
                new EmailMessage { To = "user@zealousys.com", From = "newsletter@zealousys.com", Subject = "Weekly News", Body = "This is a starred email.", IsStarred = true, IsRead = true, DateSent = DateTime.Now.AddHours(-5) },
                new EmailMessage { To = "user@zealousys.com", From = "spam@junkmail.com", Subject = "Win a Prize!", Body = "This is in the Spam folder.", IsSpam = true, DateSent = DateTime.Now.AddDays(-10) },
                new EmailMessage { To = "user@zealousys.com", From = "old@trash.com", Subject = "Old Deleted Mail", Body = "This is in the Trash folder.", IsTrash = true, DateSent = DateTime.Now.AddMonths(-1) }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating or seeding the DB.");
    }
}

// 5. CONFIGURE HTTP PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Essential for Bootstrap & CSS

app.UseRouting();
app.UseAuthorization();

// 6. ROUTING (Default to Email Inbox)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Email}/{action=Inbox}/{id?}");

app.Run();
