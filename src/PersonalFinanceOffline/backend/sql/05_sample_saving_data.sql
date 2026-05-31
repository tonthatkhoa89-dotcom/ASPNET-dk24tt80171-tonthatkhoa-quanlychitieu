
USE PersonalFinanceDb;
GO

/*
    Dữ liệu mẫu cho loại Tiết kiệm.
    Chạy sau 04_add_saving_type.sql.
*/

DECLARE @UserId INT;
SELECT @UserId = UserId FROM Users WHERE Username = N'admin';

DECLARE @SavingTypeId INT;
SELECT @SavingTypeId = TypeId FROM TransactionTypes WHERE TypeCode = 'Saving';

DECLARE @CatTienMat INT, @CatVang INT, @CatCoPhieu INT, @CatQuyTietKiem INT;
SELECT @CatTienMat = CategoryId FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Tiền mặt';
SELECT @CatVang = CategoryId FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Vàng';
SELECT @CatCoPhieu = CategoryId FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Cổ phiếu';
SELECT @CatQuyTietKiem = CategoryId FROM Categories WHERE TypeId = @SavingTypeId AND CategoryName = N'Quỹ tiết kiệm';

IF @UserId IS NULL OR @SavingTypeId IS NULL
BEGIN
    RAISERROR(N'Thiếu user admin hoặc loại Saving. Hãy chạy 04_add_saving_type.sql trước.', 16, 1);
    RETURN;
END;

DELETE FROM Transactions
WHERE UserId = @UserId
  AND Note LIKE N'[Mẫu tiết kiệm]%';

INSERT INTO Transactions(UserId, TypeId, CategoryId, TransactionDate, Amount, Note) VALUES
(@UserId, @SavingTypeId, @CatTienMat, '2026-03-07', 2000000, N'[Mẫu tiết kiệm] Gửi tiết kiệm tiền mặt tháng 03/2026'),
(@UserId, @SavingTypeId, @CatVang, '2026-03-18', 3500000, N'[Mẫu tiết kiệm] Mua vàng tích lũy tháng 03/2026'),
(@UserId, @SavingTypeId, @CatQuyTietKiem, '2026-04-08', 2500000, N'[Mẫu tiết kiệm] Gửi quỹ tiết kiệm tháng 04/2026'),
(@UserId, @SavingTypeId, @CatCoPhieu, '2026-04-22', 4000000, N'[Mẫu tiết kiệm] Mua cổ phiếu tháng 04/2026'),
(@UserId, @SavingTypeId, @CatTienMat, '2026-05-10', 3000000, N'[Mẫu tiết kiệm] Gửi tiết kiệm tiền mặt tháng 05/2026'),
(@UserId, @SavingTypeId, @CatCoPhieu, '2026-05-24', 2500000, N'[Mẫu tiết kiệm] Mua cổ phiếu tháng 05/2026');
GO

SELECT tt.TypeName, c.CategoryName, SUM(tr.Amount) AS TotalAmount
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId = tt.TypeId
INNER JOIN Categories c ON tr.CategoryId = c.CategoryId
WHERE tr.UserId = @UserId
  AND tt.TypeCode = 'Saving'
GROUP BY tt.TypeName, c.CategoryName
ORDER BY c.CategoryName;
GO
