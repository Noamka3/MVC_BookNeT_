using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using _BookNeT_.Models;
using _BookNeT_.Services;
using Newtonsoft.Json;

namespace _BookNeT_.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string _paypalClientId;
        private readonly string _paypalSecret;
        private readonly string _paypalUrl;
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();
        private readonly EmailService _emailService  = new EmailService();

        public PaymentController()
        {
            _paypalClientId = ConfigurationManager.AppSettings["PayPal:ClientId"];
            _paypalSecret   = ConfigurationManager.AppSettings["PayPal:ClientSecret"];
            _paypalUrl      = ConfigurationManager.AppSettings["PayPal:Url"];
        }

        public async Task<string> CreatePayPalOrder(decimal amount)
        {
            using (var client = new HttpClient())
            {
                var tokenRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{_paypalUrl}/v1/oauth2/token");
                var credentials  = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_paypalClientId}:{_paypalSecret}"));
                tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                tokenRequest.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var tokenResponse = await client.SendAsync(tokenRequest);
                var tokenJson     = JsonConvert.DeserializeObject<dynamic>(await tokenResponse.Content.ReadAsStringAsync());
                string accessToken = tokenJson.access_token;

                var orderRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{_paypalUrl}/v2/checkout/orders");
                orderRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                orderRequest.Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new { amount = new { currency_code = "ILS", value = amount.ToString("0.00") } }
                    }
                }), Encoding.UTF8, "application/json");

                var orderResponse = await client.SendAsync(orderRequest);
                var orderJson     = JsonConvert.DeserializeObject<dynamic>(await orderResponse.Content.ReadAsStringAsync());
                return orderJson.id;
            }
        }

        public ActionResult Checkout()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId   = (int)Session["UserID"];
            var cartItems = db.ShoppingCart
                .Where(c => c.UserID == userId)
                .Include(c => c.Books)
                .ToList();

            ViewBag.TotalAmount   = cartItems.Sum(item => item.IsBorrow ? item.Books.BorrowPrice ?? 0 : item.Books.PurchasePrice ?? 0);
            ViewBag.PayPalClientId = _paypalClientId;
            return View(cartItems);
        }

        [HttpPost]
        public JsonResult ProcessPayment(string paymentMethod)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentMethod) ||
                    (paymentMethod != "creditCard" && paymentMethod != "paypal"))
                {
                    return Json(new { success = false, message = "Invalid or missing payment method.", redirectUrl = Url.Action("Index", "ShoppingCart") });
                }

                if (Session["UserID"] == null)
                    return Json(new { success = false, message = "Session expired. Please log in again.", redirectUrl = Url.Action("Login", "Account") });

                int userId    = (int)Session["UserID"];
                var cartItems = db.ShoppingCart
                    .Where(c => c.UserID == userId)
                    .Include(c => c.Books)
                    .ToList();

                if (!cartItems.Any())
                    return Json(new { success = false, message = "Your cart is empty.", redirectUrl = Url.Action("Index", "ShoppingCart") });

                // בדיקת מגבלת השאלות
                int booksToBorrow = cartItems.Where(c => c.IsBorrow).Sum(c => c.Quantity);
                if (booksToBorrow > 0)
                {
                    int currentlyBorrowed = db.Borrowing
                        .Count(b => b.UserID == userId &&
                                    (b.Status == "Available" || b.Status == "OnLoan" || b.Status == "Borrowed"));

                    if (currentlyBorrowed + booksToBorrow > AppConstants.MaxBorrowedBooks)
                    {
                        return Json(new
                        {
                            success    = false,
                            message    = $"You can't borrow more than {AppConstants.MaxBorrowedBooks} books in total. " +
                                         $"You currently have {currentlyBorrowed} borrowed and are trying to borrow {booksToBorrow} more.",
                            redirectUrl = Url.Action("Index", "ShoppingCart")
                        });
                    }
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        decimal totalAmount    = 0;
                        var purchasedBookLines = new List<string>();

                        foreach (var item in cartItems)
                        {
                            var book = item.Books;
                            if (book == null || (book.Stock ?? 0) < item.Quantity)
                            {
                                return Json(new { success = false, message = $"Book '{book?.Title}' is not available in the desired quantity.", redirectUrl = Url.Action("Index", "ShoppingCart") });
                            }

                            decimal unitPrice    = item.IsBorrow ? (book.BorrowPrice ?? 0) : book.CalculateDiscountedPrice();
                            decimal itemTotal    = unitPrice * item.Quantity;
                            totalAmount         += itemTotal;
                            purchasedBookLines.Add($"{book.Title} (x{item.Quantity}) - {itemTotal:C}");

                            if (item.IsBorrow)
                            {
                                if (book.IsBorrowable != true)
                                    return Json(new { success = false, message = $"Book '{book.Title}' is not available for borrowing.", redirectUrl = Url.Action("Index", "ShoppingCart") });

                                for (int i = 0; i < item.Quantity; i++)
                                {
                                    db.Borrowing.Add(new Borrowing
                                    {
                                        UserID     = userId,
                                        BookID     = item.BookID,
                                        BorrowDate = DateTime.Now,
                                        DueDate    = DateTime.Now.AddDays(AppConstants.BorrowDurationDays),
                                        Status     = "Available"
                                    });
                                    db.UserBookHistory.Add(new UserBookHistory
                                    {
                                        UserID     = userId,
                                        BookID     = item.BookID,
                                        BorrowDate = DateTime.Now
                                    });
                                }

                                var waitEntry = db.WaitingList.FirstOrDefault(w => w.BookID == item.BookID && w.UserID == userId);
                                if (waitEntry != null)
                                    db.WaitingList.Remove(waitEntry);
                            }
                            else
                            {
                                if (book.PurchasePrice == null)
                                    return Json(new { success = false, message = $"Book '{book.Title}' is not available for purchase.", redirectUrl = Url.Action("Index", "ShoppingCart") });

                                for (int i = 0; i < item.Quantity; i++)
                                {
                                    db.Purchases.Add(new Purchases
                                    {
                                        UserID       = userId,
                                        BookID       = item.BookID,
                                        PurchaseDate = DateTime.Now,
                                        Amount       = unitPrice
                                    });
                                    db.UserBookHistory.Add(new UserBookHistory
                                    {
                                        UserID       = userId,
                                        BookID       = item.BookID,
                                        PurchaseDate = DateTime.Now
                                    });
                                }
                            }

                            book.Stock -= item.Quantity;
                        }

                        db.SaveChanges();
                        ClearShoppingCart(userId);
                        transaction.Commit();

                        var user = db.Users.FirstOrDefault(u => u.UserID == userId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                            SendPurchaseEmail(user.Email, user.FirstName, purchasedBookLines, totalAmount);

                        return Json(new { success = true, message = "Payment processed successfully!", redirectUrl = Url.Action("Index", "ShoppingCart") });
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "An error occurred while processing your payment. Please try again.", redirectUrl = Url.Action("Index", "ShoppingCart") });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unexpected error: " + ex.Message, redirectUrl = Url.Action("Index", "ShoppingCart") });
            }
        }

        private void SendPurchaseEmail(string email, string userName, List<string> purchasedBooks, decimal totalAmount)
        {
            string body = $"<p>Hi {userName},</p><p>Thank you for your purchase! Here are the details:</p><ul>";
            foreach (var book in purchasedBooks)
                body += $"<li>{book}</li>";
            body += $"</ul><p><strong>Total Amount: {totalAmount:C}</strong></p><p>We hope you enjoy your books!</p>";

            _emailService.Send(email, "Purchase Confirmation - BookNeT", body);
        }

        public void ClearShoppingCart(int userId)
        {
            var cartItems = db.ShoppingCart.Where(c => c.UserID == userId).ToList();
            db.ShoppingCart.RemoveRange(cartItems);
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
