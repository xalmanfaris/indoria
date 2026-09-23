using AuraLiving.Models;
using Dapper;

namespace AuraLiving.Services.Database;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMockDataService _mockDataService;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory, IMockDataService mockDataService, ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _mockDataService = mockDataService;
        _logger = logger;
    }

    public void InitializeDatabase()
    {
        try
        {
            // Step 1: Connect to Master Database to ensure 'IndoriaDb' exists
            using (var masterConnection = _connectionFactory.CreateMasterConnection())
            {
                string createDbSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'IndoriaDb')
                    BEGIN
                        CREATE DATABASE IndoriaDb;
                    END
                ";
                masterConnection.Execute(createDbSql);
                _logger.LogInformation("Database 'IndoriaDb' verified/created on SQL Server.");
            }

            // Step 2: Connect to IndoriaDb to create tables & ensure clean empty state
            using (var connection = _connectionFactory.CreateConnection())
            {
                string createTablesSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            Id NVARCHAR(50) PRIMARY KEY,
                            FullName NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(100) NOT NULL UNIQUE,
                            PasswordHash NVARCHAR(255) NOT NULL,
                            Role NVARCHAR(50) NOT NULL,
                            Phone NVARCHAR(50) NULL,
                            IsBlocked BIT NOT NULL DEFAULT 0,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IsBlocked')
                    BEGIN
                        ALTER TABLE Users ADD IsBlocked BIT NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstagramClickAnalytics')
                    BEGIN
                        CREATE TABLE InstagramClickAnalytics (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            PostId NVARCHAR(100) NOT NULL,
                            Permalink NVARCHAR(500) NOT NULL,
                            UserAgent NVARCHAR(500) NULL,
                            IpAddress NVARCHAR(100) NULL,
                            ClickedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                    BEGIN
                        CREATE TABLE Categories (
                            Id NVARCHAR(50) PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Slug NVARCHAR(100) NOT NULL UNIQUE,
                            Description NVARCHAR(MAX) NULL,
                            Image NVARCHAR(255) NULL,
                            ProductCount INT NOT NULL DEFAULT 0
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                    BEGIN
                        CREATE TABLE Products (
                            Id NVARCHAR(50) PRIMARY KEY,
                            Name NVARCHAR(255) NOT NULL,
                            Slug NVARCHAR(255) NOT NULL UNIQUE,
                            Brand NVARCHAR(100) NOT NULL,
                            Category NVARCHAR(100) NOT NULL,
                            CategorySlug NVARCHAR(100) NOT NULL,
                            Price DECIMAL(18,2) NOT NULL,
                            OriginalPrice DECIMAL(18,2) NOT NULL,
                            StockCount INT NOT NULL DEFAULT 10,
                            InStock BIT NOT NULL DEFAULT 1,
                            MainImage NVARCHAR(500) NULL,
                            ShortDescription NVARCHAR(MAX) NULL,
                            FullDescription NVARCHAR(MAX) NULL,
                            KeyFeaturesJson NVARCHAR(MAX) NULL,
                            SpecificationsJson NVARCHAR(MAX) NULL,
                            GalleryImagesJson NVARCHAR(MAX) NULL,
                            VideoUrl NVARCHAR(500) NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'KeyFeaturesJson')
                    BEGIN
                        ALTER TABLE Products ADD KeyFeaturesJson NVARCHAR(MAX) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SpecificationsJson')
                    BEGIN
                        ALTER TABLE Products ADD SpecificationsJson NVARCHAR(MAX) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'GalleryImagesJson')
                    BEGIN
                        ALTER TABLE Products ADD GalleryImagesJson NVARCHAR(MAX) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'VideoUrl')
                    BEGIN
                        ALTER TABLE Products ADD VideoUrl NVARCHAR(500) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                    BEGIN
                        CREATE TABLE Orders (
                            Id NVARCHAR(50) PRIMARY KEY,
                            UserId NVARCHAR(50) NOT NULL DEFAULT '',
                            OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
                            Status NVARCHAR(50) NOT NULL DEFAULT 'Confirmed',
                            EstimatedDelivery NVARCHAR(100) NULL,
                            CustomerEmail NVARCHAR(100) NULL,
                            CustomerName NVARCHAR(100) NULL,
                            ShippingAddressJson NVARCHAR(MAX) NULL,
                            PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'UPI',
                            SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                            Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            GrandTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                            ItemsJson NVARCHAR(MAX) NULL,
                            TimelineJson NVARCHAR(MAX) NULL,
                            CancellationReason NVARCHAR(500) NULL,
                            CancellationDate DATETIME NULL
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CancellationReason')
                    BEGIN
                        ALTER TABLE Orders ADD CancellationReason NVARCHAR(500) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CancellationDate')
                    BEGIN
                        ALTER TABLE Orders ADD CancellationDate DATETIME NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'UserId')
                    BEGIN
                        ALTER TABLE Orders ADD UserId NVARCHAR(50) NOT NULL DEFAULT '';
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'EstimatedDelivery')
                    BEGIN
                        ALTER TABLE Orders ADD EstimatedDelivery NVARCHAR(100) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'SubTotal')
                    BEGIN
                        ALTER TABLE Orders ADD SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'Discount')
                    BEGIN
                        ALTER TABLE Orders ADD Discount DECIMAL(18,2) NOT NULL DEFAULT 0;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ItemsJson')
                    BEGIN
                        ALTER TABLE Orders ADD ItemsJson NVARCHAR(MAX) NULL;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'TimelineJson')
                    BEGIN
                        ALTER TABLE Orders ADD TimelineJson NVARCHAR(MAX) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Coupons')
                    BEGIN
                        CREATE TABLE Coupons (
                            Code NVARCHAR(50) PRIMARY KEY,
                            DiscountAmount DECIMAL(18,2) NOT NULL,
                            MinimumSpend DECIMAL(18,2) NOT NULL,
                            Description NVARCHAR(255) NULL,
                            ExpiryDate DATETIME NOT NULL,
                            UsageCount INT NOT NULL DEFAULT 0,
                            IsActive BIT NOT NULL DEFAULT 1
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAddresses')
                    BEGIN
                        CREATE TABLE UserAddresses (
                            Id NVARCHAR(50) PRIMARY KEY,
                            UserId NVARCHAR(50) NOT NULL,
                            FullName NVARCHAR(100) NOT NULL,
                            Phone NVARCHAR(50) NOT NULL,
                            Email NVARCHAR(100) NULL,
                            AddressLine1 NVARCHAR(255) NOT NULL,
                            AddressLine2 NVARCHAR(255) NULL,
                            City NVARCHAR(100) NOT NULL,
                            State NVARCHAR(100) NOT NULL,
                            Pincode NVARCHAR(20) NOT NULL,
                            Landmark NVARCHAR(100) NULL,
                            Type NVARCHAR(50) NOT NULL DEFAULT 'Home',
                            IsDefault BIT NOT NULL DEFAULT 0,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WishlistItems')
                    BEGIN
                        CREATE TABLE WishlistItems (
                            Id NVARCHAR(50) PRIMARY KEY,
                            UserId NVARCHAR(50) NOT NULL,
                            ProductId NVARCHAR(50) NOT NULL,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
                    BEGIN
                        CREATE TABLE CartItems (
                            Id NVARCHAR(50) PRIMARY KEY,
                            UserId NVARCHAR(50) NOT NULL,
                            ProductId NVARCHAR(50) NOT NULL,
                            Quantity INT NOT NULL DEFAULT 1,
                            SelectedCapacity NVARCHAR(50) NULL,
                            SelectedColor NVARCHAR(50) NULL,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReviews')
                    BEGIN
                        CREATE TABLE ProductReviews (
                            Id NVARCHAR(50) PRIMARY KEY,
                            ProductId NVARCHAR(50) NOT NULL,
                            UserId NVARCHAR(50) NULL,
                            Author NVARCHAR(100) NOT NULL,
                            City NVARCHAR(100) NULL,
                            Rating INT NOT NULL DEFAULT 5,
                            Title NVARCHAR(200) NULL,
                            Comment NVARCHAR(MAX) NOT NULL,
                            VerifiedBuyer BIT NOT NULL DEFAULT 1,
                            HelpfulCount INT NOT NULL DEFAULT 0,
                            IsActive BIT NOT NULL DEFAULT 1,
                            ImageUrl NVARCHAR(500) NULL,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerNotifications')
                    BEGIN
                        CREATE TABLE CustomerNotifications (
                            Id NVARCHAR(50) PRIMARY KEY,
                            UserId NVARCHAR(50) NOT NULL,
                            Title NVARCHAR(200) NOT NULL,
                            Message NVARCHAR(MAX) NOT NULL,
                            Type NVARCHAR(50) NOT NULL DEFAULT 'Order',
                            SentVia NVARCHAR(50) NOT NULL DEFAULT 'Email & In-App',
                            IsRead BIT NOT NULL DEFAULT 0,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupportTickets')
                    BEGIN
                        CREATE TABLE SupportTickets (
                            Id NVARCHAR(50) PRIMARY KEY,
                            TicketNumber NVARCHAR(50) NOT NULL,
                            UserId NVARCHAR(50) NOT NULL,
                            CustomerName NVARCHAR(100) NOT NULL,
                            Email NVARCHAR(100) NOT NULL,
                            Subject NVARCHAR(250) NOT NULL,
                            Category NVARCHAR(50) NOT NULL DEFAULT 'General Support',
                            Priority NVARCHAR(50) NOT NULL DEFAULT 'Medium',
                            Status NVARCHAR(50) NOT NULL DEFAULT 'Open',
                            MessagesJson NVARCHAR(MAX) NULL,
                            CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                            UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReturnRequests')
                    BEGIN
                        CREATE TABLE ReturnRequests (
                            Id NVARCHAR(50) PRIMARY KEY,
                            UserId NVARCHAR(50) NOT NULL,
                            CustomerName NVARCHAR(100) NULL,
                            CustomerEmail NVARCHAR(100) NULL,
                            OrderId NVARCHAR(50) NOT NULL,
                            ProductId NVARCHAR(50) NULL,
                            ProductName NVARCHAR(255) NOT NULL,
                            ProductImage NVARCHAR(500) NULL,
                            Type NVARCHAR(50) NOT NULL DEFAULT 'Return',
                            Reason NVARCHAR(500) NOT NULL,
                            Description NVARCHAR(MAX) NULL,
                            Status NVARCHAR(50) NOT NULL DEFAULT 'Under Review',
                            RequestDate DATETIME NOT NULL DEFAULT GETDATE(),
                            RefundAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            AdminNotes NVARCHAR(MAX) NULL
                        );
                    END
                ";

                connection.Execute(createTablesSql);

                // Ensure ImageUrl column exists in ProductReviews table for existing databases
                try
                {
                    string alterReviewSql = @"
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReviews')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'ImageUrl')
                            BEGIN
                                ALTER TABLE ProductReviews ADD ImageUrl NVARCHAR(500) NULL;
                            END
                        END";
                    connection.Execute(alterReviewSql);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not verify/add ImageUrl column to ProductReviews.");
                }

                // Clear any leftover demo products, orders, and coupons from earlier initializations
                string wipeDemoDataSql = @"
                    DELETE FROM Products WHERE Id LIKE 'AURA-%' OR Id LIKE 'IND-PROD-%';
                    DELETE FROM Orders WHERE Id LIKE 'IND-%';
                    DELETE FROM Coupons;
                ";
                connection.Execute(wipeDemoDataSql);

                // Seed Default Admin Account if empty
                int userCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
                if (userCount == 0)
                {
                    string seedUsersSql = @"
                        INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, Phone)
                        VALUES 
                        ('USR-ADMIN-1', 'Super Admin', 'admin@indoria.com', 'Admin123!', 'Admin', '+91 99999 00000');
                    ";
                    connection.Execute(seedUsersSql);
                }

                // Seed Categories if empty
                int catCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Categories");
                if (catCount == 0)
                {
                    var categories = _mockDataService.GetCategories();
                    string insertCatSql = @"
                        INSERT INTO Categories (Id, Name, Slug, Description, Image, ProductCount)
                        VALUES (@Id, @Name, @Slug, @Description, @Image, 0);";
                    foreach (var c in categories)
                    {
                        connection.Execute(insertCatSql, c);
                    }
                }

                _logger.LogInformation("Dapper MS SQL Database 'IndoriaDb' wiped demo data and initialized clean catalog state.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL Server LocalDB connection note: {Message}.", ex.Message);
        }
    }
}
