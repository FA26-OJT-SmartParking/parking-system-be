# Điểm khác với mẫu `Project.CleanArchitecture`

Cấu trúc mỗi service .NET (Domain, Application, Infrastructure, Persistence, WebAPI) theo mẫu của mentor. Các điểm dưới đây khác mẫu có chủ ý, kèm lý do. Khi mentor muốn theo đúng mẫu ở điểm nào thì sửa điểm đó.

## Khác vì dự án có nhiều service trong một repo

| Điểm | Mẫu | Dự án | Lý do |
|---|---|---|---|
| Project dùng chung | Không có | `shared/ParkingSystem.ServiceDefaults` (Serilog, OpenTelemetry, health check, kiểm tra JWT, lỗi 401/403, `MigrateDatabase`) và `shared/ParkingSystem.Contracts` (sự kiện RabbitMQ) | Mẫu chỉ có một service. Các phần này giống nhau ở mọi service; sự kiện phải dùng chung giữa các service và service AI (Python) |
| Mã gRPC sinh ra | `.proto` và mã sinh nằm trong `Infrastructure/GRPC/Protos` của từng service | `.proto` nằm ở `grpc_proto/`, một project sinh mã cho cả server và client | Server và client của cùng một hợp đồng nằm ở hai service khác nhau; hai bản mã sinh cùng namespace sẽ trùng kiểu khi một project (như test tích hợp) tham chiếu cả hai. Một nơi duy nhất cũng tránh lệch hợp đồng |
| Routing | `api/[controller]` | Mỗi service tự phục vụ đường dẫn `/api/<service>/...`, gateway chuyển tiếp nguyên đường dẫn (trừ service AI bằng Python) | Đường dẫn công khai trùng đường dẫn của service nên Swagger và tài liệu API cùng một URL |

## Khác vì thư viện thương mại hoặc có lỗ hổng

| Điểm | Mẫu | Dự án | Lý do |
|---|---|---|---|
| MediatR | 14.1.0 | 12.5.0 | 12.5.0 là bản Apache-2.0 cuối; từ 13 là bản thương mại |
| MassTransit | 9.1.0 | 8.5.10 | Từ bản 9 dùng giấy phép thương mại; 8.x là Apache-2.0 |
| AutoMapper | 16.1.1 | Không dùng, map DTO viết tay trong `Application/Common/Mappers` | Bản MIT mới nhất (14.0.0) có advisory GHSA-rvv3-g6hj-g44x (mức cao); bản đã vá là bản thương mại. Quyết định ngày 29/09/2026 |
| Newtonsoft.Json | Dùng trong middleware | Dùng `System.Text.Json` có sẵn | Không cần thêm gói |

## Sửa lỗi hoặc chỗ không chạy được của mẫu

| Điểm | Mẫu | Dự án |
|---|---|---|
| Kiểu cột trong EF | `nvarchar`, `timestaptz` (không có trong PostgreSQL) | `character varying`, `timestamptz` |
| `BaseEntity.Id` | `Guid?` | `Guid` |
| Khóa ngoại User và UserAccount | `User.Id` vừa là khóa chính vừa là khóa ngoại | `UserAccount.UserId` trỏ tới `User.Id` |
| Xóa mềm | Tính bộ lọc nhưng không áp dụng | Bộ lọc truy vấn áp dụng cho mọi entity kế thừa `BaseEntity` |
| Token trong `UserAccountSession` | `AccessToken`, `RefereshToken` lưu nguyên văn | `AccessTokenHash`, `RefreshTokenHash` chỉ lưu băm SHA-256 |
| `IEventPublisher` | Yêu cầu `IIntegrationEventMessage`; `PublishManyAsync` ném `NotImplementedException` | Nhận mọi class; `PublishManyAsync` chạy thật |

## Khác vì `API Design Template` yêu cầu

| Điểm | Mẫu (code) | Dự án |
|---|---|---|
| Thân phản hồi | Thành công trả thẳng DTO, lỗi trả `{ message, errorDetails[] }` | Mọi phản hồi là `{ result, isSuccess, statusCode, message }` |
| Lỗi validation | Trả danh sách lỗi theo từng trường | `message` là lỗi đầu tiên, ví dụ `lotId is missing.` |

## Khác vì an toàn

| Điểm | Mẫu | Dự án |
|---|---|---|
| Lỗi không lường trước | Trả `exception.Message` cho client | Trả câu chung, ghi chi tiết vào log |
| `NotImplementedException` | 400 | 501 |
| Kiểm tra JWT | Khóa đối xứng trong `Application` | RS256 với khóa công khai, đặt trong `ServiceDefaults`; chỉ nhận thuật toán RS256 |
| Thiếu cấu hình | Đọc thẳng, không kiểm tra (ví dụ `Get<MessageBrokerSettings>()!`) | Không có giá trị mặc định; thiếu chuỗi kết nối, `MessageBrokerSettings`, `Jwt`, `Grpc` hoặc `Mqtt` thì service dừng khi khởi động |

## Thêm so với mẫu

- `DesignTimeDbContextFactory` trong mỗi `Persistence` để `dotnet ef` chạy không cần cấu hình.
- `MigrateDatabase` chỉ tự chạy ở Development.
- `UseHttpsRedirection` không dùng vì TLS không kết thúc trong container.
