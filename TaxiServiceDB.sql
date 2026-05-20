USE TaxiServiceDB;
GO

CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) UNIQUE NOT NULL,
    Email NVARCHAR(100),
    RegistrationDate DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Cars (
    CarID INT PRIMARY KEY IDENTITY(1,1),
    Model NVARCHAR(50) NOT NULL,
    PlateNumber NVARCHAR(20) UNIQUE NOT NULL,
    Color NVARCHAR(30),
    YearManufactured INT
);
GO


CREATE TABLE Drivers (
    DriverID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) UNIQUE NOT NULL,
    CarID INT FOREIGN KEY REFERENCES Cars(CarID),
    Rating DECIMAL(3, 2) DEFAULT 5.00, 
    Status NVARCHAR(20) DEFAULT 'Offline', 
    HireDate DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Tariffs (
    TariffID INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL, 
    BasePrice DECIMAL(10, 2) NOT NULL, 
    PricePerKm DECIMAL(10, 2) NOT NULL, 
    CommissionPercent DECIMAL(5, 2) NOT NULL 
);
GO

CREATE TABLE Trips (
    TripID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    DriverID INT FOREIGN KEY REFERENCES Drivers(DriverID), 
    TariffID INT FOREIGN KEY REFERENCES Tariffs(TariffID),
    StartAddress NVARCHAR(255) NOT NULL,
    EndAddress NVARCHAR(255) NOT NULL,
    DistanceKm DECIMAL(10, 2), 
    TotalCost DECIMAL(10, 2), 
    ServiceCommission DECIMAL(10, 2),
    Status NVARCHAR(20) DEFAULT 'Created', 
    CreatedAt DATETIME DEFAULT GETDATE(),
    CompletedAt DATETIME
);
GO

CREATE TABLE Payments (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    TripID INT FOREIGN KEY REFERENCES Trips(TripID),
    Amount DECIMAL(10, 2) NOT NULL,
    PaymentDate DATETIME DEFAULT GETDATE(),
    Method NVARCHAR(20) 
);
GO

CREATE TABLE Reviews (
    ReviewID INT PRIMARY KEY IDENTITY(1,1),
    TripID INT FOREIGN KEY REFERENCES Trips(TripID),
    Rating INT CHECK (Rating BETWEEN 1 AND 5), 
    Comment NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO


INSERT INTO Tariffs (Name, BasePrice, PricePerKm, CommissionPercent) VALUES
('Эконом', 50.00, 15.00, 20.00),
('Комфорт', 100.00, 25.00, 25.00),
('Бизнес', 200.00, 45.00, 30.00);

INSERT INTO Cars (Model, PlateNumber, Color, YearManufactured) VALUES
('Hyundai Solaris', 'А 111 АА', 'Белый', 2020),
('Toyota Camry', 'В 222 ВВ', 'Черный', 2021),
('Mercedes E-Class', 'М 333 ММ', 'Серебристый', 2022);

INSERT INTO Drivers (FullName, Phone, CarID, Status) VALUES
('Иванов Иван Иванович', '+79001112233', 1, 'Available'),
('Петров Петр Петрович', '+79004445566', 2, 'Available'),
('Сидоров Сидор Сидорович', '+79007778899', 3, 'Busy');

INSERT INTO Users (FullName, Phone, Email) VALUES
('Смирнов Алексей', '+79991234567', 'alex@mail.ru'),
('Кузнецова Мария', '+79997654321', 'maria@mail.ru');


INSERT INTO Trips (UserID, DriverID, TariffID, StartAddress, EndAddress, DistanceKm, TotalCost, ServiceCommission, Status, CompletedAt) VALUES
(1, 1, 1, 'ул. Ленина 1', 'пр. Мира 10', 10.00, 200.00, 40.00, 'Completed', DATEADD(day, -1, GETDATE()));

INSERT INTO Payments (TripID, Amount, Method) VALUES
(1, 200.00, 'Card');

INSERT INTO Reviews (TripID, Rating, Comment) VALUES
(1, 5, 'Отличная поездка, машина чистая.');

CREATE TABLE PromoCodes (
    PromoCodeID INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(50) UNIQUE NOT NULL,
    DiscountValue DECIMAL(10, 2) NOT NULL,
    DiscountType NVARCHAR(20) NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE PromoCodeUsages (
    UsageID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    PromoCodeID INT FOREIGN KEY REFERENCES PromoCodes(PromoCodeID),
    TripID INT FOREIGN KEY REFERENCES Trips(TripID),
    AppliedAt DATETIME DEFAULT GETDATE(),
    DiscountAmount DECIMAL(10, 2) NOT NULL
);
GO

ALTER TABLE Trips ADD PromoCodeID INT NULL FOREIGN KEY REFERENCES PromoCodes(PromoCodeID);
ALTER TABLE Trips ADD OriginalCost DECIMAL(10, 2) NULL;
ALTER TABLE Trips ADD DiscountAmount DECIMAL(10, 2) NULL;
GO

INSERT INTO PromoCodes (Code, DiscountValue, DiscountType, StartDate, EndDate, IsActive) VALUES
('WELCOME10', 10.00, 'Percent', DATEADD(day, -30, GETDATE()), DATEADD(day, 30, GETDATE()), 1),
('SUMMER20', 20.00, 'Percent', DATEADD(day, -10, GETDATE()), DATEADD(day, 20, GETDATE()), 1),
('FIXED50', 50.00, 'Fixed', DATEADD(day, -15, GETDATE()), DATEADD(day, 15, GETDATE()), 1),
('EXPIRED', 15.00, 'Percent', DATEADD(day, -60, GETDATE()), DATEADD(day, -30, GETDATE()), 1),
('INACTIVE', 25.00, 'Percent', DATEADD(day, -10, GETDATE()), DATEADD(day, 20, GETDATE()), 0);
GO

CREATE TABLE DriverRatings (
    DriverRatingID INT PRIMARY KEY IDENTITY(1,1),
    DriverID INT FOREIGN KEY REFERENCES Drivers(DriverID) UNIQUE,
    AverageRating DECIMAL(3, 2) DEFAULT 0.00,
    ReviewCount INT DEFAULT 0,
    LastUpdatedAt DATETIME DEFAULT GETDATE()
);
GO

INSERT INTO DriverRatings (DriverID, AverageRating, ReviewCount, LastUpdatedAt)
SELECT DriverID, Rating, 0, GETDATE()
FROM Drivers
WHERE DriverID NOT IN (SELECT DriverID FROM DriverRatings);
GO

CREATE PROCEDURE UpdateDriverRating
    @DriverID INT
AS
BEGIN
    DECLARE @AvgRating DECIMAL(3, 2);
    DECLARE @ReviewCount INT;
    
    SELECT @AvgRating = AVG(CAST(Rating AS DECIMAL(3, 2))),
           @ReviewCount = COUNT(*)
    FROM Reviews r
    JOIN Trips t ON r.TripID = t.TripID
    WHERE t.DriverID = @DriverID AND t.Status = 'Completed';
    
    UPDATE DriverRatings
    SET AverageRating = ISNULL(@AvgRating, 0.00),
        ReviewCount = ISNULL(@ReviewCount, 0),
        LastUpdatedAt = GETDATE()
    WHERE DriverID = @DriverID;
END
GO