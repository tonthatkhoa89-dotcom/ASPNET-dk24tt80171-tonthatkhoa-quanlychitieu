
USE PersonalFinanceDb;
GO

/*
    Bổ sung loại giao dịch Tiết kiệm cho database đã tạo trước đó.
    Chạy file này nếu bạn đã cài bản cũ và không muốn tạo lại database.
*/

IF NOT EXISTS (SELECT 1 FROM TransactionTypes WHERE TypeCode = 'Saving')
BEGIN
    INSERT INTO TransactionTypes(TypeCode, TypeName)
    VALUES ('Saving', N'Tiết kiệm');
END
GO

DECLARE @SavingTypeId INT;
SELECT @SavingTypeId = TypeId FROM TransactionTypes WHERE TypeCode = 'Saving';

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Tiền mặt')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Tiền mặt', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Vàng')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Vàng', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Cổ phiếu')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Cổ phiếu', 1);

IF NOT EXISTS (SELECT 1 FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Quỹ tiết kiệm')
    INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES (@SavingTypeId, N'Quỹ tiết kiệm', 1);
GO

SELECT t.TypeCode, t.TypeName, c.CategoryName
FROM TransactionTypes t
LEFT JOIN Categories c ON c.TypeId = t.TypeId
WHERE t.TypeCode = 'Saving'
ORDER BY c.CategoryName;
GO
