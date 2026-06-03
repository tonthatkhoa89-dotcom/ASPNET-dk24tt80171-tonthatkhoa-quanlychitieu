
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class TransactionsHandler : IHttpHandler, IRequiresSessionState
{
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

            if (action == "delete")
            {
                Db.Execute("DELETE FROM Transactions WHERE TransactionId=@Id AND UserId=@UserId",
                    new SqlParameter("@Id", WebUtil.I(data["transactionId"])),
                    new SqlParameter("@UserId", WebUtil.CurrentUserId(context)));
                WebUtil.WriteJson(context, new { ok = true });
                return;
            }

            int transactionId = WebUtil.I(data["transactionId"]);
            if (transactionId > 0)
            {
                Db.Execute(@"UPDATE Transactions
                             SET TypeId=@TypeId, CategoryId=@CategoryId, TransactionDate=@Date, Amount=@Amount, Note=@Note
                             WHERE TransactionId=@Id AND UserId=@UserId",
                    new SqlParameter("@Id", transactionId),
                    new SqlParameter("@UserId", WebUtil.CurrentUserId(context)),
                    new SqlParameter("@TypeId", WebUtil.I(data["typeId"])),
                    new SqlParameter("@CategoryId", WebUtil.I(data["categoryId"])),
                    new SqlParameter("@Date", WebUtil.S(data["transactionDate"])),
                    new SqlParameter("@Amount", WebUtil.D(data["amount"])),
                    new SqlParameter("@Note", WebUtil.S(data["note"])));
            }
            else
            {
                Db.Execute(@"INSERT INTO Transactions(UserId, TypeId, CategoryId, TransactionDate, Amount, Note)
                             VALUES(@UserId,@TypeId,@CategoryId,@Date,@Amount,@Note)",
                    new SqlParameter("@UserId", WebUtil.CurrentUserId(context)),
                    new SqlParameter("@TypeId", WebUtil.I(data["typeId"])),
                    new SqlParameter("@CategoryId", WebUtil.I(data["categoryId"])),
                    new SqlParameter("@Date", WebUtil.S(data["transactionDate"])),
                    new SqlParameter("@Amount", WebUtil.D(data["amount"])),
                    new SqlParameter("@Note", WebUtil.S(data["note"])));
            }

            WebUtil.WriteJson(context, new { ok = true });
            return;
        }

        WebUtil.WriteError(context, 405, "Method không hợp lệ.");
    }

    public bool IsReusable { get { return false; } }
}
