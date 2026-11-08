---
target: FolderLocker MainForm UI
total_score: 19
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 3
target_identity: "file:D:\\Tu-hoc\\project\\FolderLocker\\FolderLocker\\Views\\MainForm.cs"
target_fingerprint: "sha256:f5a873237850301accc63ce526b2a6afefd554252d23c67c2f3829ed0224d769"
target_path: "D:\\Tu-hoc\\project\\FolderLocker\\FolderLocker\\Views\\MainForm.cs"
timestamp: 2026-11-08T04-06-43Z
slug: folderlocker-views-mainform-cs
---
## Điểm hiện tại: 19/40 (Poor)

| # | Heuristic | Điểm | Vấn đề chính |
|---|-----------|------|--------------|
| 1 | Hiển thị trạng thái | 2 | Kéo file vào không có phản hồi; thanh tiến trình hiện ra trước cả hộp thoại Save; xóa file gốc diễn ra âm thầm |
| 2 | Khớp với thực tế | 3 | Có thuật ngữ khó hiểu: "Mã khôi phục hệ thống", "tải lên" (app chạy offline) |
| 3 | Kiểm soát & tự do | 1 | Không có nút Hủy; tùy chọn xóa vĩnh viễn bật sẵn; dialog mã khôi phục đóng được mà không cảnh báo |
| 4 | Nhất quán | 2 | Thông báo thành công khi mã hóa khác khi giải mã; dùng Label giả làm link; lẫn hoa/thường và Anh/Việt |
| 5 | Ngăn lỗi | 1 | Mặc định nguy hiểm; không có độ mạnh mật khẩu; chỉ báo "không khớp" sau khi bấm nút |
| 6 | Nhận biết thay vì nhớ | 2 | Ô nhập chỉ có placeholder, gõ vào là mất nhãn |
| 7 | Linh hoạt & hiệu quả | 2 | Enter không submit; không chọn file bằng bàn phím được; phải qua thêm một bước TaskDialog |
| 8 | Thẩm mỹ tối giản | 3 | Một cột, gọn; gradient và chữ IN HOA mang tính trang trí |
| 9 | Phục hồi lỗi | 2 | Hiện `ex.Message` thô; nhập sai mật khẩu thì phải chọn lại thư mục xuất mỗi lần thử |
| 10 | Trợ giúp | 1 | Không giải thích mã khôi phục, cũng không nói rõ xóa là vĩnh viễn |

**Điểm mạnh nên giữ:**
- Tự nhận diện chế độ mã hóa/giải mã theo file được chọn.
- Dialog mã khôi phục có ô lớn và nút Copy đổi trạng thái sau khi bấm.
- Dùng hộp thoại hệ thống (TaskDialog, Save/Folder dialog) và có hỏi trước khi ghi đè.

## Hạng mục (theo mức độ)

### U1 — [P0] Xóa file gốc: an toàn hơn — *làm trong P2 của bugfix*
- `chkDeleteOriginal` mặc định **tắt**; nhãn: "Xóa bản gốc sau khi mã hóa (chuyển vào Thùng rác)".
- Xóa qua **Thùng rác** (`Microsoft.VisualBasic.FileIO.FileSystem` + `RecycleOption.SendToRecycleBin`) thay vì xóa vĩnh viễn.
- Chỉ xóa **sau khi** người dùng xác nhận đã lưu mã khôi phục; báo rõ kết quả xóa (thành công/thất bại).
- Gợi ý lệnh: `/impeccable harden`

### U2 — [P1] Giữ an toàn cho mã khôi phục
- Nội dung: "Mã khôi phục — chỉ hiển thị **một lần**. Dùng mã này để mở file nếu quên mật khẩu."
- Nút "Lưu ra file .txt" (trùng với F3 trong features-plan).
- Nút "XONG" chỉ bật khi người dùng đã Copy, đã Lưu, hoặc đã tích "Tôi đã lưu mã".
- Chặn Esc/Alt+F4 bằng `FormClosing` có cảnh báo.
- Màu tiêu đề đạt độ tương phản ≥ 4.5:1 (xanh đậm `21,128,61`).
- Bố cục dùng `TableLayoutPanel` + `AutoScaleMode.Font` để không vỡ ở DPI 150% trở lên.
- Gợi ý lệnh: `/impeccable clarify`, `/impeccable harden`

### U3 — [P1] Dùng được bằng bàn phím và trình đọc màn hình
- Thêm nút "Chọn file/thư mục…" focus được trong vùng kéo thả, hoặc cho panel focus được và xử lý Enter/Space.
- `lblForgotPassword` → `LinkLabel`.
- Đặt `AccessibleName`/`AccessibleDescription` cho mọi input và nút; thêm nhãn nhỏ cố định phía trên ô nhập.
- `AcceptButton = btnAction`; sắp lại TabIndex: chọn file → mật khẩu → xác nhận → checkbox → nút chính → nút cửa sổ (hiện TabIndex 0 là nút Đóng).
- Nút con mắt dùng được bằng bàn phím (phím tắt Alt+H hoặc một nút riêng).
- Gợi ý lệnh: `/impeccable audit`

### U4 — [P1] Độ tương phản và màu dùng chung (token)
- Nút giải mã trắng trên `46,204,113` chỉ khoảng 2:1 → đổi sang khoảng `21,128,61`.
- Link `94,148,255` khoảng 3:1 → đổi sang `37,99,235`.
- Chữ phụ `Color.Gray` khoảng 3.9:1 → ≥ `90,90,90`.
- Gom khoảng 12 giá trị `FromArgb` rải rác vào một lớp `Theme` (static). Đây là nền để làm dark mode sau này (theo theme của Windows).
- Gợi ý lệnh: `/impeccable colorize`

### U5 — [P2] Trạng thái khi đang chạy
- Nhãn trạng thái dạng "Đang mã hóa… 42%", kèm nút Hủy (F2).
- Khóa toàn bộ input khi đang chạy; chặn đóng form *(phần khóa input/chặn đóng làm luôn trong P2 bugfix)*.
- Chỉ hiện thanh tiến trình **sau khi** đã chọn nơi lưu.
- Giải mã: kiểm tra mật khẩu với header **trước** khi hỏi thư mục xuất, hoặc nhớ thư mục lần trước.
- Sau khi giải mã, thông báo đường dẫn và có nút "Mở thư mục". Gộp hai kiểu thông báo thành công làm một.
- Gợi ý lệnh: `/impeccable harden`, `/impeccable polish`

### U6 — [P2] Hướng dẫn nhập mật khẩu
- Báo "không khớp" ngay tại chỗ (`TextChanged`).
- Độ dài tối thiểu; thanh độ mạnh mật khẩu (F6); gợi ý khi Caps Lock đang bật.
- Gợi ý lệnh: `/impeccable clarify`

### U7 — [P2] Kéo thả có phản hồi
- `DragEnter`/`DragLeave` đổi màu viền vùng thả.
- Thả nhiều file: báo "Chỉ xử lý mục đầu tiên" cho tới khi có batch (F5).

### U8 — [P3] Câu chữ và kiểu chữ
- "Bạn muốn **tải lên**…" → "Bạn muốn chọn…".
- Thống nhất cách viết: "Sao chép mã" / "Xong" / "Mã hóa" (bỏ trộn IN HOA và tiếng Anh). Bỏ chữ "V1/V2" trong thông báo lỗi.
- Bỏ in đậm giả trên "Segoe UI Semibold"; rút thang cỡ chữ còn khoảng 4 bậc (9 / 10 / 13 / 16).
- Icon: cân nhắc chuyển sang **Segoe Fluent Icons** (Win11), có dự phòng MDL2; thay icon con mắt vẽ tay bằng glyph từ font.

### U9 — [P3] Kỹ thuật
- Rò rỉ GDI: `DrawEyeIcon` tạo Bitmap mới mỗi lần bấm mà không dispose → cache 2 bitmap *(làm trong P2 bugfix)*.
- `ApplicationHighDpiMode` = `PerMonitorV2` trong csproj.
