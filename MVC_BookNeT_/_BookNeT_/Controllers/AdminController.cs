using System.Linq;
using System.Web.Mvc;
using _BookNeT_.Models;

namespace _BookNeT_.Controllers
{
    public class AdminController : Controller
    {
        BooknetProjectEntities2 db = new BooknetProjectEntities2();
        public ActionResult Admin()
        {
            
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Unauthorized", "Home"); 
            }
            
            var userCount = db.Users.Count();
            var bookCount = db.Books.Count();

            ViewBag.UserCount = userCount;
            ViewBag.BookCount = bookCount;

            return View();
        }
    }
}