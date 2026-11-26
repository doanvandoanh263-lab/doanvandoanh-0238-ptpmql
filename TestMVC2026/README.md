# TestMVC2026

Ứng dụng ASP.NET Core MVC (.NET 10), SQLite, Bootstrap 5 và ASP.NET Core Identity.

## Chạy ứng dụng

```powershell
cd TestMVC2026
dotnet run
```

Khi chạy lần đầu, ứng dụng tự áp dụng migration, tạo dữ liệu mẫu khoa và khởi tạo role `Admin`, `User`. Trong môi trường Development, tài khoản quản trị mặc định là:

- Email: `admin@testmvc2026.local`
- Mật khẩu: `ChangeMe123!Demo`

Đổi mật khẩu mặc định trước khi dùng dữ liệu thật. Môi trường không phải Development phải cấu hình `SeedAdmin__Email` và `SeedAdmin__Password`; ứng dụng sẽ dừng nếu thiếu để tránh khởi chạy mà không có tài khoản quản trị.

Ghi chú phát triển: nếu SMTP chưa được cấu hình, ứng dụng sẽ bỏ qua gửi email trong `Development` và không yêu cầu xác thực email trong môi trường đó, để người dùng có thể thử chức năng đăng ký/đăng nhập ở local mà không cần máy chủ mail. Trong môi trường khác, phải cấu hình `Email:*` trước khi kích hoạt gửi email xác thực, đặt lại mật khẩu và resend confirmation.

## Tính năng

- CRUD sinh viên và khoa bằng Bootstrap modal, Ajax, chống CSRF, tìm kiếm và phân trang.
- Đăng ký/đăng nhập Identity, yêu cầu xác thực email, gửi lại email xác thực và đặt lại mật khẩu.
- Quản lý role, gán role cho người dùng và cấp quyền theo từng controller action. Role `Admin` luôn có toàn quyền.
- Nhập Excel `.xlsx` cho các bảng ứng dụng `Khoa`, `SinhVien`, `RolePermissions`. Chọn ghi bổ sung (từ chối khóa trùng) hoặc thay thế (thay các bảng này trong một transaction); dữ liệu Identity không bị ảnh hưởng. Role cần có sẵn trước khi nhập quyền.

## Cấu hình email

SMTP được đọc từ cấu hình `Email` trong `appsettings.json`. Không lưu mật khẩu SMTP trong source control. Có thể đặt bằng User Secrets khi phát triển:

```powershell
dotnet user-secrets set "Email:FromAddress" "noreply@example.com"
dotnet user-secrets set "Email:Smtp:Host" "smtp.example.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Username" "smtp-user"
dotnet user-secrets set "Email:Smtp:Password" "<smtp-password>"
```

Mặc định dùng STARTTLS trên cổng 587; đặt `Email:Smtp:UseSsl=true` nếu nhà cung cấp yêu cầu TLS trực tiếp (thường cổng 465). Môi trường triển khai có thể dùng các biến môi trường tương ứng, ví dụ `Email__Smtp__Password`.

## Tệp Excel

Tải tệp mẫu từ màn hình **Nhập Excel**. Workbook cần có ba trang:

- `Khoa`: `Code`, `Name`
- `SinhVien`: `StudentCode`, `FullName`, `BirthYear`, `Email`, `Phone`, `FacultyCode`
- `RolePermissions`: `RoleName`, `Controller`, `Action`

Các mã phải duy nhất trong tệp; `FacultyCode` phải tham chiếu tới khoa trong tệp hoặc trong cơ sở dữ liệu (ở chế độ ghi bổ sung). Các quyền phải tham chiếu tới role và action tồn tại. Tệp giới hạn 10 MB.
