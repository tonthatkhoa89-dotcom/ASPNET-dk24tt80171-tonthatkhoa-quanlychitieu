CREATE DATABASE PersonalFinanceDb;
GO
USE PersonalFinanceDb;
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(64) NOT NULL,
    FullName NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE TransactionTypes (
    TypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeCode VARCHAR(20) NOT NULL UNIQUE,
    TypeName NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    TypeId INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Categories_TransactionTypes FOREIGN KEY(TypeId) REFERENCES TransactionTypes(TypeId)
);
GO

CREATE TABLE Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    TypeId INT NOT NULL,
    CategoryId INT NOT NULL,
    TransactionDate DATE NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Note NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Transactions_Users FOREIGN KEY(UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Transactions_Types FOREIGN KEY(TypeId) REFERENCES TransactionTypes(TypeId),
    CONSTRAINT FK_Transactions_Categories FOREIGN KEY(CategoryId) REFERENCES Categories(CategoryId)
);
GO

INSERT INTO Users(Username, PasswordHash, FullName, IsActive)
VALUES (N'admin', '240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'Administrator', 1);

INSERT INTO TransactionTypes(TypeCode, TypeName)
VALUES ('Income', N'Thu nhập'), ('Expense', N'Chi tiêu');

INSERT INTO Categories(TypeId, CategoryName, IsActive)
SELECT TypeId, N'Lương', 1 FROM TransactionTypes WHERE TypeCode = 'Income';
INSERT INTO Categories(TypeId, CategoryName, IsActive)
SELECT TypeId, N'Thưởng', 1 FROM TransactionTypes WHERE TypeCode = 'Income';
INSERT INTO Categories(TypeId, CategoryName, IsActive)
SELECT TypeId, N'Ăn uống', 1 FROM TransactionTypes WHERE TypeCode = 'Expense';
INSERT INTO Categories(TypeId, CategoryName, IsActive)
SELECT TypeId, N'Đi lại', 1 FROM TransactionTypes WHERE TypeCode = 'Expense';
INSERT INTO Categories(TypeId, CategoryName, IsActive)
SELECT TypeId, N'Mua sắm', 1 FROM TransactionTypes WHERE TypeCode = 'Expense';

GO
