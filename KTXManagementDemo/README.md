# Hệ thống quản lý Ký túc xá có tích hợp AI (Demo)

Bản demo ASP.NET Core MVC (.NET 8) minh hoạ đầy đủ 7 Use Case của đồ án "Hệ thống quản
lý ký túc xá có tích hợp AI", dùng EF Core In-Memory Database (không cần cài SQL Server)
và có sẵn tính năng **Chatbot AI hỏi-đáp** tích hợp trong ứng dụng.

## 1. Yêu cầu môi trường
- .NET SDK 8.0 trở lên: https://dotnet.microsoft.com/download
- (Tuỳ chọn) Visual Studio 2022 17.8+ hoặc VS Code với extension C#

## 2. Cách chạy dự án

### Cách 1: Dùng dòng lệnh (khuyên dùng, nhanh nhất)
```bash
cd KTXManagement.Demo
dotnet restore
dotnet run
```
Sau khi chạy, mở trình duyệt vào địa chỉ được in ra ở console, thường là:
`https://localhost:5001` hoặc `http://localhost:5000`

### Cách 2: Dùng Visual Studio
1. Mở file `KTXManagement.Demo.csproj` bằng Visual Studio.
2. Nhấn `F5` (hoặc Ctrl+F5 để chạy không debug).

Dữ liệu mẫu (tòa nhà, phòng, giường, sinh viên, phiếu phí, phản ánh...) được tự động
seed vào EF Core In-Memory Database ngay khi ứng dụng khởi động (xem `Data/SeedData.cs`).
Vì là In-Memory Database, dữ liệu sẽ **mất khi dừng ứng dụng** và được seed lại từ đầu
mỗi lần `dotnet run`.

## 3. Tài khoản demo (mật khẩu chung: `123456`)

| Tài khoản | Vai trò       | Ghi chú                                   |
|-----------|---------------|--------------------------------------------|
| quanly    | Quản lý KTX   | Xếp phòng, hạ tầng, thống kê, phản ánh      |
| ketoan    | Kế toán       | Tạo phiếu phí, ghi nhận thanh toán          |
| sv001     | Sinh viên     | Đang ở phòng A101                          |
| sv002     | Sinh viên     | Đang ở phòng A101, đang nợ tiền quá hạn     |
| sv003     | Sinh viên     | Đã gửi yêu cầu đăng ký ở, đang chờ xếp phòng|
| sv004     | Sinh viên     | Đang ở phòng B101, nợ một phần phí phòng    |
| sv005     | Sinh viên     | **Tài khoản minh hoạ bị khóa** (UC001-A3, đăng nhập sai quá số lần) |

## 4. Cấu hình AI (tuỳ chọn)

Mặc định `appsettings.json` để trống `AIConfig:ApiKey`, khi đó `AIService` sẽ tự dùng
bộ trả lời **rule-based** dựa trên dữ liệu đã seed — ứng dụng chạy được ngay, không cần
Internet hay API key.

Nếu muốn Chatbot trả lời tự nhiên hơn bằng Claude thật, điền API key vào
`appsettings.json`:
```json
"AIConfig": {
  "Provider": "Anthropic",
  "ApiKey": "sk-ant-xxxxxxxx",
  "Model": "claude-sonnet-4-6",
  "ApiUrl": "https://api.anthropic.com/v1/messages"
}
```
Hoặc set biến môi trường tương ứng (`AIConfig__ApiKey`) thay vì sửa file.

## 5. Sơ đồ 7 Use Case ↔ chức năng trong mã nguồn

| UC    | Nội dung                                              | Controller             |
|-------|--------------------------------------------------------|-------------------------|
| UC001 | Đăng nhập và phân quyền (kiểm tra khóa tài khoản trước, khóa sau 5 lần sai) | `AccountController` |
| UC002 | Quản lý hạ tầng KTX (tòa/tầng/phòng/giường)            | `HaTangController`      |
| UC003 | Quản lý hồ sơ sinh viên nội trú                        | `SinhVienController`    |
| UC004 | Đăng ký ở, xếp phòng (AI gợi ý), chuyển phòng, trả phòng (kiểm tra công nợ) | `DangKyOController` |
| UC005 | Theo dõi phí phòng và công nợ                          | `PhiCongNoController`   |
| UC006 | Quản lý phản ánh/sự cố và vi phạm nội quy               | `PhanAnhController`     |
| UC007 | Thống kê công suất/công nợ/phản ánh + tóm tắt bằng AI   | `ThongKeController`     |
| —     | Chatbot AI hỏi-đáp (mọi vai trò)                        | `ChatController` + `Services/AIService.cs` |

## 6. Ghi chú
- Mật khẩu được băm bằng SHA256 đơn giản (`Services/PasswordHasher.cs`) — chỉ dùng cho demo,
  không phù hợp cho môi trường production (nên dùng ASP.NET Core Identity/BCrypt).
- Vì dùng EF Core In-Memory, mọi thao tác tạo/sửa dữ liệu chỉ tồn tại trong phiên chạy hiện tại.
