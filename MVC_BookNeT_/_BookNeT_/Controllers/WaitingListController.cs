using System;
using System.Linq;
using System.Threading.Tasks;
using _BookNeT_.Models;
using _BookNeT_.Services;

namespace _BookNeT_.Services
{
    public class WaitingListService
    {
        private readonly BooknetProjectEntities2 _db;
        private readonly EmailService _emailService = new EmailService();

        public WaitingListService(BooknetProjectEntities2 db)
        {
            _db = db;
        }

        public async Task CheckAndUpdateWaitingList(int bookId)
        {
            var book = _db.Books.Find(bookId);
            if (book == null || book.Stock <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"BookID {bookId} is not available.");
                return;
            }

            var waitingUsers = _db.WaitingList
                .Where(w => w.BookID == bookId)
                .OrderBy(w => w.Position)
                .Take(AppConstants.TopWaitingUsers)
                .ToList();

            if (!waitingUsers.Any())
            {
                System.Diagnostics.Debug.WriteLine($"No users to notify for BookID {bookId}.");
                return;
            }

            var emailTasks = waitingUsers.Select(async waitingUser =>
            {
                try
                {
                    var user = _db.Users.Find(waitingUser.UserID);
                    if (user == null || string.IsNullOrEmpty(user.Email))
                        return;

                    bool sent = await _emailService.SendAsync(
                        user.Email,
                        "Book Available - BookNeT",
                        BuildAvailableEmailBody(book.Title));

                    if (sent)
                        waitingUser.NotificationDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to notify user {waitingUser.UserID}: {ex.Message}");
                }
            });

            await Task.WhenAll(emailTasks);
            await _db.SaveChangesAsync();
        }

        private string BuildAvailableEmailBody(string bookTitle)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2c3e50;'>BookNeT - Book Available!</h2>
                    <p style='color: #34495e; font-size: 16px;'>
                        Good news! The book <strong>{bookTitle}</strong> is now available for borrowing.
                    </p>
                    <p style='color: #34495e; font-size: 16px;'>
                        Please note that this offer is valid for the next 24 hours.
                    </p>
                    <div style='margin: 30px 0;'>
                        <a href='https://localhost:44300/Library'
                           style='background-color: #00B4D8; color: white; padding: 12px 25px;
                                  text-decoration: none; border-radius: 5px;'>
                            Visit Library
                        </a>
                    </div>
                    <p style='color: #7f8c8d; font-size: 14px;'>
                        If you didn't request this notification, please ignore this email.
                    </p>
                </div>";
        }
    }
}
