# Progress — 2026-11-08

Chú thích: ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⏭️ bỏ qua/hoãn

| Giai đoạn | Nội dung | Trạng thái | Commit |
|-----------|----------|-----------|--------|
| P0 | Lập plan (bugfix, features, UI) | ✅ | `docs(plan)` |
| P1 | Sửa lỗi lõi mã hóa (I/O, temp file, recovery code) | ✅ | `fix(crypto)` |
| P2 | Sửa lỗi Controller / UI | ✅ | `fix(ui)` |
| P3 | Format V3 (PBKDF2 + AES-GCM + magic header) | ✅ | `feat(crypto)!` |
| P4 | Dọn warning | ✅ | `build` |
| P5 | Unit test | ✅ | `test` |
| P6 | Tài liệu | 🔄 | |
| P7 | Cải thiện giao diện | ⬜ | |

## Nhật ký
- **2026-11-08** — Review code, build OK (0 lỗi, 23 warning). Chạy critique giao diện bằng impeccable. Lập plan.
- **P1** — Đã sửa #2, #4, #5, #13. Smoke test qua: round-trip file và folder, sai mật khẩu, mã khôi phục, ghi đè folder thay thế được thư mục cũ, file bị sửa không để lại file dở dang.
- **P2** — Đã sửa #6, #7, #9, #11, #12 và U1, U9 (một phần) từ ui-plan. Smoke test qua: chặn lưu `.enc` trong folder nguồn, mã khôi phục viết thường/có khoảng trắng vẫn mở được, xóa vào Thùng rác, phát hiện cần ghi đè. Build 0 warning.
- **P3** — Đã sửa #1, #3, #8, #10. Format V3: PBKDF2-SHA256 600k vòng + salt, AES-256-GCM theo chunk 1 MB, magic `FLCK`, key slot tách khỏi header bất biến (sẵn cho F1). Vẫn đọc được V1/V2. Smoke test qua: file 3 MB, file đúng 2 chunk, file 0 byte, sửa 1 byte / cắt mất chunk cuối → báo lỗi và không để lại file, folder có thư mục rỗng, giữ tên folder gốc dù đổi tên `.enc`, file `.enc` giả → không nhận diện.
- **P4** — Warning đã về 0 trong lúc làm P1–P3 (23 → 0). Bật `TreatWarningsAsErrors` để không phát sinh lại.
- **P5** — Project `FolderLocker.Tests` (xUnit), **45/45 pass**. Bao gồm: round-trip file (0 byte → 2 MB+, biên chunk), folder có thư mục rỗng và tên Unicode, sai mật khẩu, mã khôi phục nhiều kiểu nhập, sửa byte ở header/slot/dữ liệu/tag, cắt cụt, đảo chunk, V1/V2 cũ, tên độc hại trong payload, lưu đích trong nguồn, ghi đè thay thế folder, mã hóa lỗi không để lại file, nhận diện theo header.
