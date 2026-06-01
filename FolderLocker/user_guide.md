# Hướng dẫn sử dụng FolderLocker

## 1. Giới thiệu chung
FolderLocker là phần mềm tiện ích bảo mật cấp doanh nghiệp, được thiết kế để bảo vệ các thư mục và dữ liệu nhạy cảm trên hệ thống. Ứng dụng cung cấp khả năng khóa và quản lý truy cập thư mục thông qua cơ chế xác thực mật khẩu an toàn, đảm bảo tính riêng tư cho dữ liệu của người dùng.

## 2. Bắt đầu sử dụng
### 2.1. Yêu cầu hệ thống
- Hệ điều hành: Windows 10 hoặc Windows 11.
- Môi trường thực thi: .NET Runtime (Hệ thống sẽ tự động thông báo cài đặt nếu chưa có sẵn).

### 2.2. Khởi chạy ứng dụng
1. Tải bản phân phối (release) mới nhất của ứng dụng.
2. Giải nén gói phần mềm vào một thư mục trên máy tính.
3. Khởi chạy tệp `FolderLocker.exe`. Ứng dụng được thiết kế dưới dạng Portable, không yêu cầu các bước cài đặt phức tạp vào hệ thống.

## 3. Chức năng cốt lõi

### 3.1. Thao tác Khóa Thư Mục
1. Mở ứng dụng FolderLocker.
2. Nhấn vào tùy chọn chọn thư mục trên giao diện chính để mở hộp thoại duyệt tệp.
3. Điều hướng tới và chọn thư mục cần được bảo mật.
4. Nhập mật khẩu bảo vệ vào trường dữ liệu tương ứng. Người dùng có thể sử dụng tính năng hiển thị mật khẩu để đảm bảo tính chính xác.
5. Xác nhận lại mật khẩu (nếu hệ thống yêu cầu) và nhấn nút "Khóa".
6. Khi tiến trình hoàn tất, một thông báo thành công sẽ xuất hiện. Nếu hệ thống cung cấp Mã khôi phục (Recovery Code), vui lòng sao chép và lưu trữ mã này tại một vị trí an toàn.

### 3.2. Thao tác Mở Khóa Thư Mục
1. Mở ứng dụng FolderLocker.
2. Lựa chọn thư mục đang trong trạng thái khóa.
3. Nhập chính xác mật khẩu đã thiết lập trong quá trình khóa thư mục.
4. Nhấn nút "Mở khóa".
5. Sau khi quá trình xác thực hoàn tất và hợp lệ, thư mục sẽ được khôi phục về trạng thái truy cập bình thường.

## 4. Khuyến nghị Bảo mật
- **Chính sách Mật khẩu**: Khuyến nghị sử dụng mật khẩu mạnh, kết hợp giữa chữ cái (hoa và thường), chữ số và ký tự đặc biệt.
- **Quản lý Mã khôi phục**: Trong trường hợp tính năng mã khôi phục được bật, đây là phương tiện duy nhất để lấy lại quyền truy cập khi quên mật khẩu. Tuyệt đối không lưu trữ mã khôi phục cùng nơi với thư mục bị khóa.
- **Toàn vẹn Dữ liệu**: Hạn chế việc can thiệp thủ công, di chuyển hoặc đổi tên các tệp tin hệ thống do FolderLocker tạo ra bên trong thư mục đã khóa để tránh nguy cơ hỏng hóc dữ liệu.

## 5. Xử lý sự cố (Troubleshooting)
- **Truy cập bị từ chối khi mở khóa**: Vui lòng kiểm tra lại trạng thái của phím Caps Lock hoặc bộ gõ tiếng Việt, đảm bảo mật khẩu được nhập chính xác hoàn toàn.
- **Ứng dụng không phản hồi**: Đảm bảo bạn có đủ quyền quản trị viên (Administrator) đối với thư mục đang thao tác. Khởi động lại ứng dụng dưới quyền Administrator nếu cần thiết.
