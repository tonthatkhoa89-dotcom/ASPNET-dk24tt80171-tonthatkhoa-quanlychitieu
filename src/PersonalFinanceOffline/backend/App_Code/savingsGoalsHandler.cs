using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.SessionState;

public class SavingsGoalsHandler : IHttpHandler, IRequiresSessionState
{
    private decimal ToDecimal(object value)
    {
        if (value == null || value == DBNull.Value) return 0;
        return Convert.ToDecimal(value);
    }

    private string MonthStart()
    {
        DateTime today = DateTime.Today;
        return new DateTime(today.Year, today.Month, 1).ToString("yyyy-MM-dd");
    }

    private string MonthEnd()
    {
        DateTime today = DateTime.Today;
        return new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
    }

    public void ProcessRequest(HttpContext context)
    {
        if (!WebUtil.RequireLogin(context)) return;

        string method = context.Request.HttpMethod;
        int userId = WebUtil.CurrentUserId(context);

        if (method == "GET")
        {
            string goalSql = @"
SELECT 
    g.GoalId,
    g.GoalName,
    g.TargetAmount,
    g.MonthlyBudget,
    CONVERT(varchar(10), g.StartDate, 120) AS StartDate,
    CONVERT(varchar(10), g.TargetDate, 120) AS TargetDate,
    g.IsActive,
    ISNULL(SUM(CASE WHEN tt.TypeCode='Saving' THEN tr.Amount ELSE 0 END),0) AS SavedAmount
FROM SavingsGoals g
LEFT JOIN Transactions tr
    ON tr.UserId = g.UserId
   AND tr.TransactionDate >= g.StartDate
   AND tr.TransactionDate <= g.TargetDate
LEFT JOIN TransactionTypes tt ON tr.TypeId = tt.TypeId
WHERE g.UserId=@UserId
GROUP BY g.GoalId, g.GoalName, g.TargetAmount, g.MonthlyBudget, g.StartDate, g.TargetDate, g.IsActive
ORDER BY g.IsActive DESC, g.TargetDate ASC, g.GoalId DESC";

            var goalsTable = Db.Query(goalSql, new SqlParameter("@UserId", userId));
            var goals = new List<Dictionary<string, object>>();

            foreach (DataRow row in goalsTable.Rows)
            {
                decimal target = ToDecimal(row["TargetAmount"]);
                decimal saved = ToDecimal(row["SavedAmount"]);
                decimal remaining = Math.Max(0, target - saved);
                decimal progress = target <= 0 ? 0 : Math.Min(100, saved / target * 100);

                var item = new Dictionary<string, object>();
                item["GoalId"] = row["GoalId"];
                item["GoalName"] = row["GoalName"];
                item["TargetAmount"] = target;
                item["MonthlyBudget"] = ToDecimal(row["MonthlyBudget"]);
                item["StartDate"] = row["StartDate"];
                item["TargetDate"] = row["TargetDate"];
                item["IsActive"] = row["IsActive"];
                item["SavedAmount"] = saved;
                item["RemainingAmount"] = remaining;
                item["ProgressPercent"] = Math.Round(progress, 1);
                goals.Add(item);
            }

            string monthStart = MonthStart();
            string monthEnd = MonthEnd();

            string summarySql = @"
SELECT
    ISNULL(SUM(CASE WHEN tt.TypeCode='Income' THEN tr.Amount ELSE 0 END),0) AS MonthIncome,
    ISNULL(SUM(CASE WHEN tt.TypeCode='Expense' THEN tr.Amount ELSE 0 END),0) AS MonthExpense,
    ISNULL(SUM(CASE WHEN tt.TypeCode='Saving' THEN tr.Amount ELSE 0 END),0) AS MonthSaving
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
WHERE tr.UserId=@UserId
  AND tr.TransactionDate >= @MonthStart
  AND tr.TransactionDate <= @MonthEnd";

            var summaryTable = Db.Query(summarySql,
                new SqlParameter("@UserId", userId),
                new SqlParameter("@MonthStart", monthStart),
                new SqlParameter("@MonthEnd", monthEnd));

            decimal monthIncome = 0, monthExpense = 0, monthSaving = 0;
            if (summaryTable.Rows.Count > 0)
            {
                monthIncome = ToDecimal(summaryTable.Rows[0]["MonthIncome"]);
                monthExpense = ToDecimal(summaryTable.Rows[0]["MonthExpense"]);
                monthSaving = ToDecimal(summaryTable.Rows[0]["MonthSaving"]);
            }

            decimal monthBalance = monthIncome - monthExpense - monthSaving;

            string topExpenseSql = @"
SELECT TOP 1 c.CategoryName, SUM(tr.Amount) AS TotalAmount
FROM Transactions tr
INNER JOIN TransactionTypes tt ON tr.TypeId=tt.TypeId
INNER JOIN Categories c ON tr.CategoryId=c.CategoryId
WHERE tr.UserId=@UserId
  AND tt.TypeCode='Expense'
  AND tr.TransactionDate >= @MonthStart
  AND tr.TransactionDate <= @MonthEnd
GROUP BY c.CategoryName
ORDER BY TotalAmount DESC";

            var topTable = Db.Query(topExpenseSql,
                new SqlParameter("@UserId", userId),
                new SqlParameter("@MonthStart", monthStart),
                new SqlParameter("@MonthEnd", monthEnd));

            string topCategory = "";
            decimal topAmount = 0;
            if (topTable.Rows.Count > 0)
            {
                topCategory = WebUtil.S(topTable.Rows[0]["CategoryName"]);
                topAmount = ToDecimal(topTable.Rows[0]["TotalAmount"]);
            }

            var alerts = new List<Dictionary<string, object>>();
            var recs = new List<string>();

            foreach (var goal in goals)
            {
                bool isActive = Convert.ToBoolean(goal["IsActive"]);
                if (!isActive) continue;

                decimal budget = Convert.ToDecimal(goal["MonthlyBudget"]);
                string goalName = WebUtil.S(goal["GoalName"]);

                if (budget > 0 && monthExpense > budget)
                {
                    alerts.Add(new Dictionary<string, object> {
                        {"level", "danger"},
                        {"message", "Cảnh báo: Chi tiêu tháng này đã vượt ngân sách của mục tiêu '" + goalName + "' (" + monthExpense.ToString("N0") + " / " + budget.ToString("N0") + ")."}
                    });
                }
                else if (budget > 0 && monthExpense >= budget * 0.8M)
                {
                    alerts.Add(new Dictionary<string, object> {
                        {"level", "warning"},
                        {"message", "Lưu ý: Chi tiêu tháng này đã đạt khoảng 80% ngân sách của mục tiêu '" + goalName + "'."}
                    });
                }

                decimal remaining = Convert.ToDecimal(goal["RemainingAmount"]);
                if (remaining > 0)
                {
                    DateTime targetDate;
                    if (DateTime.TryParse(WebUtil.S(goal["TargetDate"]), out targetDate))
                    {
                        int monthsLeft = Math.Max(1, ((targetDate.Year - DateTime.Today.Year) * 12) + targetDate.Month - DateTime.Today.Month + 1);
                        decimal needPerMonth = remaining / monthsLeft;

                        if (monthSaving < needPerMonth)
                        {
                            recs.Add("Mục tiêu '" + goalName + "' cần tiết kiệm khoảng " + needPerMonth.ToString("N0") + " mỗi tháng. Tháng này bạn mới tiết kiệm " + monthSaving.ToString("N0") + ".");
                        }
                    }
                }
            }

            if (monthBalance < 0)
                recs.Add("Số dư tháng này đang âm. Nên giảm các khoản chi chưa cần thiết trước khi tăng tiết kiệm.");

            if (topAmount > 0 && monthExpense > 0)
            {
                decimal pct = topAmount / monthExpense * 100;
                if (pct >= 35)
                    recs.Add("Danh mục chi nhiều nhất là '" + topCategory + "' chiếm khoảng " + Math.Round(pct, 1) + "% tổng chi. Có thể đặt giới hạn riêng cho danh mục này.");
            }

            if (monthIncome > 0 && monthSaving < monthIncome * 0.1M)
                recs.Add("Tiết kiệm tháng này dưới 10% thu nhập. Có thể đặt mục tiêu chuyển ít nhất 10% thu nhập sang tiết kiệm.");

            if (recs.Count == 0)
                recs.Add("Tình hình tháng này ổn. Hãy tiếp tục duy trì chi tiêu trong ngân sách và tăng dần khoản tiết kiệm.");

            WebUtil.WriteJson(context, new {
                ok = true,
                data = goals,
                summary = new {
                    monthIncome = monthIncome,
                    monthExpense = monthExpense,
                    monthSaving = monthSaving,
                    monthBalance = monthBalance,
                    topExpenseCategory = topCategory,
                    topExpenseAmount = topAmount,
                    monthStart = monthStart,
                    monthEnd = monthEnd
                },
                alerts = alerts,
                recommendations = recs
            });
            return;
        }

        if (method == "POST")
        {
            var data = WebUtil.ReadJson(context);
            string action = data.ContainsKey("action") ? WebUtil.S(data["action"]).ToLower() : "save";

            if (action == "delete")
            {
                Db.Execute("DELETE FROM SavingsGoals WHERE GoalId=@GoalId AND UserId=@UserId",
                    new SqlParameter("@GoalId", WebUtil.I(data["goalId"])),
                    new SqlParameter("@UserId", userId));

                WebUtil.WriteJson(context, new { ok = true });
                return;
            }

            int goalId = WebUtil.I(data["goalId"]);
            string goalName = WebUtil.S(data["goalName"]);
            decimal targetAmount = WebUtil.D(data["targetAmount"]);
            decimal monthlyBudget = WebUtil.D(data["monthlyBudget"]);
            string startDate = WebUtil.S(data["startDate"]);
            string targetDate = WebUtil.S(data["targetDate"]);
            int isActive = WebUtil.I(data["isActive"]);

            if (goalName == "" || targetAmount <= 0 || monthlyBudget <= 0 || startDate == "" || targetDate == "")
            {
                WebUtil.WriteError(context, 400, "Vui lòng nhập đủ thông tin mục tiêu tiết kiệm.");
                return;
            }

            DateTime startDateValue;
            DateTime targetDateValue;

            if (!DateTime.TryParse(startDate, out startDateValue) ||
                !DateTime.TryParse(targetDate, out targetDateValue))
            {
                WebUtil.WriteError(context, 400, "Ngày kế hoạch tiết kiệm không hợp lệ.");
                return;
            }

            if (targetDateValue <= startDateValue)
            {
                WebUtil.WriteError(context, 400, "Ngày đạt mục tiêu phải sau ngày bắt đầu.");
                return;
            }

            if (goalId > 0)
            {
                Db.Execute(@"UPDATE SavingsGoals
                             SET GoalName=@GoalName,
                                 TargetAmount=@TargetAmount,
                                 MonthlyBudget=@MonthlyBudget,
                                 StartDate=@StartDate,
                                 TargetDate=@TargetDate,
                                 IsActive=@IsActive
                             WHERE GoalId=@GoalId AND UserId=@UserId",
                    new SqlParameter("@GoalId", goalId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@GoalName", goalName),
                    new SqlParameter("@TargetAmount", targetAmount),
                    new SqlParameter("@MonthlyBudget", monthlyBudget),
                    new SqlParameter("@StartDate", startDate),
                    new SqlParameter("@TargetDate", targetDate),
                    new SqlParameter("@IsActive", isActive == 1 ? 1 : 0));
            }
            else
            {
                Db.Execute(@"INSERT INTO SavingsGoals(UserId, GoalName, TargetAmount, MonthlyBudget, StartDate, TargetDate, IsActive)
                             VALUES(@UserId,@GoalName,@TargetAmount,@MonthlyBudget,@StartDate,@TargetDate,@IsActive)",
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@GoalName", goalName),
                    new SqlParameter("@TargetAmount", targetAmount),
                    new SqlParameter("@MonthlyBudget", monthlyBudget),
                    new SqlParameter("@StartDate", startDate),
                    new SqlParameter("@TargetDate", targetDate),
                    new SqlParameter("@IsActive", isActive == 1 ? 1 : 0));
            }

            WebUtil.WriteJson(context, new { ok = true });
            return;
        }

        WebUtil.WriteError(context, 405, "Method không hợp lệ.");
    }

    public bool IsReusable { get { return false; } }
}
