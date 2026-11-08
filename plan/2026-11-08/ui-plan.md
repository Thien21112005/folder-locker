# Plan cải thiện giao diện FolderLocker — 2026-11-08

> Nguồn: critique bằng `/impeccable critique` trên `FolderLocker/Views/MainForm.cs` + `MainForm.Designer.cs`.
> Đây là app WinForms, nên bộ detector tự động (chỉ đọc HTML) không áp dụng được. Kết quả dựa trên review thiết kế đọc từ mã nguồn, chưa chạy app để chụp màn hình.
> Chế độ bề mặt: **Operate** (công cụ). Ưu tiên: rõ ràng, an toàn, theo quy ước Windows; không cần cầu kỳ.

## Trạng thái triển khai (cập nhật cuối ngày)
| Hạng mục | Trạng thái | Giai đoạn |
|----------|-----------|-----------|
| U1 Xóa bản gốc an toàn | ✅ Thùng rác, mặc định tắt, kiểm tra .enc trước khi xóa | P2, P11 |
| U2 Mã khôi phục | ✅ `RecoveryCodeDialog` | P9 |
| U3 Bàn phím / trình đọc màn hình | ✅ vùng thả focus được, nhãn, AccessibleName, AcceptButton, TabIndex, Alt+H, Ctrl+O | P8 |
| U4 Tương phản + token màu + dark mode | ✅ `Theme` sáng/tối theo Windows | P8 |
| U5 Trạng thái khi chạy | ✅ nhãn trạng thái, nút Hủy, kiểm tra mật khẩu trước khi chọn thư mục, "Mở thư mục" | P8, P10 |
| U6 Hướng dẫn mật khẩu | ✅ độ mạnh, tối thiểu 8 ký tự, báo khớp tại chỗ, Caps Lock | P8 |
| U7 Kéo thả có phản hồi | ✅ viền đổi màu khi kéo vào; nhiều mục được xử lý luôn (batch) | P8, P12 |
| U8 Câu chữ + kiểu chữ | ✅ bỏ IN HOA/"tải lên", 4 bậc cỡ chữ, Segoe Fluent Icons | P8 |
| U9 Kỹ thuật | ✅ hết rò rỉ GDI, PerMonitorV2 | P2, P8 |
| Câu hỏi mở | Đã chốt: gộp một ô; có dark mode; chưa bắt lưu mã *trước* khi mã hóa | — |

Chưa chấm lại điểm sau khi sửa. Có thể chạy `/impeccable critique` để đo lại.

## Điểm lúc review ban đầu: 19/40 (Poor)

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

## Câu hỏi mở (cần bạn quyết)
1. Bỏ hẳn nút chuyển "Quên mật khẩu? Dùng mã khôi phục"? Model vốn đã thử cả mật khẩu lẫn mã khôi phục trên cùng một ô, nên chỉ cần một ô "Mật khẩu hoặc mã khôi phục".
2. Có muốn bắt người dùng lưu mã khôi phục **trước** khi bắt đầu mã hóa không?
3. Có làm dark mode theo theme Windows không?

## Thứ tự đề xuất
U1 (trong bugfix P2) → U2 → U3 → U4 → U5 → U6 → U7 → U8 → `/impeccable polish`.
