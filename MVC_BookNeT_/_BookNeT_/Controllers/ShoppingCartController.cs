using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _BookNeT_.Models;

namespace _BookNeT_.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();
        
        // מציג את עגלת הקניות של המשתמש
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];
            var cartItems = db.ShoppingCart
                .Where(c => c.UserID == userId)
                .Include(c => c.Books)
                .ToList();

            return View(cartItems);
        }

        // הוספת ספר לעגלה
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int bookId, bool isBorrow, bool isAjax = false)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Please login to add items to your cart.";
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            var cartItem = db.ShoppingCart.FirstOrDefault(c => c.UserID == userId && c.BookID == bookId && c.IsBorrow == isBorrow);

            if (cartItem != null)
            {
                cartItem.Quantity += 1;
            }
            else
            {
                var newCartItem = new ShoppingCart
                {
                    UserID = userId,
                    BookID = bookId,
                    Quantity = 1,
                    AddedDate = DateTime.Now,
                    IsBorrow = isBorrow,
                    IsPurchase = !isBorrow
                };
                db.ShoppingCart.Add(newCartItem);
            }

            db.SaveChanges();
            string successMsg = isBorrow 
                ? "The book has been added to your borrow cart."
                : "The book has been added to your purchase cart.";
            TempData["SuccessMessage"] = successMsg;

            if (Request.IsAjaxRequest() || isAjax)
            {
                return Json(new { success = true, message = successMsg });
            }
     
            return RedirectToAction("Index", "Library");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuickPurchase(int bookId)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];
            var book = db.Books.Find(bookId);

            if (book == null || !book.PurchasePrice.HasValue || book.Stock <= 0)
            {
                TempData["ErrorMessage"] = "Book not available for purchase.";
                return RedirectToAction("Index", "Library");
            }

            // הוספת הספר לעגלה
            var cartItem = new ShoppingCart
            {
                BookID = bookId,
                UserID = userId,
                Quantity = 1,
                IsBorrow = false,
                IsPurchase = true,
                AddedDate = DateTime.Now 
            };

            db.ShoppingCart.Add(cartItem);
            db.SaveChanges();

            return RedirectToAction("Checkout", "Payment");
        }
        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuickBorrow(int bookId)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];
            var book = db.Books.Find(bookId);

            if (book == null || !book.IsBorrowable.HasValue || !book.IsBorrowable.Value || book.Stock <= 0)
            {
                TempData["ErrorMessage"] = "Book not available for borrowing.";
                return RedirectToAction("Index", "Library");
            }

            // הוספת הספר לעגלה
            var cartItem = new ShoppingCart
            {
                BookID = bookId,
                UserID = userId,
                Quantity = 1,
                IsBorrow = true,
                IsPurchase = false,
                AddedDate = DateTime.Now 
            };

            db.ShoppingCart.Add(cartItem);
            db.SaveChanges();

            return RedirectToAction("Checkout", "Payment");
        }
        
        // הסרת פריט מהעגלה
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int cartItemId)
        {
            var cartItem = db.ShoppingCart.Find(cartItemId);
            
            if (cartItem != null && Session["UserID"] != null && cartItem.UserID == (int)Session["UserID"])
            {
                db.ShoppingCart.Remove(cartItem);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Item removed successfully!";
            }

            return RedirectToAction("Index");
        }

        // עדכון כמות של פריט
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            if (quantity < 1)
            {
                return Json(new { success = false, message = "Quantity must be at least 1" });
            }

            var cartItem = db.ShoppingCart.Find(cartItemId);
            
            if (cartItem != null && Session["UserID"] != null && cartItem.UserID == (int)Session["UserID"])
            {
                cartItem.Quantity = quantity;
                db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
        
        
        // פונקציה חדשה לקבלת מספר הפריטים בעגלה
        public ActionResult GetCartCount()
        {
            if (Session["UserID"] == null)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }

            int userId = (int)Session["UserID"];
            int count = db.ShoppingCart
                .Where(c => c.UserID == userId)
                .Sum(c => (int?)c.Quantity) ?? 0;

            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateType(int cartId, string type)
        {
            var cartItem = db.ShoppingCart.Include(c => c.Books).FirstOrDefault(c => c.CartID == cartId);

            if (cartItem == null)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
                return RedirectToAction("Index");
            }

            if (type == "Borrow")
            {
                // שינוי ל-Borrow
                if (cartItem.Books.IsBorrowable.HasValue && cartItem.Books.IsBorrowable.Value)
                {
                    cartItem.IsBorrow = true;
                    cartItem.IsPurchase = false;
                }
                else
                {
                    TempData["ErrorMessage"] = "This book is not available for borrowing.";
                }
            }
            else if (type == "Purchase")
            {
                // שינוי ל-Purchase
                cartItem.IsBorrow = false;
                cartItem.IsPurchase = true;
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "Item type updated successfully.";
            return RedirectToAction("Index");
        }

        
    }
}