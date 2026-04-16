using _BookNeT_.Models;
using _BookNeT_.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace _BookNeT_.Controllers
{
    public class ReminderController : Controller
    {
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();
        private readonly EmailService _emailService  = new EmailService();

        public void CheckAndSendRemindersForAllUsers()
        {
            var today = DateTime.Now.Date;

            var users = db.Users
                .Include(u => u.Borrowing.Select(b => b.Books))
                .ToList();

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.Email) || user.Borrowing == null || !user.Borrowing.Any())
                    continue;

                foreach (var borrowing in user.Borrowing)
                {
                    if (borrowing.Books == null || borrowing.Status != "Available")
                        continue;

                    int daysUntilDue = (borrowing.DueDate.Date - today).Days;
                    if (daysUntilDue != AppConstants.ReminderDaysBeforeDue)
                        continue;

                    try
                    {
                        SendReminderEmail(user.Email, borrowing.Books.Title, borrowing.DueDate);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error sending reminder to {user.Email}: {ex.Message}");
                    }
                }
            }
        }

        private void SendReminderEmail(string email, string bookTitle, DateTime dueDate)
        {
            string subject = "Reminder: Return Borrowed Book";
            string body    = $@"
                <p>Hello,</p>
                <p>This is a friendly reminder to return the book <strong>{bookTitle}</strong>.</p>
                <p>The due date for returning this book is <strong>{dueDate.ToShortDateString()}</strong>.</p>
                <p>Please make sure to return it on time to avoid penalties.</p>
                <br>
                <p>Thank you,<br>Your BookNeT Team</p>";

            _emailService.Send(email, subject, body);
        }
    }
}
