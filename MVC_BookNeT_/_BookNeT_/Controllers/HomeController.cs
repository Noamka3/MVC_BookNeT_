using _BookNeT_;
using _BookNeT_.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _BookNeT_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                CalculateCartCount();
            }
            else
            {
                ViewBag.CartCount = null;
            }

            ViewBag.IsHomePage = true;

            var feedbacks = db.ServiceFeedback
                .Select(f => new FeedbackViewModel
                {
                    FeedbackID = f.FeedbackID,
                    FeedbackText = f.FeedbackText,
                    Rating = f.Rating ?? 0,
                    FeedbackDate = f.FeedbackDate ?? DateTime.MinValue,
                    UserName = f.Users != null ? f.Users.FirstName + " " + f.Users.LastName : "Unknown"
                }).ToList();

            ViewBag.Feedbacks = feedbacks;
            ViewBag.BookCount = db.Books.Count();
            ViewBag.UserCount = db.Users.Count();
            ViewBag.GenreCount = db.Books.Select(b => b.Genre).Distinct().Count();
            ViewBag.FeaturedBooks = db.Books.OrderBy(b => b.BookID).Take(6).ToList();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.IsHomePage = false;
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.IsHomePage = false;
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult AddFeedback(string feedbackText, int? rating)
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userRating = rating ?? 0;
            var userEmail = Session["Email"].ToString();
            var user = db.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user != null)
            {
                var feedback = new ServiceFeedback
                {
                    UserID = user.UserID,
                    FeedbackText = feedbackText,
                    Rating = userRating,
                    FeedbackDate = DateTime.Now
                };

                db.ServiceFeedback.Add(feedback);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public ActionResult RemoveFeedback(int feedbackId)
        {
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // מציאת התגובה לפי FeedbackID
            var feedback = db.ServiceFeedback.FirstOrDefault(f => f.FeedbackID == feedbackId);

            if (feedback != null)
            {
                // הסרת התגובה מהמסד
                db.ServiceFeedback.Remove(feedback);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
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
    }

    public class FeedbackViewModel
    {
        public int FeedbackID { get; set; }
        public string FeedbackText { get; set; }
        public int Rating { get; set; }
        public DateTime FeedbackDate { get; set; }
        public string UserName { get; set; }
    }
}
