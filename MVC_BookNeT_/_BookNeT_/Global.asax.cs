using System;
using System.Timers; // יש לוודא שהוספת את הספרייה הזו
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using _BookNeT_.Controllers;

namespace _BookNeT_
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static Timer _timer;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            TestReminders();
            // הפעלת הטיימר לתזכורות
            //StartReminderService();
        }

        private void StartReminderService()
        {
            // טיימר לבדיקת השאלות פעם ביום
            _timer = new Timer(24 * 60 * 60 * 1000); // 24 שעות
            _timer.Elapsed += TimerElapsed; // חיבור לאירוע של הטיימר
            _timer.AutoReset = true; // הטיימר יחזור על עצמו כל 24 שעות
            _timer.Enabled = true; // הפעלת הטיימר
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // קריאה לפונקציה ששולחת את התזכורות
            var reminderController = new ReminderController();
            reminderController.CheckAndSendRemindersForAllUsers();
        }
        
        private void TestReminders()
        {
            try
            {
                var reminderController = new ReminderController();
                reminderController.CheckAndSendRemindersForAllUsers();
                Console.WriteLine("Reminder check executed successfully. Emails should have been sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

        }
    }
}


