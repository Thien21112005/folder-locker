# FolderLocker

> 🌐 **Language / Ngôn ngữ:** [English](#english) | [Tiếng Việt](#tiếng-việt)

---

<a name="english"></a>
# FolderLocker - Developer Guide & Project Documentation (English)

## 1. Project Overview
**FolderLocker** is a folder security and management application for Windows, developed in **C#** on the **Windows Forms** (.NET 10.0) framework. The project follows the **Model-View-Controller (MVC)** architectural pattern to ensure Separation of Concerns, maintainability, and scalability.

## 2. Security Architecture & Core Algorithms
FolderLocker employs a multi-layer security mechanism using industry-standard cryptographic technologies:

### 2.1. Standard Algorithms
- **AES (Advanced Encryption Standard)**: Serves as the core symmetric encryption algorithm for files and directories. Uses a 256-bit key combined with a cryptographically secure random Initialization Vector (IV), ensuring identical files produce completely different ciphertexts.
- **SHA-256 (Secure Hash Algorithm 256-bit)**: Applied to hash user passwords and recovery codes prior to key derivation. This prevents plaintext storage and defends against brute-force attacks.

### 2.2. Data Processing Mechanism
- **File Chunking**: For high performance and memory efficiency, large file streams are processed in 4MB buffer chunks (`BUFFER_SIZE = 4096 * 1024`). This avoids high RAM consumption and prevents `OutOfMemoryException` when encrypting multi-gigabyte files.
- **Archive Compression (`ZipArchive`)**: Before streaming encryption, folders and nested subdirectories are packaged and compressed using `ZipArchiveMode.Create`. This reduces output size and accelerates disk I/O operations.

### 2.3. Recovery Mechanism & Compatibility
- **Dual-Key Architecture (Format V2)**: Version 2 introduces an enhanced File Header format. The master decryption key (`FileKey`) is encrypted independently using two methods: one derived from the user's password and another from a random Recovery Code (`FL-XXXX-XXXX-XXXX`). Both encrypted keys are stored safely in the header metadata, enabling unlocking via either method without sacrificing entropy.
- **Backward Compatibility**: The decryption engine inspects the file header byte. If legacy format V1 (without recovery codes) is detected, it automatically routes the stream to the V1 decryption routine, ensuring access to historical encrypted data.

## 3. System Requirements & Development Environment
- **Operating System**: Windows 10 / Windows 11
- **IDE**: Visual Studio 2022 or later
- **Framework**: .NET 10.0 Windows (`net10.0-windows`)
- **Dependencies**: `Guna.UI2.WinForms` (Modern UI components, installed via NuGet)

## 4. Project Structure
```text
FolderLocker/
├── Controllers/       # Controller classes handling data flow and input validation
│   └── LockerController.cs
├── Models/            # Cryptographic algorithms, file/directory I/O, state management
│   └── LockerModel.cs
├── Views/             # Windows Forms UI and presentation logic
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   └── MainForm.resx
├── Properties/        # Assembly information and launch settings
└── ico/               # Application icons and graphic assets
```

## 5. Setup and Deployment Guide
1. Clone the repository to your local machine:
   ```bash
   git clone https://github.com/Thien21112005/folder-locker.git
   ```
2. Open `FolderLocker.slnx` or `FolderLocker/FolderLocker.csproj` in Visual Studio 2022+.
3. Restore NuGet packages to download `Guna.UI2.WinForms`.
4. Build the solution (<kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>B</kbd>).
5. Set `FolderLocker` as the Startup Project.
6. Press <kbd>F5</kbd> to run in Debug mode.

## 6. Coding Guidelines
- **MVC Pattern**: Strictly adhere to the MVC pattern. Do not mix business/crypto logic into Views, and keep UI logic out of Controllers and Models.
- **Naming Conventions**:
  - `PascalCase` for Class names, Methods, and Properties.
  - `camelCase` for Local variables and Parameters.
  - Prefix `_` for Private fields.
- **UI Design**: Use Guna UI controls for modern enterprise styling. Avoid default WinForms controls when a Guna equivalent exists.
- **Security**: Any modification to cryptographic flows in `LockerModel.cs` requires strict review. Never hardcode security keys.
- **Error Handling**: Use structured `try-catch` blocks. Avoid exposing raw exception stack traces in UI; log errors and present user-friendly alerts.

---

<a name="tiếng-việt"></a>
# Tài liệu Phát triển Dự án FolderLocker (Tiếng Việt)

## 1. Tổng quan Dự án
FolderLocker là một ứng dụng quản lý và bảo mật thư mục dành cho hệ điều hành Windows, được phát triển bằng ngôn ngữ C# trên nền tảng Windows Forms (.NET 10.0). Dự án áp dụng kiến trúc Model-View-Controller (MVC) nhằm đảm bảo sự phân tách trách nhiệm (Separation of Concerns), tính khả bảo trì và khả năng mở rộng trong tương lai.

## 2. Kiến trúc Bảo mật & Thuật toán Cốt lõi
Phần mềm sử dụng cơ chế bảo mật đa lớp (Multi-layer Security) với các công nghệ mã hóa tiêu chuẩn công nghiệp nhằm đảm bảo an toàn tuyệt đối cho dữ liệu:

### 2.1. Thuật toán Tiêu chuẩn
- **AES (Advanced Encryption Standard)**: Sử dụng làm thuật toán mã hóa đối xứng lõi cho toàn bộ dữ liệu tệp và thư mục. Thuật toán sử dụng khóa 256-bit kết hợp với khối sinh IV (Initialization Vector) ngẫu nhiên, đảm bảo ngay cả hai tệp giống hệt nhau cũng sẽ tạo ra các bản mã (ciphertext) hoàn toàn khác biệt.
- **SHA-256 (Secure Hash Algorithm 256-bit)**: Được áp dụng để băm (hash) mật khẩu người dùng và mã khôi phục trước khi đưa vào luồng mã hóa khóa. Việc này ngăn chặn tuyệt đối tình trạng lưu trữ mật khẩu thuần túy (plaintext) và chống lại các cuộc tấn công vét cạn (Brute-force attacks).

### 2.2. Cơ chế Xử lý Dữ liệu
- **File Chunking (Phân mảnh tệp)**: Để tối ưu hóa hiệu suất, hệ thống xử lý các luồng tệp tin lớn bằng cách chia nhỏ dữ liệu thành các đoạn (chunks) 4MB trong bộ đệm (`BUFFER_SIZE = 4096 * 1024`). Giải pháp này giúp kiểm soát chặt chẽ mức tiêu thụ RAM, ngăn ngừa triệt để lỗi tràn bộ nhớ (OutOfMemoryException) khi mã hóa các tệp dữ liệu có kích thước hàng Gigabyte.
- **Tích hợp nén cấu trúc (ZipArchive)**: Trước khi tiến hành mã hóa luồng, đối với các thư mục, toàn bộ hệ thống tệp con bên trong sẽ được đóng gói và nén lại thành một khối thống nhất bằng `ZipArchiveMode.Create`. Thao tác này làm giảm dung lượng file đầu ra và tăng tốc độ xử lý I/O trên ổ cứng.

### 2.3. Cơ chế Khôi phục & Tương thích
- **Kiến trúc Khóa Kép (Dual-Key Architecture - Format V2)**: Phiên bản V2 triển khai định dạng File Header mới. Khóa giải mã chính (FileKey) được mã hóa đồng thời bằng hai lớp độc lập: băm từ Mật khẩu người dùng và băm từ Mã khôi phục ngẫu nhiên (Recovery Code: `FL-XXXX-XXXX-XXXX`). Cả hai khóa mã hóa này được lưu trữ an toàn trong phần Header (metadata). Cơ chế này cho phép người dùng mở khóa bằng một trong hai cách mà không làm giảm Entropy của hệ thống.
- **Tương thích ngược (Backward Compatibility)**: Hàm giải mã được lập trình để tự động đọc File Header (byte đầu tiên). Nếu hệ thống phát hiện cấu trúc tệp được mã hóa từ phiên bản cũ (Legacy V1 - không có mã khôi phục), hệ thống sẽ tự động chuyển hướng luồng dữ liệu sang module giải mã V1 tương ứng, đảm bảo người dùng không mất quyền truy cập vào các dữ liệu lịch sử.

## 3. Yêu cầu Hệ thống & Môi trường Phát triển
- **Hệ điều hành**: Windows 10 hoặc Windows 11
- **IDE**: Visual Studio 2022 hoặc phiên bản mới hơn.
- **Framework**: .NET 10.0 Windows (`net10.0-windows`).
- **Thư viện bên thứ ba (Dependencies)**: Guna.UI2.WinForms (Sử dụng cho các thành phần giao diện người dùng hiện đại, yêu cầu khôi phục thông qua NuGet).

## 4. Cấu trúc Thư mục Dự án
Dự án được tổ chức thành các thành phần chính như sau:
- **Controllers/**: Chứa các lớp điều khiển, đóng vai trò trung gian xử lý luồng dữ liệu giữa Views và Models, đồng thời chịu trách nhiệm xác thực đầu vào và điều hướng lỗi.
- **Models/**: Bao gồm cấu trúc dữ liệu (`LockerModel.cs`), các thuật toán mã hóa AES/SHA256, thao tác cấp thấp với hệ thống tệp (File/Directory/Zip) và quản lý trạng thái.
- **Views/**: Chứa các giao diện Windows Forms (ví dụ: `MainForm.cs`) và các logic hiển thị (Presentation Logic) liên quan. Tuyệt đối không chứa logic mã hóa tại đây.
- **ico/**: Thư mục chứa các tài nguyên đồ họa của ứng dụng.
- **Properties/**: Chứa các thông tin cấu hình của Assembly và thiết lập tài nguyên khởi động.

## 5. Hướng dẫn Thiết lập và Triển khai
1. Clone kho lưu trữ (repository) về môi trường phát triển cục bộ:
   ```bash
   git clone https://github.com/Thien21112005/folder-locker.git
   ```
2. Mở tệp giải pháp (Solution `FolderLocker.slnx`) hoặc tệp dự án `FolderLocker.csproj` bằng Visual Studio.
3. Khôi phục các gói NuGet (NuGet Package Restore) để tải về thư viện `Guna.UI2.WinForms`.
4. Xây dựng dự án (Build Solution - phím tắt Ctrl + Shift + B) để biên dịch và phân tích các luồng phụ thuộc.
5. Thiết lập `FolderLocker` làm Startup Project.
6. Nhấn F5 để khởi chạy ứng dụng trong chế độ gỡ lỗi (Debug mode).

## 6. Tiêu chuẩn Mã hóa (Coding Guidelines)
- **Kiến trúc phần mềm**: Tuân thủ nghiêm ngặt mô hình MVC. Tránh việc đưa logic nghiệp vụ (Business Logic) vào Views hoặc để rò rỉ logic giao diện vào Controllers.
- **Quy tắc Đặt tên (Naming Conventions)**:
  - Sử dụng định dạng `PascalCase` đối với Tên lớp (Classes), Phương thức (Methods) và Thuộc tính (Properties).
  - Sử dụng định dạng `camelCase` đối với các biến cục bộ (Local variables) và tham số truyền vào (Parameters).
  - Thêm tiền tố dấu gạch dưới `_` cho các trường nội bộ (Private fields).
- **Thiết kế Giao diện**: Mọi thành phần UI bổ sung cần ưu tiên sử dụng bộ điều khiển của Guna UI để đồng bộ hóa phong cách thiết kế chuẩn doanh nghiệp. Hạn chế sử dụng các điều khiển WinForms mặc định nếu Guna có giải pháp thay thế.
- **Bảo mật**: Mọi thay đổi cấu trúc liên quan đến luồng mã hóa tại `LockerModel.cs` bắt buộc phải qua quy trình đánh giá chéo (Peer Review). Tuyệt đối không hard-code các khóa bảo mật (Security Keys).
- **Xử lý Ngoại lệ (Error Handling)**: Bắt buộc sử dụng các khối `try-catch` có cấu trúc rõ ràng. Không hiển thị trực tiếp Stack Trace của Exception lên giao diện người dùng; thay vào đó, hãy log lỗi vào hệ thống và trả về hộp thoại cảnh báo tiêu chuẩn.
