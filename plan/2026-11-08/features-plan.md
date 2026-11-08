# Plan mở rộng tính năng FolderLocker — 2026-11-08

> Chỉ là plan, **chưa triển khai** trong đợt này. Làm sau khi xong [bugfix-plan.md](bugfix-plan.md) (cần format V3 làm nền).
> Ưu tiên: ⭐⭐⭐ cao · ⭐⭐ vừa · ⭐ thấp. Mỗi mục là một commit riêng `feat(...)`.

## F1 — Đổi mật khẩu không cần mã hóa lại ⭐⭐⭐
- **Ý tưởng:** V3 bọc `fileKey` bằng khóa dẫn xuất từ mật khẩu. Đổi mật khẩu thì chỉ cần giải bọc bằng mật khẩu cũ (hoặc mã khôi phục), rồi bọc lại bằng mật khẩu mới với salt mới, ghi đè **phần header**. Làm gần như tức thì, file vài GB cũng vậy.
- **Lưu ý:** AAD của chunk gắn với hash header. Cần tách thành 2 phần: "header bất biến" (magic, version, nonce prefix, chunk size) dùng làm AAD, và "key slots" (có thể thay đổi) không đưa vào AAD của dữ liệu. Phải chốt chuyện này ngay khi thiết kế V3 ở P3.
- **Đã chốt ở P3:** AAD của chunk chỉ dùng phần header bất biến, nên F1 làm được mà không phải đổi format.
- UI: menu "Đổi mật khẩu" khi chọn file `.enc`. Có tùy chọn tạo mã khôi phục mới.
- Ghi header an toàn: ghi ra file tạm (copy) rồi thay thế, hoặc ghi in-place có kiểm tra độ dài slot cố định.

## F2 — Nút Hủy khi đang xử lý ⭐⭐⭐
- Truyền `CancellationToken` từ UI → Controller → Model; kiểm tra token giữa các chunk / file.
- Khi hủy: xóa file tạm (đã có cơ chế ghi tạm từ P1).
- Nút "Hủy" thay chỗ nút Action khi đang chạy.

## F3 — Lưu / in mã khôi phục ⭐⭐⭐
- Trong dialog thành công: thêm nút "Lưu ra file .txt" (`SaveFileDialog`), nội dung gồm tên file `.enc`, ngày tạo và mã.
- Cảnh báo khi bấm "XONG" mà chưa copy hoặc lưu ("Bạn chưa lưu mã khôi phục. Đóng?").

## F4 — Kiểm tra thử trước khi xóa file gốc ⭐⭐⭐
- Sau khi mã hóa, mở lại file `.enc` và xác thực toàn bộ tag GCM (không ghi ra đĩa) trước khi xóa nguồn.
- Thêm tùy chọn trong UI; mặc định bật khi "Xóa file gốc" được chọn.

## F5 — Xử lý hàng loạt (batch) ⭐⭐
- Kéo thả nhiều file/folder cùng lúc → danh sách hàng đợi, mỗi mục có trạng thái riêng.
- Dùng chung một mật khẩu. Mỗi file có mã khôi phục riêng, hoặc tùy chọn dùng chung một mã. Xuất danh sách mã ra một file.

## F6 — Đo độ mạnh mật khẩu ⭐⭐
- Thanh độ mạnh dưới ô mật khẩu (độ dài, các nhóm ký tự, danh sách mật khẩu phổ biến nhúng sẵn).
- Cảnh báo (không chặn) khi mật khẩu yếu.

## F7 — Tích hợp Windows Explorer ⭐⭐
- `Program.Main(string[] args)`: nhận đường dẫn từ dòng lệnh → mở app với file đã chọn sẵn.
- Liên kết đuôi `.enc` (double-click để mở) và menu chuột phải "Mã hóa bằng FolderLocker". Đăng ký qua registry HKCU, không cần admin. Có nút bật/tắt trong phần cài đặt.

## F8 — Xóa an toàn file gốc ⭐
- Ghi đè ngẫu nhiên 1 lượt rồi mới xóa. Ghi rõ trong UI và tài liệu: **không đảm bảo trên SSD** (do wear-leveling/TRIM), chỉ là biện pháp giảm rủi ro.

## F9 — Giữ metadata ⭐
- Lưu thời gian sửa/tạo của file và folder vào payload (zip entry đã có `LastWriteTime`; với file đơn cần thêm trường). Khôi phục lại khi giải mã.

## F10 — Nhật ký hoạt động ⭐
- Log ra `%LOCALAPPDATA%\FolderLocker\logs` (không ghi mật khẩu/mã). Nút "Mở thư mục log" để báo lỗi.

## F11 — CLI / tự động hóa ⭐
- `FolderLocker.exe encrypt <path> --out <file>`, mật khẩu đọc từ stdin. Phục vụ script/backup.

## F12 — Phát hành ⭐⭐
- GitHub Actions: build + test mỗi lần push; publish single-file self-contained (`win-x64`) và đính kèm vào Release.
