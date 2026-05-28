using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

public static class Db
{
    public static SqlConnection Open()
    {
        var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["FinanceDb"].ConnectionString);
        cn.Open();
        return cn;
    }

    public static DataTable Query(string sql, params SqlParameter[] parameters)
    {
        using (var cn = Open())
        using (var cmd = new SqlCommand(sql, cn))
        using (var da = new SqlDataAdapter(cmd))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            var table = new DataTable();
            da.Fill(table);
            return table;
        }
    }

    public static object Scalar(string sql, params SqlParameter[] parameters)
    {
        using (var cn = Open())
        using (var cmd = new SqlCommand(sql, cn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }
    }

    public static int Execute(string sql, params SqlParameter[] parameters)
    {
        using (var cn = Open())
        using (var cmd = new SqlCommand(sql, cn))
        {
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }
    }
}
