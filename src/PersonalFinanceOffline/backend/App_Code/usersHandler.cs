using System;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class UsersHandler : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        if (!WebUtil.RequireAdmin(context)) return;

        string method = context.Request.HttpMethod;

        if (method == "GET")
        {
            string sql = @"SELECT UserId, Username, FullName, IsActive, IsAdmin,
                                  CONVERT(varchar(19), CreatedAt, 120) AS CreatedAt
                           FROM Users
                           ORDER BY UserId";
            WebUtil.WriteJson(context, new { ok = true, data = WebUtil.ToRows(Db.Query(sql)) });
            return;
        }

        if (method == "POST")
        {
            var data = WebUtil.ReadJson(context);
            string action = data.ContainsKey("action") ? WebUtil.S(data["action"]).ToLower() : "save";

            if (action == "delete")
            {
                int id = WebUtil.I(data["userId"]);
                if (id == WebUtil.CurrentUserId(context))
                {
                    WebUtil.WriteError(context, 400, "Không xóa user đang đăng nhập.");
                    return;
                }

                Db.Execute("DELETE FROM Users WHERE UserId=@Id", new SqlParameter("@Id", id));
                WebUtil.WriteJson(context, new { ok = true });
                return;
            }

            int userId = WebUtil.I(data["userId"]);
            string password = data.ContainsKey("password") ? WebUtil.S(data["password"]) : "";
            int isAdmin = WebUtil.I(data.ContainsKey("isAdmin") ? data["isAdmin"] : 0) == 1 ? 1 : 0;

            if (userId == WebUtil.CurrentUserId(context) && isAdmin == 0)
            {
                WebUtil.WriteError(context, 400, "Không thể tự bỏ quyền admin của user đang đăng nhập.");
                return;
            }

            if (userId > 0)
            {
                if (password != "")
                {
                    Db.Execute(@"UPDATE Users
                                 SET Username=@Username,
                                     PasswordHash=@Hash,
                                     FullName=@FullName,
                                     IsActive=@Active,
                                     IsAdmin=@IsAdmin
                                 WHERE UserId=@Id",
                        new SqlParameter("@Id", userId),
                        new SqlParameter("@Username", WebUtil.S(data["username"])),
                        new SqlParameter("@Hash", WebUtil.Sha256(password)),
                        new SqlParameter("@FullName", WebUtil.S(data["fullName"])),
                        new SqlParameter("@Active", WebUtil.I(data["isActive"]) == 1),
                        new SqlParameter("@IsAdmin", isAdmin));
                }
                else
                {
                    Db.Execute(@"UPDATE Users
                                 SET Username=@Username,
                                     FullName=@FullName,
                                     IsActive=@Active,
                                     IsAdmin=@IsAdmin
                                 WHERE UserId=@Id",
                        new SqlParameter("@Id", userId),
                        new SqlParameter("@Username", WebUtil.S(data["username"])),
                        new SqlParameter("@FullName", WebUtil.S(data["fullName"])),
                        new SqlParameter("@Active", WebUtil.I(data["isActive"]) == 1),
                        new SqlParameter("@IsAdmin", isAdmin));
                }
            }
            else
            {
                if (password == "")
                {
                    WebUtil.WriteError(context, 400, "Mật khẩu không được rỗng.");
                    return;
                }

                Db.Execute(@"INSERT INTO Users(Username, PasswordHash, FullName, IsActive, IsAdmin)
                             VALUES(@Username,@Hash,@FullName,@Active,@IsAdmin)",
                    new SqlParameter("@Username", WebUtil.S(data["username"])),
                    new SqlParameter("@Hash", WebUtil.Sha256(password)),
                    new SqlParameter("@FullName", WebUtil.S(data["fullName"])),
                    new SqlParameter("@Active", WebUtil.I(data["isActive"]) == 1),
                    new SqlParameter("@IsAdmin", isAdmin));
            }

            WebUtil.WriteJson(context, new { ok = true });
            return;
        }

        WebUtil.WriteError(context, 405, "Method không hợp lệ.");
    }

    public bool IsReusable { get { return false; } }
}
