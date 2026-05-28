using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class LoginHandler : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        if (context.Request.HttpMethod != "POST")
        {
            WebUtil.WriteError(context, 405, "Chỉ hỗ trợ POST.");
            return;
        }

        var data = WebUtil.ReadJson(context);
        string username = WebUtil.S(data.ContainsKey("username") ? data["username"] : "");
        string password = WebUtil.S(data.ContainsKey("password") ? data["password"] : "");
        string hash = WebUtil.Sha256(password);

        var table = Db.Query(
            @"SELECT TOP 1 UserId, Username, FullName
              FROM Users
              WHERE Username=@Username AND PasswordHash=@PasswordHash AND IsActive=1",
            new SqlParameter("@Username", username),
            new SqlParameter("@PasswordHash", hash));

        if (table.Rows.Count == 0)
        {
            WebUtil.WriteError(context, 400, "Sai tài khoản hoặc mật khẩu.");
            return;
        }

        context.Session["UserId"] = Convert.ToInt32(table.Rows[0]["UserId"]);
        context.Session["Username"] = table.Rows[0]["Username"].ToString();

        WebUtil.WriteJson(context, new {
            ok = true,
            user = new {
                userId = table.Rows[0]["UserId"],
                username = table.Rows[0]["Username"],
                fullName = table.Rows[0]["FullName"]
            }
        });
    }

    public bool IsReusable { get { return false; } }
}
