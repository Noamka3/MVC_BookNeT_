using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace _BookNeT_.Models.BookUser
{
    public class UserBookHistoryViewModel
    {
        public int InteractionID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? BorrowDate { get; set; }
        public string InteractionType
        {
            get
            {
                if (PurchaseDate.HasValue)
                    return "Purchase";
                if (BorrowDate.HasValue)
                    return "Borrow";
                return "Unknown";
            }
        }
    }
}