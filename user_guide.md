# Hướng dẫn sử dụng FolderLocker

## 1. Giới thiệu chung
FolderLocker mã hóa một file hoặc cả một thư mục thành **một file `.enc` duy nhất**. Muốn mở lại dữ liệu thì phải có mật khẩu hoặc mã khôi phục. Ứng dụng chạy hoàn toàn trên máy bạn, không gửi dữ liệu đi đâu.

> FolderLocker **không khóa thư mục tại chỗ**. Nó tạo ra một file `.enc` mới; bản gốc vẫn còn nguyên, trừ khi bạn chọn xóa bản gốc.

## 2. Bắt đầu sử dụng
### 2.1. Yêu cầu hệ thống
- Windows 10 hoặc Windows 11.
- .NET 10 Desktop Runtime (Windows sẽ tự báo nếu máy chưa có).

### 2.2. Khởi chạy
1. Tải bản phát hành mới nhất và giải nén vào một thư mục bất kỳ.
2. Chạy `FolderLocker.exe`. Ứng dụng là dạng portable, không cần cài đặt.

## 3. Chức năng chính

### 3.1. Mã hóa file / thư mục
1. Kéo thả file hoặc thư mục vào khung giữa cửa sổ, hoặc bấm vào khung rồi chọn **Chọn File** / **Chọn Thư mục**.
2. Nhập **Mật khẩu** và **Xác nhận mật khẩu** (bấm biểu tượng con mắt để hiện hoặc ẩn).
3. *(Tùy chọn)* Tích **"Xóa bản gốc sau khi mã hóa (chuyển vào Thùng rác)"**. Tùy chọn này mặc định tắt.
4. Bấm **MÃ HÓA DỮ LIỆU**, rồi chọn nơi lưu file `.enc`. Không được lưu bên trong chính thư mục đang mã hóa.
5. Khi xong, cửa sổ **Mã khôi phục** hiện ra (dạng `FL-XXXX-XXXX-XXXX`):
   - Bấm **Copy Mã** rồi cất mã ở nơi an toàn, **tách biệt** với file `.enc`.
   - Mã này **chỉ hiển thị một lần**. Đây là cách duy nhất để mở file nếu bạn quên mật khẩu.
6. Nếu đã chọn xóa bản gốc, bản gốc được chuyển vào Thùng rác **sau khi** bạn đóng cửa sổ mã khôi phục.

### 3.2. Giải mã file `.enc`
1. Kéo thả hoặc chọn file `.enc`. Ứng dụng tự chuyển sang chế độ **GIẢI MÃ DỮ LIỆU** (nút màu xanh lá).
2. Nhập mật khẩu. Nếu quên mật khẩu, bấm **"Quên mật khẩu? Dùng mã khôi phục"** rồi nhập mã khôi phục. Mã gõ chữ thường hoặc có khoảng trắng vẫn được chấp nhận.
3. Bấm **GIẢI MÃ DỮ LIỆU** và chọn thư mục để xuất kết quả.
4. Nếu ở đó đã có file/thư mục trùng tên, ứng dụng sẽ hỏi trước khi ghi đè. Khi ghi đè thư mục, thư mục cũ được **thay thế hoàn toàn** chứ không gộp.

## 4. Khuyến nghị bảo mật
- **Mật khẩu mạnh:** tối thiểu 12 ký tự, kết hợp chữ hoa, chữ thường, số và ký tự đặc biệt. Có thể dùng một cụm từ dài dễ nhớ.
- **Mã khôi phục:** lưu ở trình quản lý mật khẩu hoặc giấy cất kỹ. Không lưu cùng chỗ với file `.enc`.
- **Sao lưu:** file `.enc` bị hỏng (ổ đĩa lỗi, copy dở dang...) thì không mở được nữa. Hãy giữ bản sao lưu cho dữ liệu quan trọng.
- **Thùng rác:** bản gốc bị xóa vẫn nằm trong Thùng rác. Hãy dọn Thùng rác nếu bạn muốn bản gốc không còn khôi phục được dễ dàng.

## 5. Xử lý sự cố
| Thông báo | Nguyên nhân / cách xử lý |
|-----------|---------------------------|
| "Sai mật khẩu hoặc mã khôi phục!" | Kiểm tra Caps Lock và bộ gõ tiếng Việt (Unikey/EVKey có thể tự đổi ký tự). Thử lại bằng mã khôi phục. |
| "Dữ liệu mã hóa bị hỏng hoặc đã bị chỉnh sửa." | File `.enc` bị lỗi khi sao chép hoặc bị sửa. Hãy dùng bản sao lưu khác. |
| "Đây không phải file do FolderLocker mã hóa." | File không đúng định dạng. Có thể chỉ là một file có đuôi `.enc` của phần mềm khác. |
| "File được tạo bởi phiên bản FolderLocker mới hơn." | Cập nhật ứng dụng lên bản mới nhất. |
| "Không thể lưu file mã hóa bên trong chính thư mục đang mã hóa." | Chọn nơi lưu nằm ngoài thư mục nguồn. |
| "Không có quyền truy cập file/thư mục." | File đang được chương trình khác mở, hoặc bạn không có quyền với thư mục đó. Đóng chương trình kia hoặc chọn thư mục khác. |
