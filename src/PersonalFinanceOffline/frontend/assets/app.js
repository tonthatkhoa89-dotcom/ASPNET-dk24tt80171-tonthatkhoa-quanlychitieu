const API_BASE = "../backend/api";
let types = [];
let categories = [];
let currentChartPeriod = 'day';

function el(id) { return document.getElementById(id); }
function money(v) { return Number(v || 0).toLocaleString("vi-VN"); }

function isIncomeRow(r) {
  return String(r.TypeCode || "").toLowerCase() === "income" || String(r.TypeName || "").toLowerCase().indexOf("thu") >= 0;
}
function typeBadge(r) {
  return isIncomeRow(r)
    ? '<span class="badge badge-income">Thu</span>'
    : '<span class="badge badge-expense">Chi</span>';
}
function signedAmount(r) {
  const value = Number(r.Amount || 0);
  if (isIncomeRow(r)) return '<span class="amount-income">+ ' + money(value) + '</span>';
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
}

function showTab(id) {
  document.querySelectorAll(".tab").forEach(x => x.classList.add("hidden"));
  document.querySelectorAll(".nav").forEach(x => x.classList.remove("active"));
  el(id).classList.remove("hidden");
  const nav = [...document.querySelectorAll(".nav")].find(b => b.textContent.toLowerCase().includes("giao"));
  if (nav) nav.classList.add("active");
  if (id === "transactions") loadStatistics();
}

async function loadTypes() {
  const res = await api("transactionTypes.ashx");
  types = res.data;
}

async function loadCategories() {
  const res = await api("categories.ashx");
  categories = res.data;
}

function fillAllSelects() {
  fillTypeSelect("transactionType", false);
  fillTypeSelect("filterType", true);
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

function getFilters() {
  const q = new URLSearchParams();
  q.set("from", el("filterFrom").value);
  q.set("to", el("filterTo").value);
  q.set("typeId", el("filterType").value || 0);
  q.set("categoryId", el("filterCategory").value || 0);
  q.set("keyword", el("filterKeyword").value || "");
  return q.toString();
}

async function loadTransactions() {
  const res = await api("transactions.ashx?" + getFilters());
  renderTransactions(res.data);
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
    action: "save",
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
    const balance = Number(res.summary.balance || 0);

    el("totalIncome").innerHTML = '<span class="text-income">+ ' + money(income) + '</span>';
    el("totalExpense").innerHTML = '<span class="text-expense">- ' + money(expense) + '</span>';
    el("balance").innerHTML = balance >= 0
      ? '<span class="text-income">+ ' + money(balance) + '</span>'
      : '<span class="text-expense">- ' + money(Math.abs(balance)) + '</span>';

    drawBarChart("categoryChart", res.expenseByCategory || [], "CategoryName", "TotalAmount", "Chi theo danh mục", "expense");
    drawPeriodChart("monthChart", res.byPeriod || [], currentChartPeriod);
  } catch (e) {
    el("totalIncome").innerText = "Lỗi";
    el("totalExpense").innerText = "Lỗi";
    el("balance").innerText = "Lỗi";
    drawText("categoryChart", "Không tải được dữ liệu thống kê: " + e.message);
    drawText("monthChart", "Không tải được dữ liệu thống kê: " + e.message);
  }
}

function drawText(canvasId, text) {
  const canvas = el(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);
  ctx.font = "16px Arial";
  ctx.fillText(text, 20, 40);
}

function drawBarChart(canvasId, rows, labelField, valueField, title, mode) {
  const canvas = el(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);
  ctx.font = "16px Arial";
  ctx.fillStyle = "#111827";
  ctx.fillText(title, 20, 28);
  if (!rows.length) {
    ctx.fillStyle = "#64748b";
    ctx.fillText("Không có dữ liệu", 20, 70);
    return;
  }
  const max = Math.max(...rows.map(r => Number(r[valueField])));
  const barW = Math.max(28, (canvas.width - 80) / rows.length - 16);
  rows.forEach((r,i) => {
    const value = Number(r[valueField]);
    const h = max ? value / max * 210 : 0;
    const x = 50 + i * (barW + 16);
    const y = 270 - h;
    ctx.fillStyle = "#dc2626";
    ctx.fillRect(x, y, barW, h);
    ctx.fillStyle = "#111827";
    ctx.fillText(money(value), x, Math.max(48, y - 6));
    ctx.save();
    ctx.translate(x, 292);
    ctx.rotate(-0.5);
    ctx.fillText(String(r[labelField]).substring(0,14), 0, 0);
    ctx.restore();
  });
}

function drawPeriodChart(canvasId, rows, period) {
  const canvas = el(canvasId), ctx = canvas.getContext("2d");
  ctx.clearRect(0,0,canvas.width,canvas.height);
  const title = period === "day" ? "Thu / Chi theo 7 ngày gần nhất"
    : period === "year" ? "Thu / Chi theo 3 năm gần nhất"
    : "Thu / Chi theo 12 tháng gần nhất";
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
  if (!rows.length) {
    ctx.fillStyle = "#64748b";
    ctx.fillText("Không có dữ liệu", 20, 90);
    return;
  }
  const max = Math.max(1, ...rows.flatMap(r => [Number(r.IncomeAmount || 0), Number(r.ExpenseAmount || 0)]));
  const groupW = Math.max(38, (canvas.width - 90) / rows.length - 10);
  rows.forEach((r,i) => {
    const x = 45 + i * (groupW + 10);
    const inc = Number(r.IncomeAmount || 0);
    const exp = Number(r.ExpenseAmount || 0);
    const incH = inc / max * 180;
    const expH = exp / max * 180;
    ctx.fillStyle = "#16a34a";
    ctx.fillRect(x, 270 - incH, groupW/2 - 2, incH);
    ctx.fillStyle = "#dc2626";
    ctx.fillRect(x + groupW/2 + 2, 270 - expH, groupW/2 - 2, expH);
    ctx.fillStyle = "#111827";
    ctx.font = "12px Arial";
    const label = r.PeriodLabel || r.PeriodKey || "";
    ctx.save();
    ctx.translate(x, 292);
    if (period === "month") ctx.rotate(-0.45);
    ctx.fillText(label, 0, 0);
    ctx.restore();
  });
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
