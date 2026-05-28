using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

public static class WebUtil
{
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

    public static Dictionary<string, object> ReadJson(HttpContext context)
    {
        string body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
        if (string.IsNullOrWhiteSpace(body)) return new Dictionary<string, object>();
        return Json.Deserialize<Dictionary<string, object>>(body);
    }

    public static void WriteJson(HttpContext context, object data)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Write(Json.Serialize(data));
    }

    public static void WriteError(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        WriteJson(context, new { ok = false, message = message });
    }

    public static int CurrentUserId(HttpContext context)
    {
        if (context.Session == null || context.Session["UserId"] == null) return 0;
        return Convert.ToInt32(context.Session["UserId"]);
    }

    public static bool RequireLogin(HttpContext context)
    {
        if (CurrentUserId(context) > 0) return true;
        WriteError(context, 401, "Chưa đăng nhập.");
        return false;
    }

    public static string S(object value)
    {
        return value == null ? "" : value.ToString().Trim();
    }

    public static int I(object value)
    {
        int result;
        return int.TryParse(S(value), out result) ? result : 0;
    }

    public static decimal D(object value)
    {
        decimal result;
        return decimal.TryParse(S(value), out result) ? result : 0;
    }

    public static string Sha256(string text)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }

    public static List<Dictionary<string, object>> ToRows(DataTable table)
    {
        var rows = new List<Dictionary<string, object>>();
        foreach (DataRow row in table.Rows)
        {
            var item = new Dictionary<string, object>();
            foreach (DataColumn col in table.Columns)
                item[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            rows.Add(item);
        }
        return rows;
    }
}
