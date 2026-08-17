<div align="center">

# 🛒 BÁCH HÓA XANH – E-COMMERCE & O2O RETAIL PLATFORM
### Hệ thống Thương Mại Điện Tử & Quản Trị Bán Lẻ Đa Kênh (Omnichannel Retail)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=for-the-badge)](https://docs.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/Microsoft%20SQL%20Server-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![Google Gemini AI](https://img.shields.io/badge/Google%20Gemini-AI%20Assistant-4285F4?style=for-the-badge&logo=google&logoColor=white)](https://ai.google.dev/)
[![VNPay](https://img.shields.io/badge/Payment-VNPay%20%7C%20VietQR-005BAA?style=for-the-badge)](https://vnpay.vn/)

<p align="center">
  Một giải pháp thương mại điện tử kết hợp bán lẻ tại quầy (O2O) toàn diện mô phỏng chuỗi siêu thị <b>Bách Hóa XANH</b>, được xây dựng trên nền tảng <b>ASP.NET Core 8 MVC</b>, tích hợp cổng thanh toán <b>VNPay / VietQR</b>, trợ lý ảo <b>Gemini AI</b>, phân hệ <b>POS tại quầy</b>, <b>quản lý kho theo lô hạn dùng</b> và <b>quản trị nhân sự (HRM)</b>.
</p>

[Tính năng nổi bật](#-tính-năng-nổi-bật) • [Công nghệ sử dụng](#-công-nghệ-sử-dụng) • [Kiến trúc hệ thống](#-cấu-trúc-thư-mục-dự-án) • [Hướng dẫn cài đặt](#-hướng-dẫn-cài-đặt--chạy-local)

---

</div>

## 🌟 Tính năng nổi bật

### 1. 🛍️ Phân hệ Mua sắm Trực tuyến (Customer Web Store)
- **Danh mục & Tìm kiếm đa tiêu chí**: Lọc thông minh theo ngành hàng, mức giá, khuyến mãi, tìm kiếm từ khóa tức thì.
- **Giỏ hàng & Đặt hàng**: Quản lý giỏ hàng linh hoạt, áp dụng mã giảm giá (`Discount Coupons`) và chương trình khuyến mãi combo (`Combo Promotions`).
- **Cổng thanh toán đa dạng (Multi-channel Payment)**:
  - Tích hợp cổng thanh toán trực tuyến **VNPay Sandbox**.
  - Thanh toán chuyển khoản tự động qua **VietQR / SePay** (tự động nhận diện giao dịch qua Webhook/API).
  - Thanh toán khi nhận hàng (COD).
- **Gamification & Khách hàng thân thiết**: Tích lũy điểm thưởng thành viên, bảng xếp hạng khách hàng (`Leaderboard`), lịch sử tích điểm và đổi quà.
- **Trợ lý mua sắm AI (Gemini Chatbot)**: Tích hợp Google Gemini AI tư vấn sản phẩm, gợi ý thực đơn, trả lời thắc mắc của khách hàng tự động 24/7.
- **Theo dõi đơn hàng**: Kiểm tra trạng thái đơn hàng thời gian thực qua mã vận đơn hoặc tài khoản cá nhân.

### 2. 🏪 Phân hệ Bán lẻ tại quầy (POS - Point of Sale)
- Giao diện thu ngân siêu thị trực quan, hỗ trợ tìm kiếm sản phẩm nhanh, quét barcode.
- Xử lý đơn hàng tại quầy nhanh chóng, áp dụng giảm giá, in hóa đơn.
- Đồng bộ tồn kho tức thì với hệ thống thương mại điện tử trực tuyến.

### 3. 📦 Quản lý Kho & Lô hàng (Inventory & Batch Management)
- Quản lý nhập - xuất - tồn kho chi tiết theo từng chi nhánh siêu thị.
- **Quản lý theo Lô hàng (Batches) & Hạn sử dụng (Expiry Dates)**: Theo dõi chặt chẽ hạn dùng của thực phẩm tươi sống / đồ hộp, tự động cảnh báo hàng cận date để xả hàng khuyến mãi.

### 4. 👥 Quản trị Nhân sự & Chi nhánh (HRM & Branches)
- Quản lý danh sách nhân viên theo từng chi nhánh, cửa hàng.
- Phân ca làm việc, quản lý lịch làm và chấm công cho nhân viên/thu ngân.

### 5. 📊 Bảng điều khiển Quản trị & Báo cáo (Admin Dashboard & Analytics)
- Báo cáo thống kê tổng quan: Doanh thu theo ngày/tháng/năm, số lượng đơn hàng, tỷ lệ hoàn tất.
- Biểu đồ phân tích doanh số theo chi nhánh, top sản phẩm bán chạy nhất.
- Quản lý toàn diện Sản phẩm, Danh mục, Đơn hàng, Mã giảm giá và Tài khoản người dùng.

### 6. 🔒 Bảo mật & Phân quyền (Security & Authorization)
- Xác thực và phân quyền đa cấp độ (**Role-based Authorization**) bằng **ASP.NET Core Identity**: `Admin`, `Staff`, `Cashier`, `Customer`.
- Hỗ trợ đăng nhập nhanh **Single Sign-On (SSO)** qua **Google OAuth 2.0** và **Facebook Login**.
- Mã hóa dữ liệu và bảo vệ session với **ASP.NET Data Protection**.

---

## 🛠 Công nghệ sử dụng

| Lĩnh vực | Công nghệ / Thư viện | Mô tả |
| :--- | :--- | :--- |
| **Framework Backend** | ASP.NET Core 8 MVC (C#) | Kiến trúc MVC mạnh mẽ, tối ưu hiệu năng |
| **ORM / Data Access** | Entity Framework Core 8.0 | Code-First Migrations, LINQ queries tối ưu |
| **Database** | Microsoft SQL Server / LocalDB | Cơ sở dữ liệu quan hệ hoàn chỉnh |
| **Authentication** | ASP.NET Core Identity + OAuth 2.0 | Xác thực nội bộ, Google & Facebook SSO |
| **Payment Gateway** | VNPay SDK & SePay VietQR API | Tích hợp thanh toán trực tuyến & quét mã QR |
| **Artificial Intelligence**| Google Gemini AI API | Chatbot thông minh hỗ trợ khách hàng |
| **Frontend** | HTML5, CSS3, JavaScript, Razor View | Giao diện Responsive tối ưu trên Mobile & PC |
| **UI Framework** | Bootstrap 5, FontAwesome, Chart.js | Giao diện hiện đại, biểu đồ trực quan |
| **Email Service** | MailKit / SmtpClient (Gmail SMTP) | Tự động gửi email xác thực & hóa đơn đơn hàng |

---

## 📁 Cấu trúc thư mục dự án

```text
WebBanHang/
├── WebBanHang-main/
│   ├── Areas/                        # Phân hệ Admin / Sub-systems
│   ├── Controllers/                  # Điều hướng & xử lý nghiệp vụ
│   │   ├── HomeController.cs         # Trang chủ & danh mục
│   │   ├── CartController.cs         # Giỏ hàng & đặt hàng
│   │   ├── PaymentController.cs      # Xử lý thanh toán VNPay / VietQR
│   │   ├── PosController.cs          # Phân hệ bán lẻ tại quầy POS
│   │   ├── ChatController.cs         # AI Assistant (Gemini)
│   │   ├── InventoryBatchesController.cs # Quản lý lô hàng & hạn dùng
│   │   ├── HRMController.cs          # Quản trị nhân sự chi nhánh
│   │   ├── ReportsController.cs      # Báo cáo doanh thu & biểu đồ
│   │   └── ...
│   ├── Models/                       # Domain Entities & Data Models
│   ├── ViewModels/                   # ViewModels truyền dữ liệu cho Views
│   ├── Views/                        # Razor Views giao diện người dùng
│   ├── Services/                     # Email, AI, Payment Services
│   ├── Data/                         # ApplicationDbContext & Migrations
│   ├── wwwroot/                      # Static files (CSS, JS, Images, Libs)
│   ├── appsettings.json              # Cấu hình hệ thống (Sanitized for Git)
│   ├── appsettings.Example.json      # File mẫu hướng dẫn cấu hình
│   └── Program.cs                    # Điểm khởi chạy & Dependency Injection
└── README.md
```

---

## 🚀 Hướng dẫn cài đặt & Chạy Local

### 1. Yêu cầu môi trường
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) hoặc mới hơn
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (với workload *ASP.NET and web development*) hoặc Visual Studio Code / Rider
* [Microsoft SQL Server](https://www.microsoft.com/sql-server/) hoặc LocalDB đi kèm Visual Studio

### 2. Các bước cài đặt
1. **Clone repository về máy**:
   ```bash
   git clone https://github.com/AriesStorm247/bachhoaxanh-ecommerce-aspnetcore8.git
   cd bachhoaxanh-ecommerce-aspnetcore8/WebBanHang-main
   ```

2. **Cấu hình chuỗi kết nối Database & API Keys**:
   - Sao chép file `appsettings.Example.json` thành `appsettings.Local.json` (hoặc sửa trực tiếp trong `appsettings.json`):
   ```json
   {
     "EmailSettings": {
       "SmtpHost": "smtp.gmail.com",
       "SmtpPort": 587,
       "EnableSsl": true,
       "SenderEmail": "your-email@gmail.com",
       "SenderName": "Bách Hóa XANH",
       "AppPassword": "YOUR_GMAIL_APP_PASSWORD"
     },
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=WebBanHangDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     },
     "AI": {
       "Endpoint": "https://generativelanguage.googleapis.com",
       "Model": "gemini-2.5-flash",
       "ApiKey": "YOUR_GEMINI_API_KEY"
     },
     "Payment": {
       "Gateway": "Demo",
       "VnPay": {
         "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
         "TmnCode": "YOUR_VNPAY_TMN_CODE",
         "HashSecret": "YOUR_VNPAY_HASH_SECRET"
       },
       "BankTransfer": {
         "BankCode": "ACB",
         "AccountNumber": "YOUR_ACCOUNT_NUMBER",
         "AccountName": "YOUR_ACCOUNT_NAME",
         "QrTemplate": "compact2",
         "OrderPrefix": "DH_",
         "PosOrderPrefix": "BHX_",
         "ApiUrl": "https://my.sepay.vn/userapi/transactions/list",
         "ApiKey": "YOUR_SEPAY_API_KEY",
         "ApiHeaderName": "Authorization",
         "ApiAuthScheme": "Bearer"
       }
     },
     "Authentication": {
       "Google": {
         "ClientId": "YOUR_GOOGLE_CLIENT_ID",
         "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
       },
       "Facebook": {
         "AppId": "YOUR_FACEBOOK_APP_ID",
         "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
       }
     }
   }
   ```

3. **Cập nhật Database (EF Core Migration)**:
   Mở Terminal tại thư mục `WebBanHang-main` và chạy:
   ```bash
   dotnet ef database update
   ```
   *(Hệ thống sẽ tự động khởi tạo bảng và seed dữ liệu ban đầu)*

4. **Khởi chạy ứng dụng**:
   ```bash
   dotnet run
   ```
   Hoặc nhấn **F5** trong Visual Studio 2022.

5. **Truy cập ứng dụng**:
   - Giao diện người dùng: `https://localhost:5001` (hoặc cổng HTTP do console hiển thị).

### 3. Tài khoản mẫu để kiểm thử (Default Test Accounts)
Hệ thống tự động nạp sẵn dữ liệu mẫu (Danh mục, Sản phẩm có ảnh, Chi nhánh, Tồn kho) và các tài khoản kiểm thử cho từng vai trò ngay lần chạy đầu tiên:

| Vai trò (Role) | Email đăng nhập | Mật khẩu | Quyền hạn & Chức năng |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin@bachhoaxanh.vn` | `Admin@123456` | Toàn quyền Admin: Quản lý sản phẩm, đơn hàng, kho bãi, nhân sự (HRM), báo cáo doanh thu |
| **Nhân viên (Staff)** | `staff@bachhoaxanh.vn` | `Staff@123456` | Bán hàng POS tại quầy thu ngân, quản lý kho chi nhánh |
| **Khách hàng (Customer)** | `customer@gmail.com` | `Customer@123456` | Mua sắm trực tuyến, thanh toán VNPay/VietQR, tích điểm thành viên, Gemini AI Chatbot |

> 💡 **Hướng dẫn tùy chỉnh Email & Tài khoản theo thông tin cá nhân của bạn:**
>
> 1. **Thay đổi tài khoản mẫu mặc định**: Mở file `WebBanHang-main/Data/DbSeeder.cs` và chỉnh sửa các biến `adminEmail`, `staffEmail`, `customerEmail` cùng mật khẩu theo thông tin cá nhân của bạn trước khi chạy lần đầu.
> 2. **Tạo tài khoản mới trực tiếp trên Web**: Bạn có thể bấm **Đăng ký** bằng email cá nhân trên giao diện website, sau đó dùng tài khoản `admin@bachhoaxanh.vn` để gán quyền (Role) cho tài khoản mới tại trang quản trị.
> 3. **Nhận Email hóa đơn & xác thực về hộp thư thật**: Điền Email và [Mật khẩu ứng dụng Gmail (App Password)](https://myaccount.google.com/apppasswords) của bạn vào mục `EmailSettings` trong file `appsettings.Local.json`.

---

## 👨‍💻 Tác giả (Author)
* **Họ và tên**: Võ Văn Phú
* **Email**: Anhmuoi270280@gmail.com
* **LinkedIn**: [www.linkedin.com/in/phú-võ-văn-078441429](https://www.linkedin.com/in/ph%C3%BA-v%C3%B5-v%C4%83n-078441429/)
* **Portfolio / GitHub**: https://github.com/AriesStorm247

---

## 📄 License
Dự án được phát triển phục vụ mục đích học tập, nghiên cứu và làm đồ án tốt nghiệp / đồ án môn học Thương Mại Điện Tử.
