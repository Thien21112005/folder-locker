# Plan sửa lỗi FolderLocker — 2026-11-08

> Tiến độ cập nhật tại [progress.md](progress.md). Mỗi giai đoạn = 1 commit theo chuẩn Conventional Commits.

## Mục tiêu
Sửa toàn bộ 13 lỗi + phần tài liệu đã phát hiện trong buổi review, không làm mất khả năng giải mã các file `.enc` cũ (V1, V2).

## Bảng ánh xạ lỗi → giai đoạn

| # | Lỗi | Mức độ | Giai đoạn |
|---|-----|--------|-----------|
| 1 | SHA-256 trực tiếp làm khóa, không salt | 🔴 | P3 (format V3: PBKDF2 + salt) |
| 2 | Recovery code dùng `System.Random` | 🔴 | P1 |
| 3 | AES-CBC không có xác thực (MAC) | 🔴 | P3 (AES-GCM theo chunk) |
| 4 | File `.tmp` plaintext / file `.enc` dở dang bị bỏ lại | 🔴 | P1 |
| 5 | `Read` thiếu byte, `nameLen` không kiểm tra, path traversal | 🔴 | P1 |
| 6 | Kết quả xóa file gốc bị bỏ qua | 🔴 | P2 |
| 7 | Trạng thái "quên mật khẩu" không reset | 🟠 | P2 |
| 8 | Nhận diện chế độ chỉ dựa vào đuôi `.enc` | 🟠 | P3 (magic header `FLCK`) |
| 9 | Mã khôi phục không chuẩn hóa (trim/upper) | 🟠 | P2 |
| 10 | Mất thư mục rỗng khi mã hóa folder | 🟠 | P3 |
| 11 | Lưu `.enc` bên trong chính folder đang mã hóa | 🟠 | P2 |
| 12 | `async void` không try/catch, form không khóa khi đang chạy | 🟠 | P2 |
| 13 | Ghi đè folder = gộp thay vì thay thế | 🟠 | P1 |
| D | Tài liệu sai lệch, 23 warning | 🟡 | P4, P6 |

## Giai đoạn

### P0 — Lập plan
- Tạo `plan/2026-11-08/` (plan sửa lỗi, plan tính năng, plan giao diện, progress).
- Commit: `docs(plan): add 2026-11-08 bugfix, feature and UI plans`

### P1 — Sửa lỗi lõi mã hóa (chưa đổi format)
- `RecoveryCode.Generate()` dùng `RandomNumberGenerator.GetString`.
- Thay mọi `Read(...)` cần đủ byte bằng `ReadExactly(...)`.
- Kiểm tra `nameLen` (1..1024 byte) và làm sạch tên file (`SafeFileName`: chỉ lấy `Path.GetFileName`, chặn rỗng / `.` / `..` / ký tự không hợp lệ).
- Ghi ra **file/thư mục tạm** rồi mới `Move` vào vị trí cuối → lỗi giữa chừng không để lại file hỏng, không ghi đè mất dữ liệu cũ.
- `try/finally` xóa file zip tạm (plaintext) và file `.enc` dở dang khi mã hóa lỗi.
- Ghi đè folder: giải nén vào thư mục tạm, thành công mới xóa folder cũ và đổi tên → thay thế, không gộp (#13).
- Commit: `fix(crypto): harden decryption I/O and clean up partial output`

### P2 — Sửa lỗi Controller / UI
- Controller trả `OperationResult` (record) thay cho tuple + chuỗi ma thuật `"OVERWRITE_WARNING"`.
- Chặn đích lưu `.enc` nằm trong folder nguồn (#11).
- Kiểm tra kết quả `DeleteSource`, báo cho người dùng nếu xóa thất bại (#6).
- Chuẩn hóa mã khôi phục: `Trim`, bỏ khoảng trắng, `ToUpperInvariant` (#9). Ô nhập vẫn thử cả mật khẩu và mã khôi phục.
- Hàm `ApplyMode()` / `SetRecoveryMode()` duy nhất quản lý trạng thái form → hết lệch nhãn/ô mật khẩu (#7).
- `btnAction_Click` bọc try/catch/finally; khóa toàn bộ input (kéo thả, ô nhập, checkbox, link) khi đang chạy; chặn đóng form khi đang xử lý (#12).
- `Clipboard.SetText` bọc try/catch.
- Từ critique UI (U1, U9): checkbox xóa gốc mặc định **tắt**, xóa vào **Thùng rác**; cache bitmap icon con mắt (hết rò rỉ GDI); `AcceptButton`.
- Commit: `fix(ui): reset form state, normalize recovery code and guard async actions`

### P3 — Format V3 (breaking cho file mới, vẫn đọc được V1/V2)
- Header: magic `FLCK` + version `3` + flags + salt riêng cho mật khẩu và mã khôi phục + số vòng PBKDF2.
- KDF: `Rfc2898DeriveBytes.Pbkdf2` SHA-256, 600 000 vòng, salt 16 byte (#1).
- Khóa file 256-bit được bọc bằng AES-GCM (nonce 12 + tag 16), AAD = header.
- Dữ liệu: AES-GCM theo chunk 1 MB, nonce = prefix ngẫu nhiên + số thứ tự chunk, AAD = hash header + chỉ số chunk + cờ chunk cuối → phát hiện sửa/đảo/cắt cụt file (#3).
- Payload lưu cả tên gốc cho folder; thêm entry thư mục rỗng vào zip (#10).
- `LockerModel.DetectFormat()` đọc header để chọn chế độ; file không có magic chỉ coi là V1/V2 khi đuôi `.enc` (#8).
- Tách `LockerModel.cs` thành: `Crypto/KeyDerivation`, `Crypto/RecoveryCode`, `Crypto/ChunkedAesGcmStream`, `Formats/FormatV3`, `Formats/LegacyFormats`, `LockerModel` (facade).
- Commit: `feat(crypto)!: introduce authenticated V3 file format with PBKDF2`

### P4 — Dọn warning
- Sửa toàn bộ cảnh báo nullable / CA2022 (mục tiêu 0 warning).
- Commit: `refactor: resolve nullable and analyzer warnings`

### P5 — Unit test
- Project `FolderLocker.Tests` (xUnit): round-trip file/folder/file 0 byte/folder rỗng lồng nhau, sai mật khẩu, mã khôi phục (có khoảng trắng, chữ thường), file bị sửa 1 byte → báo lỗi, file bị cắt cụt, giải mã V1/V2 cũ, tên file độc hại trong payload, đích nằm trong nguồn.
- Commit: `test: add round-trip and tamper tests for crypto formats`

### P6 — Tài liệu
- README: mô tả V3 (PBKDF2 + AES-GCM), bỏ thư mục `Properties/` không tồn tại, sửa các khẳng định sai về SHA-256.
- user_guide: mô tả đúng luồng thực tế (tạo file `.enc`, nút "MÃ HÓA/GIẢI MÃ DỮ LIỆU", mã khôi phục).
- Commit: `docs: align README and user guide with actual behavior`

### P7 — Cải thiện giao diện (theo critique impeccable)
- Đợt này chỉ làm phần an toàn (U1, khóa input, rò rỉ GDI) trong P2. Các mục U2–U8 làm đợt sau, xem [ui-plan.md](ui-plan.md).
- Commit sau này: `feat(ui): ...` / `fix(ui): ...` theo từng nhóm.

## Ngoài phạm vi đợt này
- Các tính năng mở rộng → [features-plan.md](features-plan.md).
- Xóa an toàn (ghi đè dữ liệu trước khi xóa) → nằm trong features-plan vì phụ thuộc SSD/TRIM.
