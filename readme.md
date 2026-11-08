# FolderLocker

> 🌐 **Language / Ngôn ngữ:** [English](#english) | [Tiếng Việt](#tiếng-việt)

---

<a name="english"></a>
# FolderLocker - Developer Guide & Project Documentation (English)

## 1. Project Overview
**FolderLocker** is a file and folder encryption tool for Windows, written in **C#** with **Windows Forms** (.NET 10.0). It turns a file or folder into a single encrypted `.enc` file and back. The project follows the **Model-View-Controller (MVC)** pattern.

## 2. Security Architecture

### 2.1. File format V3 (current)
| Part | Content |
|------|---------|
| Immutable header (14 B) | magic `FLCK`, version `3`, flags (folder bit), chunk size, 4-byte nonce prefix |
| Key slots | one slot for the password, one for the recovery code: PBKDF2 iterations, 16-byte salt, and the 256-bit file key wrapped with **AES-256-GCM** |
| Data | **AES-256-GCM** chunks of 1 MB, each with its own 16-byte authentication tag |

- **Key derivation:** `PBKDF2-HMAC-SHA256` with **600,000 iterations** and a random salt per slot (OWASP 2023). This makes brute-forcing passwords expensive. A random salt also means the same password never produces the same key twice.
- **Authenticated encryption:** each chunk's nonce encodes its index, and its AAD binds the immutable header plus a "final chunk" flag. Any modified byte, reordered chunk, or truncated file is detected, and decryption stops without leaving output files behind.
- **Recovery code:** `FL-XXXX-XXXX-XXXX`, generated with a CSPRNG (≈ 62 bits). The file key is wrapped once with the password and once with the recovery code, so either unlocks the file. Input is normalised: case, spaces and missing dashes are tolerated.
- **Payload:** the original file/folder name (sanitised on decrypt to block path traversal) followed by the file bytes, or a zip of the folder. Empty sub-folders are kept.

### 2.2. Safe output handling
- Results are written to a temporary sibling file/folder first. They are moved into place only after decryption fully succeeds and every chunk has been authenticated.
- Overwriting an existing folder **replaces** it. The old folder is removed only after the new one is in place.
- "Delete original after encrypting" sends the source to the **Recycle Bin** and is off by default.

### 2.3. Backward compatibility
Files from older versions (V1, V2) are detected automatically and can still be decrypted. Those formats use unsalted SHA-256 keys and AES-CBC without authentication. New files are always written as V3.

> ⚠️ Files created by this version (V3) cannot be opened by older FolderLocker builds.

## 3. System Requirements & Development Environment
- **Operating System**: Windows 10 / Windows 11
- **IDE**: Visual Studio 2022 (17.14+) or later, or the .NET CLI
- **SDK**: .NET 10 (`net10.0-windows`)
- **Dependencies**: `Guna.UI2.WinForms` (NuGet); tests use xUnit

## 4. Project Structure
```text
FolderLocker/
├── Controllers/
│   └── LockerController.cs      # Input validation, friendly error messages
├── Models/
│   ├── LockerModel.cs           # Facade: Encrypt / Decrypt / DetectFormat / Recycle Bin
│   ├── Crypto/
│   │   ├── KeyDerivation.cs     # PBKDF2 (+ legacy SHA-256 for V1/V2)
│   │   └── AesGcmChunkStreams.cs# Chunked AES-GCM writer/reader streams
│   ├── Formats/
│   │   ├── FormatV3.cs          # Current file format
│   │   └── LegacyFormats.cs     # Read-only V1/V2 support
│   ├── PayloadIO.cs             # Names, zip, atomic writes
│   ├── PathSafety.cs            # File-name sanitising, temp paths, commit
│   ├── RecoveryCode.cs          # Generate / normalise recovery codes
│   └── OperationResult.cs       # Result record returned to the View
├── Views/                       # Windows Forms UI
└── ico/
FolderLocker.Tests/              # xUnit tests (round-trip, tampering, legacy)
plan/                            # Dated plans and progress logs
```

## 5. Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/Thien21112005/folder-locker.git
   ```
2. Open `FolderLocker.slnx` in Visual Studio, or use the CLI:
   ```bash
   dotnet build FolderLocker.slnx
   dotnet test FolderLocker.slnx
   dotnet run --project FolderLocker
   ```

## 6. Coding Guidelines
- **MVC**: no crypto or file logic in Views; no UI code in Models/Controllers.
- **Naming**: `PascalCase` for types/methods/properties, `camelCase` for locals/parameters, `_` prefix for private fields.
- **UI**: prefer Guna UI controls for a consistent look.
- **Security**: any change to `Models/Crypto` or `Models/Formats` must keep all tests green and add tests for the new behaviour. Never hard-code keys. Changing the on-disk format requires a new version number.
- **Errors**: Controllers return `OperationResult` with user-friendly messages; never show raw stack traces.
- **Warnings**: the build treats warnings as errors.

---

<a name="tiếng-việt"></a>
# Tài liệu Phát triển Dự án FolderLocker (Tiếng Việt)

## 1. Tổng quan
**FolderLocker** là công cụ mã hóa file và thư mục trên Windows, viết bằng **C#** trên **Windows Forms** (.NET 10.0). Ứng dụng biến một file/thư mục thành một file `.enc` đã mã hóa, và giải mã ngược lại. Dự án theo mô hình **MVC**.

## 2. Kiến trúc Bảo mật

### 2.1. Định dạng file V3 (hiện tại)
| Phần | Nội dung |
|------|----------|
| Header bất biến (14 byte) | magic `FLCK`, version `3`, cờ (folder), kích thước chunk, nonce prefix 4 byte |
| Key slot | một slot cho mật khẩu, một slot cho mã khôi phục: số vòng PBKDF2, salt 16 byte, khóa file 256-bit được bọc bằng **AES-256-GCM** |
| Dữ liệu | Các chunk **AES-256-GCM** 1 MB, mỗi chunk có tag xác thực 16 byte riêng |

- **Dẫn xuất khóa:** `PBKDF2-HMAC-SHA256`, **600.000 vòng**, salt ngẫu nhiên cho từng slot (theo OWASP 2023). Cách này làm việc dò mật khẩu tốn kém hơn nhiều. Nhờ salt ngẫu nhiên, cùng một mật khẩu không bao giờ tạo ra cùng một khóa.
- **Mã hóa có xác thực:** nonce của mỗi chunk chứa số thứ tự chunk; AAD gắn với header bất biến và cờ "chunk cuối". Chỉ cần sửa một byte, đảo thứ tự chunk hay cắt cụt file là bị phát hiện ngay. Khi đó quá trình giải mã dừng lại và không để lại file kết quả nào.
- **Mã khôi phục:** dạng `FL-XXXX-XXXX-XXXX`, sinh bằng CSPRNG (≈ 62 bit). Khóa file được bọc hai lần, một lần bằng mật khẩu và một lần bằng mã khôi phục, nên dùng cái nào cũng mở được. Mã nhập vào được chuẩn hóa: chấp nhận chữ thường, khoảng trắng, thiếu dấu gạch.
- **Payload:** tên gốc của file/thư mục, rồi đến nội dung file hoặc file zip của thư mục. Tên được làm sạch khi giải mã để chặn path traversal. Thư mục con rỗng vẫn được giữ.

### 2.2. Ghi kết quả an toàn
- Kết quả luôn được ghi ra file/thư mục tạm trước. Chỉ khi giải mã thành công và toàn bộ chunk đã được xác thực thì mới chuyển vào vị trí cuối.
- Ghi đè thư mục đã tồn tại là **thay thế** chứ không gộp. Thư mục cũ chỉ bị xóa khi thư mục mới đã vào chỗ.
- Tùy chọn "Xóa bản gốc sau khi mã hóa" chuyển bản gốc vào **Thùng rác** và mặc định tắt.

### 2.3. Tương thích ngược
File tạo bởi phiên bản cũ (V1, V2) được nhận diện tự động và vẫn giải mã được. Hai định dạng này dùng khóa SHA-256 không salt và AES-CBC không có xác thực. File mới luôn được tạo theo V3.

> ⚠️ File tạo bởi phiên bản này (V3) không mở được bằng các bản FolderLocker cũ.

## 3. Yêu cầu Hệ thống & Môi trường Phát triển
- **Hệ điều hành**: Windows 10 hoặc Windows 11
- **IDE**: Visual Studio 2022 (17.14+) trở lên, hoặc .NET CLI
- **SDK**: .NET 10 (`net10.0-windows`)
- **Thư viện**: `Guna.UI2.WinForms` (NuGet); phần test dùng xUnit

## 4. Cấu trúc Thư mục
- **Controllers/**: kiểm tra đầu vào, chuyển lỗi thành thông báo thân thiện, trả về `OperationResult`.
- **Models/**:
  - `LockerModel.cs`: facade.
  - `Crypto/`: PBKDF2 và các stream AES-GCM theo chunk.
  - `Formats/`: V3, và V1/V2 chỉ để đọc.
  - `PayloadIO.cs`, `PathSafety.cs`, `RecoveryCode.cs`.
- **Views/**: giao diện Windows Forms. Tuyệt đối không chứa logic mã hóa.
- **FolderLocker.Tests/**: unit test xUnit (round-trip, file bị sửa/cắt cụt, định dạng cũ, đường dẫn độc hại...).
- **plan/**: kế hoạch và tiến độ, chia theo ngày.

## 5. Thiết lập
1. Clone repository:
   ```bash
   git clone https://github.com/Thien21112005/folder-locker.git
   ```
2. Mở `FolderLocker.slnx` bằng Visual Studio, hoặc dùng CLI:
   ```bash
   dotnet build FolderLocker.slnx
   dotnet test FolderLocker.slnx
   dotnet run --project FolderLocker
   ```

## 6. Tiêu chuẩn Code
- **MVC**: không đặt logic mã hóa/file trong Views; không đặt code UI trong Models/Controllers.
- **Đặt tên**: `PascalCase` cho lớp/phương thức/thuộc tính, `camelCase` cho biến cục bộ/tham số, tiền tố `_` cho field private.
- **Giao diện**: ưu tiên control của Guna UI để đồng bộ phong cách.
- **Bảo mật**: mọi thay đổi trong `Models/Crypto` hoặc `Models/Formats` phải giữ toàn bộ test xanh và bổ sung test cho hành vi mới. Không hard-code khóa. Thay đổi định dạng file trên đĩa phải tăng số version.
- **Xử lý lỗi**: Controller trả về `OperationResult` với thông báo dễ hiểu; không hiển thị stack trace.
- **Warning**: build coi warning là lỗi.
