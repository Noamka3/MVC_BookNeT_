using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using _BookNeT_.Models;
using _BookNeT_.Models.viewModels;
using _BookNeT_.Services;

namespace _BookNeT_.Controllers
{
    public class AccountController : Controller
    {
        private readonly BooknetProjectEntities2 db = new BooknetProjectEntities2();
        private readonly EmailService _emailService  = new EmailService();

        private void SetWhiteNavbar()
        {
            ViewBag.IsHomePage    = true;
            ViewBag.NavbarColor   = "#ffffff";
            ViewBag.NavbarIconColor = "#ffffff";
        }

        public ActionResult Index()
        {
            SetWhiteNavbar();
            return View();
        }

        public ActionResult Register()
        {
            SetWhiteNavbar();
            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            SetWhiteNavbar();

            if (!ModelState.IsValid)
                return View(model);

            if (db.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "The email already exists in the system.");
                return View(model);
            }

            if (db.Users.Any(u => u.Phone == model.Phone))
            {
                ModelState.AddModelError("Phone", "This phone number is already registered in the system.");
                return View(model);
            }

            if (model.Age < 1)
            {
                ModelState.AddModelError("Age", "Age must be greater than 0.");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
                return View(model);
            }

            var user = new Users
            {
                FirstName        = model.FirstName,
                LastName         = model.LastName,
                Phone            = model.Phone,
                Role             = "User",
                Age              = model.Age,
                RegistrationDate = DateTime.Now,
                Email            = model.Email,
                Password         = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            try
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Login", "Account", new { showSuccess = true });
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again later.");
                return View(model);
            }
        }

        public ActionResult Login()
        {
            SetWhiteNavbar();
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            SetWhiteNavbar();

            if (!ModelState.IsValid)
                return View(model);

            var user = db.Users.FirstOrDefault(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            Session["Email"]     = user.Email;
            Session["UserID"]    = user.UserID;
            Session["Role"]      = user.Role;
            Session["FirstName"] = user.FirstName;

            if (model.RememberMe)
            {
                // מאחסן רק את האימייל — לא את הסיסמה
                var authCookie = new HttpCookie("UserLogin")
                {
                    Values   = { ["Email"] = user.Email },
                    Expires  = DateTime.Now.AddDays(AppConstants.RememberMeDays),
                    HttpOnly = true
                };
                Response.Cookies.Add(authCookie);
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult VerifyEmail()
        {
            SetWhiteNavbar();
            return View();
        }

        [HttpPost]
        public ActionResult VerifyEmail(VerifyEmailModel model)
        {
            SetWhiteNavbar();

            if (!ModelState.IsValid)
                return View(model);

            var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "The provided email does not exist in the system.");
                return View(model);
            }

            try
            {
                SendResetPasswordEmail(model.Email);
                TempData["SuccessMessage"] = "A password reset email has been sent.";
                return RedirectToAction("PasswordResetSent");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while sending the email: {ex.Message}");
                return View(model);
            }
        }

        public ActionResult PasswordResetSent()
        {
            SetWhiteNavbar();
            return View();
        }

        private void SendResetPasswordEmail(string email)
        {
            string baseUrl   = "https://localhost:44300";
            string resetLink = $"{baseUrl}/Account/ChangePassword?email={email}";

            string body = $@"
                <p>Click the following link to reset your password:</p>
                <p><a href='{resetLink}' target='_blank'>Reset Password</a></p>";

            _emailService.Send(email, "Password Reset - BookNeT", body);
        }

        [HttpGet]
        public ActionResult ChangePassword(string email)
        {
            SetWhiteNavbar();

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid or missing email address.";
                return RedirectToAction("Login");
            }

            Session["Email"] = email;
            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            SetWhiteNavbar();

            if (!ModelState.IsValid)
                return View(model);

            var email = Session["Email"] as string;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ErrorPage");

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                ModelState.AddModelError("", "The passwords do not match. Please try again.");
                return View(model);
            }

            try
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No user found with this email.");
                    return View(model);
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                db.SaveChanges();

                TempData["SuccessMessage"] = "The password has been changed successfully!";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }
    }
}
