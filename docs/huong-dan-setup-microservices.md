# Hướng dẫn setup hệ thống microservices

Đọc file này là chạy được hệ thống trên máy và hiểu nó được chia ra sao. Hệ thống tìm và quản lý bãi đỗ xe thông minh (3D + AI), gồm hai repo trong GitHub organization `FA26-OJT-SmartParking`: `parking-system-be` (backend, có file này) và `parking-system-fe` (frontend Next.js). Đặt hai repo cạnh nhau trong một thư mục.

**Trạng thái:** khung kỹ thuật đã xong (cấu trúc service, database, gRPC, RabbitMQ, MQTT, gateway, test, CI) và có một luồng chạy thật từ camera giả lập tới sơ đồ 3D. **Chưa có nghiệp vụ** (đăng nhập, đặt chỗ, thanh toán, thông báo).

## 1. Chạy thử trên máy

**Cần cài:** Docker Desktop, .NET SDK 10, Node.js 20.9 trở lên, Git.

Trên Windows, nếu chưa có Docker Desktop, mở PowerShell bằng quyền Administrator:

```
wsl --install --no-distribution
```

Khởi động lại máy nếu được yêu cầu, rồi tải `Docker Desktop Installer.exe` từ docker.com. Nếu ổ C: ít chỗ, cài chương trình và dữ liệu sang ổ khác (thay `F:` bằng ổ của bạn):

```
Start-Process '.\Docker Desktop Installer.exe' -Wait -ArgumentList 'install','--accept-license','--installation-dir=F:\Docker\Desktop','--wsl-default-data-root=F:\Docker\wsl'
```

**Backend:**

```
cd parking-system-be/deploy
cp ../.env.example .env
bash generate-jwt-keys.sh      # in 2 dòng; chép dòng JWT_PUBLIC_KEY vào .env
```

Trong `.env` điền `POSTGRES_PASSWORD`, `SERVICE_DB_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD` (giá trị tùy ý cho máy dev). Rồi chạy:

```
docker compose --profile sim up --build
```

Mặc định chỉ chạy 5 container lõi: postgres, rabbitmq, gateway, parking, booking. Profile bật thêm phần cần dùng:

| Profile | Bật thêm |
|---|---|
| `sim` | camera giả lập (gửi tin MQTT giả cho 10 chỗ `A-01` đến `A-10`) |
| `ai` | service AI (Python) |
| `observability` | Aspire Dashboard, xem log và trace ở `http://localhost:18888`; đặt thêm `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889` trong `.env` |
| `full` | tất cả, kể cả identity, payment, notification (đang là khung trống) |

Đo ngày 30/09/2026: 5 container lõi cùng camera giả lập dùng khoảng 0,5 GB RAM, đủ 11 container khoảng 0,84 GB (chưa tính máy ảo của Docker Desktop).

**Kiểm tra** (gateway ở cổng 8088, không dùng 8080 vì hay bị chiếm):

| Địa chỉ | Kết quả đúng |
|---|---|
| `http://localhost:8088/health` | 200 |
| `http://localhost:8088/api/booking/lots/00000000-0000-0000-0000-000000000001/availability` | 200, số chỗ trống và có xe (cần `sim`) |
| `http://localhost:8088/api/ai/events/recent` | 200, các sự kiện gần nhất (cần `ai` và `sim`) |

**Frontend:**

```
cd parking-system-fe
npm ci
cp .env.example .env.local      # NEXT_PUBLIC_API_BASE_URL=http://localhost:8088
npm run dev
```

Mở `http://localhost:3000`: sơ đồ 10 chỗ đổi màu theo tin từ camera giả lập (xanh: trống, đỏ: có xe, xám: chưa có dữ liệu).

**Dừng và làm lại từ đầu:** `docker compose --profile full down` để dừng; thêm `-v` để xóa cả dữ liệu database (dữ liệu dev chỉ là dữ liệu mẫu).

## 2. Hệ thống gồm gì

```
Trình duyệt (Next.js, cổng 3000)
      │  REST + SignalR (WebSocket)
      ▼
API Gateway (YARP, cổng 8088) ── kiểm tra JWT, chuyển request
      │
      ├── identity ────────────► schema identity
      ├── parking ─────────────► schema parking   ◄── MQTT ── camera giả lập
      ├── booking ─ gRPC ─────► parking (GetLotSlots, AssignSlot)
      │           └ gRPC ─────► payment (CheckDebt)
      ├── payment ─────────────► schema payment
      ├── notification ────────► schema notification
      └── ai (Python) ◄── RabbitMQ (sự kiện SlotStatusChanged)

RabbitMQ: sự kiện giữa các service (MassTransit) và MQTT cho camera (plugin, cổng 1883)
PostgreSQL: một container, một database `parking_system`, mỗi service một schema riêng
```

Trình duyệt chỉ gọi gateway. Service chỉ làm việc trong schema của mình; cần dữ liệu của service khác thì gọi gRPC hoặc nghe sự kiện.

| Thành phần | Việc đang làm |
|---|---|
| `gateway` | Định tuyến, kiểm tra JWT, CORS cho `http://localhost:3000` |
| `parking` | Nhận MQTT, ghi trạng thái từng chỗ, phát sự kiện, SignalR hub `/hubs/parking`, gRPC `GetLotSlots` |
| `booking` | API số chỗ trống, gọi gRPC sang parking và payment |
| `identity`, `payment`, `notification` | Khung trống (có database, chưa có chức năng). `AssignSlot` và `CheckDebt` trả `Unimplemented` có chủ ý, để không cho xe vào bằng câu trả lời giả |
| `ai` (Python) | Đọc sự kiện RabbitMQ, giữ các sự kiện gần nhất trong bộ nhớ; không dùng database |
| `edge/camera-simulator` | Cứ 2 giây gửi một tin MQTT giả cho một chỗ ngẫu nhiên |

**Luồng chạy thật hiện có:** camera giả lập gửi MQTT (topic `lot/{lotId}/slot/{slotCode}`) → `parking` ghi bảng `SlotStates` và đưa sự kiện `SlotStatusChanged` vào outbox trong cùng transaction rồi gửi lên RabbitMQ → `parking` đẩy thay đổi tới trình duyệt qua SignalR (sự kiện `slotStatusChanged`) → `ai` nhận sự kiện. `booking` trả số chỗ trống bằng cách gọi gRPC sang `parking`.

**Công nghệ:**

| Lớp | Dùng |
|---|---|
| Service .NET | .NET 10, ASP.NET Core, MediatR 12.5.0, FluentValidation 12.1.1, Swashbuckle |
| Database | PostgreSQL 17, Entity Framework Core 10 + Npgsql, EF Core migrations |
| Gateway | YARP |
| Gọi giữa service | gRPC |
| Sự kiện | RabbitMQ 4 + MassTransit 8.5.10 (outbox và inbox) |
| Camera | MQTT qua plugin của RabbitMQ (thư viện MQTTnet ở parking) |
| Thời gian thực | SignalR |
| Log, trace | Serilog, OpenTelemetry, Aspire Dashboard (tùy chọn) |
| Service AI | Python 3.12, FastAPI, aio-pika, grpcio |
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS 4, Three.js |
| Test, CI | xUnit, pytest, Jest; Docker Compose, GitHub Actions |

Không nâng MassTransit lên 9 và MediatR lên 13 trở lên (bản thương mại), và không dùng AutoMapper: phần map DTO viết tay.

## 3. Cấu trúc code

```
parking-system-be/
├─ services/
│  ├─ identity | parking | booking | payment | notification/   # service .NET, cấu trúc ở dưới
│  └─ ai/                      # Python: app/ (FastAPI, gRPC, RabbitMQ), tests/
├─ gateway/Gateway/            # YARP, route ở appsettings.json
├─ shared/
│  ├─ ParkingSystem.Contracts/       # kiểu sự kiện RabbitMQ dùng chung
│  └─ ParkingSystem.ServiceDefaults/ # Serilog, OpenTelemetry, health, kiểm tra JWT, lỗi 401/403, MigrateDatabase
├─ grpc_proto/                 # hợp đồng gRPC (*.proto) + project sinh mã C#
├─ tests/                      # test tích hợp gRPC giữa các service
├─ edge/camera-simulator/      # Python, gửi MQTT giả
├─ deploy/                     # docker-compose.yml, init-db.sh, generate-jwt-keys.sh
└─ docs/                       # tài liệu
```

**Mỗi service .NET** theo mẫu `Project.CleanArchitecture` của mentor, 5 project và một project test:

| Project | Chứa gì |
|---|---|
| `X.Domain` | `Base/`, `Entities/`, `Enum/`. Không phụ thuộc gì |
| `X.Application` | `Usecase/<Tên>/` (Command hoặc Query, Handler, Validator), `DTOs/`, `Common/` (Behaviors, Interfaces, Models, Mappers), `Resources.resx` (thông báo) |
| `X.Infrastructure` | `GRPC/` (Client, Services, Interceptors), `MessageBroker/` (MassTransit), `Consumers/` |
| `X.Persistence` | `ApplicationDbContext`, `EntityTypeConfigurations/`, `Repositories/`, `UnitOfWork`, `Migrations/` |
| `X.WebAPI` | `Program.cs`, `Controllers/`, `Middleware/`, Swagger |
| `X.Tests` | xUnit |

Chiều phụ thuộc: `WebAPI → Infrastructure → Persistence → Application → Domain`. `Application` chỉ khai báo interface (`IUnitOfWork`, `IUnitOfGrpc`, `IEventPublisher`); các lớp ngoài cài đặt.

**Một request đi qua:** controller (kế thừa `ApiControllerBase`) gửi Command hoặc Query qua MediatR → `ValidationBehavior` chạy validator → handler chạy → `ExceptionHandlingMiddleware` đổi mọi lỗi thành phản hồi chuẩn (theo `API Design Template`):

```json
{ "result": { }, "isSuccess": true, "statusCode": 200, "message": "..." }
```

Lỗi dùng cùng dạng với `result` là `null`: 400 (dữ liệu vào sai, `message` là lỗi đầu tiên), 401 và 403, 404, 501 (chức năng chưa làm), 503 (service khác không gọi được), 500 (câu chung, chi tiết chỉ ghi log).

**Thêm một use case:** (1) entity ở `Domain/Entities` nếu cần; (2) `Usecase/<Tên>/` với Query hoặc Command, Validator, Handler, DTO; (3) interface mới ở `Application/Common/Interfaces`, cài đặt ở `Persistence` hoặc `Infrastructure`; (4) controller trả `Success(kết quả, thông báo)`; (5) đổi mô hình dữ liệu thì thêm migration; (6) viết test; (7) mô tả API theo `API Design Template` ở `docs/api/`. Ví dụ đầy đủ: `services/booking/Booking.Application/Usecase/Availability` và `AvailabilityController`.

## 4. Giao tiếp giữa các thành phần

**REST qua gateway.** Service tự phục vụ đường dẫn `/api/...`, gateway chuyển tiếp nguyên đường dẫn (trừ service AI). Route ở `gateway/Gateway/appsettings.json`:

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

**gRPC** (`grpc_proto/`, chỉ trong mạng nội bộ, cổng 8081): `parking.proto` (`GetLotSlots`, `AssignSlot`), `payment.proto` (`CheckDebt`), `ai.proto` (`GetRecentSlotEvents`). Một project sinh mã C# cho cả server và client; service AI sinh mã Python từ cùng file bằng `services/ai/gen_proto.py`.

**Sự kiện RabbitMQ.** Kiểu sự kiện khai báo ở `shared/ParkingSystem.Contracts/Events.cs`. MassTransit gửi mỗi kiểu tới exchange `ParkingSystem.Contracts:<TênKiểu>` và service AI bám đúng tên đó, nên đổi tên kiểu hoặc namespace là thay đổi phá vỡ. Hiện chỉ `SlotStatusChanged` được gửi và nghe. Gửi đi qua outbox, nhận qua inbox (bảng trong schema của service), nên một tin giao hai lần chỉ được xử lý một lần.

## 5. Cơ sở dữ liệu

- **PostgreSQL 17**, một container, **một database `parking_system`**, một tài khoản `app_svc` (mật khẩu `SERVICE_DB_PASSWORD`; quản trị bằng tài khoản `postgres` với `POSTGRES_PASSWORD`). Mỗi service làm việc trong **schema riêng**: `identity`, `parking`, `booking`, `payment`, `notification`. Bảng, bảng outbox/inbox của MassTransit và `__EFMigrationsHistory` của service nằm trong schema đó nên không trùng tên nhau. `deploy/postgres/init-db.sh` tạo database và schema, chỉ chạy khi volume `pgdata` còn trống.
- **Kết nối từ máy host** (DBeaver, pgAdmin, psql): `localhost:5433`, database `parking_system`, user `app_svc`. Không cần cài PostgreSQL riêng; cổng 5433 (không phải 5432) để không đụng PostgreSQL cài sẵn.
- **Bảng hiện có:** `identity` có `users`, `user_accounts`, `user_account_sessions`, `old_passwords` (bảng mẫu của template); `parking` có `SlotStates`; `booking`, `payment`, `notification` chưa có bảng nghiệp vụ. Mỗi schema có thêm `InboxState`, `OutboxMessage`, `OutboxState`, `__EFMigrationsHistory`.
- **Migration** nằm ở `X.Persistence/Migrations` và tự chạy khi service khởi động ở chế độ Development. Thêm migration mới (cài công cụ một lần: `dotnet tool install -g dotnet-ef`):

```
dotnet ef migrations add <TenMigration> --project services/<tên>/<Tên>.Persistence --startup-project services/<tên>/<Tên>.Persistence --output-dir Migrations
```

  Mỗi service có một test báo lỗi khi mô hình đổi mà chưa có migration.

## 6. Cấu hình và phát triển

**Biến trong `deploy/.env`** (không commit; mẫu ở `.env.example`):

| Biến | Ý nghĩa |
|---|---|
| `POSTGRES_PASSWORD`, `SERVICE_DB_PASSWORD` | Mật khẩu quản trị PostgreSQL; mật khẩu tài khoản `app_svc` |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | RabbitMQ, dùng luôn cho MQTT |
| `JWT_PUBLIC_KEY` | Khóa công khai RS256 (base64, một dòng); gateway và service dùng nó để kiểm tra token |
| `VNPAY_*`, `PUBLIC_BASE_URL`, `SMTP_*` | Dành cho payment và notification, chưa có code dùng |

Service **không có giá trị mặc định** trong code: thiếu chuỗi kết nối, `MessageBrokerSettings`, `Jwt:PublicKey`, `Grpc:*` hoặc `Mqtt:*` thì service dừng ngay khi khởi động và báo tên khóa thiếu. Token JWT ký RS256; chưa service nào cấp token vì đăng nhập chưa làm.

**Chạy một service ngoài Docker để debug:** `docker compose stop <tên>`, đặt cấu hình bằng biến môi trường (dùng `__` thay `:`) hoặc `dotnet user-secrets`, rồi `dotnet run --project services/<tên>/<Tên>.WebAPI`. Cần: `ConnectionStrings:DefaultConnection` (`Host=localhost;Port=5433;Database=parking_system;Username=app_svc;Password=...`), `MessageBrokerSettings:HostName` (`localhost`), `:UserName`, `:Password` (như RabbitMQ trong `.env`), `Jwt:PublicKey`; parking thêm `Mqtt:Host`, `Mqtt:Username`, `Mqtt:Password`; booking cần `Grpc:Parking` và `Grpc:Payment` (đã có trong `appsettings.Development.json`). Ở chế độ Development, mở Swagger UI ở gốc của service.

**Test:**

| Việc | Lệnh |
|---|---|
| Test .NET (41 test) | `dotnet test ParkingSystem.slnx` |
| Test AI (4 test) | `cd services/ai && pip install -r requirements-dev.txt && pytest` |
| Frontend (6 test) | `npm test`, `npm run lint`, `npm run typecheck` |

**CI (GitHub Actions):** mỗi service, gateway, service AI và test tích hợp một workflow trong `.github/workflows/`, chạy khi push vào `main`, `develop` và trên pull request đổi thư mục liên quan. Frontend có `ci.yml` (lint, kiểm tra kiểu, test, build).

**Git:** nhánh `features/Implementation_<Tên>`, `features/Design_<Tên>`, `hotfix/Bug_<Tên>`, `release/sprint_x` gộp vào `develop` bằng pull request theo mẫu ở `.github/pull_request_template.md`; commit bằng tiếng Anh, mỗi commit một thay đổi; không commit `.env` hay khóa. Chi tiết: `docs/workflow.md`.

## 7. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Service dừng khi khởi động, log nói thiếu một khóa cấu hình | Điền khóa đó vào `deploy/.env` (hoặc user-secrets khi chạy ngoài Docker) |
| Log `Failed executing DbCommand ... __EFMigrationsHistory` lúc service lên lần đầu | Bình thường: EF thấy bảng lịch sử migration chưa có rồi tạo nó |
| Migration báo bảng đã tồn tại, hoặc không thấy database `parking_system` | Volume dữ liệu do bản cũ tạo. Chạy `docker compose --profile full down -v` rồi `up --build` |
| Sơ đồ 3D không đổi màu | Chưa bật profile `sim`, hoặc gateway chưa chạy (`/health`) |
| Docker báo lỗi I/O hoặc không build được | Kiểm tra ổ đĩa còn trống; nếu ổ C: đầy, chuyển dữ liệu Docker sang ổ khác ở Settings → Resources → Advanced → Disk image location |

## 8. Đọc thêm

| File | Nội dung |
|---|---|
| `docs/workflow.md` | Quy trình Git, issue board GitLab, tài liệu mỗi sprint |
| `docs/template-deviations.md` | Chỗ khác mẫu `Project.CleanArchitecture` và lý do |
| `docs/quyet-dinh-nghiep-vu.md` | Các điểm nghiệp vụ còn mâu thuẫn và câu hỏi cho mentor |
