
using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class TransactionTypesHandler : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        if (!WebUtil.RequireLogin(context)) return;
        string method = context.Request.HttpMethod;

        if (method == "GET")
        {
            var rows = WebUtil.ToRows(Db.Query("SELECT TypeId, TypeCode, TypeName FROM TransactionTypes ORDER BY TypeId"));
            WebUtil.WriteJson(context, new { ok = true, data = rows });
            return;
        }

        if (method == "POST")
        {
            var data = WebUtil.ReadJson(context);
            string action = data.ContainsKey("action") ? WebUtil.S(data["action"]).ToLower() : "save";

            if (action == "delete")
            {
                Db.Execute("DELETE FROM TransactionTypes WHERE TypeId=@Id",
                    new SqlParameter("@Id", WebUtil.I(data["typeId"])));
                WebUtil.WriteJson(context, new { ok = true });
                return;
            }

            int typeId = WebUtil.I(data["typeId"]);
            if (typeId > 0)
            {
                Db.Execute("UPDATE TransactionTypes SET TypeCode=@Code, TypeName=@Name WHERE TypeId=@Id",
                    new SqlParameter("@Id", typeId),
                    new SqlParameter("@Code", WebUtil.S(data["typeCode"])),
                    new SqlParameter("@Name", WebUtil.S(data["typeName"])));
            }
            else
            {
                Db.Execute("INSERT INTO TransactionTypes(TypeCode, TypeName) VALUES(@Code,@Name)",
                    new SqlParameter("@Code", WebUtil.S(data["typeCode"])),
                    new SqlParameter("@Name", WebUtil.S(data["typeName"])));
            }

            WebUtil.WriteJson(context, new { ok = true });
            return;
        }

        WebUtil.WriteError(context, 405, "Method không hợp lệ.");
    }

    public bool IsReusable { get { return false; } }
}
