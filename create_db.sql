-- Create database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '_BookNeT_project')
    CREATE DATABASE [_BookNeT_project];
GO

USE [_BookNeT_project];
GO

-- Users table (no FK dependencies)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE [dbo].[Users] (
    [UserID]           INT            IDENTITY(1,1) NOT NULL,
    [FirstName]        NVARCHAR(50)   NOT NULL,
    [LastName]         NVARCHAR(50)   NOT NULL,
    [Email]            NVARCHAR(100)  NOT NULL,
    [Phone]            NVARCHAR(15)   NULL,
    [Password]         NVARCHAR(255)  NOT NULL,
    [Role]             NVARCHAR(20)   NOT NULL,
    [Age]              INT            NULL,
    [RegistrationDate] DATE           NULL,
    [ImageUrl]         NVARCHAR(150)  NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserID])
);
GO

-- Books table (no FK dependencies)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Books' AND xtype='U')
CREATE TABLE [dbo].[Books] (
    [BookID]             INT             IDENTITY(1,1) NOT NULL,
    [Title]              NVARCHAR(200)   NOT NULL,
    [Author]             NVARCHAR(100)   NOT NULL,
    [Publisher]          NVARCHAR(100)   NULL,
    [YearOfPublication]  INT             NULL,
    [Formats]            NVARCHAR(MAX)   NULL,
    [Genre]              NVARCHAR(50)    NULL,
    [PurchasePrice]      DECIMAL(10,2)   NULL,
    [BorrowPrice]        DECIMAL(10,2)   NULL,
    [Stock]              INT             NULL,
    [IsBorrowable]       BIT             NULL,
    [AgeRestriction]     INT             NULL,
    [IsDiscounted]       BIT             NULL,
    [DiscountEndDate]    DATE            NULL,
    [BorrowDate]         DATE            NULL,
    [Status]             NVARCHAR(20)    NULL,
    [ImageUrl]           NVARCHAR(100)   NULL,
    [DiscountPercentage] INT             NULL,
    [Description]        NVARCHAR(MAX)   NULL,
    CONSTRAINT [PK_Books] PRIMARY KEY ([BookID])
);
GO

-- BookFeedback
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BookFeedback' AND xtype='U')
CREATE TABLE [dbo].[BookFeedback] (
    [FeedbackID]   INT           IDENTITY(1,1) NOT NULL,
    [UserID]       INT           NULL,
    [FeedbackText] NVARCHAR(MAX) NULL,
    [Rating]       INT           NULL,
    [FeedbackDate] DATETIME      NULL,
    [BookID]       INT           NULL,
    CONSTRAINT [PK_BookFeedback] PRIMARY KEY ([FeedbackID]),
    CONSTRAINT [FK__BookFeedb__UserI__0C85DE4D] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
    CONSTRAINT [FK__BookFeedb__BookI__0E6E26BF] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID])
);
GO

-- Borrowing
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Borrowing' AND xtype='U')
CREATE TABLE [dbo].[Borrowing] (
    [BorrowID]   INT          IDENTITY(1,1) NOT NULL,
    [UserID]     INT          NOT NULL,
    [BookID]     INT          NOT NULL,
    [BorrowDate] DATE         NOT NULL,
    [DueDate]    DATE         NOT NULL,
    [Status]     NVARCHAR(20) NULL,
    CONSTRAINT [PK_Borrowing] PRIMARY KEY ([BorrowID]),
    CONSTRAINT [FK__Borrowing__UserI__76969D2E] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
    CONSTRAINT [FK__Borrowing__BookI__778AC167] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID])
);
GO

-- Purchases
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Purchases' AND xtype='U')
CREATE TABLE [dbo].[Purchases] (
    [PurchaseID]   INT           IDENTITY(1,1) NOT NULL,
    [UserID]       INT           NOT NULL,
    [BookID]       INT           NOT NULL,
    [PurchaseDate] DATE          NULL,
    [Amount]       DECIMAL(10,2) NOT NULL,
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([PurchaseID]),
    CONSTRAINT [FK__Purchases__UserI__787EE5A0] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
    CONSTRAINT [FK__Purchases__BookI__797309D9] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID])
);
GO

-- ServiceFeedback
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ServiceFeedback' AND xtype='U')
CREATE TABLE [dbo].[ServiceFeedback] (
    [FeedbackID]   INT            IDENTITY(1,1) NOT NULL,
    [UserID]       INT            NOT NULL,
    [FeedbackText] NVARCHAR(1000) NULL,
    [Rating]       INT            NULL,
    [FeedbackDate] DATE           NULL,
    CONSTRAINT [PK_ServiceFeedback] PRIMARY KEY ([FeedbackID]),
    CONSTRAINT [FK__ServiceFe__UserI__7A672E12] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID])
);
GO

-- ShoppingCart
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ShoppingCart' AND xtype='U')
CREATE TABLE [dbo].[ShoppingCart] (
    [CartID]     INT  IDENTITY(1,1) NOT NULL,
    [UserID]     INT  NOT NULL,
    [BookID]     INT  NOT NULL,
    [Quantity]   INT  NOT NULL,
    [AddedDate]  DATE NULL,
    [IsBorrow]   BIT  NOT NULL,
    [IsPurchase] BIT  NOT NULL,
    CONSTRAINT [PK_ShoppingCart] PRIMARY KEY ([CartID]),
    CONSTRAINT [FK__ShoppingC__UserI__7B5B524B] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
    CONSTRAINT [FK__ShoppingC__BookI__7C4F7684] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID])
);
GO

-- UserBookHistory
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserBookHistory' AND xtype='U')
CREATE TABLE [dbo].[UserBookHistory] (
    [InteractionID] INT      IDENTITY(1,1) NOT NULL,
    [UserID]        INT      NOT NULL,
    [BookID]        INT      NOT NULL,
    [PurchaseDate]  DATETIME NULL,
    [BorrowDate]    DATETIME NULL,
    CONSTRAINT [PK_UserBookHistory] PRIMARY KEY ([InteractionID]),
    CONSTRAINT [FK_UserBookHistory_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
    CONSTRAINT [FK_UserBookHistory_Books] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID])
);
GO

-- UserFavoriteBooks
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserFavoriteBooks' AND xtype='U')
CREATE TABLE [dbo].[UserFavoriteBooks] (
    [FavoriteID] INT      IDENTITY(1,1) NOT NULL,
    [UserID]     INT      NOT NULL,
    [BookID]     INT      NOT NULL,
    [DateAdded]  DATETIME NULL,
    CONSTRAINT [PK_UserFavoriteBooks] PRIMARY KEY ([FavoriteID]),
    CONSTRAINT [FK_UserFavoriteBooks_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
    CONSTRAINT [FK_UserFavoriteBooks_Books] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID])
);
GO

-- WaitingList
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WaitingList' AND xtype='U')
CREATE TABLE [dbo].[WaitingList] (
    [WaitingID]        INT      IDENTITY(1,1) NOT NULL,
    [BookID]           INT      NOT NULL,
    [UserID]           INT      NOT NULL,
    [Position]         INT      NOT NULL,
    [NotificationSent] BIT      NOT NULL,
    [JoinDate]         DATETIME NOT NULL,
    [NotificationDate] DATETIME NULL,
    CONSTRAINT [PK_WaitingList] PRIMARY KEY ([WaitingID]),
    CONSTRAINT [FK__WaitingLi__BookI__7F2BE32F] FOREIGN KEY ([BookID]) REFERENCES [dbo].[Books]([BookID]),
    CONSTRAINT [FK__WaitingLi__UserI__00200768] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID])
);
GO

-- Seed: Admin user (password: Admin123!)
-- BCrypt hash of 'Admin123!'
INSERT INTO [dbo].[Users] (FirstName, LastName, Email, Phone, Password, Role, Age, RegistrationDate)
SELECT 'Admin', 'BookNeT', 'admin@booknet.com', '0500000000',
       '$2a$11$p3ICnzUYj1F1FKQkNb3YDe5Nm.Y1RG3RB7JLa9z9h8u3r4vWH6Z4e',
       'Admin', 30, GETDATE()
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE Email = 'admin@booknet.com');
GO

-- Seed: Sample books
INSERT INTO [dbo].[Books] (Title, Author, Publisher, YearOfPublication, Genre, PurchasePrice, BorrowPrice, Stock, IsBorrowable, AgeRestriction, IsDiscounted, Status, Description)
SELECT 'The Great Gatsby', 'F. Scott Fitzgerald', 'Scribner', 1925, 'Fiction', 49.90, 9.90, 5, 1, 0, 0, 'Available', 'A classic American novel set in the Jazz Age.'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Books] WHERE Title = 'The Great Gatsby');

INSERT INTO [dbo].[Books] (Title, Author, Publisher, YearOfPublication, Genre, PurchasePrice, BorrowPrice, Stock, IsBorrowable, AgeRestriction, IsDiscounted, Status, Description)
SELECT '1984', 'George Orwell', 'Secker & Warburg', 1949, 'Dystopian', 39.90, 7.90, 3, 1, 0, 0, 'Available', 'A dystopian novel about a totalitarian society.'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Books] WHERE Title = '1984');

INSERT INTO [dbo].[Books] (Title, Author, Publisher, YearOfPublication, Genre, PurchasePrice, BorrowPrice, Stock, IsBorrowable, AgeRestriction, IsDiscounted, Status, Description)
SELECT 'Harry Potter and the Sorcerer''s Stone', 'J.K. Rowling', 'Bloomsbury', 1997, 'Fantasy', 59.90, 12.90, 10, 1, 0, 0, 'Available', 'The first book in the Harry Potter series.'
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Books] WHERE Title = 'Harry Potter and the Sorcerer''s Stone');
GO

PRINT 'Database created and seeded successfully!';
GO
