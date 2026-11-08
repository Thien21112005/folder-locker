# Progress — 2026-11-08

Chú thích: ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⏭️ bỏ qua/hoãn

## Đợt 1 — Sửa lỗi

| Giai đoạn | Nội dung | Trạng thái | Commit |
|-----------|----------|-----------|--------|
| P0 | Lập plan (bugfix, features, UI) | ✅ | `docs(plan)` |
| P1 | Sửa lỗi lõi mã hóa (I/O, temp file, recovery code) | ✅ | `fix(crypto)` |
| P2 | Sửa lỗi Controller / UI | ✅ | `fix(ui)` |
| P3 | Format V3 (PBKDF2 + AES-GCM + magic header) | ✅ | `feat(crypto)!` |
| P4 | Dọn warning | ✅ | `build` |
| P5 | Unit test | ✅ | `test` |
| P6 | Tài liệu | ✅ | `docs` |

## Đợt 2 — Giao diện + tính năng mở rộng

Bạn đã duyệt "làm hết". Quyết định cho các câu hỏi mở trong ui-plan:
- Gộp thành **một ô** "Mật khẩu hoặc mã khôi phục", bỏ nút chuyển "Quên mật khẩu?".
- Có **dark mode** theo theme Windows, làm cùng U4.
- Chưa bắt lưu mã khôi phục *trước* khi mã hóa. Thay vào đó, dialog sau khi mã hóa chặn đóng cho tới khi đã lưu hoặc đã xác nhận (U2).

| Giai đoạn | Nội dung | Trạng thái | Commit |
|-----------|----------|-----------|--------|
| P8 | U3, U4, U6, U7, U8, U9 + dark mode + F6: dựng lại cửa sổ chính (token màu, bàn phím, nhãn, báo lỗi tại chỗ, độ mạnh mật khẩu) | ✅ | `feat(ui)` |
| P9 | U2 + F3: dialog mã khôi phục an toàn, lưu ra .txt | ✅ | `feat(ui)` |
| P10 | U5 + F2: nhãn trạng thái, nút Hủy, kiểm tra mật khẩu trước khi chọn thư mục, nút "Mở thư mục" | ✅ | `feat` |
| P11 | F4 + F8: kiểm tra file .enc trước khi xóa bản gốc, xóa an toàn, lưu cài đặt | ✅ | `feat` |
| P12 | F5: xử lý hàng loạt | ✅ | `feat` |
| P13 | F1: đổi mật khẩu | ✅ | `feat` |
| P14 | F9: giữ thời gian tạo/sửa file | ✅ | `feat(crypto)` |
| P15 | F10: nhật ký hoạt động | 🔄 | |
| P16 | F7: tích hợp Windows Explorer + mở file từ dòng lệnh | ⬜ | |
| P17 | F11: CLI | ⬜ | |
| P18 | F12: GitHub Actions | ⬜ | |
| P19 | Polish UI, chụp kiểm tra, cập nhật tài liệu | ⬜ | |

## Nhật ký
- **2026-11-08** — Review code, build OK (0 lỗi, 23 warning). Chạy critique giao diện bằng impeccable. Lập plan.
- **P1** — Đã sửa #2, #4, #5, #13. Smoke test qua: round-trip file và folder, sai mật khẩu, mã khôi phục, ghi đè folder thay thế được thư mục cũ, file bị sửa không để lại file dở dang.
- **P2** — Đã sửa #6, #7, #9, #11, #12 và U1, U9 (một phần) từ ui-plan. Smoke test qua: chặn lưu `.enc` trong folder nguồn, mã khôi phục viết thường/có khoảng trắng vẫn mở được, xóa vào Thùng rác, phát hiện cần ghi đè. Build 0 warning.
- **P3** — Đã sửa #1, #3, #8, #10. Format V3: PBKDF2-SHA256 600k vòng + salt, AES-256-GCM theo chunk 1 MB, magic `FLCK`, key slot tách khỏi header bất biến (sẵn cho F1). Vẫn đọc được V1/V2. Smoke test qua: file 3 MB, file đúng 2 chunk, file 0 byte, sửa 1 byte / cắt mất chunk cuối → báo lỗi và không để lại file, folder có thư mục rỗng, giữ tên folder gốc dù đổi tên `.enc`, file `.enc` giả → không nhận diện.
- **P4** — Warning đã về 0 trong lúc làm P1–P3 (23 → 0). Bật `TreatWarningsAsErrors` để không phát sinh lại.
- **P5** — Project `FolderLocker.Tests` (xUnit), **45/45 pass**. Bao gồm: round-trip file (0 byte → 2 MB+, biên chunk), folder có thư mục rỗng và tên Unicode, sai mật khẩu, mã khôi phục nhiều kiểu nhập, sửa byte ở header/slot/dữ liệu/tag, cắt cụt, đảo chunk, V1/V2 cũ, tên độc hại trong payload, lưu đích trong nguồn, ghi đè thay thế folder, mã hóa lỗi không để lại file, nhận diện theo header.
- **P6** — README (song ngữ) mô tả đúng V3, cấu trúc thư mục mới, lệnh build/test. user_guide viết lại theo luồng thực tế, kèm bảng xử lý sự cố.
- **Kết thúc đợt 1** — 13/13 lỗi đã sửa, 45/45 test pass, build 0 warning.
- **P8** — Dựng lại cửa sổ chính:
  - Lớp `Theme` với token màu light/dark theo Windows (tự đổi khi người dùng đổi theme); mọi cặp chữ/nền ≥ 4.5:1.
  - Vùng thả focus được bằng Tab, chọn bằng Enter/Space/Ctrl+O, viền đổi màu khi kéo vào; thả lên label con cũng nhận.
  - Nhãn cố định trên từng ô nhập; một ô duy nhất "Mật khẩu hoặc mã khôi phục" khi giải mã.
  - Thanh độ mạnh mật khẩu (F6), tối thiểu 8 ký tự, cảnh báo khi mật khẩu yếu; báo "chưa khớp" ngay khi gõ; gợi ý Caps Lock; Alt+H hiện/ẩn mật khẩu.
  - Thông báo thống nhất bằng TaskDialog; nút "Mở thư mục" sau khi giải mã.
  - Font 4 bậc; icon Segoe Fluent (dự phòng MDL2); DPI PerMonitorV2; menu ⚙ có mục trợ giúp "Mã khôi phục là gì?" và "Phím tắt".
  - Đã render ảnh kiểm tra cả light/dark, mã hóa/giải mã. 59/59 test pass.
- **P9** — Dialog mã khôi phục mới (`RecoveryCodeDialog`):
  - Nói rõ "chỉ hiển thị MỘT LẦN" và cảnh báo không lưu chung chỗ với file .enc.
  - Font Consolas để phân biệt 0/O; nút "Lưu ra file .txt…" (F3), cảnh báo nếu lưu cùng thư mục với .enc.
  - "Xong" chỉ bật sau khi đã sao chép / đã lưu / đã tích xác nhận; Esc/Alt+F4 hỏi lại.
  - Hỗ trợ sẵn nhiều mã (cho batch). Có viền hệ thống và thanh tiêu đề tối theo theme. Đã render ảnh kiểm tra.
- **P10** — Nút chính chuyển thành "Hủy" khi đang chạy (F2). CancellationToken đi xuống tận vòng lặp chunk/từng file; hủy thì không để lại file nào. Nhãn trạng thái "Đang mã hóa… 42%"; tiến độ thư mục tính theo byte. Đóng app khi đang chạy → hỏi "Hủy và đóng". Giải mã: kiểm tra mật khẩu **trước** khi hỏi thư mục xuất; nhớ thư mục xuất lần trước (`AppSettings`). 67/67 test pass.
- **P11** — F4: trước khi xóa bản gốc, đọc lại toàn bộ file .enc và xác thực mọi chunk (`Verify`), nếu không đạt thì giữ nguyên bản gốc và báo lý do. F8: tùy chọn "Xóa vĩnh viễn" trong menu ⚙ (ghi đè ngẫu nhiên, đổi tên, rồi xóa); khi bật có cảnh báo về SSD/snapshot; nhãn checkbox đổi theo chế độ. 71/71 test pass.
- **P12** — F5 hàng loạt:
  - Kéo thả hoặc chọn nhiều file/thư mục (Ctrl trong hộp thoại). Trộn file .enc với file thường thì bị từ chối kèm giải thích.
  - Mã hóa: chọn một thư mục đích; trùng tên thì đánh số "(2)"; file đã tồn tại thì hỏi một lần. Một dialog hiện tất cả mã khôi phục; giữ cửa sổ mở cho tới khi người dùng đã thấy mã, kể cả khi bấm đóng giữa chừng.
  - Giải mã: kiểm tra mật khẩu với tất cả trước, rồi hỏi "tiếp tục với số còn lại". Hỏi ghi đè có ô "Áp dụng cho các mục còn lại".
  - Tóm tắt kết quả có phần "Xem chi tiết" lỗi và nút "Mở thư mục". Đã render ảnh trạng thái nhiều mục và đang chạy.
- **P13** — F1 đổi mật khẩu:
  - Mở khóa bằng mật khẩu hoặc mã khôi phục hiện tại, rồi bọc lại khóa file vào key slot mới (salt mới). Dữ liệu không bị mã hóa lại; ghi qua file tạm rồi thay thế.
  - Tùy chọn tạo mã khôi phục mới (mã cũ hết hiệu lực).
  - Dialog nói rõ giới hạn: bản sao cũ vẫn mở được bằng mật khẩu cũ. File V1/V2 được hướng dẫn giải mã rồi mã hóa lại.
  - Nút "Đổi mật khẩu của file này…" chỉ hiện ở chế độ giải mã một file. 75/75 test pass.
- **P14** — F9: bit cờ `0x02` trong header V3 báo payload có thời gian sửa/tạo (UTC) ngay sau tên gốc. Giải mã khôi phục thời gian cho file và thư mục gốc; file bên trong zip giữ thời gian sửa (độ chính xác 2 giây của zip). File V3 không có cờ này vẫn đọc bình thường. Verify và đổi mật khẩu đều xử lý được trường mới. 77/77 test pass.
