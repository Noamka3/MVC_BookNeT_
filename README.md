<div align="center">

# 📚 BookNeT
### Online Bookstore Management System

![ASP.NET MVC](https://img.shields.io/badge/ASP.NET_MVC-5.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![PayPal](https://img.shields.io/badge/PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white)

**A full-stack web application for buying, borrowing, and managing books online.**

*Developed by [Noam Kadosh](https://github.com/Noamka3) & Eden Cohen*

</div>

---

## 🌟 About The Project

**BookNeT** is a comprehensive online bookstore built with **ASP.NET MVC 5 (C#)**. The platform allows users to browse a rich book catalog, purchase or borrow books, manage their personal library, and interact with a full payment system — all through a clean, responsive interface.

The project was developed as an academic capstone demonstrating real-world software engineering practices: layered architecture, secure authentication, database-first ORM, third-party API integration, and clean code principles.

---

## ✨ Features

### 👤 User System
- Registration & login with **BCrypt** password hashing
- Role-based access control — **User** and **Admin** roles
- "Remember Me" cookie with `HttpOnly` flag
- Profile management: name, email, phone, age, profile image
- Password reset via email link

### 📖 Book Catalog & Library
- Full book catalog with cover images, descriptions, genres, and age restrictions
- Advanced search & filtering by title, author, genre, year, and price range
- Book detail pages with reader reviews and ratings
- Add books to **Favorites**
- View personal **Book History** (purchases & borrowings)

### 🛒 Shopping Cart & Checkout
- Add books for **Purchase** or **Borrow** in one cart
- Toggle between purchase/borrow per item
- Live quantity controls with real-time price updates
- Persistent cart stored in the database

### 💳 Payment System
- **Credit Card** payment flow with full form validation
- **PayPal** integration via PayPal REST API (sandbox)
- Automatic order confirmation email sent after purchase
- Purchase and borrowing records saved to database

### 📬 Borrowing & Waiting List
- Borrow up to **3 books** simultaneously
- Automatic due date (30 days) tracking
- **Waiting list** system — users notified by email when a book becomes available
- Return borrowed books with automatic stock restoration
- Expired borrowing auto-cleanup

### 🔔 Reminder System
- Automated email reminders sent **5 days before** a book's due date

### 🛠️ Admin Panel
- Full **CRUD** for books (create, edit, delete with cascade)
- Bulk discount management per genre with date validation
- User management dashboard
- View all orders, borrowings, and waiting lists

---

## 🏗️ Architecture & Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# / ASP.NET MVC 5 |
| Database | Microsoft SQL Server + Entity Framework 6 (Database-First) |
| Frontend | Razor Views, Bootstrap 5, JavaScript (jQuery), Font Awesome |
| Authentication | BCrypt.Net password hashing, Session-based auth |
| Email | Gmail SMTP via `System.Net.Mail` |
| Payments | PayPal REST API (sandbox) |
| Version Control | Git / GitHub |

---

## 🗂️ Project Structure

```
BookNeT/
├── Controllers/
│   ├── AccountController.cs      # Register, Login, Password Reset
│   ├── BooksController.cs        # Admin book CRUD + discounts
│   ├── LibraryController.cs      # Book catalog, search, filters
│   ├── PaymentController.cs      # Checkout, PayPal, order processing
│   ├── ProfileController.cs      # User profile, borrowings, purchases
│   ├── ShoppingCartController.cs # Cart management
│   ├── ReminderController.cs     # Due-date email reminders
│   ├── WaitingListController.cs  # Waiting list + notifications
│   ├── EmailService.cs           # Centralized email service
│   └── AppConstants.cs           # App-wide constants
├── Models/
│   ├── Books.cs
│   ├── Users.cs
│   ├── ShoppingCart.cs
│   ├── Borrowing.cs
│   ├── Purchases.cs
│   ├── WaitingList.cs
│   └── ViewModels/
├── Views/
│   ├── Account/
│   ├── Library/
│   ├── ShoppingCart/
│   ├── Payment/
│   ├── Profile/
│   ├── Books/  (Admin)
│   └── Shared/
└── Web.config                    # App configuration (not committed)
```

---

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.7.2
- SQL Server (LocalDB or full instance)
- A Gmail account with an App Password for SMTP

### Setup

**1. Clone the repository**
```bash
git clone https://github.com/Noamka3/BookNeT.git
cd BookNeT
```

**2. Restore NuGet packages**
```bash
# In Visual Studio: right-click solution → Restore NuGet Packages
# Or via CLI:
nuget restore
```

**3. Configure Web.config**

Create `MVC_BookNeT_/_BookNeT_/Web.config` and add your credentials:
```xml
<appSettings>
  <add key="Smtp:Username" value="your-email@gmail.com" />
  <add key="Smtp:Password" value="your-gmail-app-password" />
  <add key="PayPal:ClientId"     value="your-paypal-client-id" />
  <add key="PayPal:ClientSecret" value="your-paypal-secret" />
  <add key="PayPal:Url"          value="https://api-m.sandbox.paypal.com" />
</appSettings>
```

**4. Set up the database**
- Open SQL Server Management Studio (or use LocalDB)
- Run `create_db.sql` to create and seed the database
- The connection string in `Web.config` points to `(localdb)\MSSQLLocalDB` by default

**5. Run the project**
```
Press F5 in Visual Studio — the app runs on https://localhost:44300
```

---

## 📸 Screenshots

| Page | Preview |
|------|---------|
| Home / Library | Browse and search the full book catalog |
| Shopping Cart | Manage items, toggle purchase/borrow, live totals |
| Checkout | Order summary + Credit Card / PayPal payment |
| Profile | View purchased books, active borrowings, favorites |
| Admin Panel | Manage books, users, discounts, and orders |

---

## 🔐 Security Highlights

- Passwords hashed with **BCrypt** (never stored in plain text)
- SMTP & PayPal credentials stored in **Web.config** (excluded from version control via `.gitignore`)
- Anti-Forgery tokens on all POST forms (`[ValidateAntiForgeryToken]`)
- `HttpOnly` flag on Remember Me cookie
- Role-based authorization checks on all admin routes
- Database transactions on critical operations (payment, book deletion)

---

## 👥 Authors

| Name | GitHub |
|------|--------|
| Noam Kadosh | [@Noamka3](https://github.com/Noamka3) |
| Eden Cohen | — |

---

<div align="center">

Made with ❤️ as part of an academic full-stack development course.

</div>
