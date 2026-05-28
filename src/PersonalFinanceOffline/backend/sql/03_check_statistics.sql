
USE PersonalFinanceDb;
GO

DECLARE @UserId INT;
SELECT @UserId = UserId FROM Users WHERE Username = N'admin';

SELECT 
    COUNT(*) AS TotalTransactions,
    MIN(TransactionDate) AS FromDate,
    MAX(TransactionDate) AS ToDate
FROM Transactions
WHERE UserId = @UserId;

SELECT
    ISNULL(SUM(CASE WHEN tt.TypeCode='Income' THEN tr.Amount ELSE 0 END),0) AS TotalIncome,
    ISNULL(SUM(CASE WHEN tt.TypeCode='Expense' THEN tr.Amount ELSE 0 END),0) AS TotalExpense,
    ISNULL(SUM(CASE WHEN tt.TypeCode='Saving' THEN tr.Amount ELSE 0 END),0) AS TotalSaving,
    ISNULL(SUM(CASE WHEN tt.TypeCode='Income' THEN tr.Amount ELSE -tr.Amount END),0) AS Balance
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId = tt.TypeId
WHERE tr.UserId = @UserId;

SELECT c.CategoryName, SUM(tr.Amount) AS TotalAmount
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId = tt.TypeId
INNER JOIN Categories c ON tr.CategoryId = c.CategoryId
WHERE tr.UserId = @UserId
  AND tt.TypeCode = 'Expense'
GROUP BY c.CategoryName
ORDER BY TotalAmount DESC;

SELECT CONVERT(varchar(7), tr.TransactionDate, 120) AS MonthKey,
    SUM(CASE WHEN tt.TypeCode='Income' THEN tr.Amount ELSE 0 END) AS IncomeAmount,
    SUM(CASE WHEN tt.TypeCode='Expense' THEN tr.Amount ELSE 0 END) AS ExpenseAmount
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId = tt.TypeId
WHERE tr.UserId = @UserId
GROUP BY CONVERT(varchar(7), tr.TransactionDate, 120)
ORDER BY MonthKey;
GO
