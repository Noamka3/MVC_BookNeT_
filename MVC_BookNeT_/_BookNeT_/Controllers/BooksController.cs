using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using _BookNeT_.Models;
using _BookNeT_.Services;


namespace _BookNeT_.Controllers
{
    public class BooksController : Controller
    {
        private BooknetProjectEntities2 db = new BooknetProjectEntities2();
        
        private bool IsAdmin()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Admin";
        }
        
        public ActionResult Index()
        {
            
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }
            return View(db.Books.ToList());
        }
        
        public ActionResult Details(int? id)
        {
            
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Books book = db.Books.Find(id);
            if (book == null)
            {
                return HttpNotFound();
            }
            return View(book);
        }

        public ActionResult Create()
        {
            
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }

            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Books book, string[] SelectedFormats)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }

            // בדיקה אם ספר עם אותם פרטים כבר קיים
            if (IsBookExists(book.Title, book.Author, book.YearOfPublication))
            {
                ModelState.AddModelError("", "Error: A book with the same title, author, and year of publication already exists.");
                return View(book);
            }
            
            if (ModelState.IsValid)
            {
                if (book.IsDiscounted == true)
                {
                    if (!IsDiscountValid(book.DiscountEndDate, book.DiscountPercentage, out string discountError))
                    {
                        ModelState.AddModelError("DiscountEndDate", discountError);
                        return View(book);
                    }
                }
                else
                {
                    book.DiscountPercentage = null;
                    book.DiscountEndDate    = null;
                }

                if (SelectedFormats != null)
                {
                    book.Formats = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedFormats);
                }

                db.Books.Add(book);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(book);
        }

        
        public ActionResult Edit(int? id)
        {
            
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Books book = db.Books.Find(id);
            if (book == null)
            {
                return HttpNotFound();
            }
            
            if (book.Stock == 0 && book.IsBorrowable == true)
            {
                book.Status = "Out of Stock";
                db.SaveChanges();  
            }
            return View(book);
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<ActionResult> Edit(Books book, string[] SelectedFormats)
{
    if (!IsAdmin())
    {
        return RedirectToAction("Unauthorized", "Home");
    }

    // בדיקה אם ספר עם אותם פרטים כבר קיים (למעט הספר הנוכחי)
    if (IsBookExists(book.Title, book.Author, book.YearOfPublication, book.BookID))
    {
        ModelState.AddModelError("", "Error: A book with the same title, author, and year of publication already exists.");
        return View(book);
    }
    
    if (ModelState.IsValid)
    {
        var originalBook = db.Books.Find(book.BookID);
        if (originalBook == null)
        {
            return HttpNotFound();
        }

        // שמירת המלאי המקורי לפני העדכון
        int previousStock = originalBook.Stock ?? 0;

        // עדכון הערכים בספר המקורי
        originalBook.Title = book.Title;
        originalBook.Author = book.Author;
        originalBook.Publisher = book.Publisher;
        originalBook.YearOfPublication = book.YearOfPublication;
        originalBook.Genre = book.Genre;
        originalBook.PurchasePrice = book.PurchasePrice;
        originalBook.BorrowPrice = book.BorrowPrice;
        originalBook.Stock = book.Stock;
        originalBook.IsBorrowable = book.IsBorrowable;
        originalBook.AgeRestriction = book.AgeRestriction;
        originalBook.ImageUrl = book.ImageUrl;
        originalBook.Description = book.Description;
        originalBook.Status = book.Status;

        // עדכון שדות הנחה
        if (book.IsDiscounted == true)
        {
            if (!IsDiscountValid(book.DiscountEndDate, book.DiscountPercentage, out string discountError))
            {
                ModelState.AddModelError("DiscountEndDate", discountError);
                return View(book);
            }
            originalBook.IsDiscounted       = true;
            originalBook.DiscountPercentage = book.DiscountPercentage;
            originalBook.DiscountEndDate    = book.DiscountEndDate;
        }
        else
        {
            originalBook.IsDiscounted       = false;
            originalBook.DiscountPercentage = null;
            originalBook.DiscountEndDate    = null;
        }

        if (SelectedFormats != null)
        {
            originalBook.Formats = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedFormats);
        }

        db.SaveChanges();

        // נבדוק אם המלאי עבר מ-0 לערך חיובי
        if (previousStock == 0 && (originalBook.Stock ?? 0) > 0)
        {
            // קריאה לרשימת ההמתנה
            originalBook.Status = "Available";  // אם המלאי עבר מ-0 לערך חיובי, שים את הסטטוס כ-Available
            db.SaveChanges();
            
            var waitingListService = new WaitingListService(db);
            await waitingListService.CheckAndUpdateWaitingList(book.BookID);
        }

        return RedirectToAction("Index");
    }

    return View(book);
}

public ActionResult Delete(int? id)
{
    if (!IsAdmin())
    {
        return RedirectToAction("Unauthorized", "Home");
    }

    if (id == null)
    {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
    }
    Books book = db.Books.Find(id);
    if (book == null)
    {
        return HttpNotFound();
    }
    return View(book);
}


[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public ActionResult DeleteConfirmed(int id)
{
    if (!IsAdmin())
        return RedirectToAction("Unauthorized", "Home");

    var book = db.Books.Find(id);
    if (book == null)
        return HttpNotFound();

    using (var transaction = db.Database.BeginTransaction())
    {
        try
        {
            db.Borrowing.RemoveRange(db.Borrowing.Where(b => b.BookID == id));
            db.Purchases.RemoveRange(db.Purchases.Where(p => p.BookID == id));
            db.ShoppingCart.RemoveRange(db.ShoppingCart.Where(sc => sc.BookID == id));
            db.WaitingList.RemoveRange(db.WaitingList.Where(wl => wl.BookID == id));
            db.UserBookHistory.RemoveRange(db.UserBookHistory.Where(ub => ub.BookID == id));
            db.UserFavoriteBooks.RemoveRange(db.UserFavoriteBooks.Where(ufb => ufb.BookID == id));
            db.BookFeedback.RemoveRange(db.BookFeedback.Where(bf => bf.BookID == id));
            db.Books.Remove(book);
            db.SaveChanges();
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            TempData["Error"] = "An error occurred while deleting the book. Please try again.";
            return RedirectToAction("Index");
        }
    }

    return RedirectToAction("Index");
}

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyBulkDiscount(string Genre, int DiscountPercentage, DateTime DiscountEndDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }

            if (DiscountEndDate < DateTime.Today || DiscountEndDate > DateTime.Today.AddDays(AppConstants.MaxDiscountDays))
            {
                TempData["Error"] = "Discount end date must be between today and 7 days from now";
                return RedirectToAction("Index");
            }

            var booksQuery = db.Books.AsQueryable();
            if (!string.IsNullOrEmpty(Genre))
            {
                booksQuery = booksQuery.Where(b => b.Genre == Genre);
            }

            foreach (var book in booksQuery)
            {
                book.IsDiscounted = true;
                book.DiscountPercentage = DiscountPercentage;
                book.DiscountEndDate = DiscountEndDate;
            }

            db.SaveChanges();
            TempData["Success"] = "Bulk discount applied successfully";
            return RedirectToAction("Index");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelBulkDiscount(string Genre)
        {
            if (!IsAdmin()) return RedirectToAction("Unauthorized", "Home");

            var booksQuery = db.Books.Where(b => b.IsDiscounted == true);
            if (!string.IsNullOrEmpty(Genre))
            {
                booksQuery = booksQuery.Where(b => b.Genre == Genre);
            }

            foreach (var book in booksQuery)
            {
                book.IsDiscounted = false;
                book.DiscountPercentage = null;
                book.DiscountEndDate = null;
            }

            db.SaveChanges();
            TempData["Success"] = string.IsNullOrEmpty(Genre) ? 
                "All discounts cancelled successfully" : 
                $"Discounts for {Genre} books cancelled successfully";
            return RedirectToAction("Index");
        }
        
        private bool IsDiscountValid(DateTime? endDate, decimal? percentage, out string error)
        {
            var today   = DateTime.Today;
            var maxDate = today.AddDays(AppConstants.MaxDiscountDays);

            if (!endDate.HasValue || endDate.Value < today || endDate.Value > maxDate)
            {
                error = $"Discount end date must be between today and {AppConstants.MaxDiscountDays} days from now.";
                return false;
            }

            if (!percentage.HasValue || percentage <= 0)
            {
                error = "Please enter a valid discount percentage.";
                return false;
            }

            error = null;
            return true;
        }

        private bool IsBookExists(string title, string author, int? yearOfPublication, int? excludeBookId = null)
        {
            var query = db.Books.Where(b =>
                b.Title.ToLower() == title.ToLower() &&
                b.Author.ToLower() == author.ToLower() &&
                b.YearOfPublication == yearOfPublication);

            if (excludeBookId.HasValue)
            {
                query = query.Where(b => b.BookID != excludeBookId.Value);
            }

            return query.Any();
        }

        
        
    }
}
