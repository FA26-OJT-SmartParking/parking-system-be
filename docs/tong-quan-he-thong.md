# Tổng quan hệ thống

Tài liệu cho người mới vào dự án: hệ thống gồm những gì, chạy ra sao, dữ liệu nằm ở đâu, viết thêm code ở đâu. Số liệu ghi theo code ngày 29/09/2026, sau khi pull request refactor theo template (#5) được merge.

## 1. Dự án và trạng thái

Hệ thống tìm và quản lý bãi đỗ xe thông minh (3D + AI), dự án OJT. Gồm hai repo trong GitHub organization `FA26-OJT-SmartParking`:

| Repo | Nội dung |
|---|---|
| `parking-system-be` | Backend: gateway, các service .NET, service AI (Python), Docker Compose, tài liệu |
| `parking-system-fe` | Frontend: Next.js, sơ đồ bãi 3D |

Trên máy nên đặt hai repo cạnh nhau trong một thư mục tổng. Thư mục tổng không phải git repo; thư mục `docs/` của nó chứa SRS, context và tài liệu của mentor.

**Đã có:** khung kỹ thuật (cấu trúc service, database và migration, gRPC, RabbitMQ, MQTT, gateway, log/trace, Docker Compose, test, CI) và một luồng chạy thật từ camera giả lập tới sơ đồ 3D.

**Chưa có:** nghiệp vụ. Chưa có đăng nhập, đặt chỗ, thanh toán, thông báo. Nhiều điểm nghiệp vụ còn mâu thuẫn giữa các tài liệu nguồn, xem `docs/quyet-dinh-nghiep-vu.md`.

## 2. Kiến trúc

```
Trình duyệt (Next.js, cổng 3000)
      │  REST + SignalR (WebSocket)
      ▼
API Gateway (YARP, cổng 8088) ── kiểm tra JWT, chuyển request
      │
      ├── identity ────────────► identity_db
      ├── parking ─────────────► parking_db     ◄── MQTT ── camera giả lập
      ├── booking ─ gRPC ─────► parking (GetLotSlots, AssignSlot)
      │           └ gRPC ─────► payment (CheckDebt)
      ├── payment ─────────────► payment_db
      ├── notification ────────► notification_db
      └── ai (Python) ◄── RabbitMQ (sự kiện SlotStatusChanged)

RabbitMQ: sự kiện giữa các service (MassTransit) và MQTT cho camera (plugin, cổng 1883)
PostgreSQL: một container, mỗi service một database riêng
```

Nguyên tắc: trình duyệt chỉ gọi gateway. Service không truy vấn database của service khác; cần dữ liệu thì gọi gRPC hoặc nghe sự kiện.

**Luồng chạy thật hiện có** (camera → sơ đồ 3D):

1. `camera-simulator` gửi MQTT vào RabbitMQ, topic `lot/{lotId}/slot/{slotCode}`, nội dung `{ "status": "Available" | "Occupied", "at": "<giờ UTC>" }`.
2. `parking` nhận tin, ghi bảng `SlotStates`, đưa sự kiện `SlotStatusChanged` vào outbox trong cùng transaction rồi gửi lên RabbitMQ.
3. `parking` đẩy thay đổi tới trình duyệt qua SignalR (hub `/hubs/parking`, sự kiện `slotStatusChanged`).
4. `ai` đọc `SlotStatusChanged` từ RabbitMQ, giữ các sự kiện gần nhất trong bộ nhớ (`GET /api/ai/events/recent`).
5. `booking` trả số chỗ trống bằng cách gọi gRPC sang `parking` (`GET /api/booking/lots/{lotId}/availability`).

## 3. Các thành phần

| Thành phần | Việc đang làm |
|---|---|
| `gateway` | Định tuyến, kiểm tra JWT, CORS cho `http://localhost:3000` |
| `identity` | Khung trống (có database và các bảng tài khoản mẫu của template, chưa có API) |
| `parking` | Nhận MQTT, lưu trạng thái chỗ, phát sự kiện, SignalR hub, gRPC `GetLotSlots` (`AssignSlot` trả `Unimplemented`) |
| `booking` | API số chỗ trống; gọi gRPC sang parking và payment |
| `payment` | gRPC `CheckDebt` (trả `Unimplemented`) |
| `notification` | Khung trống |
| `ai` (Python) | Đọc sự kiện RabbitMQ, `GET /events/recent`, gRPC `GetRecentSlotEvents`; không dùng database |
| `edge/camera-simulator` | Cứ 2 giây gửi một tin MQTT giả cho một chỗ ngẫu nhiên trong 10 chỗ `A-01` đến `A-10` của bãi `00000000-0000-0000-0000-000000000001` |

`AssignSlot` và `CheckDebt` trả `Unimplemented` có chủ ý để không cho xe vào bằng câu trả lời giả; sẽ làm khi nghiệp vụ chốt.

## 4. Công nghệ

| Lớp | Công nghệ (phiên bản) |
|---|---|
| Service .NET | .NET 10, ASP.NET Core, MediatR 12.5.0, FluentValidation 12.1.1, Swashbuckle 10.1.7 |
| Truy cập dữ liệu | Entity Framework Core 10.0.12 + Npgsql 10.0.3, EF Core migrations |
| Database | PostgreSQL 17 |
| Gateway | YARP 2.3.0 |
| Gọi giữa service | gRPC (Grpc.AspNetCore 2.84.0, protobuf) |
| Sự kiện | RabbitMQ 4 + MassTransit 8.5.10 (outbox và inbox trên EF Core) |
| Camera | MQTT: plugin MQTT của RabbitMQ; thư viện MQTTnet 5.2 ở parking |
| Thời gian thực | SignalR |
| Log, trace | Serilog, OpenTelemetry, Aspire Dashboard 13 |
| Service AI | Python 3.12, FastAPI 0.141, uvicorn, aio-pika, grpcio |
| Frontend | Next.js 16.3, React 19, TypeScript, Tailwind CSS 4, Three.js, `@microsoft/signalr` |
| Test | xUnit, pytest, Jest |
| Chạy và CI | Docker Compose, GitHub Actions |

Có giữ bản mã nguồn mở của MassTransit (8.x) và MediatR (12.5.0) vì bản mới hơn là bản thương mại. Không dùng AutoMapper: phần map DTO viết tay.

## 5. Cấu trúc thư mục

```
parking-system-be/
├─ services/
│  ├─ identity | parking | booking | payment | notification/   # service .NET, cấu trúc ở mục 6
│  └─ ai/                       # Python: app/ (FastAPI, gRPC, RabbitMQ), tests/
├─ gateway/Gateway/             # YARP: route ở appsettings.json
├─ shared/
│  ├─ ParkingSystem.Contracts/       # kiểu sự kiện RabbitMQ dùng chung
│  └─ ParkingSystem.ServiceDefaults/ # Serilog, OpenTelemetry, health, kiểm tra JWT, lỗi 401/403, MigrateDatabase
├─ grpc_proto/                  # hợp đồng gRPC (*.proto) + project sinh mã C#
├─ tests/ParkingSystem.IntegrationTests/  # gRPC thật giữa các service, chạy trong bộ nhớ
├─ edge/camera-simulator/       # Python, gửi MQTT giả
├─ deploy/                      # docker-compose.yml, init-db.sh, plugin RabbitMQ, generate-jwt-keys.sh
├─ docs/                        # tài liệu này và các tài liệu khác (mục 13)
├─ .github/                     # workflow CI, mẫu pull request
├─ Directory.Packages.props     # phiên bản gói NuGet ở một chỗ
└─ ParkingSystem.slnx
```

Frontend (`parking-system-fe/src`): `app/` (các trang: trang chủ có sơ đồ 3D, `login`, `booking`, `payment`, `owner-dashboard`, `admin`, `parking-3d`), `components/`, `hooks/useSlotStatuses.ts` (SignalR), `api/` (`http.ts`, `signalr.ts`), `lib/`, `types/`. Các trang ngoài sơ đồ 3D mới là giao diện, chưa gọi API.

## 6. Cấu trúc một service .NET

Mỗi service theo mẫu `Project.CleanArchitecture` của mentor, 5 project và một project test:

| Project | Chứa gì |
|---|---|
| `X.Domain` | `Base/` (BaseEntity), `Entities/`, `Enum/`. Không phụ thuộc gì |
| `X.Application` | `Usecase/<Tên>/` (Command hoặc Query, Handler, Validator), `DTOs/`, `Common/` (Behaviors, Interfaces, Models, Mappers), `Resources.resx` (thông báo) |
| `X.Infrastructure` | `GRPC/` (Client, Services, Interceptors), `MessageBroker/` (MassTransit), `Consumers/` |
| `X.Persistence` | `ApplicationDbContext`, `EntityTypeConfigurations/`, `Repositories/`, `UnitOfWork`, `Migrations/` |
| `X.WebAPI` | `Program.cs`, `Controllers/`, `Middleware/`, Swagger |
| `X.Tests` | xUnit |

Chiều phụ thuộc: `WebAPI → Infrastructure → Persistence → Application → Domain`. `Application` chỉ khai báo interface (`IUnitOfWork`, `IUnitOfGrpc`, `IEventPublisher`), lớp bên ngoài cài đặt.

**Một request đi qua:** controller (kế thừa `ApiControllerBase`) gửi Command hoặc Query qua MediatR → `ValidationBehavior` chạy các validator → handler chạy → `ExceptionHandlingMiddleware` đổi mọi lỗi thành phản hồi chuẩn.

**Phản hồi chuẩn** (theo `API Design Template`), cả khi thành công lẫn lỗi:

```json
{ "result": { }, "isSuccess": true, "statusCode": 200, "message": "..." }
```

| Tình huống | Mã | `result` |
|---|---|---|
| Thành công | 200 | dữ liệu |
| Sai dữ liệu vào (validation, JSON hỏng) | 400 | `null`, `message` là lỗi đầu tiên |
| Chưa đăng nhập, không đủ quyền | 401, 403 | `null` |
| Không có route hoặc dữ liệu | 404 | `null` |
| Chức năng chưa làm | 501 | `null` |
| Service khác không gọi được | 503 | `null` |
| Lỗi không lường trước | 500 | `null`, câu chung (chi tiết chỉ ghi log) |

**Thêm một use case:** (1) entity nếu cần ở `Domain/Entities`; (2) `Usecase/<Tên>/` với Query hoặc Command, Validator, Handler và DTO; (3) interface cần thêm ở `Application/Common/Interfaces`, cài đặt ở `Persistence` hoặc `Infrastructure`; (4) controller trả `Success(kết quả, thông báo)`; (5) đổi mô hình dữ liệu thì thêm migration; (6) viết test; (7) mô tả API theo `API Design Template`, đặt ở `docs/api/` (xem file mẫu trong pull request thiết kế API #3). Ví dụ đầy đủ: `services/booking/Booking.Application/Usecase/Availability` và `AvailabilityController`.

Project dùng chung `shared/ParkingSystem.ServiceDefaults` gọi trong `Program.cs`: `AddServiceDefaults` (log, trace, health, kiểm tra JWT), `UseServiceDefaults` (log request, xác thực, `/health`), `MigrateDatabase`.

## 7. Giao tiếp giữa các thành phần

**REST qua gateway.** Service tự phục vụ đường dẫn `/api/...`, gateway chuyển tiếp nguyên đường dẫn (trừ service AI). Cấu hình ở `gateway/Gateway/appsettings.json`.

| Đường dẫn | Tới | Cần token |
|---|---|---|
| `/api/auth/*` | identity | không |
| `/api/identity/*` | identity | có |
| `GET /api/parking/lots/*` | parking | không |
| `/api/parking/*` | parking | có |
| `/hubs/*` (SignalR) | parking | không |
| `GET /api/booking/lots/*` | booking | không |
| `/api/booking/*` | booking | có |
| `/api/payment/vnpay/ipn` | payment | không |
| `/api/payment/*` | payment | có |
| `/api/notifications/*` | notification | có |
| `/api/ai/*` (gateway bỏ tiền tố `/api/ai`) | ai | không |

**gRPC** (`grpc_proto/`, chỉ trong mạng nội bộ, cổng 8081): `parking.proto` (`GetLotSlots`, `AssignSlot`), `payment.proto` (`CheckDebt`), `ai.proto` (`GetRecentSlotEvents`). Một project (`grpc_proto/ParkingSystem.Grpc.csproj`) sinh mã C# cho cả server và client; service AI sinh mã Python từ cùng file (`services/ai/gen_proto.py`).

**Sự kiện RabbitMQ.** Kiểu sự kiện khai báo ở `shared/ParkingSystem.Contracts/Events.cs`; MassTransit gửi mỗi kiểu tới exchange `ParkingSystem.Contracts:<TênKiểu>`, service AI bám vào đúng tên đó nên đổi tên kiểu hoặc namespace là thay đổi phá vỡ. Hiện chỉ `SlotStatusChanged` được gửi và được nghe; các kiểu còn lại là khai báo cho sau. Gửi sự kiện đi qua outbox (bảng trong database của service), nhận đi qua inbox, nên một tin giao hai lần chỉ được xử lý một lần.

## 8. Cơ sở dữ liệu

- **PostgreSQL 17**, một container cho cả hệ thống, mỗi service một database và một tài khoản riêng: `identity_db`, `parking_db`, `booking_db`, `payment_db`, `notification_db` (và `ai_db`, tạo sẵn nhưng service AI không dùng). Tài khoản `<tên>_svc` dùng mật khẩu `SERVICE_DB_PASSWORD`; tài khoản quản trị `postgres` dùng `POSTGRES_PASSWORD`. Script tạo: `deploy/postgres/init-db.sh` (chỉ chạy khi volume `pgdata` còn trống).
- **Truy cập từ máy host:** `localhost:5433` (không dùng 5432 vì hay bị PostgreSQL cài sẵn chiếm).
- **Bảng hiện có:**

| Database | Bảng |
|---|---|
| `identity_db` | `users`, `user_accounts`, `user_account_sessions`, `old_passwords` |
| `parking_db` | `SlotStates` (trạng thái mới nhất của từng chỗ) |
| `booking_db`, `payment_db`, `notification_db` | chưa có bảng nghiệp vụ |
| tất cả | `InboxState`, `OutboxMessage`, `OutboxState` (MassTransit), `__EFMigrationsHistory` |

  Entity `Reservation` (booking) và `ParkingSlot` (parking) đã có ở `Domain` nhưng chưa ánh xạ thành bảng.
- **Migration** nằm ở `X.Persistence/Migrations`, tự chạy khi service khởi động ở chế độ Development (`MigrateDatabase`). Thêm migration mới:

```
dotnet ef migrations add <TenMigration> --project services/<tên>/<Tên>.Persistence --startup-project services/<tên>/<Tên>.Persistence --output-dir Migrations
```

  Cài công cụ một lần: `dotnet tool install -g dotnet-ef`. Mỗi service có một test báo lỗi khi mô hình đổi mà chưa có migration.
- **Chuỗi kết nối** đọc từ khóa `ConnectionStrings:DefaultConnection`; thiếu thì service dừng khi khởi động.
- **Redis:** có một container `redis` trong compose nhưng chưa có code nào dùng.

## 9. Cổng và địa chỉ

| Địa chỉ | Là gì |
|---|---|
| `http://localhost:8088` | Gateway. `/health` kiểm tra sống |
| `http://localhost:3000` | Frontend |
| `http://localhost:18888` | Aspire Dashboard (log, trace, metrics) |
| `http://localhost:15672` | RabbitMQ management (tài khoản `RABBITMQ_USER`) |
| `localhost:5433` | PostgreSQL |
| `localhost:5672` | RabbitMQ AMQP (cho service chạy ngoài Docker) |
| `localhost:18889` | Aspire OTLP (cho service chạy ngoài Docker) |
| `:1883` | MQTT, camera trong mạng LAN gửi vào đây |
| `8080`, `8081` trong container | REST và SignalR (8080); gRPC (8081, parking, payment, ai) |

Các cổng dev (5433, 5672, 15672, 18888, 18889) chỉ mở trên `127.0.0.1`; máy khác chỉ vào được gateway (8088) và MQTT (1883). Cổng 8088 (thay vì 8080) và 5433 (thay vì 5432) được chọn để không đụng phần mềm hay bị cài sẵn.

## 10. Cấu hình và bảo mật

**Biến môi trường** (`deploy/.env`, chép từ `.env.example`, không commit):

| Biến | Ý nghĩa |
|---|---|
| `POSTGRES_PASSWORD`, `SERVICE_DB_PASSWORD` | Mật khẩu PostgreSQL |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | RabbitMQ, dùng luôn cho MQTT |
| `JWT_PUBLIC_KEY` | Khóa công khai RS256 (base64 DER, một dòng) để gateway và service kiểm tra token |
| `VNPAY_TMN_CODE`, `VNPAY_HASH_SECRET`, `PUBLIC_BASE_URL` | Dành cho payment, chưa có code dùng |
| `SMTP_HOST`, `SMTP_USER`, `SMTP_PASSWORD` | Dành cho notification, chưa có code dùng |

**Không có giá trị mặc định trong code:** thiếu chuỗi kết nối, `MessageBrokerSettings`, `Jwt:PublicKey`, `Grpc:*` hoặc `Mqtt:*` thì service dừng ngay khi khởi động và thông báo tên khóa thiếu.

**JWT:** ký RS256. Gateway và mọi service chỉ giữ khóa công khai (chỉ nhận thuật toán RS256). Chưa service nào cấp token vì đăng nhập chưa làm. Lấy cặp khóa: `bash deploy/generate-jwt-keys.sh` (in hai dòng; `JWT_PUBLIC_KEY` vào `.env`, giữ `JWT_PRIVATE_KEY` cho service sẽ cấp token).

**Cấu hình khi chạy service ngoài Docker** (biến môi trường, dùng `__` thay `:`, hoặc `dotnet user-secrets`): `ConnectionStrings:DefaultConnection`, `MessageBrokerSettings:HostName`, `:UserName`, `:Password`, `Jwt:PublicKey`; parking thêm `Mqtt:Host`, `Mqtt:Username`, `Mqtt:Password`; booking cần `Grpc:Parking` và `Grpc:Payment` (đã có trong `appsettings.Development.json`).

## 11. Chạy dự án

Cần: Docker Desktop, .NET SDK 10, Node.js 20.9 trở lên, máy nên có 16 GB RAM (cả hệ thống khoảng 12 container).

**Backend**

```
cd parking-system-be/deploy
cp ../.env.example .env        # điền giá trị; JWT_PUBLIC_KEY lấy từ generate-jwt-keys.sh
docker compose --profile sim up --build
```

`--profile sim` bật thêm camera giả lập. Kiểm tra: `http://localhost:8088/health`, `http://localhost:8088/api/booking/lots/00000000-0000-0000-0000-000000000001/availability`, `http://localhost:8088/api/ai/events/recent`.

**Frontend**

```
cd parking-system-fe
npm ci
cp .env.example .env.local      # NEXT_PUBLIC_API_BASE_URL=http://localhost:8088
npm run dev
```

Mở `http://localhost:3000`: sơ đồ 10 chỗ đổi màu theo tin từ camera giả lập (xanh: trống, đỏ: có xe, xám: chưa có dữ liệu).

**Test**

| Việc | Lệnh |
|---|---|
| Test .NET (36 test) | `dotnet test ParkingSystem.slnx` |
| Test AI (4 test) | `cd services/ai && pip install -r requirements-dev.txt && pytest` |
| Frontend (6 test, lint, kiểm tra kiểu) | `npm test`, `npm run lint`, `npm run typecheck` |

**Chạy một service ngoài Docker để debug:** `docker compose stop <tên>`, đặt cấu hình như mục 10, rồi `dotnet run --project services/<tên>/<Tên>.WebAPI`. Ở chế độ Development, mở Swagger UI ở gốc của service.

## 12. CI và quy trình Git

- **CI (GitHub Actions):** mỗi service .NET, gateway, service AI và integration test một workflow trong `.github/workflows/`, chỉ chạy khi thư mục liên quan đổi, khi push vào `main`, `develop` và trên pull request. Frontend có `ci.yml` (lint, kiểm tra kiểu, test, build).
- **Nhánh** (theo Gitlab Guide của mentor): `main` (chỉ nhận nhánh release đã test), `develop`, `features/Implementation_<UserStory>` và `features/Design_<UserStory>` (thiết kế API hoặc Figma), `hotfix/Bug_<UserStory>`, `release/sprint_x`. Code làm trên nhánh feature rồi gộp vào `develop` bằng pull request.
- **Commit:** tiếng Anh, mỗi commit một thay đổi, mô tả rõ (không "Update", "Fix bug"). Không commit `.env` hay khóa.
- **Pull request:** dùng mẫu có sẵn ở `.github/pull_request_template.md`, chỉ viết lại phần Change Description.
- **GitLab của FSoft Academy** chỉ dùng để báo cáo (issue board), code và pull request ở GitHub. Chi tiết: `docs/workflow.md`.

## 13. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Service dừng khi khởi động, log nói thiếu một khóa cấu hình | Điền khóa đó vào `deploy/.env` (hoặc user-secrets khi chạy ngoài Docker) |
| Log `Failed executing DbCommand ... __EFMigrationsHistory` lúc service lên lần đầu | Bình thường: EF kiểm tra bảng lịch sử migration chưa có rồi tạo nó |
| Migration báo bảng đã tồn tại | Database tạo bằng bản cũ (`EnsureCreated`). Chạy `docker compose down -v` rồi `up --build` (mất dữ liệu dev, chỉ là dữ liệu mẫu) |
| Cổng 8080 hoặc 5432 bị chiếm | Không sao: dự án dùng 8088 và 5433 |
| Docker báo lỗi I/O hoặc không build được | Kiểm tra ổ đĩa còn trống; nếu ổ C: đầy, chuyển dữ liệu Docker sang ổ khác ở Settings → Resources → Advanced |
| Sơ đồ 3D không đổi màu | Chưa bật `--profile sim` (không có camera giả lập), hoặc gateway chưa chạy (`/health`) |

## 14. Tài liệu khác trong `docs/`

| File | Nội dung |
|---|---|
| `workflow.md` | Quy trình Git, issue board GitLab, tài liệu mỗi sprint |
| `template-deviations.md` | Chỗ khác mẫu `Project.CleanArchitecture` và lý do |
| `quyet-dinh-nghiep-vu.md` | Các điểm nghiệp vụ còn mâu thuẫn, đề xuất và câu hỏi cho mentor |
| `huong-dan-setup-microservices.md` | Hướng dẫn cài đặt chi tiết (cài WSL và Docker Desktop trên Windows, chuyển ổ đĩa) |
