
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _BookNeT_.Models;
using System.Collections.Generic;
using _BookNeT_.Models.BookUser;
using _BookNeT_.Services;

namespace _BookNeT_.Controllers
{
    public class LibraryController : Controller
    {
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();
        private readonly WaitingListService _waitingListService;

        public LibraryController()
        {
            _waitingListService = new WaitingListService(db);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult JoinWaitingList(int bookId)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];
            var book = db.Books.Find(bookId);

            if (book == null)
            {
                return HttpNotFound();
            }

            // בדיקה אם המשתמש כבר ברשימת ההמתנה
            var existingWait = db.WaitingList.FirstOrDefault(w => w.BookID == bookId && w.UserID == userId);
            if (existingWait != null)
            {
                TempData["ErrorMessage"] = "You are already on the waiting list for this book.";
                return RedirectToAction("Details", new { id = bookId });
            }

            // מציאת המיקום הבא ברשימת ההמתנה
            int nextPosition = db.WaitingList
                .Where(w => w.BookID == bookId)
                .Select(w => w.Position)
                .DefaultIfEmpty(0)
                .Max() + 1;

            // הוספת המשתמש לרשימת ההמתנה
            var waitingEntry = new WaitingList
            {
                BookID = bookId,
                UserID = userId,
                Position = nextPosition,
                JoinDate = DateTime.Now,
                NotificationSent = false,
                NotificationDate = null
            };

            db.WaitingList.Add(waitingEntry);
            db.SaveChanges();

            TempData["SuccessMessage"] = "You have been added to the waiting list.";
            return RedirectToAction("Details", new { id = bookId });
        }
        
       
       //פונקציה האם המשתמש נמצא ב-3 המקומות הראשונים 
       private bool CanUserBorrowOrPurchase(int bookId, int userId)
       {
           // שליפת שלושת המקומות הראשונים ברשימת ההמתנה של הספר
           var topWaitingUsers = db.WaitingList
               .Where(w => w.BookID == bookId)
               .OrderBy(w => w.Position)
               .Take(3)
               .Select(w => w.UserID)
               .ToList();

           // אם אין רשימת המתנה, הספר זמין לכולם
           if (!topWaitingUsers.Any())
           {
               return true;
           }

           // בדיקה אם המשתמש נמצא בין שלושת המקומות הראשונים
           return topWaitingUsers.Contains(userId);
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public ActionResult RemoveFromWaitingList(int bookId)
       {
           if (Session["UserID"] == null)
           {
               return RedirectToAction("Login", "Account");
           }

           int userId = (int)Session["UserID"];
           var waitEntry = db.WaitingList.FirstOrDefault(w => w.BookID == bookId && w.UserID == userId);

           if (waitEntry != null)
           {
               db.WaitingList.Remove(waitEntry);

               // עדכון מיקומים למשתמשים הנותרים ברשימה
               var remainingEntries = db.WaitingList
                   .Where(w => w.BookID == bookId && w.Position > waitEntry.Position)
                   .OrderBy(w => w.Position);

               foreach (var entry in remainingEntries)
               {
                   entry.Position--;
               }

               db.SaveChanges();

               TempData["SuccessMessage"] = "You have been removed from the waiting list.";
           }
           else
           {
               TempData["ErrorMessage"] = "You are not on the waiting list for this book.";
           }

           return RedirectToAction("Details", new { id = bookId });
       }

       
       
       [HttpGet]
       public JsonResult GetWaitingListInfo(int bookId)
       {
           try
           {
               var book = db.Books.Find(bookId);
               if (book == null)
               {
                   return Json(new { success = false, message = "Book not found." }, JsonRequestBehavior.AllowGet);
               }

               var currentDate = DateTime.Now;

               // שליפת נתונים מהבסיס נתונים
               var waitingList = db.WaitingList
                   .Where(w => w.BookID == bookId)
                   .OrderBy(w => w.Position)
                   .Select(w => new
                   {
                       w.Position,
                       w.UserID,
                       UserName = w.Users.FirstName + " " + w.Users.LastName,
                       JoinDate = w.JoinDate
                   })
                   .ToList();

               // חישוב זמינות צפויה
               var waitingListWithEstimates = waitingList
                   .Select((w, index) => new
                   {
                       Position = w.Position,
                       w.UserID,
                       w.UserName,
                       EstimatedAvailability = (index + 1) * 30 - (int)(currentDate - w.JoinDate).TotalDays
                   })
                   .ToList();

               bool isAdmin = Session["Role"]?.ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

               return Json(new
               {
                   success = true,
                   waitingCount = waitingListWithEstimates.Count,
                   waitingList = waitingListWithEstimates,
                   isAdmin = isAdmin
               }, JsonRequestBehavior.AllowGet);
           }
           catch (Exception ex)
           {
               return Json(new { success = false, message = $"Error: {ex.Message}" }, JsonRequestBehavior.AllowGet);
           }
       }
       
      public ActionResult Index(string searchTerm, string sortBy, string author, string genre,
    decimal? minPrice, decimal? maxPrice, bool? isBorrowable,
    bool? showDiscounted, int? publicationYear, string ageRestriction, string purchaseMethod)
{
    bool isAdmin = false;
    // בדיקה אם המשתמש מחובר
    if (Session["UserID"] == null)
    {
        return RedirectToAction("Login", "Account"); // הפניה להתחברות אם המשתמש לא מחובר
    }
    
    if (Session["Role"] != null)
    {
        isAdmin = string.Equals(Session["Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase);
    }
    
    int userId = (int)Session["UserID"]; // מזהה המשתמש המחובר
    UpdateExpiredDiscounts(); // עדכון הנחות שפג תוקפן
    CalculateCartCount(); // חישוב כמות הפריטים בעגלה

    if (Session["UserID"] != null)
    {
        ViewBag.FavoriteBooks = db.UserFavoriteBooks
            .Where(f => f.UserID == userId)
            .Select(f => f.BookID)
            .ToList();
    }
    else
    {
        ViewBag.FavoriteBooks = new List<int>();
    }

    var books = db.Books.Include(b => b.WaitingList).AsQueryable();

    // search book in the search field
    if (!string.IsNullOrEmpty(searchTerm))
    {
        books = books.Where(b => b.Title.Contains(searchTerm));
    }

    // filtering
    if (!string.IsNullOrEmpty(author))
    {
        books = books.Where(b => b.Author.Contains(author));
    }

    if (!string.IsNullOrEmpty(genre))
    {
        books = books.Where(b => b.Genre == genre);
    }

    if (minPrice.HasValue)
    {
        books = books.Where(b => b.PurchasePrice >= minPrice.Value);
    }

    if (maxPrice.HasValue)
    {
        books = books.Where(b => b.PurchasePrice <= maxPrice.Value);
    }

    // סינון לפי שיטת רכישה
    if (!string.IsNullOrEmpty(purchaseMethod))
    {
        if (purchaseMethod == "Purchase")
        {
            books = books.Where(b => b.PurchasePrice.HasValue && b.PurchasePrice.Value > 0);
        }
        else if (purchaseMethod == "Borrow")
        {
            books = books.Where(b => b.IsBorrowable == true);
        }
    }
    
    // סינון לפי טווח גילאים
    if (!string.IsNullOrEmpty(ageRestriction))
    {
        int ageCategory;
        if (int.TryParse(ageRestriction, out ageCategory))
        {
            books = books.Where(b => b.AgeRestriction == ageCategory);
        }
    }
    
    
    if (showDiscounted.HasValue && showDiscounted.Value)
    {
        var currentDate = DateTime.Now;
        books = books.Where(b => b.IsDiscounted == true &&
                                 b.DiscountEndDate.HasValue &&
                                 b.DiscountEndDate.Value >= currentDate);
    }

    if (publicationYear.HasValue)
    {
        books = books.Where(b => b.YearOfPublication == publicationYear.Value);
    }

    // sorting
    switch (sortBy)
    {
        case "PriceAsc":
            books = books.OrderBy(b => b.PurchasePrice); // Sort price from lowest to highest
            break;
        case "PriceDesc":
            books = books.OrderByDescending(b => b.PurchasePrice); // Sort price from highest to lowest
            break;
        case "MostPurchased":
            books = books
                .OrderByDescending(b => db.UserBookHistory.Count(h => h.BookID == b.BookID)); // Sort by interactions in UserBookHistory
            break;
        case "Genre":
            books = books.OrderBy(b => b.Genre);
            break;
        case "YearDesc":
            books = books.OrderByDescending(b => b.YearOfPublication); // Sort year from newest to oldest
            break;
        case "YearAsc":
            books = books.OrderBy(b => b.YearOfPublication); // Sort year from oldest to newest
            break;
        default:
            books = books.OrderByDescending(b => b.BookID); // Default - sort by bookID
            break;
    }

    ViewBag.Authors = db.Books
        .Where(b => b.Author != null)
        .Select(b => b.Author)
        .Distinct()
        .ToList();

    ViewBag.Genres = db.Books
        .Where(b => b.Genre != null)
        .Select(b => b.Genre)
        .Distinct()
        .ToList();

    var maxYear = db.Books.Max(b => b.YearOfPublication ?? DateTime.Now.Year);
    var minYear = db.Books.Min(b => b.YearOfPublication ?? DateTime.Now.Year - 100);

    ViewBag.MaxYear = maxYear;
    ViewBag.MinYear = minYear;

    Dictionary<int, bool> bookAvailability = new Dictionary<int, bool>();
    foreach (var book in books)
    {
        bookAvailability[book.BookID] = !book.WaitingList.Any() || 
                                        CanUserBorrowOrPurchase(book.BookID, userId);
    }
    ViewBag.BookAvailability = bookAvailability;
    ViewBag.IsAdmin = isAdmin;
    return View(books.ToList());}

public ActionResult Details(int id)
{
    var book = db.Books.Find(id);
    if (book == null)
    {
        return HttpNotFound();
    }

    bool isAdmin = false;
    // בדיקה אם המשתמש מחובר
    if (Session["UserID"] == null)
    {
        return RedirectToAction("Login", "Account"); // הפניה להתחברות אם המשתמש לא מחובר
    }
    
    if (Session["Role"] != null)
    {
        isAdmin = string.Equals(Session["Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase);
    }
    UpdateExpiredDiscounts();
    CalculateCartCount();

    var historyRecords = db.UserBookHistory
        .Include(h => h.Books)
        .Include(h => h.Users)
        .Where(h => h.BookID == id)
        .ToList();

    int historyCount = historyRecords.Count;


    ViewBag.HistoryCount = historyCount;
    
    bool canAddReview = false;
    if (Session["UserID"] != null)
    {
        int userId = (int)Session["UserID"];
        canAddReview = db.UserBookHistory.Any(h => h.UserID == userId && h.BookID == id);

        // הוספת בדיקת הזמינות
        Dictionary<int, bool> bookAvailability = new Dictionary<int, bool>();
        bookAvailability[book.BookID] = !book.WaitingList.Any() || 
                                        CanUserBorrowOrPurchase(book.BookID, userId);
        ViewBag.BookAvailability = bookAvailability;
    }

    ViewBag.CanAddReview = canAddReview;

    var reviews = db.BookFeedback
        .Include(r => r.Users)
        .Where(r => r.BookID == id)
        .OrderByDescending(r => r.FeedbackDate)
        .ToList();

    ViewBag.Reviews = reviews;
    ViewBag.IsAdmin = isAdmin;

    return View(book);
}

public ActionResult DetailsModal(int id)
{
    var book = db.Books.Find(id);
    if (book == null) return HttpNotFound();

    bool isAdmin = Session["Role"] != null &&
                   string.Equals(Session["Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase);

    UpdateExpiredDiscounts();
    CalculateCartCount();

    var historyCount = db.UserBookHistory.Count(h => h.BookID == id);
    ViewBag.HistoryCount = historyCount;

    bool canAddReview = false;
    if (Session["UserID"] != null)
    {
        int userId = (int)Session["UserID"];
        canAddReview = db.UserBookHistory.Any(h => h.UserID == userId && h.BookID == id);
        Dictionary<int, bool> bookAvailability = new Dictionary<int, bool>();
        bookAvailability[book.BookID] = !book.WaitingList.Any() ||
                                        CanUserBorrowOrPurchase(book.BookID, userId);
        ViewBag.BookAvailability = bookAvailability;
    }
    else
    {
        Dictionary<int, bool> bookAvailability = new Dictionary<int, bool>();
        bookAvailability[book.BookID] = false;
        ViewBag.BookAvailability = bookAvailability;
    }

    ViewBag.CanAddReview = canAddReview;
    ViewBag.Reviews = db.BookFeedback
        .Include(r => r.Users)
        .Where(r => r.BookID == id)
        .OrderByDescending(r => r.FeedbackDate)
        .ToList();
    ViewBag.IsAdmin = isAdmin;

    return PartialView("_DetailsModal", book);
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(BookFeedback feedback)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            // בדיקה אם המשתמש רכש או השאיל את הספר
            bool hasAccess = db.UserBookHistory.Any(h => h.UserID == userId && h.BookID == feedback.BookID);

            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You cannot leave a review for a book you haven't borrowed or purchased.";
                return RedirectToAction("Details", new { id = feedback.BookID });
            }

            if (ModelState.IsValid)
            {
                var newFeedback = new BookFeedback
                {
                    BookID = feedback.BookID,
                    UserID = userId,
                    FeedbackText = feedback.FeedbackText,
                    Rating = feedback.Rating,
                    FeedbackDate = DateTime.Now
                };

                db.BookFeedback.Add(newFeedback);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Your review has been added successfully!";
            }

            return RedirectToAction("Details", new { id = feedback.BookID });
        }


        private void CalculateCartCount()
        {
            if (Session["UserID"] != null)
            {
                int userId = (int)Session["UserID"];
                ViewBag.CartCount = db.ShoppingCart
                    .Where(c => c.UserID == userId)
                    .Sum(c => (int?)c.Quantity) ?? 0;
            }
            else
            {
                ViewBag.CartCount = 0;
            }
        }

        // cleaning the resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        private void UpdateExpiredDiscounts()
        {
            var currentDate = DateTime.Now;
            var expiredBooks = db.Books
                .Where(b => b.IsDiscounted == true &&
                            b.DiscountEndDate.HasValue &&
                            b.DiscountEndDate.Value < currentDate)
                .ToList();

            foreach (var book in expiredBooks)
            {
                book.IsDiscounted = false;
                book.DiscountPercentage = null;
                book.DiscountEndDate = null; 
            }

            if (expiredBooks.Any())
            {
                db.SaveChanges();
            }
        }

        [HttpPost]
        public JsonResult ToggleFavorite(int bookId)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false });
            }

            int userId = (int)Session["UserID"];

            // בדיקת קיומו של הספר ברשימת המועדפים
            var favorite = db.UserFavoriteBooks.FirstOrDefault(f => f.UserID == userId && f.BookID == bookId);

            if (favorite != null)
            {
                // אם הספר כבר ברשימת המועדפים - הסרה
                db.UserFavoriteBooks.Remove(favorite);
                db.SaveChanges();

                return Json(new { success = true, isFavorite = false });
            }
            else
            {
                // אם הספר אינו ברשימת המועדפים - הוספה
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
        
        // 20 הספרים החדשים ביותר
        public ActionResult Newest()
        {
            var newestBooks = db.Books
                .OrderByDescending(b => b.BookID) // מיין מזהה מהגבוה לנמוך (ספרים חדשים יותר)
                .Take(20) // קח 20 ספרים אחרונים
                .ToList();

            ViewBag.Title = "Newest Books";
            return View("Newest", newestBooks); // החזר את התוצאה לתצוגה בשם Newest
        }
        
        public ActionResult BestOf2024()
        {
            var popularBooks = db.UserBookHistory
                .GroupBy(h => h.BookID)
                .Where(g => g.Count() >= 10)  // ספרים שמופיעים 10 פעמים או יותר
                .Select(g => new
                {
                    BookID = g.Key,
                    InteractionCount = g.Count()
                })
                .Join(db.Books,
                    p => p.BookID,
                    b => b.BookID,
                    (p, b) => b)
                .Where(b => b.YearOfPublication == 2024)
                .ToList();

            return View("BestOf2024", popularBooks);
        }

        public ActionResult KidsBooks()
        {
            var popularKidsBooks = db.UserBookHistory
                .GroupBy(h => h.BookID)
                .Where(g => g.Count() >= 10)  // ספרים שמופיעים 10 פעמים או יותר
                .Select(g => new
                {
                    BookID = g.Key,
                    InteractionCount = g.Count()
                })
                .Join(db.Books,
                    p => p.BookID,
                    b => b.BookID,
                    (b, book) => book)
                .Where(b => b.AgeRestriction >= 8 && b.AgeRestriction <= 12)
                .ToList();

            return View("KidsBooks", popularKidsBooks);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveBookReview(int reviewId)
        {
            if (Session["Role"] == null || string.Compare(Session["Role"].ToString(), "Admin") != 0)
            {
                return RedirectToAction("Index");
            }

            var review = db.BookFeedback.Find(reviewId);
            if (review != null && review.BookID.HasValue) 
            {
                int bookId = review.BookID.Value; 
        
                // מחק את הביקורת
                db.BookFeedback.Remove(review);
                db.SaveChanges();
        
                TempData["SuccessMessage"] = "Review removed successfully.";
                
                return RedirectToAction("Details", new { id = bookId });
            }

            // במקרה שהביקורת לא נמצאה 
            TempData["ErrorMessage"] = "Review not found or invalid.";
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveUserFromWaitingList(int bookId, int userId)
        {
            if (Session["Role"] == null || string.Compare(Session["Role"].ToString(), "Admin") != 0)
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var waitingEntry = db.WaitingList.FirstOrDefault(w => w.BookID == bookId && w.UserID == userId);
        
                if (waitingEntry != null)
                {
                    db.WaitingList.Remove(waitingEntry);
            
                    var remainingEntries = db.WaitingList
                        .Where(w => w.BookID == bookId && w.Position > waitingEntry.Position)
                        .OrderBy(w => w.Position);
                
                    foreach (var entry in remainingEntries)
                    {
                        entry.Position--;
                    }
            
                    db.SaveChanges();
            
                    return Json(new { success = true });
                }
        
                return Json(new { success = false, message = "User not found in waiting list" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error removing user from waiting list" });
            }
        }
        
    }
}