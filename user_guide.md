# Hướng dẫn sử dụng FolderLocker

## 1. Giới thiệu chung
FolderLocker mã hóa file hoặc thư mục thành **file `.enc`**. Muốn mở lại phải có mật khẩu hoặc mã khôi phục. Ứng dụng chạy hoàn toàn trên máy bạn và không gửi dữ liệu đi đâu.

> FolderLocker **không khóa thư mục tại chỗ**. Nó tạo ra file `.enc` mới; bản gốc vẫn còn nguyên, trừ khi bạn chọn xóa bản gốc.

## 2. Bắt đầu
- Windows 10 hoặc Windows 11.
- Tải bản phát hành, giải nén rồi chạy `FolderLocker.exe`. Không cần cài đặt.
  - Bản `win-x64` chạy được ngay.
  - Bản `framework-dependent` nhẹ hơn nhưng cần cài .NET 10 Desktop Runtime.
- Giao diện tự chuyển sáng/tối theo cài đặt của Windows.

## 3. Mã hóa
1. Kéo thả **một hoặc nhiều** file/thư mục vào khung, hoặc nhấn vào khung (hay **Ctrl+O**) rồi chọn. Giữ Ctrl để chọn nhiều mục.
2. Nhập **Mật khẩu** (tối thiểu 8 ký tự) và **Xác nhận mật khẩu**.
   - Thanh màu dưới ô cho biết độ mạnh của mật khẩu.
   - Ô xác nhận báo ngay nếu chưa khớp.
   - **Alt+H** để hiện/ẩn mật khẩu.
3. *(Tùy chọn)* Tích **"Xóa bản gốc sau khi mã hóa"**. Bản gốc chỉ bị xóa sau khi file `.enc` đã được đọc lại và kiểm tra thành công.
4. Bấm **Mã hóa dữ liệu** (hoặc Enter) rồi chọn nơi lưu.
   - Một mục: chọn tên file `.enc`.
   - Nhiều mục: chọn một thư mục; mỗi mục thành một file `<tên>.enc`.
5. Trong lúc chạy, nút chính đổi thành **Hủy**. Hủy sẽ không để lại file dở dang.
6. Cửa sổ **Mã khôi phục** hiện ra (dạng `FL-XXXX-XXXX-XXXX`, mỗi file một mã):
   - Mã **chỉ hiển thị một lần**. Đây là cách duy nhất mở file nếu bạn quên mật khẩu.
   - Bấm **Sao chép mã** hoặc **Lưu ra file .txt…**, rồi cất ở nơi an toàn, **tách biệt** với file `.enc`.
   - Nút **Xong** chỉ bật sau khi bạn đã sao chép, đã lưu, hoặc đã tích "Tôi đã ghi lại mã ở nơi an toàn khác".

## 4. Giải mã
1. Kéo thả hoặc chọn một hay nhiều file `.enc`. Ứng dụng tự chuyển sang chế độ **Giải mã dữ liệu** (nút màu xanh lá).
2. Nhập **mật khẩu hoặc mã khôi phục** vào cùng một ô. Mã gõ chữ thường hoặc có khoảng trắng vẫn được chấp nhận.
3. Bấm **Giải mã dữ liệu**. Ứng dụng kiểm tra mật khẩu trước, rồi mới hỏi thư mục xuất (lần sau sẽ mở sẵn thư mục bạn chọn lần trước).
4. Nếu ở đó đã có file/thư mục trùng tên, ứng dụng hỏi trước khi ghi đè. Khi giải mã nhiều file, có ô "Áp dụng cho các mục còn lại". Ghi đè thư mục là **thay thế hoàn toàn**, không gộp.
5. Khi xong, bấm **Mở thư mục** để xem kết quả. Thời gian tạo/sửa của file gốc được giữ nguyên.

## 5. Đổi mật khẩu
1. Chọn **một** file `.enc` rồi bấm **"Đổi mật khẩu của file này…"** (dưới nút Giải mã).
2. Nhập mật khẩu (hoặc mã khôi phục) hiện tại, rồi mật khẩu mới hai lần.
3. *(Tùy chọn)* Tích **"Tạo mã khôi phục mới"**; mã cũ sẽ hết hiệu lực.

Việc đổi diễn ra gần như tức thì vì dữ liệu không bị mã hóa lại. Lưu ý: nếu bạn còn **bản sao cũ** của file `.enc`, bản đó vẫn mở được bằng mật khẩu cũ. Nếu mật khẩu cũ đã bị lộ, hãy giải mã rồi mã hóa lại.

## 6. Menu ⚙ (góc trên bên phải)
| Mục | Tác dụng |
|-----|----------|
| Xóa vĩnh viễn bản gốc | Thay vì vào Thùng rác, bản gốc được ghi đè bằng dữ liệu ngẫu nhiên rồi xóa. **Không khôi phục được.** Trên SSD/USB, dữ liệu cũ vẫn có thể còn sót, nên đây chỉ là biện pháp giảm rủi ro. |
| Thêm vào menu chuột phải của Explorer | Chuột phải file/thư mục → "Mã hóa bằng Folder Locker" (Windows 11: trong "Show more options"); double-click `.enc` để mở. Chỉ ảnh hưởng tài khoản của bạn. |
| Mã khôi phục là gì? / Phím tắt | Trợ giúp nhanh. |
| Mở thư mục nhật ký | Nhật ký chỉ ghi loại thao tác và kết quả, **không** có tên file, đường dẫn hay mật khẩu. Hữu ích khi báo lỗi. |

## 7. Phím tắt
| Phím | Tác dụng |
|------|----------|
| Ctrl+O | Chọn file / thư mục |
| Enter | Mã hóa / giải mã |
| Alt+H | Hiện / ẩn mật khẩu đang nhập |
| Tab | Chuyển giữa các ô |

## 8. Dòng lệnh (cho người dùng nâng cao)
```powershell
"mat-khau" | .\FolderLocker.exe encrypt "D:\Tai lieu" --out E:\Backup
$env:FOLDERLOCKER_PASSWORD = "mat-khau"; .\FolderLocker.exe decrypt E:\Backup\*.enc --out D:\Khoi phuc
.\FolderLocker.exe help
```
Mật khẩu chỉ nhận qua stdin hoặc biến môi trường. Lệnh `encrypt` in mã khôi phục ra màn hình, nên hãy lưu lại.

## 9. Khuyến nghị bảo mật
- **Mật khẩu mạnh:** nên dài 12 ký tự trở lên; một cụm từ dài dễ nhớ là lựa chọn tốt.
- **Mã khôi phục:** cất trong trình quản lý mật khẩu, hoặc giấy cất kỹ. Không để chung chỗ với file `.enc`.
- **Sao lưu:** file `.enc` bị hỏng thì không mở được nữa, nên hãy giữ bản sao cho dữ liệu quan trọng. Lệnh `verify` giúp kiểm tra file còn nguyên vẹn hay không.

## 10. Xử lý sự cố
| Thông báo | Nguyên nhân / cách xử lý |
|-----------|---------------------------|
| "Sai mật khẩu hoặc mã khôi phục!" | Kiểm tra Caps Lock (ứng dụng có cảnh báo) và bộ gõ tiếng Việt. Thử lại bằng mã khôi phục. |
| "Dữ liệu mã hóa bị hỏng hoặc đã bị chỉnh sửa." | File `.enc` bị lỗi khi sao chép hoặc bị sửa. Hãy dùng bản sao lưu khác. |
| "Đây không phải file do FolderLocker mã hóa." | File không đúng định dạng; có thể là file `.enc` của phần mềm khác. |
| "File được tạo bởi phiên bản FolderLocker mới hơn." | Cập nhật ứng dụng. |
| "File định dạng cũ không đổi mật khẩu được." | Giải mã rồi mã hóa lại để chuyển sang định dạng mới. |
| "Bản gốc chưa bị xóa" | File `.enc` chưa qua được bước kiểm tra, hoặc bản gốc đang được mở. Bản gốc được giữ nguyên để an toàn. |
| "Không thể vừa mã hóa vừa giải mã" | Bạn đã chọn lẫn file `.enc` với file thường. Hãy chọn riêng từng loại. |
