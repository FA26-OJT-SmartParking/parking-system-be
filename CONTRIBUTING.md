# Quy tắc đóng góp (Backend)

Áp dụng cho mọi thay đổi trong repo này. Mục 1 đến 3 theo "Git Lab & Pull Request Guide" v1.0 của mentor; mục 4 và 5 là quy ước của nhóm cho backend.

## 1. Nhánh

| Nhánh | Dùng để | Gộp vào |
|---|---|---|
| `main` | Chỉ nhận nhánh đã test kỹ và được chấp nhận, thường là nhánh release | |
| `develop` | Nhận nhánh feature và hotfix | `main`, qua nhánh release |
| `features/Implementation_<UserStory>` | Viết code cho một user story | `develop` |
| `features/Design_<UserStory>` | Tạo hoặc cập nhật thiết kế; với backend phải có API Design | `develop` |
| `hotfix/Bug_<UserStory>` | Sửa lỗi nghiêm trọng, test ở máy trước khi gộp | `main` và `develop` |
| `release/sprint_x` | Tạo từ `develop` để demo sprint x (ví dụ `release/sprint_1`); phải có release note | `main` |

## 2. Commit

- Nói rõ đã thay đổi gì; không dùng "Update", "Fix bug" mà không biết cập nhật hay sửa cái gì.
- Mỗi commit một việc (thêm tính năng, sửa lỗi, refactor...); không gộp các thay đổi không liên quan.

## 3. Pull request

- Mọi tính năng gộp vào sprint phải qua pull request.
- Dùng đúng mẫu, copy nguyên và chỉ viết lại phần Change Description: `.github/pull_request_template.md` cho implementation, `.github/PULL_REQUEST_TEMPLATE/design.md` cho design (thêm `?template=design.md` vào URL khi tạo PR).
- Tiêu đề PR theo dạng `[Feature]`, `[Fix]`, `[Refactor]`...
- Trước khi xin review phải đạt Definition of Done trong mẫu: test và CI chạy qua, không có dữ liệu nhạy cảm hay secret trong code, tài liệu API đã cập nhật.

## 4. Quy ước nhóm

- Commit message bằng tiếng Anh, có tiền tố như `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`.
- Không commit `.env`, khóa hay mật khẩu; chỉ `.env.example` được đưa lên.
- Xóa nhánh feature sau khi merge.
- Trong repo chỉ có README, file này và mô tả API ở `docs/api/`. Hướng dẫn và ghi chú của nhóm để ngoài repo.

## 5. Quy tắc code backend

- Mỗi service .NET theo mẫu `Project.CleanArchitecture` của mentor: `Domain`, `Application`, `Infrastructure`, `Persistence`, `WebAPI`, `Tests`. Phụ thuộc một chiều: `WebAPI → Infrastructure → Persistence → Application → Domain`.
- Use case mới đặt ở `X.Application/Usecase/<Tên>/` (Command hoặc Query, Validator, Handler). Controller kế thừa `ApiControllerBase` và trả phản hồi `{ result, isSuccess, statusCode, message }` theo `API Design Template`.
- Mỗi API mới có file mô tả ở `docs/api/<tên>.md` theo `API Design Template`, làm trên nhánh Design.
- Service chỉ đọc ghi schema database của mình; cần dữ liệu của service khác thì gọi gRPC (hợp đồng ở `grpc_proto/`) hoặc nghe sự kiện RabbitMQ.
- Đổi mô hình dữ liệu phải kèm migration; test sẽ báo lỗi nếu thiếu.
- Không đổi tên kiểu hoặc namespace trong `shared/ParkingSystem.Contracts`: service AI bám theo tên exchange sinh ra từ đó.
- Giữ MassTransit 8 và MediatR 12.5 (bản mới hơn là bản thương mại); không dùng AutoMapper, map DTO viết tay.
- Listener hoặc consumer chạy nền (MQTT, RabbitMQ) đặt ở `X.Infrastructure`: chỉ kết nối, đọc và phân tích tin nhắn rồi gửi Command qua MediatR. Nghiệp vụ nằm ở Handler trong `X.Application`; WebAPI không dùng trực tiếp DbContext hay MassTransit.
- Không đặt giá trị cấu hình mặc định trong code; cấu hình mới khai báo trong `.env.example` hoặc `appsettings.Development.json`.
