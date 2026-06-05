# Thông tin đề tài ASP.NET

| GVHD | TS Đoàn Phước Miền |
|---|---|
| Đề tài | Ứng dụng quản lý thu chi cá nhân |
| Lớp | DK24TT80171 |
| Họ và tên | Tôn Thất Khoa |
| MSSV | 170124890 |

---

# 1. Giới thiệu ứng dụng

Phát triển một ứng dụng giúp người dùng quản lý thu nhập, chi tiêu hàng ngày một cách khoa học và trực quan. Ứng dụng hỗ trợ ghi chép, phân loại, và thống kê các khoản chi tiêu nhằm giúp người dùng kiểm soát tài chính cá nhân, xác định thói quen chi tiêu, và lập kế hoạch tiết kiệm hiệu quả.​Quản lý danh mục thu chi: Người dùng thêm/sửa/xóa các khoản thu nhập (Income) và chi tiêu (Expense). Ghi nhận thông tin: ngày, loại chi tiêu, số tiền, ghi chú.​Thống kê tài chính: Hiển thị tổng thu, tổng chi, số dư còn lại trong ngày/tháng/năm. Biểu đồ trực quan (cột hoặc tròn) thể hiện tỷ lệ chi tiêu theo loại.​Tìm kiếm, lọc và tổng hợp, thống kê dữ liệu: Lọc theo khoảng thời gian, loại chi tiêu, hoặc từ khóa ghi chú. Tổng hợp, thống kê theo nhiều tiêu chí: thời gian, loại chi tiêu...
Hệ thống cho phép người dùng:

* Quản lý các khoản thu nhập
* Quản lý các khoản chi tiêu
* Quản lý các khoản tiết kiệm
* Quản lý danh mục thu chi
* Quản lý loại giao dịch
* Quản lý người dùng
* Lập kế hoạch tiết kiệm
* Theo dõi số dư tài chính
* Thống kê và biểu đồ tài chính theo ngày, tháng, năm
* Phân quyền Admin và User

---

# 2. Công nghệ sử dụng

| Phần | Công nghệ / Ghi chú |
|---|---|
| Backend | ASP.NET Framework 4.8<br>ASP.NET Web Handler (.ashx)<br>C#<br>ADO.NET<br>IIS Web Server |
| Frontend | HTML5<br>CSS3<br>JavaScript (Vanilla JS) |
| Database | Microsoft SQL Server 2018<br>SQL Server Express / SQL Server Developer |
| Môi trường triển khai | Windows 10 / 11<br>IIS<br>.NET Framework 4.8 |
---

# 3. Cấu trúc thư mục

```text
Repository
│
├── setup
│   │
│   └── scriptdeploy.zip
│       ├── run_deploy.bat
│       ├── deploy_personal_finance_iis.ps1
│       ├── run_cleanup.bat
│       └── ...
│
├── src
│   │
│   └── PersonalFinanceOffline
│       │
│       ├── backend
│       │   ├── api
│       │   ├── App_Code
│       │   ├── sql
│       │   └── Web.config
│       │
│       └── frontend
│           ├── assets
│           ├── views
│           └── index.html
│       
│
└── thesis
    │
    ├── doc
    │
    └── pdf
```

---

# 4. Cách thức triển khai cài đặt source code

## Yêu cầu hệ thống (bắt buộc)

<table>
  <tr>
    <th colspan="2"><div align="center"><strong>Bắt buộc:</strong></div></th>
  </tr>
  <tr>
    <td>Hệ điều hành</td>
    <td>Windows 10 / 11</td>
  </tr>
  <tr>
    <td>Database</td>
    <td>Phải cài sẵn SQL Server Express (khuyến nghị) / Developer / Standard</td>
  </tr>
  <tr>
    <td colspan="2"><div align="center"><strong>Script sẽ tự động enable:</strong></div></td>
  </tr>
  <tr>
    <td>Enable Feature</td>
    <td>IIS<br>.NET Framework 4.8</td>
  </tr>
</table>

---
# Triển khai bằng script Auto Deploy
### B0. Giải nén

Giải nén:

```text
setup/scriptdeploy.zip
```

### B1. Chạy Deploy

Chạy (Run as administrator):

```text
run_deploy.bat
```

Nhập tên SQL Instance. Vd: SQLEXPRESS hoặc MSSQLSERVER

Nếu để trống sẽ sử dụng mặc định:

```text
SQLEXPRESS
```
Nhấn enter và chờ checklist hoàn tất.
### B2. Hoàn tất

Sau khi script hoàn tất, truy cập:

```text
http://localhost/PersonalFinanceOffline/frontend/index.html
```
---

# Cài đặt thủ công

### B0. Cài đặt SQL Server

Cài đặt SQL Server Express (khuyến nghị) hoặc SQL Server bất kỳ.

### B1. Chuẩn bị source

Giải nén source vào:

```text
C:\inetpub\wwwroot\PersonalFinanceOffline
```

### B2. Cấu hình Database

* Cập nhật Connection String trong:

```text
backend\Web.config
```

* Chạy các file SQL trong:

```text
backend\sql
```

### B3. Cấu hình IIS

* Tạo Application Pool
* Tạo Website/Virtual Directory `PersonalFinanceOffline`
* Convert thư mục `backend` thành **Application**

### B4. Phân quyền Database

Cấp quyền SQL cho App Pool đang sử dụng.

### B5. Khởi động hệ thống

```cmd
iisreset
```

Truy cập:

```text
http://localhost/PersonalFinanceOffline/frontend/index.html
```

---

# 5. Dọn dẹp hệ thống

Chạy (Run as administrator):

```text
run_cleanup.bat
```

Nhập:

```text
Tên SQL Instance. Vd: SQLEXPRESS hoặc MSSQLSERVER
```

Xác nhận:

```text
yes
```

và nhấn **Enter** để bắt đầu dọn dẹp.

# 6. Tài khoản mặc định

## Quản trị viên

```text
Username: admin
Password: admin123
```

## Người dùng thường

```text
Username: user01
Password: user01123
```

---
