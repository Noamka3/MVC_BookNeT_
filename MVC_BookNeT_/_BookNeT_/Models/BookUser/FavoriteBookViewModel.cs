using System;

namespace _BookNeT_.Models.BookUser
{
    public class FavoriteBookViewModel
    {
        public int FavoriteID { get; set; }
        public int UserID { get; set; }
        public int BookID { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? FavoriteDate { get; set; }
        public string Title { get; set; } // שם הספר
        public string Author { get; set; } // שם המחבר
        public string UserName { get; set; } // שם המשתמש
        public DateTime? DateAdded { get; set; } // התאריך שבו הספר נוסף למועדפים
    }
}