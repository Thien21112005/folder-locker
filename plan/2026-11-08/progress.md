# Progress — 2026-11-08

Chú thích: ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⏭️ bỏ qua/hoãn

| Giai đoạn | Nội dung | Trạng thái | Commit |
|-----------|----------|-----------|--------|
| P0 | Lập plan (bugfix, features, UI) | ✅ | `docs(plan)` |
| P1 | Sửa lỗi lõi mã hóa (I/O, temp file, recovery code) | ✅ | `fix(crypto)` |
| P2 | Sửa lỗi Controller / UI | 🔄 | |
| P3 | Format V3 (PBKDF2 + AES-GCM + magic header) | ⬜ | |
| P4 | Dọn warning | ⬜ | |
| P5 | Unit test | ⬜ | |
| P6 | Tài liệu | ⬜ | |
| P7 | Cải thiện giao diện | ⬜ | |

## Nhật ký
- **2026-11-08** — Review code, build OK (0 lỗi, 23 warning). Chạy critique giao diện bằng impeccable. Lập plan.
- **P1** — Đã sửa #2, #4, #5, #13. Smoke test qua: round-trip file và folder, sai mật khẩu, mã khôi phục, ghi đè folder thay thế được thư mục cũ, file bị sửa không để lại file dở dang.
