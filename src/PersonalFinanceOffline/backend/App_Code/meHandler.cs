using System;
using System.Web;
using System.Web.SessionState;

public class MeHandler : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        int userId = WebUtil.CurrentUserId(context);
        if (userId == 0)
        {
            WebUtil.WriteJson(context, new { ok = false });
            return;
        }

        bool isAdmin = WebUtil.CurrentUserIsAdmin(context);

        WebUtil.WriteJson(context, new {
            ok = true,
            user = new {
                userId = userId,
                username = context.Session["Username"],
                isAdmin = isAdmin
            }
        });
    }

    public bool IsReusable { get { return false; } }
}
