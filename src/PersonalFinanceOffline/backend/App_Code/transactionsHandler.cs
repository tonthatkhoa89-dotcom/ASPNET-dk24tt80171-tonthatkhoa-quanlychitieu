using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class TransactionsHandler : IHttpHandler, IRequiresSessionState
{
    private string GetTypeCode(int typeId)
    {
        object value = Db.Scalar(
            "SELECT TypeCode FROM TransactionTypes WHERE TypeId=@TypeId",
            new SqlParameter("@TypeId", typeId));

        return value == null || value == DBNull.Value ? "" : value.ToString();
    }

    private decimal GetAvailableBalanceUntilMonth(int userId, int excludeTransactionId, DateTime transactionDate)
    {
        DateTime monthStart = new DateTime(transactionDate.Year, transactionDate.Month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

        object value = Db.Scalar(@"
SELECT ISNULL(SUM(
    CASE
        WHEN tt.TypeCode = 'Income' THEN tr.Amount
        WHEN tt.TypeCode IN ('Expense', 'Saving') THEN -tr.Amount
        ELSE 0
    END
), 0)
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId = tt.TypeId
WHERE tr.UserId = @UserId
  AND tr.TransactionDate <= @MonthEnd
  AND (@ExcludeTransactionId = 0 OR tr.TransactionId <> @ExcludeTransactionId)",
            new SqlParameter("@UserId", userId),
            new SqlParameter("@MonthEnd", monthEnd),
            new SqlParameter("@ExcludeTransactionId", excludeTransactionId));

        return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
    }

    private bool ValidateTransactionAmount(HttpContext context, int userId, int transactionId, int typeId, decimal amount, string transactionDateText)
    {
        if (typeId <= 0)
        {
            WebUtil.WriteError(context, 400, "Loại giao dịch không hợp lệ.");
            return false;
        }

        if (amount <= 0)
        {
            WebUtil.WriteError(context, 400, "Số tiền phải lớn hơn 0.");
            return false;
        }

        DateTime transactionDate;
        if (!DateTime.TryParse(transactionDateText, out transactionDate))
        {
            WebUtil.WriteError(context, 400, "Ngày giao dịch không hợp lệ.");
            return false;
        }

        string typeCode = GetTypeCode(typeId);
        if (typeCode == "Expense" || typeCode == "Saving")
        {
            decimal availableBalance = GetAvailableBalanceUntilMonth(userId, transactionId, transactionDate);

            if (amount > availableBalance)
            {
                string typeName = typeCode == "Expense" ? "khoản chi" : "khoản tiết kiệm";

                WebUtil.WriteError(
                    context,
                    400,
                    "Không thể lưu " + typeName +
                    " vì số tiền " + amount.ToString("N0") +
                    " vượt số dư lũy kế hiện có đến tháng " + transactionDate.ToString("MM/yyyy") +
                    " là " + availableBalance.ToString("N0") + "."
                );
                return false;
            }
        }

        return true;
    }

    public void ProcessRequest(HttpContext context)
    {
        if (!WebUtil.RequireLogin(context)) return;
        string method = context.Request.HttpMethod;

        if (method == "GET")
        {
            string fromDateText = WebUtil.S(context.Request.QueryString["from"]);
            string toDateText = WebUtil.S(context.Request.QueryString["to"]);

            if (fromDateText != "" && toDateText != "")
            {
                DateTime fromDateValue;
                DateTime toDateValue;

                if (!DateTime.TryParse(fromDateText, out fromDateValue) ||
                    !DateTime.TryParse(toDateText, out toDateValue))
                {
                    WebUtil.WriteError(context, 400, "Ngày lọc giao dịch không hợp lệ.");
                    return;
                }

                if (toDateValue < fromDateValue)
                {
                    WebUtil.WriteError(context, 400, "Ngày đến không được trước ngày bắt đầu.");
                    return;
                }
            }

            int page = WebUtil.I(context.Request.QueryString["page"]);
            int pageSize = WebUtil.I(context.Request.QueryString["pageSize"]);

            if (page <= 0) page = 1;
            if (pageSize != 10 && pageSize != 20 && pageSize != 50) pageSize = 10;

            string whereSql = @" WHERE tr.UserId=@UserId
                           AND (@From='' OR tr.TransactionDate >= CONVERT(date,@From))
                           AND (@To='' OR tr.TransactionDate <= CONVERT(date,@To))
                           AND (@TypeId=0 OR tr.TypeId=@TypeId)
                           AND (@CategoryId=0 OR tr.CategoryId=@CategoryId)
                           AND (@Keyword='' OR tr.Note LIKE '%' + @Keyword + '%') ";

            SqlParameter[] filterParams = new SqlParameter[] {
                new SqlParameter("@UserId", WebUtil.CurrentUserId(context)),
                new SqlParameter("@From", WebUtil.S(context.Request.QueryString["from"])),
                new SqlParameter("@To", WebUtil.S(context.Request.QueryString["to"])),
                new SqlParameter("@TypeId", WebUtil.I(context.Request.QueryString["typeId"])),
                new SqlParameter("@CategoryId", WebUtil.I(context.Request.QueryString["categoryId"])),
                new SqlParameter("@Keyword", WebUtil.S(context.Request.QueryString["keyword"]))
            };

            object countValue = Db.Scalar(@"SELECT COUNT(1)
                           FROM Transactions tr
                           INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
                           INNER JOIN Categories c ON tr.CategoryId=c.CategoryId " + whereSql,
                           filterParams);

            int totalRows = Convert.ToInt32(countValue);
            int totalPages = totalRows == 0 ? 1 : (int)Math.Ceiling(totalRows / (decimal)pageSize);

            if (page > totalPages) page = totalPages;
            int offset = (page - 1) * pageSize;

            string sql = @"SELECT tr.TransactionId, tr.TransactionDate, tr.TypeId, tt.TypeCode, tt.TypeName,
                                  tr.CategoryId, c.CategoryName, tr.Amount, tr.Note
                           FROM Transactions tr
                           INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
                           INNER JOIN Categories c ON tr.CategoryId=c.CategoryId " + whereSql + @"
                           ORDER BY tr.TransactionDate DESC, tr.TransactionId DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] dataParams = new SqlParameter[] {
                new SqlParameter("@UserId", WebUtil.CurrentUserId(context)),
                new SqlParameter("@From", WebUtil.S(context.Request.QueryString["from"])),
                new SqlParameter("@To", WebUtil.S(context.Request.QueryString["to"])),
                new SqlParameter("@TypeId", WebUtil.I(context.Request.QueryString["typeId"])),
                new SqlParameter("@CategoryId", WebUtil.I(context.Request.QueryString["categoryId"])),
                new SqlParameter("@Keyword", WebUtil.S(context.Request.QueryString["keyword"])),
                new SqlParameter("@Offset", offset),
                new SqlParameter("@PageSize", pageSize)
            };

            var rows = WebUtil.ToRows(Db.Query(sql, dataParams));

            WebUtil.WriteJson(context, new {
                ok = true,
                data = rows,
                page = page,
                pageSize = pageSize,
                totalRows = totalRows,
                totalPages = totalPages
            });
            return;
        }

        if (method == "POST")
        {
            var data = WebUtil.ReadJson(context);
            string action = data.ContainsKey("action") ? WebUtil.S(data["action"]).ToLower() : "save";
            int userId = WebUtil.CurrentUserId(context);

            if (action == "delete")
            {
                Db.Execute("DELETE FROM Transactions WHERE TransactionId=@Id AND UserId=@UserId",
                    new SqlParameter("@Id", WebUtil.I(data["transactionId"])),
                    new SqlParameter("@UserId", userId));
                WebUtil.WriteJson(context, new { ok = true });
                return;
            }

            int transactionId = WebUtil.I(data["transactionId"]);
            int typeId = WebUtil.I(data["typeId"]);
            int categoryId = WebUtil.I(data["categoryId"]);
            decimal amount = WebUtil.D(data["amount"]);
            string transactionDate = WebUtil.S(data["transactionDate"]);
            string note = WebUtil.S(data["note"]);

            if (!ValidateTransactionAmount(context, userId, transactionId, typeId, amount, transactionDate))
                return;

            if (transactionId > 0)
            {
                Db.Execute(@"UPDATE Transactions
                             SET TypeId=@TypeId, CategoryId=@CategoryId, TransactionDate=@Date, Amount=@Amount, Note=@Note
                             WHERE TransactionId=@Id AND UserId=@UserId",
                    new SqlParameter("@Id", transactionId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@TypeId", typeId),
                    new SqlParameter("@CategoryId", categoryId),
                    new SqlParameter("@Date", transactionDate),
                    new SqlParameter("@Amount", amount),
                    new SqlParameter("@Note", note));
            }
            else
            {
                Db.Execute(@"INSERT INTO Transactions(UserId, TypeId, CategoryId, TransactionDate, Amount, Note)
                             VALUES(@UserId,@TypeId,@CategoryId,@Date,@Amount,@Note)",
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@TypeId", typeId),
                    new SqlParameter("@CategoryId", categoryId),
                    new SqlParameter("@Date", transactionDate),
                    new SqlParameter("@Amount", amount),
                    new SqlParameter("@Note", note));
            }

            WebUtil.WriteJson(context, new { ok = true });
            return;
        }

        WebUtil.WriteError(context, 405, "Method không hợp lệ.");
    }

    public bool IsReusable { get { return false; } }
}
