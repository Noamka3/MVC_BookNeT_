using _BookNeT_.Models;
using _BookNeT_.Models.viewModels;
using _BookNeT_.Models.BookUser;
using _BookNeT_.Services;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace BookNeT_.Controllers
{
    public class ProfileController : Controller
    {
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();
        private readonly EmailService _emailService  = new EmailService();
        public ActionResult Tools()
        {
            ViewBag.Title = "כלים שימושיים";
            return View();
        }

        [HttpGet]

        public ActionResult Details()
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account"); // אם לא מחובר, הפנה להתחברות
            }

            string email = Session["Email"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return HttpNotFound("User not found.");
            }
            
            Session["FirstName"] = user.FirstName;
            Session["LastName"] = user.LastName;

            return View(user); 
        }

        [HttpPost]
        public ActionResult SaveProfile(string email, string phone, int? age, string firstName, string lastName)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                var userId = Session["UserID"].ToString();
                var user = db.Users.FirstOrDefault(u => u.UserID.ToString() == userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // עדכון מספר הטלפון
                if (!string.IsNullOrEmpty(phone))
                {
                    if (!Regex.IsMatch(phone, @"^\d{10}$"))
                    {
                        return Json(new { success = false, message = "Phone number must be exactly 10 digits." });
                    }
                    user.Phone = phone;
                }

                // עדכון כתובת אימייל
                if (!string.IsNullOrEmpty(email))
                {
                    if (!IsValidEmail(email))
                    {
                        return Json(new { success = false, message = "Invalid email format." });
                    }
                    user.Email = email;
                }

                // עדכון גיל
                if (age.HasValue && age > AppConstants.MinUserAge)
                {
                    user.Age = age;
                }

                // עדכון שם פרטי ושם משפחה
                if (!string.IsNullOrEmpty(firstName))
                {
                    user.FirstName = firstName;
                }

                if (!string.IsNullOrEmpty(lastName))
                {
                    user.LastName = lastName;
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Profile updated successfully.",
                    phone = user.Phone,
                    age = user.Age,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        
        // פונקציית בדיקת תקינות של מייל
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            // ביטוי רגולרי לבדוק האם המייל תקין
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            return emailRegex.IsMatch(email);
        }

        
        [HttpPost]
        public ActionResult UploadProfileImage(HttpPostedFileBase ProfileImage)
        {
            try
            {
                // בדיקה אם ה-Session קיים
                if (Session["UserID"] == null)
                {
                    Debug.WriteLine("Session expired.");
                    return Json(new { success = false, message = "Session expired." });
                }

                // השגת ה-UserID מה-Session
                var userId = Session["UserID"].ToString();

                // שליפת המשתמש מה-Database
                var user = db.Users.FirstOrDefault(u => u.UserID.ToString() == userId);
                if (user == null)
                {
                    Debug.WriteLine("User not found.");
                    return Json(new { success = false, message = "User not found." });
                }
                if (ProfileImage != null && ProfileImage.ContentLength > 0)
                {
                    var imageUrl = UploadImageAndSaveToDatabase(ProfileImage);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // עדכון השדה ProfileImageUrl עם ה-URL שנוצר
                        user.ImageUrl = imageUrl;

                        try
                        {
                            db.SaveChanges();
                            Debug.WriteLine("Image URL saved to database.");
                            return Json(new { success = true, message = "Profile image updated successfully." });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error saving changes to database: {ex.Message}");
                            return Json(new { success = false, message = $"Database save error: {ex.Message}" });
                        }
                    }
                }

                Debug.WriteLine("Failed to upload image.");
                return Json(new { success = false, message = "No valid image uploaded." });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
                return Json(new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpPost]
        public ActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            try
            {
                // בדיקת אם המשתמש מחובר
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                // בדיקת האם הסיסמאות תואמות
                if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    return Json(new { success = false, message = "Both password fields are required." });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Passwords do not match." });
                }

                // בדיקת אורך הסיסמה
                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters long." });
                }

                // קבלת ה-UserID מה-Session ושליפת המשתמש מה-Database
                var userId = Session["UserID"].ToString();
                var user = db.Users.FirstOrDefault(u => u.UserID.ToString() == userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // הצפנת הסיסמה החדשה באמצעות BCrypt
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                db.SaveChanges();

                return Json(new { success = true, message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        
        private string UploadImageAndSaveToDatabase(HttpPostedFileBase image)
        {
            try
            {
                // יצירת שם ייחודי לתמונה
                var newFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

                // הגדרת נתיב לשמירת הקובץ
                var savePath = Server.MapPath($"~/Images/{newFileName}");

                // שמירת התמונה במיקום הפיזי
                image.SaveAs(savePath);

                // יצירת URL לתמונה
                var imageUrl = $"/Images/{newFileName}";

                Debug.WriteLine($"Image saved successfully at: {imageUrl}");
                return imageUrl;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving image to server: {ex.Message}");
                return string.Empty;
            }
        }


        [HttpGet]
        public ActionResult Support()
        {
            // בדוק האם המשתמש מחובר על ידי בדיקת Session
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account"); // אם לא מחובר, הפנה להתחברות
            }

            string email = Session["Email"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return HttpNotFound("User not found.");
            }

            return View(user); // שליחת המידע ל-View

        }
        [HttpPost]
        public ActionResult SubmitSupportRequest(string subject, string message)
        {
            try
            {
                // בדיקה האם יש מידע מהטופס
                if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(subject))
                {
                    return Json(new { success = false, message = "You must select a subject and enter a message to submit." });
                }

                // בדיקה אם נבחר נושא או נכתבה הודעה רק לשם מניעת שליחת מיילים מיותרים
                if (subject == "Select a Subject..." || string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { success = false, message = "Please select a valid subject and write a meaningful message." });
                }

                string userEmail = Session["Email"]?.ToString();
                string userId = Session["UserID"]?.ToString();
                string firstName = Session["FirstName"]?.ToString() ?? "Unknown";
                string lastName = Session["LastName"]?.ToString() ?? "";
                string userFullName = $"{firstName} {lastName}".Trim(); 

                // בדיקת אם יש מידע על המשתמש
                if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Session expired or user data unavailable." });
                }

                // שליחת המייל
                SendSupportEmail(userEmail, userId, userFullName, subject, message);

                return Json(new { success = true, message = "Your request has been sent successfully to support." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        
        private void SendSupportEmail(string userEmail, string userId, string fullName, string subject, string messageContent)
        {
            string body = $"User Full Name: {fullName}\nUser Email: {userEmail}\nUser ID: {userId}\n\nSubject: {subject}\nMessage:\n{messageContent}";
            _emailService.Send("booknet.site@gmail.com", $"Support Request: {subject} from {fullName}", body);
        }
        public ActionResult Books()
        {
            ViewBag.Title = "User Details";
            return View();
        }

        [HttpPost]
        public ActionResult DeleteBook(int purchaseId)
        {
            try
            {
                // בדיקת Session
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int userId = (int)Session["UserID"];

                // שליפת הספר לפרופיל המשתמש
                var purchase = db.Purchases.FirstOrDefault(p => p.PurchaseID == purchaseId && p.UserID == userId);

                if (purchase == null)
                {
                    return Json(new { success = false, message = "The book does not exist in your profile." });
                }

                // מחיקת הספר מפרופיל המשתמש בלבד
                db.Purchases.Remove(purchase);
                db.SaveChanges();

                return Json(new { success = true, message = "The book has been successfully deleted from your profile." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        
        [HttpGet]
        public ActionResult PurchasedBooks()
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string email = Session["Email"].ToString();

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return HttpNotFound("User not found.");
            } 
            
            var purchasedBooks = db.Purchases
                .Include(p => p.Books) 
                .Where(p => p.UserID == user.UserID)
                .AsEnumerable() 
                .Select(p => new PurchasedBookViewModel
                {
                    ImageUrl = p.Books.ImageUrl, 
                    PurchaseID = p.PurchaseID,
                    BookID = p.BookID,
                    Title = p.Books.Title,
                    Author = p.Books.Author,
                    PurchaseDate = p.PurchaseDate
                })
                .ToList();

            return View(purchasedBooks);
        }

        
        
        [HttpGet]
        public ActionResult BorrowingBooks()
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string email = Session["Email"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return HttpNotFound("User not found.");
            }

            // איתור ספרים שפג תוקפם
            var expiredBooks = db.Borrowing
                .Where(b => b.UserID == user.UserID && b.DueDate < DateTime.Now)
                .Include(b => b.Books) // כולל את הספרים הקשורים
                .ToList();

            // החזרת הספרים למלאי והסרתם מבסיס הנתונים
            if (expiredBooks.Any())
            {
                foreach (var expiredBook in expiredBooks)
                {
                    if (expiredBook.Books != null)
                    {
                        expiredBook.Books.Stock = (expiredBook.Books.Stock ?? 0) + 1; // עדכון המלאי
                    }
                }

                db.Borrowing.RemoveRange(expiredBooks); // הסרת השאלות שפג תוקפן
                db.SaveChanges(); // שמירת השינויים בבסיס הנתונים
            }

            // שאילתת LINQ להחזרת הספרים הפעילים למשתמש
            var BorrowBooks = db.Borrowing
                .Include(p => p.Books)
                .Where(p => p.UserID == user.UserID)
                .AsEnumerable()
                .Select(p => new BorrowingBookViewModel
                {
                    ImageUrl = p.Books.ImageUrl,
                    BorrowingID = p.BorrowID,
                    BookID = p.BookID,
                    Title = p.Books.Title,
                    Author = p.Books.Author,
                    BorrowDate = p.BorrowDate,
                    DueDate = p.DueDate
                })
                .ToList();

            return View(BorrowBooks);
        }

        [HttpGet]
        public ActionResult ShowDownloadFormats(int bookId)
        {
            try
            {
                var book = db.Books.FirstOrDefault(b => b.BookID == bookId);
                if (book == null)
                {
                    return Json(new { success = false, message = "Book not found." }, JsonRequestBehavior.AllowGet);
                }

                var formats = JsonConvert.DeserializeObject<List<string>>(book.Formats ?? "[]")
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();

                if (formats.Count == 0)
                {
                    return Json(new { success = false, message = "No formats available for this book." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, formats }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpGet]
        public ActionResult DownloadBook(int bookId, string selectedFormat)
        {
            try
            {
                // בדיקת קיום הספר במסד הנתונים
                var book = db.Books.FirstOrDefault(b => b.BookID == bookId);
                if (book == null)
                {
                    return HttpNotFound("Book not found.");
                }

                // בדיקת קיום הפורמט
                var formats = JsonConvert.DeserializeObject<List<string>>(book.Formats ?? "[]")
                    .Select(f => f.Trim())
                    .ToList();

                if (!formats.Contains(selectedFormat))
                {
                    return HttpNotFound("Invalid format selected.");
                }

                // יצירת תוכן הקובץ
                var fileContent = $"This is a placeholder file for the book '{book.Title}' in {selectedFormat} format.";
                var fileName = $"{book.Title.Replace(" ", "_")}.{selectedFormat.ToLower()}";

                // החזרת הקובץ להורדה
                var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"An error occurred: {ex.Message}");
            }
        }

        public ActionResult DownloadPdf()
        {
            var pdfContent = new byte[0]; // תוכן PDF ריק
            return File(pdfContent, "application/pdf", "BookInfo.pdf");
        }

        [HttpPost]
        public async Task<ActionResult> EndBorrowing(int borrowingId)
        {
            try
            {
                // שליפת הרשומה של ההשאלה כולל הספר
                var borrowing = db.Borrowing.Include(b => b.Books).FirstOrDefault(b => b.BorrowID == borrowingId);
                if (borrowing == null)
                {
                    return Json(new { success = false, message = "Borrowing record not found." });
                }

                if (borrowing.Books != null)
                {
                    var book = db.Books.FirstOrDefault(b => b.BookID == borrowing.BookID);
                    if (book == null)
                    {
                        return Json(new { success = false, message = "Book not found in the database." });
                    }

                    // עדכון מלאי הספר
                    book.Stock = (book.Stock ?? 0) + 1;

                    // הסרת הרשומה של ההשאלה בטבלת Borrowing
                    db.Borrowing.Remove(borrowing);
                    db.SaveChanges();

                    // טיפול ברשימת ההמתנה
                    var waitingListService = new WaitingListService(db);
                    await waitingListService.CheckAndUpdateWaitingList(book.BookID);

                    return Json(new { success = true, message = "Borrowing period ended successfully, and waiting list processing has started." });
                }

                return Json(new { success = false, message = "Book record not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        
        [HttpGet]
        public ActionResult FavoriteBooks()
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string email = Session["Email"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return HttpNotFound("User not found.");
            }

            ViewBag.FavoriteBooks = db.UserFavoriteBooks
                .Where(f => f.UserID == user.UserID)
                .Select(f => f.BookID)
                .ToList();

            var favoriteBooks = db.UserFavoriteBooks
                .Include(f => f.Books)
                .Where(f => f.UserID == user.UserID)
                .Select(f => new FavoriteBookViewModel
                {
                    BookID = f.BookID,
                    Title = f.Books.Title,
                    Author = f.Books.Author,
                    ImageUrl = f.Books.ImageUrl,
                    FavoriteDate = f.DateAdded,
                    UserName = f.Users.FirstName
                })
                .ToList();

            return View(favoriteBooks);
        }
        
        
        [HttpGet]
        public ActionResult BookHistory()
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string email = Session["Email"].ToString();

            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return HttpNotFound("User not found.");
            }

            var bookHistory = db.UserBookHistory
                .Include(h => h.Books) 
                .Where(h => h.UserID == user.UserID)
                .AsEnumerable() 
                .Select(h => new UserBookHistoryViewModel
                {
                    InteractionID = h.InteractionID,
                    BookID = h.BookID,
                    Title = h.Books.Title,
                    Author = h.Books.Author,
                    ImageUrl = h.Books.ImageUrl,
                    PurchaseDate = h.PurchaseDate,
                    BorrowDate = h.BorrowDate
                })
                .ToList();

            return View(bookHistory);
        }
        [HttpPost]
        public ActionResult ToggleFavorite(int bookId)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false });
                }

                int userId = (int)Session["UserID"];
                var favorite = db.UserFavoriteBooks.FirstOrDefault(f => f.UserID == userId && f.BookID == bookId);

                if (favorite != null)
                {
                    db.UserFavoriteBooks.Remove(favorite);
                    db.SaveChanges();
                    return Json(new { success = true, isFavorite = false });
                }
                else
                {
                    db.UserFavoriteBooks.Add(new UserFavoriteBooks
                    {
                        UserID = userId,
                        BookID = bookId,
                        DateAdded = DateTime.Now
                    });
                    db.SaveChanges();
                    return Json(new { success = true, isFavorite = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}