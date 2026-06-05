IF DB_ID(N'PersonalFinanceDb') IS NULL
BEGIN
    CREATE DATABASE PersonalFinanceDb;
END
GO

USE PersonalFinanceDb;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash VARCHAR(64) NOT NULL,
        FullName NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsAdmin BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH(N'dbo.Users', N'IsAdmin') IS NULL
BEGIN
    ALTER TABLE Users ADD IsAdmin BIT NOT NULL CONSTRAINT DF_Users_IsAdmin DEFAULT 0;
END
GO

IF OBJECT_ID(N'dbo.TransactionTypes', N'U') IS NULL
BEGIN
    CREATE TABLE TransactionTypes (
        TypeId INT IDENTITY(1,1) PRIMARY KEY,
        TypeCode VARCHAR(20) NOT NULL UNIQUE,
        TypeName NVARCHAR(50) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE Categories (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        TypeId INT NOT NULL,
        CategoryName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_Categories_TransactionTypes FOREIGN KEY(TypeId) REFERENCES TransactionTypes(TypeId)
    );
END
GO

IF OBJECT_ID(N'dbo.SavingsGoals', N'U') IS NULL
BEGIN
    CREATE TABLE SavingsGoals (
        GoalId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        GoalName NVARCHAR(150) NOT NULL,
        TargetAmount DECIMAL(18,2) NOT NULL,
        MonthlyBudget DECIMAL(18,2) NOT NULL,
        StartDate DATE NOT NULL,
        TargetDate DATE NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_SavingsGoals_Users FOREIGN KEY(UserId) REFERENCES Users(UserId)
    );
END
GO

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
BEGIN
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
END
GO

IF NOT EXISTS (SELECT 1 FROM TransactionTypes WHERE TypeCode = 'Income')
    INSERT INTO TransactionTypes(TypeCode, TypeName) VALUES ('Income', N'Thu nhập');

IF NOT EXISTS (SELECT 1 FROM TransactionTypes WHERE TypeCode = 'Expense')
    INSERT INTO TransactionTypes(TypeCode, TypeName) VALUES ('Expense', N'Chi tiêu');

IF NOT EXISTS (SELECT 1 FROM TransactionTypes WHERE TypeCode = 'Saving')
    INSERT INTO TransactionTypes(TypeCode, TypeName) VALUES ('Saving', N'Tiết kiệm');
GO

DECLARE @IncomeTypeId INT, @ExpenseTypeId INT, @SavingTypeId INT;

SELECT @IncomeTypeId = TypeId FROM TransactionTypes WHERE TypeCode = 'Income';
SELECT @ExpenseTypeId = TypeId FROM TransactionTypes WHERE TypeCode = 'Expense';
SELECT @SavingTypeId = TypeId FROM TransactionTypes WHERE TypeCode = 'Saving';

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@IncomeTypeId AND CategoryName=N'Lương')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@IncomeTypeId, N'Lương', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@IncomeTypeId AND CategoryName=N'Thưởng')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@IncomeTypeId, N'Thưởng', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@ExpenseTypeId AND CategoryName=N'Ăn uống')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@ExpenseTypeId, N'Ăn uống', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@ExpenseTypeId AND CategoryName=N'Đi lại')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@ExpenseTypeId, N'Đi lại', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@ExpenseTypeId AND CategoryName=N'Mua sắm')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@ExpenseTypeId, N'Mua sắm', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@SavingTypeId AND CategoryName=N'Tiền mặt')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Tiền mặt', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@SavingTypeId AND CategoryName=N'Vàng')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Vàng', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@SavingTypeId AND CategoryName=N'Cổ phiếu')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Cổ phiếu', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId=@SavingTypeId AND CategoryName=N'Quỹ tiết kiệm')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Quỹ tiết kiệm', 1);
GO
