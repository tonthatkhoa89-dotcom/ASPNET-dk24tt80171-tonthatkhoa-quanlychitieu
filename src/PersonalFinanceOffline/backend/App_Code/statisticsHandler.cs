
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class StatisticsHandler : IHttpHandler, IRequiresSessionState
{
    private SqlParameter[] BuildParams(HttpContext context, string from, string to, int typeId, int categoryId, string keyword)
    {
        return new SqlParameter[] {
            new SqlParameter("@UserId", WebUtil.CurrentUserId(context)),
            new SqlParameter("@From", from),
            new SqlParameter("@To", to),
            new SqlParameter("@TypeId", typeId),
            new SqlParameter("@CategoryId", categoryId),
            new SqlParameter("@Keyword", keyword)
        };
    }

    private SqlParameter[] BuildChartParams(HttpContext context, DateTime from, DateTime to, int typeId, int categoryId, string keyword)
    {
        return new SqlParameter[] {
            new SqlParameter("@UserId", WebUtil.CurrentUserId(context)),
            new SqlParameter("@FromDate", from.Date),
            new SqlParameter("@ToDate", to.Date),
            new SqlParameter("@TypeId", typeId),
            new SqlParameter("@CategoryId", categoryId),
            new SqlParameter("@Keyword", keyword)
        };
    }

    public void ProcessRequest(HttpContext context)
    {
        if (!WebUtil.RequireLogin(context)) return;

        string from = WebUtil.S(context.Request.QueryString["from"]);
        string to = WebUtil.S(context.Request.QueryString["to"]);
        int typeId = WebUtil.I(context.Request.QueryString["typeId"]);
        int categoryId = WebUtil.I(context.Request.QueryString["categoryId"]);
        string keyword = WebUtil.S(context.Request.QueryString["keyword"]);
        string period = WebUtil.S(context.Request.QueryString["period"]).ToLower();

        if (period != "day" && period != "month" && period != "year")
            period = "month";

        string filter = @" WHERE tr.UserId=@UserId
                           AND (@From='' OR tr.TransactionDate >= CONVERT(date,@From))
                           AND (@To='' OR tr.TransactionDate <= CONVERT(date,@To))
                           AND (@TypeId=0 OR tr.TypeId=@TypeId)
                           AND (@CategoryId=0 OR tr.CategoryId=@CategoryId)
                           AND (@Keyword='' OR tr.Note LIKE '%' + @Keyword + '%') ";

        string summarySql = @"SELECT
            ISNULL(SUM(CASE WHEN tt.TypeCode='Income' THEN tr.Amount ELSE 0 END),0) AS TotalIncome,
            ISNULL(SUM(CASE WHEN tt.TypeCode='Expense' THEN tr.Amount ELSE 0 END),0) AS TotalExpense,
            ISNULL(SUM(CASE WHEN tt.TypeCode='Saving' THEN tr.Amount ELSE 0 END),0) AS TotalSaving
            FROM Transactions tr
            INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId " + filter;

        string incomeByCategorySql = @"SELECT c.CategoryName, SUM(tr.Amount) AS TotalAmount
            FROM Transactions tr
            INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
            INNER JOIN Categories c ON tr.CategoryId=c.CategoryId " + filter + @"
            AND tt.TypeCode='Income'
            GROUP BY c.CategoryName
            ORDER BY TotalAmount DESC";

        string expenseByCategorySql = @"SELECT c.CategoryName, SUM(tr.Amount) AS TotalAmount
            FROM Transactions tr
            INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
            INNER JOIN Categories c ON tr.CategoryId=c.CategoryId " + filter + @"
            AND tt.TypeCode='Expense'
            GROUP BY c.CategoryName
            ORDER BY TotalAmount DESC";

        string savingByCategorySql = @"SELECT c.CategoryName, SUM(tr.Amount) AS TotalAmount
            FROM Transactions tr
            INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
            INNER JOIN Categories c ON tr.CategoryId=c.CategoryId " + filter + @"
            AND tt.TypeCode='Saving'
            GROUP BY c.CategoryName
            ORDER BY TotalAmount DESC";

        var summary = Db.Query(summarySql, BuildParams(context, from, to, typeId, categoryId, keyword));
        decimal income = System.Convert.ToDecimal(summary.Rows[0]["TotalIncome"]);
        decimal expense = System.Convert.ToDecimal(summary.Rows[0]["TotalExpense"]);
        decimal saving = System.Convert.ToDecimal(summary.Rows[0]["TotalSaving"]);

        var incomeByCategory = Db.Query(incomeByCategorySql, BuildParams(context, from, to, typeId, categoryId, keyword));
        var expenseByCategory = Db.Query(expenseByCategorySql, BuildParams(context, from, to, typeId, categoryId, keyword));
        var savingByCategory = Db.Query(savingByCategorySql, BuildParams(context, from, to, typeId, categoryId, keyword));

        DateTime today = DateTime.Today;
        DateTime chartFrom;
        DateTime chartTo = today;

        if (period == "day")
        {
            chartFrom = today.AddDays(-6);
        }
        else if (period == "year")
        {
            chartFrom = new DateTime(today.Year - 2, 1, 1);
            chartTo = new DateTime(today.Year, 12, 31);
        }
        else
        {
            chartFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            chartTo = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);
        }

        string groupExpr;
        if (period == "day")
            groupExpr = "CONVERT(varchar(10), tr.TransactionDate, 120)";
        else if (period == "year")
            groupExpr = "CONVERT(varchar(4), YEAR(tr.TransactionDate))";
        else
            groupExpr = "CONVERT(varchar(7), tr.TransactionDate, 120)";

        string chartSql = @"SELECT " + groupExpr + @" AS PeriodKey,
            SUM(CASE WHEN tt.TypeCode='Income' THEN tr.Amount ELSE 0 END) AS IncomeAmount,
            SUM(CASE WHEN tt.TypeCode='Expense' THEN tr.Amount ELSE 0 END) AS ExpenseAmount,
            SUM(CASE WHEN tt.TypeCode='Saving' THEN tr.Amount ELSE 0 END) AS SavingAmount
            FROM Transactions tr
            INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
            WHERE tr.UserId=@UserId
              AND tr.TransactionDate >= @FromDate
              AND tr.TransactionDate <= @ToDate
              AND (@TypeId=0 OR tr.TypeId=@TypeId)
              AND (@CategoryId=0 OR tr.CategoryId=@CategoryId)
              AND (@Keyword='' OR tr.Note LIKE '%' + @Keyword + '%')
            GROUP BY " + groupExpr + @"
            ORDER BY PeriodKey";

        var chartRaw = Db.Query(chartSql, BuildChartParams(context, chartFrom, chartTo, typeId, categoryId, keyword));
        var map = new Dictionary<string, DataRow>();
        foreach (DataRow row in chartRaw.Rows)
            map[row["PeriodKey"].ToString()] = row;

        var chartRows = new List<Dictionary<string, object>>();

        if (period == "day")
        {
            for (int i = 0; i < 7; i++)
            {
                var d = chartFrom.AddDays(i);
                string key = d.ToString("yyyy-MM-dd");
                decimal inc = 0, exp = 0, sav = 0;
                if (map.ContainsKey(key))
                {
                    inc = System.Convert.ToDecimal(map[key]["IncomeAmount"]);
                    exp = System.Convert.ToDecimal(map[key]["ExpenseAmount"]);
                    sav = System.Convert.ToDecimal(map[key]["SavingAmount"]);
                }
                chartRows.Add(new Dictionary<string, object> {
                    {"PeriodKey", key},
                    {"PeriodLabel", d.ToString("dd/MM")},
                    {"IncomeAmount", inc},
                    {"ExpenseAmount", exp},
                    {"SavingAmount", sav}
                });
            }
        }
        else if (period == "year")
        {
            for (int year = chartFrom.Year; year <= chartTo.Year; year++)
            {
                string key = year.ToString();
                decimal inc = 0, exp = 0, sav = 0;
                if (map.ContainsKey(key))
                {
                    inc = System.Convert.ToDecimal(map[key]["IncomeAmount"]);
                    exp = System.Convert.ToDecimal(map[key]["ExpenseAmount"]);
                    sav = System.Convert.ToDecimal(map[key]["SavingAmount"]);
                }
                chartRows.Add(new Dictionary<string, object> {
                    {"PeriodKey", key},
                    {"PeriodLabel", key},
                    {"IncomeAmount", inc},
                    {"ExpenseAmount", exp},
                    {"SavingAmount", sav}
                });
            }
        }
        else
        {
            DateTime d = chartFrom;
            for (int i = 0; i < 12; i++)
            {
                string key = d.ToString("yyyy-MM");
                decimal inc = 0, exp = 0, sav = 0;
                if (map.ContainsKey(key))
                {
                    inc = System.Convert.ToDecimal(map[key]["IncomeAmount"]);
                    exp = System.Convert.ToDecimal(map[key]["ExpenseAmount"]);
                    sav = System.Convert.ToDecimal(map[key]["SavingAmount"]);
                }
                chartRows.Add(new Dictionary<string, object> {
                    {"PeriodKey", key},
                    {"PeriodLabel", d.ToString("MM/yyyy")},
                    {"IncomeAmount", inc},
                    {"ExpenseAmount", exp},
                    {"SavingAmount", sav}
                });
                d = d.AddMonths(1);
            }
        }

        WebUtil.WriteJson(context, new {
            ok = true,
            period = period,
            chartFrom = chartFrom.ToString("yyyy-MM-dd"),
            chartTo = chartTo.ToString("yyyy-MM-dd"),
            summary = new { totalIncome = income, totalExpense = expense, totalSaving = saving, balance = income - expense - saving },
            incomeByCategory = WebUtil.ToRows(incomeByCategory),
            expenseByCategory = WebUtil.ToRows(expenseByCategory),
            savingByCategory = WebUtil.ToRows(savingByCategory),
            byPeriod = chartRows
        });
    }

    public bool IsReusable { get { return false; } }
}
