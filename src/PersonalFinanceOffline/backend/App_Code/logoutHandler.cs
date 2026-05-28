using System.Web;
using System.Web.SessionState;

public class LogoutHandler : IHttpHandler, IRequiresSessionState
{
    public void ProcessRequest(HttpContext context)
    {
        context.Session.Clear();
        WebUtil.WriteJson(context, new { ok = true });
    }

    public bool IsReusable { get { return false; } }
}
