# QueueLink - Online Queue Management Web App

**QueueLink** là nền tảng Web App giúp khách hàng lấy số thứ tự trực tuyến và theo dõi hàng chờ theo thời gian thực tại các địa điểm dịch vụ đông khách.

## Tech Stack

- **ASP.NET Core MVC** (.NET 10)
- **SQL Server** với **Entity Framework Core 9** (Code First)
- **ASP.NET Core Identity** (Authentication & Authorization)
- **SignalR** (Realtime update)
- **Bootstrap 5** (UI Framework)
- **QRCoder** (QR Code generation)
- **JavaScript** (SignalR client)

## Tính năng chính

- Khách hàng quét QR → lấy số online → theo dõi realtime
- Staff gọi số, đánh dấu phục vụ, hoàn tất, vắng mặt, hủy
- Admin quản lý địa điểm, hàng chờ, xem QR code, thống kê
- Realtime update bằng SignalR khi staff thay đổi trạng thái ticket

## Yêu cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB, SQL Express, hoặc SQL Server full)

## Cấu hình

### 1. Connection String

Mở file `appsettings.json`, chỉnh sửa connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=QueueLinkDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### 2. Chạy Migration

```bash
cd QueueLink
dotnet ef database update
```

> Nếu chưa có tool `dotnet ef`, cài đặt:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### 3. Chạy ứng dụng

```bash
dotnet run
```

Mở trình duyệt tại: **http://localhost:5180** (hoặc cổng hiển thị trên terminal)

## Tài khoản Demo

| Vai trò | Email | Mật khẩu |
|---------|-------|-----------|
| **Admin** | admin@queuelink.com | Admin@123 |
| **Staff** | staff@queuelink.com | Staff@123 |

## Luồng Demo

1. **Admin login** → `/Admin/Dashboard`
2. Vào **Queue Services** → chọn Details của một queue → xem và **tải mã QR**
3. **Staff login** → `/StaffQueue` → chọn một queue → bấm **"Gọi số tiếp theo"**
4. Mở trình duyệt mới (incognito), vào **http://localhost:5180/Queue**
5. Chọn queue → nhập thông tin → nhận **ticket A001**
6. Quan sát trạng thái realtime → Staff bấm Complete → trang khách cập nhật tự động

## Cấu trúc thư mục

```
QueueLink/
├── Controllers/          # MVC Controllers
├── Data/                 # ApplicationDbContext
├── Hubs/                 # SignalR Hub
├── Models/               # Entity classes
├── Services/             # Business logic + SeedData
├── ViewModels/           # View Models
├── Views/                # Razor Views
├── wwwroot/              # Static files (CSS, JS)
├── Program.cs            # App startup
└── appsettings.json      # Configuration
```

## Seed Data

Khi chạy lần đầu, hệ thống tự động tạo:

- **3 venues**: Dookki Buffet, QueueLink Photobooth, Safari Food Court
- **3 queues**: Buffet Table Queue (A), Photobooth Session (P), Take-away Counter (T)
- **2 tài khoản**: Admin + Staff

## QR Code

QR code trỏ đến link dạng: `https://yourdomain.com/Queue/Join/{queueId}`

Admin có thể xem và tải QR code tại trang **QueueService > Details**.

## SignalR Events

| Event | Mô tả |
|-------|--------|
| `QueueUpdated` | Staff Dashboard reload khi có ticket mới |
| `TicketUpdated` | Trang khách reload khi trạng thái thay đổi |
| `CurrentlyCallingChanged` | Cập nhật số đang gọi trên dashboard và trang khách |

## Chưa triển khai (MVP)

- GPS anti-spam
- POS integration
- Mobile app

---

**QueueLink MVP** - Built with ASP.NET Core MVC
