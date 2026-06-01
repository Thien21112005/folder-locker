# Tài liệu Phát triển Dự án FolderLocker (Developer Guide)

## 1. Tổng quan Dự án
FolderLocker là một ứng dụng quản lý và bảo mật thư mục dành cho hệ điều hành Windows, được phát triển bằng ngôn ngữ C# trên nền tảng Windows Forms. Dự án áp dụng kiến trúc Model-View-Controller (MVC) nhằm đảm bảo sự phân tách trách nhiệm (Separation of Concerns), tính khả bảo trì và khả năng mở rộng trong tương lai.

## 2. Yêu cầu Hệ thống & Môi trường Phát triển
- **IDE**: Visual Studio 2022 hoặc phiên bản mới hơn.
- **Framework**: .NET Framework hoặc .NET Core / .NET 5+ (Vui lòng kiểm tra file `.csproj` để xác định chính xác Target Framework).
- **Thư viện bên thứ ba (Dependencies)**: Guna.UI2.WinForms (Sử dụng cho các thành phần giao diện người dùng hiện đại).

## 3. Cấu trúc Thư mục Dự án
Dự án được tổ chức thành các thành phần chính như sau:
- **Controllers/**: Chứa các lớp điều khiển, đóng vai trò trung gian xử lý luồng dữ liệu giữa Views và Models, đồng thời chịu trách nhiệm xác thực đầu vào.
- **Models/**: Bao gồm cấu trúc dữ liệu, các thuật toán mã hóa, thao tác cấp thấp với hệ thống tệp (File System) và quản lý trạng thái.
- **Views/**: Chứa các giao diện Windows Forms (ví dụ: MainForm.cs) và các logic hiển thị (Presentation Logic) liên quan.
- **ico/**: Thư mục chứa các tài nguyên đồ họa và biểu tượng (icons) của ứng dụng.
- **Properties/**: Chứa các thông tin cấu hình của Assembly và thiết lập ứng dụng.

## 4. Hướng dẫn Thiết lập và Triển khai
1. Clone kho lưu trữ (repository) về môi trường phát triển cục bộ.
2. Mở tệp giải pháp (Solution) hoặc tệp dự án `FolderLocker.csproj` bằng Visual Studio.
3. Khôi phục các gói NuGet (NuGet Package Restore). Cần đảm bảo thư viện `Guna.UI2.WinForms` được cài đặt và tham chiếu hợp lệ.
4. Xây dựng dự án (Build Solution - phím tắt Ctrl + Shift + B) để biên dịch và kiểm tra các phụ thuộc.
5. Thiết lập `FolderLocker` làm Startup Project.
6. Nhấn F5 hoặc chọn "Start" để khởi chạy ứng dụng trong chế độ gỡ lỗi (Debug mode).

## 5. Tiêu chuẩn Mã hóa (Coding Guidelines)
- **Kiến trúc phần mềm**: Tuân thủ nghiêm ngặt mô hình MVC. Tránh việc đưa logic nghiệp vụ (Business Logic) vào Views hoặc để rò rỉ logic giao diện vào Controllers.
- **Quy tắc Đặt tên (Naming Conventions)**:
  - Sử dụng định dạng `PascalCase` đối với Tên lớp (Classes), Phương thức (Methods) và Thuộc tính (Properties).
  - Sử dụng định dạng `camelCase` đối với các biến cục bộ (Local variables) và tham số truyền vào (Parameters).
  - Thêm tiền tố dấu gạch dưới `_` cho các trường nội bộ (Private fields), ví dụ: `_cryptoService`.
- **Thiết kế Giao diện**: Mọi thành phần UI bổ sung cần ưu tiên sử dụng bộ điều khiển của Guna UI để đồng bộ hóa phong cách thiết kế chuẩn doanh nghiệp. Tránh sử dụng các điều khiển WinForms mặc định nếu Guna có giải pháp thay thế.
- **Bảo mật**: Các thay đổi liên quan đến thuật toán mã hóa tại tầng `Models` bắt buộc phải qua quy trình đánh giá chéo (Peer Review). Tuyệt đối không hard-code các khóa bảo mật (Security Keys) hoặc vô hiệu hóa các bước kiểm tra an ninh trong quá trình gỡ lỗi.
- **Xử lý Ngoại lệ (Error Handling)**: Bắt buộc sử dụng các khối `try-catch` có cấu trúc. Không hiển thị trực tiếp Stack Trace của Exception lên giao diện người dùng; thay vào đó, hãy sử dụng các hộp thoại cảnh báo tiêu chuẩn.
