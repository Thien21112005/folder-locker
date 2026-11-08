# FolderLocker

> 🌐 **Language / Ngôn ngữ:** [English](#english) | [Tiếng Việt](#tiếng-việt)

---

<a name="english"></a>
# FolderLocker - Developer Guide & Project Documentation (English)

## 1. Project Overview
**FolderLocker** is a file and folder encryption tool for Windows, written in **C#** with **Windows Forms** (.NET 10.0). It turns files or folders into encrypted `.enc` files and back. The project follows the **Model-View-Controller (MVC)** pattern.

### Features
- Encrypt / decrypt a file, a folder, or **many items at once** (drag & drop or multi-select).
- One field accepts **either the password or the recovery code** when decrypting.
- **Recovery code** shown once, with copy / save-to-.txt; the dialog cannot be dismissed by accident.
- **Change password** of an `.enc` file instantly (data is not re-encrypted), optionally rotating the recovery code.
- **Cancel** any running operation; partial files are always cleaned up.
- Originals can be removed after encryption **only after the new `.enc` has been re-read and verified**: to the Recycle Bin (default) or by **secure delete** (overwrite + delete).
- Password strength meter, inline confirmation check, Caps Lock hint.
- **Light / dark theme** following Windows, keyboard-operable (Tab, Enter, Ctrl+O, Alt+H), screen-reader labels, Per-Monitor DPI.
- Optional **Explorer integration**: right-click → "Encrypt with Folder Locker", double-click `.enc` to open.
- **Command-line interface** for scripts and backups.
- Original file/folder timestamps are preserved.
- Privacy-preserving activity log (no names, paths or secrets).

## 2. Security Architecture

### 2.1. File format V3 (current)
| Part | Content |
|------|---------|
| Immutable header (14 B) | magic `FLCK`, version `3`, flags (bit0 folder, bit1 timestamps), chunk size, 4-byte nonce prefix |
| Key slots | one slot for the password, one for the recovery code: PBKDF2 iterations, 16-byte salt, and the 256-bit file key wrapped with **AES-256-GCM** |
| Data | **AES-256-GCM** chunks of 1 MB, each with its own 16-byte authentication tag |

- **Key derivation:** `PBKDF2-HMAC-SHA256`, **600,000 iterations**, random salt per slot (OWASP 2023).
- **Authenticated encryption:** each chunk's nonce encodes its index, and its AAD binds the immutable header plus a "final chunk" flag. A modified byte, reordered chunks or a truncated file are all detected. Decryption then stops without leaving output behind.
- **Key slots are separate from the immutable header**, so changing the password only rewrites the slots and the data stays untouched.
- **Recovery code:** `FL-XXXX-XXXX-XXXX` from a CSPRNG (≈ 62 bits). Input is normalised (case, spaces, missing dashes).
- **Payload:** original name (sanitised on decrypt to block path traversal), timestamps, then the file bytes or a zip of the folder (empty sub-folders kept).

### 2.2. Safe output handling
- Results are written to a temporary sibling and moved into place only after every chunk has been authenticated.
- Overwriting a folder **replaces** it; the old one is removed only after the new one is in place.
- Cancelling or failing at any point removes temporary files, including the plaintext temp zip used for folders.

### 2.3. Backward compatibility
Files from older versions (V1, V2) are detected and can still be decrypted. Those formats used unsalted SHA-256 keys and unauthenticated AES-CBC. New files are always written as V3.

> ⚠️ Files created by this version (V3) cannot be opened by older FolderLocker builds.

## 3. Requirements
- **OS**: Windows 10 / 11
- **SDK**: .NET 10 (`net10.0-windows`), Visual Studio 2022 17.14+ or the .NET CLI
- **Dependencies**: `Guna.UI2.WinForms` (NuGet); tests use xUnit

## 4. Project Structure
```text
FolderLocker/
├── Program.cs                   # Entry: CLI verbs, single instance, crash handler
├── Cli/CliRunner.cs             # encrypt / decrypt / verify from the command line
├── Controllers/LockerController.cs  # Validation, friendly errors, logging
├── Models/
│   ├── LockerModel.cs           # Facade: Encrypt / Decrypt / Verify / ChangePassword / Detect
│   ├── Crypto/                  # PBKDF2, chunked AES-GCM streams
│   ├── Formats/                 # FormatV3 + read-only V1/V2
│   ├── PayloadIO.cs, PathSafety.cs, SecureDelete.cs
│   ├── RecoveryCode.cs, PasswordStrength.cs, OperationResult.cs
├── Services/                    # AppSettings, AppLog, ShellIntegration, SingleInstance
└── Views/
    ├── MainForm(.Designer).cs   # Main window
    ├── RecoveryCodeDialog.cs, ChangePasswordDialog.cs, Dialogs.cs
    ├── Controls/DropZonePanel.cs
    └── Theming/                 # Theme tokens (light/dark), control styles
FolderLocker.Tests/              # xUnit: formats, tampering, legacy, CLI, registry, log privacy…
plan/                            # Dated plans and progress logs
.github/workflows/ci.yml         # Build + test; release on v* tags
```

## 5. Build, Test, Run
```bash
git clone https://github.com/Thien21112005/folder-locker.git
dotnet build FolderLocker.slnx
dotnet test FolderLocker.slnx
dotnet run --project FolderLocker
```
Release: push a tag such as `v3.0.0`. CI publishes a self-contained single-file exe, a framework-dependent exe and `SHA256SUMS.txt`.

### Command line
```powershell
"my-password" | .\FolderLocker.exe encrypt "D:\Docs" --out E:\Backup        # prints <recovery code><TAB><file.enc>
$env:FOLDERLOCKER_PASSWORD = "my-password"
.\FolderLocker.exe decrypt E:\Backup\Docs.enc --out D:\Restore
.\FolderLocker.exe verify E:\Backup\Docs.enc
.\FolderLocker.exe help
```
Passwords are read only from `FOLDERLOCKER_PASSWORD` or stdin, never from arguments. Exit codes: 0 ok, 1 partial failure, 2 usage.

## 6. Coding Guidelines
- **MVC**: no crypto/file logic in Views; Views talk only to `LockerController`.
- **Naming**: `PascalCase` types/members, `camelCase` locals/parameters, `_` prefix for private fields.
- **UI**: colours only from `Theme` tokens (`Views/Theming`); every text/background pair must meet WCAG AA in both themes. Every input needs a visible label and an `AccessibleName`.
- **Security**: changes in `Models/Crypto` or `Models/Formats` must keep all tests green and add tests. Never hard-code keys. Changing the on-disk format requires a new version or a header flag.
- **Privacy**: never log passwords, recovery codes, file names or paths.
- **Errors**: Controllers return `OperationResult` with user-friendly messages and never throw to the View.
- **Warnings**: the build treats warnings as errors.

---

<a name="tiếng-việt"></a>
# Tài liệu Phát triển Dự án FolderLocker (Tiếng Việt)

## 1. Tổng quan
**FolderLocker** là công cụ mã hóa file và thư mục trên Windows, viết bằng **C#** trên **Windows Forms** (.NET 10.0). Ứng dụng biến file/thư mục thành file `.enc` đã mã hóa, và giải mã ngược lại. Dự án theo mô hình **MVC**.

### Tính năng
- Mã hóa / giải mã một file, một thư mục, hoặc **nhiều mục cùng lúc** (kéo thả hoặc chọn nhiều).
- Khi giải mã, **một ô duy nhất** nhận cả mật khẩu lẫn mã khôi phục.
- **Mã khôi phục** chỉ hiển thị một lần, có nút sao chép và lưu ra file .txt; dialog không thể bị đóng nhầm.
- **Đổi mật khẩu** file `.enc` ngay lập tức (không mã hóa lại dữ liệu), có thể tạo mã khôi phục mới.
- **Hủy** thao tác đang chạy; file dở dang luôn được dọn sạch.
- Chỉ xóa bản gốc **sau khi file `.enc` đã được đọc lại và kiểm tra**. Mặc định chuyển vào Thùng rác, hoặc chọn **xóa an toàn** (ghi đè rồi xóa).
- Thanh độ mạnh mật khẩu, báo "chưa khớp" ngay khi gõ, gợi ý Caps Lock.
- **Giao diện sáng / tối** theo Windows. Dùng được hoàn toàn bằng bàn phím (Tab, Enter, Ctrl+O, Alt+H), có nhãn cho trình đọc màn hình, hiển thị đúng ở mọi mức DPI.
- Tùy chọn **tích hợp Explorer**: chuột phải → "Mã hóa bằng Folder Locker", double-click file `.enc` để mở.
- **Giao diện dòng lệnh (CLI)** cho script và sao lưu tự động.
- Giữ nguyên thời gian tạo/sửa của file và thư mục gốc.
- Nhật ký hoạt động không ghi tên file, đường dẫn hay mật khẩu.

## 2. Kiến trúc Bảo mật

### 2.1. Định dạng file V3 (hiện tại)
| Phần | Nội dung |
|------|----------|
| Header bất biến (14 byte) | magic `FLCK`, version `3`, cờ (bit0 thư mục, bit1 có thời gian), kích thước chunk, nonce prefix 4 byte |
| Key slot | một slot cho mật khẩu, một slot cho mã khôi phục: số vòng PBKDF2, salt 16 byte, khóa file 256-bit bọc bằng **AES-256-GCM** |
| Dữ liệu | Các chunk **AES-256-GCM** 1 MB, mỗi chunk có tag xác thực 16 byte riêng |

- **Dẫn xuất khóa:** `PBKDF2-HMAC-SHA256`, **600.000 vòng**, salt ngẫu nhiên cho từng slot (theo OWASP 2023).
- **Mã hóa có xác thực:** nonce của mỗi chunk chứa số thứ tự chunk; AAD gắn với header bất biến và cờ "chunk cuối". Sửa một byte, đảo thứ tự chunk hay cắt cụt file đều bị phát hiện, và quá trình giải mã dừng lại mà không để lại file kết quả.
- **Key slot tách khỏi header bất biến**, nên đổi mật khẩu chỉ cần ghi lại phần slot; dữ liệu giữ nguyên.
- **Mã khôi phục:** dạng `FL-XXXX-XXXX-XXXX`, sinh bằng CSPRNG (≈ 62 bit). Mã nhập vào được chuẩn hóa: chấp nhận chữ thường, khoảng trắng, thiếu dấu gạch.
- **Payload:** tên gốc (được làm sạch khi giải mã để chặn path traversal), thời gian tạo/sửa, rồi đến nội dung file hoặc file zip của thư mục (giữ cả thư mục con rỗng).

### 2.2. Ghi kết quả an toàn
- Kết quả được ghi ra file tạm cùng thư mục, và chỉ được chuyển vào vị trí cuối khi mọi chunk đã được xác thực.
- Ghi đè thư mục là **thay thế** chứ không gộp. Thư mục cũ chỉ bị xóa khi thư mục mới đã vào chỗ.
- Hủy hoặc gặp lỗi ở bất kỳ bước nào thì file tạm đều được dọn, kể cả file zip tạm chứa dữ liệu đã giải mã của thư mục.

### 2.3. Tương thích ngược
File V1/V2 vẫn được nhận diện và giải mã được. Hai định dạng này dùng khóa SHA-256 không salt và AES-CBC không có xác thực. File mới luôn được tạo theo V3.

> ⚠️ File tạo bởi phiên bản này (V3) không mở được bằng các bản FolderLocker cũ.

## 3. Yêu cầu
- **Hệ điều hành**: Windows 10 / 11
- **SDK**: .NET 10 (`net10.0-windows`), Visual Studio 2022 17.14+ hoặc .NET CLI
- **Thư viện**: `Guna.UI2.WinForms` (NuGet); phần test dùng xUnit

## 4. Cấu trúc thư mục
Xem sơ đồ ở phần tiếng Anh. Tóm tắt:
- **Controllers/**: kiểm tra đầu vào, chuyển lỗi thành thông báo thân thiện, ghi nhật ký.
- **Models/**: định dạng file, mã hóa, ghi file an toàn, xóa an toàn, độ mạnh mật khẩu.
- **Services/**: cài đặt, nhật ký, tích hợp Explorer, chế độ một phiên bản duy nhất.
- **Cli/**: giao diện dòng lệnh.
- **Views/**: các cửa sổ, dialog, theme (token màu sáng/tối) và control tùy biến.
- **FolderLocker.Tests/**: unit test.
- **plan/**: kế hoạch và tiến độ theo ngày.
- **.github/workflows/**: CI và phát hành.

## 5. Build, test, chạy
```bash
git clone https://github.com/Thien21112005/folder-locker.git
dotnet build FolderLocker.slnx
dotnet test FolderLocker.slnx
dotnet run --project FolderLocker
```
Phát hành: push tag dạng `v3.0.0`. CI sẽ tạo file exe đầy đủ (không cần cài .NET), file exe nhỏ (cần .NET 10 Desktop Runtime), và `SHA256SUMS.txt`.

### Dòng lệnh
```powershell
"mat-khau" | .\FolderLocker.exe encrypt "D:\Tai lieu" --out E:\Backup     # in ra <mã khôi phục><TAB><file.enc>
$env:FOLDERLOCKER_PASSWORD = "mat-khau"
.\FolderLocker.exe decrypt E:\Backup\"Tai lieu.enc" --out D:\Khoi phuc
.\FolderLocker.exe verify E:\Backup\"Tai lieu.enc"
.\FolderLocker.exe help
```
Mật khẩu chỉ đọc từ biến môi trường `FOLDERLOCKER_PASSWORD` hoặc stdin, không bao giờ từ tham số. Mã thoát: 0 = thành công, 1 = có mục thất bại, 2 = sai cú pháp.

## 6. Tiêu chuẩn code
- **MVC**: View không chứa logic mã hóa/file và chỉ làm việc với `LockerController`.
- **Đặt tên**: `PascalCase` cho kiểu/thành viên, `camelCase` cho biến cục bộ/tham số, tiền tố `_` cho field private.
- **Giao diện**: màu chỉ lấy từ token `Theme` (`Views/Theming`); mọi cặp chữ/nền phải đạt WCAG AA ở cả hai theme. Mọi ô nhập đều có nhãn hiển thị và `AccessibleName`.
- **Bảo mật**: thay đổi trong `Models/Crypto` hoặc `Models/Formats` phải giữ toàn bộ test xanh và bổ sung test. Không hard-code khóa. Thay đổi định dạng trên đĩa phải tăng version hoặc thêm cờ header.
- **Quyền riêng tư**: không bao giờ ghi log mật khẩu, mã khôi phục, tên file hay đường dẫn.
- **Xử lý lỗi**: Controller trả về `OperationResult` với thông báo dễ hiểu, không ném exception lên View.
- **Warning**: build coi warning là lỗi.
