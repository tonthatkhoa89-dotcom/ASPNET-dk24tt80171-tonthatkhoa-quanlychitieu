
using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class CategoriesHandler : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        if (!WebUtil.RequireLogin(context)) return;
        string method = context.Request.HttpMethod;

        if (method == "GET")
        {
            string sql = @"SELECT c.CategoryId, c.TypeId, t.TypeCode, t.TypeName, c.CategoryName, c.IsActive
                           FROM Categories c
                           INNER JOIN TransactionTypes t ON c.TypeId = t.TypeId
                           ORDER BY t.TypeId, c.CategoryName";
            WebUtil.WriteJson(context, new { ok = true, data = WebUtil.ToRows(Db.Query(sql)) });
            return;
        }

        if (method == "POST")
        {
            if (!WebUtil.RequireAdmin(context)) return;
            var data = WebUtil.ReadJson(context);
            string action = data.ContainsKey("action") ? WebUtil.S(data["action"]).ToLower() : "save";

            if (action == "delete")
            {
                Db.Execute("DELETE FROM Categories WHERE CategoryId=@Id",
                    new SqlParameter("@Id", WebUtil.I(data["categoryId"])));
                WebUtil.WriteJson(context, new { ok = true });
                return;
            }

            int categoryId = WebUtil.I(data["categoryId"]);
            if (categoryId > 0)
            {
                Db.Execute("UPDATE Categories SET TypeId=@TypeId, CategoryName=@Name, IsActive=@Active WHERE CategoryId=@Id",
                    new SqlParameter("@Id", categoryId),
                    new SqlParameter("@TypeId", WebUtil.I(data["typeId"])),
                    new SqlParameter("@Name", WebUtil.S(data["categoryName"])),
                    new SqlParameter("@Active", WebUtil.I(data["isActive"]) == 1));
            }
            else
            {
                Db.Execute("INSERT INTO Categories(TypeId, CategoryName, IsActive) VALUES(@TypeId,@Name,@Active)",
                    new SqlParameter("@TypeId", WebUtil.I(data["typeId"])),
                    new SqlParameter("@Name", WebUtil.S(data["categoryName"])),
                    new SqlParameter("@Active", WebUtil.I(data["isActive"]) == 1));
            }

            WebUtil.WriteJson(context, new { ok = true });
            return;
        }

        WebUtil.WriteError(context, 405, "Method không hợp lệ.");
    }

    public bool IsReusable { get { return false; } }
}
