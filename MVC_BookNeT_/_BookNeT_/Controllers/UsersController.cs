using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using _BookNeT_.Models;
using _BookNeT_.Models.viewModels;

namespace _BookNeT_Project.Controllers
{
    public class UsersController : Controller
    {
        BooknetProjectEntities2 db = new BooknetProjectEntities2();
        
        private bool IsAdmin()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Admin";
        }
        
        // GET: Users
        public ActionResult Users()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Unauthorized", "Home");
            }
            return View(db.Users.ToList());
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            return PartialView("admin_Details_user", user);
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserID,FirstName,LastName,Email,Phone,Password,Role,Age,RegistrationDate")] Users user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Users");
            }

            return View(user);
        }
        [HttpGet]
        public ActionResult Edits(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            
            return PartialView("admin_edit_user", user);
        }
        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetEdits(Users updatedUser)
        {
            // בדיקה אם הנתונים תקינים
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed. Please check the form.";
                return RedirectToAction("Users");
            }

            // בדיקה אם מספר הטלפון קיים למשתמש אחר
            if (db.Users.Any(u => u.Phone == updatedUser.Phone && u.UserID != updatedUser.UserID))
            {
                TempData["ErrorMessage"] = "Phone number already exists in the system.";
                return RedirectToAction("Users");
            }

            // בדיקה אם המייל קיים למשתמש אחר
            if (db.Users.Any(u => u.Email == updatedUser.Email && u.UserID != updatedUser.UserID))
            {
                TempData["ErrorMessage"] = "Email address already exists in the system.";
                return RedirectToAction("Users");
            }

            // עדכון הנתונים במשתמש הקיים
            var user = db.Users.FirstOrDefault(u => u.UserID == updatedUser.UserID);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Users");
            }

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Email = updatedUser.Email;
            user.Phone = updatedUser.Phone;

            // שמירת השינויים ב-DB
            db.SaveChanges();

            TempData["SuccessMessage"] = "Details updated successfully.";
            return RedirectToAction("Users");
        }
        [HttpPost]
        public ActionResult ValidateUnique(string phone, string email, int userId)
        {
            // בדיקת ייחודיות של מספר הטלפון
            if (db.Users.Any(u => u.Phone == phone && u.UserID != userId))
            {
                TempData["ErrorMessage"] = "Phone number already exists in the system.";
                return RedirectToAction("Users");
            }
        
            // בדיקת ייחודיות של האימייל
            if (db.Users.Any(u => u.Email == email && u.UserID != userId))
            {
                TempData["ErrorMessage"] = "Email address already exists in the system.";
                return RedirectToAction("Users");
            }
        
            TempData["SuccessMessage"] = "Validation passed. You can proceed.";
            return RedirectToAction("Users");
        }

        // POST: Users/Edit/5
        [HttpPost]
        public ActionResult Edit([Bind(Include = "UserID,FirstName,LastName,Email,Phone,Password,Role,Age,RegistrationDate")] Users user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Users");
            }

            return View(user);
        }

        public ActionResult Delete(int? id)
{
    // אם ה-ID לא קיים, נחזיר שגיאה
    if (id == null)
    {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
    }

    // חפש את המשתמש ב-DB לפי ה-ID
    Users user = db.Users.Find(id);

    // אם המשתמש לא נמצא, נחזיר שגיאה
    if (user == null)
    {
        return HttpNotFound();
    }

    try
    {
        // מחיקת כל ההשאלות של המשתמש
        var borrowings = db.Borrowing.Where(b => b.UserID == user.UserID).ToList();
        foreach (var borrowing in borrowings)
        {
            db.Borrowing.Remove(borrowing);
        }

        // מחיקת כל הרכישות של המשתמש
        var purchases = db.Purchases.Where(p => p.UserID == user.UserID).ToList();
        foreach (var purchase in purchases)
        {
            db.Purchases.Remove(purchase);
        }

        // מחיקת כל המשובים של המשתמש
        var feedbacks = db.ServiceFeedback.Where(f => f.UserID == user.UserID).ToList();
        foreach (var feedback in feedbacks)
        {
            db.ServiceFeedback.Remove(feedback);
        }
        
        // מחיקת כל המשובים על הספר של המשתמש
        var BookFeedbacks = db.BookFeedback.Where(f => f.UserID == user.UserID).ToList();
        foreach (var feedback in BookFeedbacks)
        {
            db.BookFeedback.Remove(feedback);
        }

        // מחיקת כל הספרים המועדפים של המשתמש
        var favoriteBooks = db.UserFavoriteBooks.Where(f => f.UserID == user.UserID).ToList();
        foreach (var favoriteBook in favoriteBooks)
        {
            db.UserFavoriteBooks.Remove(favoriteBook);
        }
        
        // מחיקת היסטוריית הספרים של המשתמש
        var bookHistory = db.UserBookHistory.Where(h => h.UserID == user.UserID).ToList();
        foreach (var history in bookHistory)
        {
            db.UserBookHistory.Remove(history);
        }
        
        //מחיקת המשתמש מרשימת המתנה 
        var waitingList = db.WaitingList.Where(f => f.UserID == user.UserID).ToList();
        foreach (var waiter in waitingList)
        {
            db.WaitingList.Remove(waiter);
        }
        
        // מחיקת המשתמש עצמו
        db.Users.Remove(user);


        db.SaveChanges();

        // הצגת הודעת הצלחה
        TempData["SuccessMessage"] = "The User " + user.FirstName + " " + user.LastName + " and all related data have been successfully deleted.";
    }
    catch (Exception ex)
    {

        TempData["ErrorMessage"] = $"An error occurred while deleting the user: {ex.Message}";
    }


    return RedirectToAction("Users");
}
        

        // GET: Users/ChangeAdmin/5
        [HttpGet]
        public ActionResult ChangeRole(int? id)
        {
            // אם ה-ID לא קיים, נחזיר שגיאה
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // חפש את המשתמש ב-DB לפי ה-ID
            Users user = db.Users.Find(id);

            // אם המשתמש לא נמצא, נחזיר שגיאה
            if (user == null)
            {
                return HttpNotFound();
            }
            if (user.Role == "Admin")
            {
                user.Role = "user";
            }
            else
            {
                user.Role = "Admin";
            }

            db.SaveChanges();

            // החזרה לדף הרשימה עם הודעת הצלחה
            TempData["SuccessMessage"] = "The User " + user.FirstName + " has been successfully updated to " + user.Role;
            return RedirectToAction("Users");
        }
        // GET: Users/Search


        // Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
