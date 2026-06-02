const API_BASE = "../backend/api";
let types = [];
let categories = [];
let currentChartPeriod = 'day';
let transactionCurrentPage = 1;
let transactionPageSize = 10;
let transactionTotalPages = 1;
let transactionTotalRows = 0;

function el(id) { return document.getElementById(id); }
function money(v) { return Number(v || 0).toLocaleString("vi-VN"); }
function isIncomeRow(r) {
  return String(r.TypeCode || "").toLowerCase() === "income" || String(r.TypeName || "").toLowerCase().indexOf("thu") >= 0;
}
function isSavingRow(r) {
  return String(r.TypeCode || "").toLowerCase() === "saving" || String(r.TypeName || "").toLowerCase().indexOf("tiết") >= 0;
}
function typeBadge(r) {
  if (isIncomeRow(r)) return '<span class="badge badge-income">Thu</span>';
  if (isSavingRow(r)) return '<span class="badge badge-saving">Tiết kiệm</span>';
  return '<span class="badge badge-expense">Chi</span>';
}
function signedAmount(r) {
  const value = Number(r.Amount || 0);
  if (isIncomeRow(r)) return '<span class="amount-income">+ ' + money(value) + '</span>';
  if (isSavingRow(r)) return '<span class="amount-saving">◇ ' + money(value) + '</span>';
  return '<span class="amount-expense">- ' + money(value) + '</span>';
}
function formatDate(v) {
  if (!v) return "";
  if (typeof v === "string" && v.indexOf("/Date(") === 0) {
    const match = /\/Date\((\d+)/.exec(v);
    if (match) return new Date(Number(match[1])).toISOString().substring(0, 10);
  }
  return String(v).substring(0, 10);
}
function today() { return new Date().toISOString().substring(0,10); }

function addMonths(dateString, months) {
  const d = dateString ? new Date(dateString) : new Date();
  d.setMonth(d.getMonth() + months);
  return d.toISOString().substring(0, 10);
}

async function api(path, options) {
  const response = await fetch(API_BASE + "/" + path, {
    credentials: "same-origin",
    headers: { "Content-Type": "application/json" },
    ...options
  });
  const data = await response.json();
  if (!response.ok || data.ok === false) throw new Error(data.message || "Có lỗi xảy ra.");
  return data;
}

async function login() {
  try {
    const data = await api("login.ashx", {
      method: "POST",
      body: JSON.stringify({ username: el("loginUsername").value, password: el("loginPassword").value })
    });
    el("loginView").classList.add("hidden");
    el("appView").classList.remove("hidden");
    await initData();
  } catch (e) {
    el("loginMessage").innerText = e.message;
  }
}

async function logout() {
  await api("logout.ashx", { method: "POST" });
  location.reload();
}

async function initData() {
  el("transactionDate").value = today();
  await loadTypes();
  await loadCategories();
  fillAllSelects();
  await loadTransactions();
  await loadStatistics();
  await loadSavingsGoals();
  await loadUsers();
}

function showTab(id) {
  document.querySelectorAll(".tab").forEach(x => x.classList.add("hidden"));
  document.querySelectorAll(".nav").forEach(x => x.classList.remove("active"));
  el(id).classList.remove("hidden");

  const keyword = id === "transactions" ? "giao"
    : id === "savingsGoals" ? "kế hoạch"
    : id === "categories" ? "danh"
    : id === "types" ? "loại"
    : "user";

  const nav = [...document.querySelectorAll(".nav")]
    .find(b => b.textContent.toLowerCase().includes(keyword));
  if (nav) nav.classList.add("active");

  if (id === "transactions") loadStatistics();
  if (id === "savingsGoals") loadSavingsGoals();
}

async function loadTypes() {
  const res = await api("transactionTypes.ashx");
  types = res.data;
  renderTypes();
}

async function loadCategories() {
  const res = await api("categories.ashx");
  categories = res.data;
  renderCategories();
}

function fillAllSelects() {
  fillTypeSelect("transactionType", false);
  fillTypeSelect("filterType", true);
  fillTypeSelect("categoryType", false);
  fillCategorySelect("transactionCategory", el("transactionType").value);
  fillCategorySelect("filterCategory", "", true);
}

function fillTypeSelect(id, all) {
  const html = (all ? '<option value="0">Tất cả</option>' : "") +
    types.map(t => `<option value="${t.TypeId}">${t.TypeName}</option>`).join("");
  el(id).innerHTML = html;
}

function fillCategorySelect(id, typeId, all) {
  const filtered = categories.filter(c => !typeId || typeId == "0" || c.TypeId == typeId);
  el(id).innerHTML = (all ? '<option value="0">Tất cả</option>' : "") +
    filtered.map(c => `<option value="${c.CategoryId}">${c.CategoryName}</option>`).join("");
}


function updateTransactionPagination(meta) {
  transactionCurrentPage = Number(meta.page || transactionCurrentPage || 1);
  transactionPageSize = Number(meta.pageSize || transactionPageSize || 10);
  transactionTotalPages = Math.max(1, Number(meta.totalPages || 1));
  transactionTotalRows = Number(meta.totalRows || 0);

  const pageInfo = el("transactionPageInfo");
  if (pageInfo) {
    pageInfo.innerText = "Trang " + transactionCurrentPage + "/" + transactionTotalPages + " - Tổng " + transactionTotalRows + " giao dịch";
  }

  const pageSizeSelect = el("transactionPageSize");
  if (pageSizeSelect) {
    pageSizeSelect.value = String(transactionPageSize);
  }
}

function changeTransactionPageSize() {
  const select = el("transactionPageSize");
  transactionPageSize = Number(select ? select.value : 10);
  transactionCurrentPage = 1;
  loadTransactions();
}

function goTransactionPage(step) {
  const nextPage = transactionCurrentPage + step;
  if (nextPage < 1 || nextPage > transactionTotalPages) return;
  transactionCurrentPage = nextPage;
  loadTransactions();
}

function getFilters() {
  const q = new URLSearchParams();
  q.set("from", el("filterFrom").value);
  q.set("to", el("filterTo").value);
  q.set("typeId", el("filterType").value || 0);
  q.set("categoryId", el("filterCategory").value || 0);
  q.set("keyword", el("filterKeyword").value || "");
  q.set("page", transactionCurrentPage || 1);
  q.set("pageSize", transactionPageSize || 10);
  return q.toString();
}

async function loadTransactions(resetPage) {
  if (resetPage === true) {
    transactionCurrentPage = 1;
  }

  const select = el("transactionPageSize");
  if (select) {
    transactionPageSize = Number(select.value || transactionPageSize || 10);
  }

  const res = await api("transactions.ashx?" + getFilters());
  renderTransactions(res.data || []);
  updateTransactionPagination({
    page: res.page || transactionCurrentPage,
    pageSize: res.pageSize || transactionPageSize,
    totalRows: res.totalRows || 0,
    totalPages: res.totalPages || 1
  });
  await loadStatistics();
}

function renderTransactions(rows) {
  el("transactionTable").innerHTML = table(["Ngày","Loại","Danh mục","Số tiền","Ghi chú",""], rows.map(r => [
    formatDate(r.TransactionDate),
    typeBadge(r),
    r.CategoryName,
    signedAmount(r),
    r.Note || "",
    `<button onclick='editTransaction(${JSON.stringify(r)})'>Sửa</button>
     <button class="danger" onclick="deleteTransaction(${r.TransactionId})">Xóa</button>`
  ]));
}

function table(headers, rows) {
  return `<table><thead><tr>${headers.map(h=>`<th>${h}</th>`).join("")}</tr></thead>
  <tbody>${rows.map(r=>`<tr>${r.map(c=>`<td>${c}</td>`).join("")}</tr>`).join("")}</tbody></table>`;
}

async function saveTransaction() {
  const data = {
    transactionId: Number(el("transactionId").value || 0),
    transactionDate: el("transactionDate").value,
    typeId: Number(el("transactionType").value),
    categoryId: Number(el("transactionCategory").value),
    amount: Number(el("transactionAmount").value),
    note: el("transactionNote").value
  };
  if (!data.transactionDate || !data.typeId || !data.categoryId || data.amount <= 0) {
    alert("Vui lòng nhập đủ ngày, loại, danh mục và số tiền > 0.");
    return;
  }
  data.action = "save";
  await api("transactions.ashx", {
    method: "POST",
    body: JSON.stringify(data)
  });
  clearTransactionForm();
  await loadTransactions();
}

function editTransaction(r) {
  el("transactionId").value = r.TransactionId;
  el("transactionDate").value = formatDate(r.TransactionDate);
  el("transactionType").value = r.TypeId;
  fillCategorySelect("transactionCategory", r.TypeId);
  el("transactionCategory").value = r.CategoryId;
  el("transactionAmount").value = r.Amount;
  el("transactionNote").value = r.Note || "";
  window.scrollTo(0,0);
}

async function deleteTransaction(id) {
  if (!confirm("Xóa giao dịch này?")) return;
  await api("transactions.ashx", {
    method: "POST",
    body: JSON.stringify({ action: "delete", transactionId: id })
  });
  await loadTransactions();
}

function clearTransactionForm() {
  el("transactionId").value = "";
  el("transactionDate").value = today();
  el("transactionAmount").value = "";
  el("transactionNote").value = "";
}

function resetFilters() {
  el("filterFrom").value = "";
  el("filterTo").value = "";
  el("filterType").value = "0";
  fillCategorySelect("filterCategory", "", true);
  el("filterKeyword").value = "";
  transactionCurrentPage = 1;
  loadTransactions();
}


function setChartPeriod(period) {
  currentChartPeriod = period;
  document.querySelectorAll(".period").forEach(b => b.classList.remove("active"));
  const btn = el("btnPeriod" + period.charAt(0).toUpperCase() + period.slice(1));
  if (btn) btn.classList.add("active");
  loadStatistics();
}

function statisticsQuery() {
  const q = new URLSearchParams(getFilters());
  q.set("period", currentChartPeriod);
  return q.toString();
}

async function loadStatistics() {
  try {
    const res = await api("statistics.ashx?" + statisticsQuery());
    const income = Number(res.summary.totalIncome || 0);
    const expense = Number(res.summary.totalExpense || 0);
    const saving = Number(res.summary.totalSaving || 0);
    const balance = Number(res.summary.balance || 0);

    el("totalIncome").innerHTML = '<span class="text-income">+ ' + money(income) + '</span>';
    el("totalExpense").innerHTML = '<span class="text-expense">- ' + money(expense) + '</span>';
    el("totalSaving").innerHTML = '<span class="text-saving">◇ ' + money(saving) + '</span>';
    el("balance").innerHTML = balance >= 0
      ? '<span class="text-income">+ ' + money(balance) + '</span>'
      : '<span class="text-expense">- ' + money(Math.abs(balance)) + '</span>';

    const incomeCard = el("totalIncome").closest(".card");
    const expenseCard = el("totalExpense").closest(".card");
    const savingCard = el("totalSaving").closest(".card");
    const balanceCard = el("balance").closest(".card");
    incomeCard.classList.add("card-income");
    expenseCard.classList.add("card-expense");
    savingCard.classList.add("card-saving");
    balanceCard.classList.remove("card-balance-positive", "card-balance-negative");
    balanceCard.classList.add(balance >= 0 ? "card-balance-positive" : "card-balance-negative");

    drawPieChart("expensePieChart", res.expenseByCategory || [], "Chi tiêu theo danh mục", "expense");
    drawPieChart("incomePieChart", res.incomeByCategory || [], "Thu nhập theo danh mục", "income");
    drawPieChart("savingPieChart", res.savingByCategory || [], "Tiết kiệm theo danh mục", "saving");
    drawPeriodChart("monthChart", res.byPeriod || [], currentChartPeriod);
  } catch (e) {
    el("totalIncome").innerText = "Lỗi";
    el("totalExpense").innerText = "Lỗi";
    if (el("totalSaving")) el("totalSaving").innerText = "Lỗi";
    el("balance").innerText = "Lỗi";
    drawText("expensePieChart", "Không tải được dữ liệu thống kê: " + e.message);
    drawText("incomePieChart", "Không tải được dữ liệu thống kê: " + e.message);
    drawText("savingPieChart", "Không tải được dữ liệu thống kê: " + e.message);
    drawText("monthChart", "Không tải được dữ liệu thống kê: " + e.message);
  }
}

function drawText(canvasId, text) {
  const canvas = el(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);
  ctx.font = "16px Arial";
  ctx.fillText(text, 20, 40);
}

function pieColors(mode) {
  if (mode === "income") return ["#16a34a", "#22c55e", "#86efac", "#15803d", "#bbf7d0", "#4ade80"];
  if (mode === "saving") return ["#2563eb", "#60a5fa", "#93c5fd", "#1d4ed8", "#bfdbfe", "#3b82f6"];
  return ["#dc2626", "#ef4444", "#fca5a5", "#b91c1c", "#fecaca", "#f87171"];
}

function drawPieChart(canvasId, rows, title, mode) {
  const canvas = el(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);
  ctx.font = "16px Arial";
  ctx.fillStyle = "#111827";
  ctx.fillText(title, 20, 28);

  const total = rows.reduce((s, r) => s + Number(r.TotalAmount || 0), 0);
  if (!rows.length || total <= 0) {
    ctx.fillStyle = "#64748b";
    ctx.fillText("Không có dữ liệu", 20, 70);
    return;
  }

  const colors = pieColors(mode);
  const cx = 150;
  const cy = 175;
  const radius = 105;
  let startAngle = -Math.PI / 2;

  rows.forEach((r, i) => {
    const value = Number(r.TotalAmount || 0);
    const angle = value / total * Math.PI * 2;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, radius, startAngle, startAngle + angle);
    ctx.closePath();
    ctx.fillStyle = colors[i % colors.length];
    ctx.fill();
    startAngle += angle;
  });

  let legendX = 300;
  let legendY = 70;
  ctx.font = "14px Arial";
  rows.forEach((r, i) => {
    const value = Number(r.TotalAmount || 0);
    const pct = total ? value / total * 100 : 0;
    ctx.fillStyle = colors[i % colors.length];
    ctx.fillRect(legendX, legendY - 12, 14, 14);
    ctx.fillStyle = "#111827";
    ctx.fillText(
      String(r.CategoryName).substring(0, 18) + ": " + pct.toFixed(1) + "% (" + money(value) + ")",
      legendX + 22,
      legendY
    );
    legendY += 24;
  });
}


function drawPeriodChart(canvasId, rows, period) {
  const canvas = el(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);

  const title = period === "day"
    ? "Thu / Chi / Tiết kiệm theo 7 ngày gần nhất"
    : period === "year"
      ? "Thu / Chi / Tiết kiệm theo 3 năm gần nhất"
      : "Thu / Chi / Tiết kiệm theo 12 tháng gần nhất";

  const titleNode = el("periodChartTitle");
  if (titleNode) titleNode.innerText = title;

  ctx.font = "16px Arial";
  ctx.fillStyle = "#111827";
  ctx.fillText(title, 20, 28);

  ctx.font = "14px Arial";
  ctx.fillStyle = "#16a34a";
  ctx.fillRect(20, 44, 14, 14);
  ctx.fillStyle = "#111827";
  ctx.fillText("Thu nhập", 40, 56);

  ctx.fillStyle = "#dc2626";
  ctx.fillRect(130, 44, 14, 14);
  ctx.fillStyle = "#111827";
  ctx.fillText("Chi tiêu", 150, 56);

  ctx.fillStyle = "#2563eb";
  ctx.fillRect(230, 44, 14, 14);
  ctx.fillStyle = "#111827";
  ctx.fillText("Tiết kiệm", 250, 56);

  if (!rows.length) {
    ctx.fillStyle = "#64748b";
    ctx.fillText("Không có dữ liệu", 20, 90);
    return;
  }

  const max = Math.max(1, ...rows.flatMap(r => [
    Number(r.IncomeAmount || 0),
    Number(r.ExpenseAmount || 0),
    Number(r.SavingAmount || 0)
  ]));
  const groupW = Math.max(45, (canvas.width - 90) / rows.length - 10);
  const barW = Math.max(8, groupW / 3 - 3);

  rows.forEach((r,i) => {
    const x = 45 + i * (groupW + 10);
    const inc = Number(r.IncomeAmount || 0);
    const exp = Number(r.ExpenseAmount || 0);
    const sav = Number(r.SavingAmount || 0);
    const incH = inc / max * 180;
    const expH = exp / max * 180;
    const savH = sav / max * 180;

    ctx.fillStyle = "#16a34a";
    ctx.fillRect(x, 270 - incH, barW, incH);

    ctx.fillStyle = "#dc2626";
    ctx.fillRect(x + barW + 3, 270 - expH, barW, expH);

    ctx.fillStyle = "#2563eb";
    ctx.fillRect(x + (barW + 3) * 2, 270 - savH, barW, savH);

    ctx.fillStyle = "#111827";
    ctx.font = "12px Arial";
    const label = r.PeriodLabel || r.PeriodKey || "";
    ctx.save();
    ctx.translate(x, 292);
    if (period === "month") ctx.rotate(-0.45);
    ctx.fillText(label, 0, 0);
    ctx.restore();

    if (period === "year") {
      ctx.fillStyle = "#16a34a";
      ctx.fillText(money(inc), x, Math.max(78, 270 - incH - 6));
      ctx.fillStyle = "#dc2626";
      ctx.fillText(money(exp), x + barW + 3, Math.max(92, 270 - expH - 6));
      ctx.fillStyle = "#2563eb";
      ctx.fillText(money(sav), x + (barW + 3) * 2, Math.max(106, 270 - savH - 6));
    }
  });
}

function renderCategories() {
  el("categoryTable").innerHTML = table(["Loại","Danh mục","Hoạt động",""], categories.map(c => [
    c.TypeName, c.CategoryName, c.IsActive ? "Có" : "Không",
    `<button onclick='editCategory(${JSON.stringify(c)})'>Sửa</button>
     <button class="danger" onclick="deleteCategory(${c.CategoryId})">Xóa</button>`
  ]));
}

async function saveCategory() {
  const data = {
    categoryId: Number(el("categoryId").value || 0),
    typeId: Number(el("categoryType").value),
    categoryName: el("categoryName").value,
    isActive: Number(el("categoryActive").value)
  };
  data.action = "save";
  await api("categories.ashx", { method: "POST", body: JSON.stringify(data) });
  clearCategoryForm();
  await loadCategories();
  fillAllSelects();
}

function editCategory(c) {
  el("categoryId").value = c.CategoryId;
  el("categoryType").value = c.TypeId;
  el("categoryName").value = c.CategoryName;
  el("categoryActive").value = c.IsActive ? "1" : "0";
}

async function deleteCategory(id) {
  if (!confirm("Xóa danh mục này?")) return;
  await api("categories.ashx", {
    method: "POST",
    body: JSON.stringify({ action: "delete", categoryId: id })
  });
  await loadCategories();
  fillAllSelects();
}

function clearCategoryForm() {
  el("categoryId").value = "";
  el("categoryName").value = "";
  el("categoryActive").value = "1";
}

function renderTypes() {
  el("typeTable").innerHTML = table(["Mã","Tên",""], types.map(t => [
    t.TypeCode, t.TypeName,
    `<button onclick='editType(${JSON.stringify(t)})'>Sửa</button>
     <button class="danger" onclick="deleteType(${t.TypeId})">Xóa</button>`
  ]));
}

async function saveType() {
  const data = {
    typeId: Number(el("typeId").value || 0),
    typeCode: el("typeCode").value,
    typeName: el("typeName").value
  };
  data.action = "save";
  await api("transactionTypes.ashx", { method: "POST", body: JSON.stringify(data) });
  clearTypeForm();
  await loadTypes();
  fillAllSelects();
}

function editType(t) {
  el("typeId").value = t.TypeId;
  el("typeCode").value = t.TypeCode;
  el("typeName").value = t.TypeName;
}

async function deleteType(id) {
  if (!confirm("Xóa loại này?")) return;
  await api("transactionTypes.ashx", {
    method: "POST",
    body: JSON.stringify({ action: "delete", typeId: id })
  });
  await loadTypes();
  fillAllSelects();
}

function clearTypeForm() {
  el("typeId").value = "";
  el("typeCode").value = "";
  el("typeName").value = "";
}

async function loadUsers() {
  const res = await api("users.ashx");
  el("userTable").innerHTML = table(["Tài khoản","Họ tên","Hoạt động",""], res.data.map(u => [
    u.Username, u.FullName || "", u.IsActive ? "Có" : "Không",
    `<button onclick='editUser(${JSON.stringify(u)})'>Sửa</button>
     <button class="danger" onclick="deleteUser(${u.UserId})">Xóa</button>`
  ]));
}

async function saveUser() {
  const data = {
    userId: Number(el("userId").value || 0),
    username: el("username").value,
    fullName: el("fullName").value,
    password: el("password").value,
    isActive: Number(el("userActive").value)
  };
  data.action = "save";
  await api("users.ashx", { method: "POST", body: JSON.stringify(data) });
  clearUserForm();
  await loadUsers();
}

function editUser(u) {
  el("userId").value = u.UserId;
  el("username").value = u.Username;
  el("fullName").value = u.FullName || "";
  el("password").value = "";
  el("userActive").value = u.IsActive ? "1" : "0";
}

async function deleteUser(id) {
  if (!confirm("Xóa user này?")) return;
  await api("users.ashx", {
    method: "POST",
    body: JSON.stringify({ action: "delete", userId: id })
  });
  await loadUsers();
}

function clearUserForm() {
  el("userId").value = "";
  el("username").value = "";
  el("fullName").value = "";
  el("password").value = "";
  el("userActive").value = "1";
}


async function loadSavingsGoals() {
  try {
    const res = await api("savingsGoals.ashx");
    renderSavingsDashboard(res);
    renderSavingsGoals(res.data || []);
  } catch (e) {
    if (el("budgetAlerts")) el("budgetAlerts").innerHTML = '<div class="alert alert-danger">Không tải được kế hoạch tiết kiệm: ' + e.message + '</div>';
  }
}

function renderSavingsDashboard(res) {
  const summary = res.summary || {};
  const income = Number(summary.monthIncome || 0);
  const expense = Number(summary.monthExpense || 0);
  const saving = Number(summary.monthSaving || 0);
  const balance = Number(summary.monthBalance || 0);

  if (el("goalMonthIncome")) el("goalMonthIncome").innerHTML = '<span class="text-income">+ ' + money(income) + '</span>';
  if (el("goalMonthExpense")) el("goalMonthExpense").innerHTML = '<span class="text-expense">- ' + money(expense) + '</span>';
  if (el("goalMonthSaving")) el("goalMonthSaving").innerHTML = '<span class="text-saving">◇ ' + money(saving) + '</span>';
  if (el("goalMonthBalance")) el("goalMonthBalance").innerHTML = balance >= 0
    ? '<span class="text-income">+ ' + money(balance) + '</span>'
    : '<span class="text-expense">- ' + money(Math.abs(balance)) + '</span>';

  const alerts = res.alerts || [];
  const recommendations = res.recommendations || [];

  el("budgetAlerts").innerHTML = alerts.length
    ? alerts.map(a => '<div class="alert ' + (a.level === "danger" ? "alert-danger" : "alert-warning") + '">' + a.message + '</div>').join("")
    : '<div class="alert alert-ok">Chưa có cảnh báo ngân sách trong tháng này.</div>';

  el("savingRecommendations").innerHTML = recommendations.length
    ? recommendations.map(r => '<div class="recommendation-item">' + r + '</div>').join("")
    : '<div class="recommendation-item">Chưa đủ dữ liệu để đưa ra gợi ý tiết kiệm.</div>';
}

function renderSavingsGoals(rows) {
  el("savingsGoalTable").innerHTML = table(
    ["Mục tiêu", "Thời gian", "Mục tiêu", "Đã tiết kiệm", "Còn thiếu", "Tiến độ", "Ngân sách/tháng", "Trạng thái", ""],
    rows.map(g => [
      g.GoalName,
      formatDate(g.StartDate) + " → " + formatDate(g.TargetDate),
      money(g.TargetAmount),
      money(g.SavedAmount),
      money(g.RemainingAmount),
      '<div class="progress-wrap"><div class="progress-bar" style="width:' + Math.min(100, Number(g.ProgressPercent || 0)) + '%"></div></div><div class="progress-text">' + Number(g.ProgressPercent || 0).toFixed(1) + '%</div>',
      money(g.MonthlyBudget),
      g.IsActive ? "Đang theo dõi" : "Tạm dừng",
      `<button onclick='editSavingsGoal(${JSON.stringify(g)})'>Sửa</button>
       <button class="danger" onclick="deleteSavingsGoal(${g.GoalId})">Xóa</button>`
    ])
  );
}

async function saveSavingsGoal() {
  const data = {
    action: "save",
    goalId: Number(el("goalId").value || 0),
    goalName: el("goalName").value,
    targetAmount: Number(el("goalTargetAmount").value),
    monthlyBudget: Number(el("goalMonthlyBudget").value),
    startDate: el("goalStartDate").value,
    targetDate: el("goalTargetDate").value,
    isActive: Number(el("goalActive").value)
  };

  if (!data.goalName || data.targetAmount <= 0 || data.monthlyBudget <= 0 || !data.startDate || !data.targetDate) {
    alert("Vui lòng nhập đủ tên mục tiêu, số tiền mục tiêu, ngân sách tháng, ngày bắt đầu và ngày đạt mục tiêu.");
    return;
  }

  await api("savingsGoals.ashx", {
    method: "POST",
    body: JSON.stringify(data)
  });

  clearSavingsGoalForm();
  await loadSavingsGoals();
}

function editSavingsGoal(g) {
  el("goalId").value = g.GoalId;
  el("goalName").value = g.GoalName;
  el("goalTargetAmount").value = Number(g.TargetAmount || 0);
  el("goalMonthlyBudget").value = Number(g.MonthlyBudget || 0);
  el("goalStartDate").value = formatDate(g.StartDate);
  el("goalTargetDate").value = formatDate(g.TargetDate);
  el("goalActive").value = g.IsActive ? "1" : "0";
  window.scrollTo(0,0);
}

async function deleteSavingsGoal(id) {
  if (!confirm("Xóa mục tiêu tiết kiệm này?")) return;

  await api("savingsGoals.ashx", {
    method: "POST",
    body: JSON.stringify({ action: "delete", goalId: id })
  });

  await loadSavingsGoals();
}

function clearSavingsGoalForm() {
  el("goalId").value = "";
  el("goalName").value = "";
  el("goalTargetAmount").value = "";
  el("goalMonthlyBudget").value = "";
  el("goalStartDate").value = today();
  el("goalTargetDate").value = addMonths(today(), 12);
  el("goalActive").value = "1";
}


(async function checkLogin() {
  try {
    const res = await api("me.ashx");
    if (res.ok) {
      el("loginView").classList.add("hidden");
      el("appView").classList.remove("hidden");
      await initData();
    }
  } catch(e) {}
})();
