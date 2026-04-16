using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace _BookNeT_.Models.BookUser
{
    public class BorrowingBookViewModel
    {
        public string ImageUrl { get; set; }
        public int BorrowingID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime? BorrowDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}