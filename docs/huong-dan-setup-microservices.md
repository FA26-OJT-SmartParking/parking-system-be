# Hướng dẫn setup microservices

Hệ thống tìm & quản lý bãi đỗ xe thông minh (3D + AI), dự án OJT 3 tháng. Kiến trúc: microservices, gọi nhau bằng gRPC (mentor yêu cầu), sự kiện qua RabbitMQ, mỗi service một database, mỗi service theo cấu trúc của mẫu `Project.CleanArchitecture` do mentor cấp (Domain, Application, Infrastructure, Persistence, WebAPI), chạy bằng Docker Compose.

> **Lưu ý (29/09):** nhiều điểm kỹ thuật và nghiệp vụ còn phụ thuộc tài liệu nào là nguồn chuẩn (xem mục 12). Khung hiện tại bám `context_v10.docx`.

## 1. Tổng quan

```
Trình duyệt (Next.js + React + Three.js, một ứng dụng duy nhất, repo riêng)
        │  REST + SignalR (WebSocket)
        ▼
API Gateway (YARP) ── kiểm tra JWT, CORS, định tuyến
        │  REST (gateway → service)
        ├── identity      (.NET)   → schema identity
        ├── parking       (.NET)   → schema parking       + SignalR hub
        ├── booking       (.NET)   → schema booking
        ├── payment       (.NET)   → schema payment       ──► VNPay
        ├── notification  (.NET)   → schema notification  ──► SMTP
        └── ai            (Python)                        ──► OSRM
Service gọi nhau khi cần kết quả ngay: gRPC (file .proto ở grpc_proto/), ví dụ booking → parking
Sự kiện giữa các service: RabbitMQ + MassTransit
Camera AI / trình giả lập ──MQTT──► RabbitMQ (plugin MQTT) ──► parking (sau này cả booking)
Dùng chung: PostgreSQL (1 container, 1 database `parking_system`, mỗi service một schema), Aspire Dashboard tùy chọn (log, trace, metrics)
```

## 2. Frontend ở repo riêng

Chỉ backend chia thành microservices. Frontend là **một** ứng dụng Next.js (React + Three.js) ở repo riêng [`parking-system-fe`](https://github.com/FA26-OJT-SmartParking/parking-system-fe), chỉ gọi gateway, nên backend chia bao nhiêu service cũng không ảnh hưởng.

- Chia thư mục theo tính năng: `auth`, `parking-3d`, `booking`, `payment`, `owner-dashboard`, `admin`.
- Code Three.js để ở một module dùng chung cho sơ đồ 3D của khách và dashboard của chủ bãi.
- Phần chủ bãi và quản trị tải riêng khi cần (lazy load).

## 3. Trách nhiệm từng service

| Service | Làm gì | Quy tắc |
|---|---|---|
| identity | Tài khoản, OTP email, đăng nhập, JWT, vai trò khách / chủ bãi / nhân viên / quản trị, xe của khách | BR-10, BR-11 |
| parking | Bãi, khu, sức chứa, cách tính phí và bảng giá, tỷ lệ suất đặt trước, layout JSON, trạng thái chỗ, xếp chỗ, hub SignalR, sự kiện camera khu vực, duyệt bãi | BR-06, BR-15, BR-16, BR-17 |
| booking | 2 loại đặt chỗ, suất, phiên đỗ vào/ra (camera cổng hoặc nhân viên), vé QR xe đạp, hàng chờ xác nhận biển số, tính phí, tự hủy đặt chỗ quá hạn | BR-01, BR-02, BR-04, BR-07, BR-13, BR-14, BR-18, BR-19, BR-20, BR-22, BR-23 |
| payment | VNPay (thanh toán, cọc, hoàn tiền, IPN), tiền mặt do nhân viên xác nhận, nợ theo biển số và bãi, doanh thu theo bãi, tự trừ qua token (P2) | BR-05, BR-09, BR-21, BR-24 |
| notification | Email (OTP, nhắc nhở), thông báo trong ứng dụng | — |
| ai | Gợi ý bãi (chấm điểm, gọi OSRM), dự báo lấp đầy 15 phút (baseline rồi LightGBM) | BR-08 |

Mỗi service chỉ đọc database của mình. Cần dữ liệu của service khác thì gọi API, hoặc giữ một bản sao lấy từ sự kiện.

## 4. Giao tiếp

### 4.1 Gateway

Cấu hình ở `gateway/Gateway/appsettings.json`.

| Đường dẫn | Service | Cần JWT |
|---|---|---|
| `/api/identity/auth/**` | identity | Không (đăng ký, đăng nhập, OTP) |
| `/api/identity/**` | identity | Có |
| `GET /api/parking/lots/**` | parking | Không (khách xem bãi) |
| `/api/parking/**` | parking | Có |
| `/hubs/**` | parking (SignalR) | Không (sơ đồ 3D real-time) |
| `GET /api/booking/lots/**` | booking | Không (xem chỗ trống của bãi) |
| `/api/booking/**` | booking | Có |
| `/api/payment/vnpay/ipn` | payment | Không, payment tự kiểm tra chữ ký VNPay |
| `/api/payment/**` | payment | Có |
| `/api/notifications/**` | notification | Có |
| `/api/ai/**` | ai | Không |

Gateway bỏ tiền tố `/api/<service>` trước khi chuyển tiếp, ví dụ `/api/ai/events/recent` tới ai thành `/events/recent`. CORS cho phép `http://localhost:3000` (Next.js lúc dev).

### 4.2 gRPC giữa các service (không qua gateway, chỉ trong mạng Docker)

Hợp đồng viết trong `grpc_proto/*.proto`; C# sinh code bằng `Grpc.Tools` (project `grpc_proto/ParkingSystem.Grpc.csproj`), Python sinh bằng `services/ai/gen_proto.py`. Service phục vụ gRPC nghe cổng 8081 (HTTP/2, không mở ra máy host); REST và SignalR vẫn ở cổng 8080.

| Hợp đồng | Bên phục vụ | Bên gọi | Trạng thái |
|---|---|---|---|
| `parking.proto` → `GetLotSlots` | parking | booking | Đã chạy: `GET /api/booking/lots/{lotId}/availability` |
| `ai.proto` → `GetRecentSlotEvents` | ai (Python) | chưa có | Đã có server và test |

Sẽ thêm khi làm nghiệp vụ: xếp chỗ khi xe có đặt trước vào cổng (booking → parking, BR-15), kiểm tra nợ trước khi cho xe vào (booking → payment, BR-05).

Lưu ý: gRPC nội bộ hiện chưa xác thực service gọi service; mạng Docker riêng không thay thế xác thực, cần chốt cách làm trước khi triển khai ngoài máy dev.

### 4.3 Sự kiện qua RabbitMQ

Khai báo trong `shared/ParkingSystem.Contracts/Events.cs`.

| Sự kiện | Service phát | Service nhận |
|---|---|---|
| OtpRequested | identity | notification |
| LotApproved, LotUpdated | parking | booking, ai |
| SlotStatusChanged, ZoneOccupancyChanged | parking | ai |
| ReservationCreated | booking | payment, ai |
| DepositPaid, DepositFailed | payment | booking |
| ReservationConfirmed, ReservationCancelled | booking | payment, notification, ai |
| SessionOpened, SessionClosed | booking | parking, payment, ai |
| PaymentCompleted, RefundCompleted | payment | booking, notification |

- MassTransit phát mỗi loại sự kiện vào exchange tên `ParkingSystem.Contracts:<TênSựKiện>` (kiểu fanout). Đổi tên sự kiện hay namespace là làm vỡ service khác.
- Publish trong service .NET đi qua outbox: gọi `Publish(...)` rồi `SaveChangesAsync()` trên DbContext của service thì sự kiện mới được gửi. Nhận sự kiện đi qua inbox, sự kiện đến trùng chỉ xử lý một lần.
- ai (Python) đọc bằng `aio-pika`: tạo queue riêng, bind vào exchange ở trên, lấy nội dung ở trường `message` của JSON (xem `services/ai/app/events.py`).

### 4.4 Camera (MQTT)

Camera và trình giả lập gửi MQTT tới RabbitMQ cổng 1883.

| Topic | Nội dung | Service nhận |
|---|---|---|
| `lot/{lotId}/gate/in`, `lot/{lotId}/gate/out` | biển số, loại xe, độ tin cậy, thời điểm | booking (chưa làm) |
| `lot/{lotId}/slot/{slotCode}` | `status`, `confidence`, `at` | parking |

parking đọc bằng thư viện MQTTnet (`services/parking/Parking.WebAPI/Workers/MqttSlotListener.cs`). Mỗi tin nhắn được lưu thành trạng thái chỗ (bảng `SlotStates`), chuyển thành sự kiện `SlotStatusChanged` trong cùng một transaction (outbox) và đẩy lên sơ đồ 3D qua SignalR (`slotStatusChanged`).

## 5. Các luồng chính

### Đặt chỗ có cọc (saga)

1. Khách gọi `POST /api/booking/reservations`. booking kiểm tra còn suất (BR-23), tạo đặt chỗ ở trạng thái **chờ cọc**, giữ suất và phát `ReservationCreated`.
2. payment nhận sự kiện và ghi khoản cọc cần thu. Khách gọi `POST /api/payment/deposits/{reservationId}` để lấy link VNPay. Nếu payment chưa kịp nhận sự kiện thì API trả 409; frontend chờ khoảng 1 giây rồi gọi lại.
3. VNPay gọi IPN. payment kiểm tra chữ ký rồi phát `DepositPaid` (hoặc `DepositFailed`).
4. booking nhận `DepositPaid`, chuyển đặt chỗ sang **đã xác nhận** và phát `ReservationConfirmed`.
5. Quá hạn chờ cọc thì booking hủy, nhả suất và phát `ReservationCancelled`. Tiền cọc về sau khi đã hủy thì payment tự hoàn.

### Xe vào cổng

1. Camera gửi `lot/{lotId}/gate/in`, hoặc nhân viên nhập biển số (BR-20).
2. booking: độ tin cậy dưới 0.9 thì đưa vào hàng chờ nhân viên xác nhận (BR-07). Sau đó gọi payment kiểm tra nợ (BR-05).
3. booking mở phiên và phát `SessionOpened`. Nếu xe có đặt trước thì gọi parking xếp chỗ (BR-15).

### Xe ra

1. Camera gửi `lot/{lotId}/gate/out`, hoặc nhân viên nhập biển số.
2. booking tính phí (BR-19, BR-04), trừ cọc (BR-14) và phát `SessionClosed`.
3. payment: tự trừ nếu khách đã liên kết VNPay (BR-24); nếu không thì khách quét QR hoặc trả tiền mặt (BR-21). Xong thì phát `PaymentCompleted`.
4. Xe đi mà chưa trả: nhân viên đánh dấu, payment ghi nợ theo biển số và bãi (BR-05).

## 6. Cấu trúc repo

```
parking-system-be/
├─ grpc_proto/                               # hợp đồng gRPC (.proto) + project sinh code C#
├─ shared/
│  ├─ ParkingSystem.Contracts/               # sự kiện RabbitMQ dùng chung
│  └─ ParkingSystem.ServiceDefaults/         # cấu hình chung cho mọi service .NET
├─ gateway/Gateway/                          # YARP
├─ services/
│  ├─ identity|parking|booking|payment|notification/
│  │  ├─ <Tên>.Domain/                       # Base (BaseEntity), Entities, Enum (không phụ thuộc gì)
│  │  ├─ <Tên>.Application/                  # Usecase (Command/Query, Handler, Validator), DTOs, Common (Behaviors, Interfaces, Models, Mappers), Resources.resx
│  │  ├─ <Tên>.Infrastructure/               # GRPC (Client, Services, Interceptors), MessageBroker (MassTransit), Consumers
│  │  ├─ <Tên>.Persistence/                  # ApplicationDbContext, EntityTypeConfigurations, Repositories, UnitOfWork, Migrations
│  │  ├─ <Tên>.WebAPI/                       # Program.cs, Controllers, Middleware, Swagger; parking thêm SignalR hub và worker MQTT
│  │  ├─ <Tên>.Tests/                        # xUnit
│  │  └─ Dockerfile                          # build context là gốc repo
│  └─ ai/                                    # Python FastAPI + gRPC
├─ tests/ParkingSystem.IntegrationTests/     # test gRPC thật giữa các service, chạy trong bộ nhớ
├─ edge/camera-simulator/                    # Python, gửi MQTT giả lập
├─ deploy/                                   # docker-compose.yml, init-db.sh, enabled_plugins
├─ docs/                                     # tài liệu này, quy trình nhóm
├─ .github/                                  # workflow CI, mẫu pull request
├─ Directory.Build.props                     # net10.0, nullable, implicit usings
├─ Directory.Packages.props                  # phiên bản gói NuGet ở một chỗ
├─ nuget.config
├─ ParkingSystem.slnx
└─ .env.example
```

Chiều phụ thuộc của mỗi service: `WebAPI → Infrastructure → Persistence → Application → Domain`. `Application` chỉ khai báo interface (ví dụ `IUnitOfGrpc`, `IUnitOfWork`, `IEventPublisher`), `Persistence` và `Infrastructure` cài đặt chúng; `WebAPI` ghép tất cả trong `Program.cs`. Mỗi request đi qua MediatR: controller gửi Command hoặc Query, `ValidationBehavior` chạy các validator (FluentValidation) rồi mới tới handler; lỗi được `ExceptionHandlingMiddleware` đổi thành phản hồi chuẩn `{ result, isSuccess, statusCode, message }` của API Design Template. Phần map DTO viết tay (không dùng AutoMapper). Consumer RabbitMQ đặt trong `Infrastructure/Consumers` (MassTransit quét assembly Infrastructure). Các điểm khác mẫu và lý do: `docs/template-deviations.md`.

## 7. Chạy trên máy

1. Cài WSL và Docker Desktop. Trên Windows, mở PowerShell bằng quyền Administrator:
   - Chạy `wsl --install --no-distribution`, rồi khởi động lại máy nếu được yêu cầu.
   - Tải `Docker Desktop Installer.exe` từ docker.com. Nếu ổ C: ít chỗ, cài chương trình và dữ liệu sang ổ khác (thay `F:` bằng ổ của bạn):
     ```
     Start-Process '.\Docker Desktop Installer.exe' -Wait -ArgumentList 'install','--accept-license','--installation-dir=F:\Docker\Desktop','--wsl-default-data-root=F:\Docker\wsl'
     ```
   - Đã cài vào ổ C: rồi thì chuyển dữ liệu ở Settings → Resources → Advanced → Disk image location.
2. Tại thư mục `deploy`, chép `../.env.example` thành `.env` rồi điền giá trị (chạy `bash generate-jwt-keys.sh` trong thư mục `deploy` để lấy `JWT_PUBLIC_KEY`).
3. `docker compose up --build` chạy 5 container lõi (postgres, rabbitmq, gateway, parking, booking). Thêm profile khi cần: `--profile sim` (trình giả lập camera), `--profile ai`, `--profile observability` (Aspire Dashboard, đặt thêm `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889` trong `.env`), `--profile full` (tất cả).
4. Kiểm tra (trên máy host, gateway dùng cổng 8088 và PostgreSQL dùng 5433 để không trùng phần mềm cài sẵn hay chiếm 8080 và 5432):
   - Gateway: `http://localhost:8088/health`
   - Aspire Dashboard (log, trace, profile `observability`): `http://localhost:18888`
   - RabbitMQ: `http://localhost:15672`
   - Luồng mẫu camera → parking → ai (profile `sim` và `ai`): `http://localhost:8088/api/ai/events/recent`

Các cổng chỉ dùng khi dev (PostgreSQL 5433, RabbitMQ 5672 và 15672, Aspire 18888 và 18889) chỉ mở trên `127.0.0.1`. Máy khác chỉ vào được gateway (8088) và MQTT (1883).

Chạy một service .NET ngoài Docker để debug, ví dụ parking:

1. `docker compose stop parking` để container của service đó không chạy song song.
2. Đặt cấu hình bằng biến môi trường (dùng `__` thay cho `:`) hoặc `dotnet user-secrets` (chạy `dotnet user-secrets init` một lần cho project):
   - `ConnectionStrings:DefaultConnection` = `Host=localhost;Port=5433;Database=parking_system;Username=app_svc;Password=<SERVICE_DB_PASSWORD>`
   - `MessageBrokerSettings:HostName` = `localhost`, cùng `MessageBrokerSettings:UserName` và `MessageBrokerSettings:Password` như `RABBITMQ_USER` và `RABBITMQ_PASSWORD` trong `.env`. Riêng parking thêm `Mqtt:Host` = `localhost`, `Mqtt:Username`, `Mqtt:Password`.
   - `Jwt:PublicKey` như `JWT_PUBLIC_KEY` trong `.env`. Booking cần `Grpc:Parking` và `Grpc:Payment` (đã có trong `appsettings.Development.json`).
   - Service không có giá trị mặc định cho các khóa trên: thiếu khóa nào thì dừng ngay khi khởi động.
   - `OTEL_EXPORTER_OTLP_ENDPOINT` = `http://localhost:18889` nếu muốn xem log và trace trên Aspire Dashboard.
3. `dotnet run --project services/parking/Parking.WebAPI`, rồi gọi thẳng service ở cổng nó in ra. Gateway trong Docker vẫn trỏ tới container, nên request qua gateway không tới bản đang debug.

## 8. Cấu hình chung (`shared/ParkingSystem.ServiceDefaults`)

Mỗi service .NET chỉ cần vài dòng trong `Program.cs`:

- `builder.AddServiceDefaults("<tên>")`: Serilog; OpenTelemetry cho trace và metrics; log, trace, metrics gửi về Aspire Dashboard khi có `OTEL_EXPORTER_OTLP_ENDPOINT`; health check; JWT.
- `app.UseServiceDefaults(...)`: log request, (middleware lỗi của service nếu truyền vào), xác thực, phân quyền, endpoint `/health`. Lỗi 401 và 403 trả cùng dạng phản hồi chuẩn.
- `app.MigrateDatabase<ApplicationDbContext>()`: chạy các migration còn thiếu khi ở Development. Ở môi trường khác, chạy `dotnet ef database update` như một bước triển khai.

MassTransit (RabbitMQ, outbox và inbox) được đăng ký trong `Infrastructure` của từng service từ mục `MessageBrokerSettings`, như mẫu.

Phiên bản gói ghi ở `Directory.Packages.props`. Giữ các bản mã nguồn mở: MassTransit 8.5.10 (từ bản 9 là bản thương mại) và MediatR 12.5.0 (từ bản 13 là bản thương mại). Không dùng AutoMapper: bản MIT mới nhất (14.0.0) có advisory GHSA-rvv3-g6hj-g44x và bản đã vá là bản thương mại.

## 9. Quy ước chung

- .NET 10 (LTS), PostgreSQL 17, RabbitMQ 4. Python trong container là 3.12; máy dev có 3.10 trở lên cũng chạy test được.
- Tiền lưu bằng số nguyên (đồng). Thời gian lưu theo UTC, hiển thị theo giờ Việt Nam; quy tắc 22:00 (BR-04) tính theo giờ Việt Nam.
- JWT ký bằng RS256 (NFR-SEC-003): gateway và từng service chỉ có khóa công khai để kiểm tra. Chưa có service nào cấp token vì đăng nhập chưa làm (nghiệp vụ chưa xong). SRS quy định access token 24 giờ, refresh token 7 ngày, mật khẩu băm bcrypt cost 12 (NFR-SEC-001).
- Không commit khóa thật: `.env` nằm trong `.gitignore`, chỉ commit `.env.example`. Khóa VNPay chỉ có ở payment.
- Gói NuGet mặc định lưu ở ổ C:. Muốn chuyển sang ổ khác thì đặt biến môi trường `NUGET_PACKAGES`, ví dụ `F:\dev\nuget-packages`.
- Git, commit, pull request: xem `docs/workflow.md`.

## 10. Kiểm thử và CI

- Unit test: `dotnet test ParkingSystem.slnx`; ai: `cd services/ai`, `pip install -r requirements-dev.txt`, `pytest`.
- Integration: chạy service với PostgreSQL và RabbitMQ thật (Testcontainers) khi bắt đầu có nghiệp vụ.
- Saga: cọc thất bại, hết hạn chờ cọc, IPN của VNPay đến trễ hoặc đến 2 lần.
- CI: Gitlab Guide chỉ ghi dòng "CI/CD pipeline passes successfully" trong Definition of Done, không quy định pipeline cụ thể. Các workflow trong `.github/workflows/` là thiết kế của nhóm: mỗi service một workflow, chỉ chạy khi thư mục của service (hoặc `grpc_proto/`, `shared/`) thay đổi; workflow `integration` chạy test gRPC giữa booking và parking; kích hoạt trên push vào `main`, `develop` và trên pull request. Đến 29/09/2026 các workflow này chưa chạy lần nào trên GitHub.
- CD chưa làm; cách triển khai khi demo chưa chốt.

## 11. Lưu ý

- **RAM:** mặc định chỉ 5 container. Bật thêm profile nào thì tốn thêm phần đó; `--profile full` chạy tất cả (khoảng 11 container).
- **IPN của VNPay cần URL công khai:** địa chỉ `https://<tên miền>/api/payment/vnpay/ipn` khai báo trong trang quản trị merchant của VNPay. Lúc dev dùng tunnel (Cloudflare Tunnel hoặc ngrok).
- **OSRM:** máy chủ demo công khai có giới hạn truy cập. Gọi nhiều thì cache kết quả hoặc tự chạy OSRM bằng Docker.
- **Máy ảo demo công khai:** file compose đã chỉ mở các cổng dev trên `127.0.0.1`. Ở tường lửa của máy ảo chỉ mở cổng gateway (8088, hoặc 80/443 qua reverse proxy); mở thêm 1883 khi có camera thật gửi từ nơi khác. Aspire Dashboard đang tắt đăng nhập nên không được đưa ra Internet.
- **Kubernetes:** để giai đoạn sau.

## 12. Quyết định còn mở (rà soát ngày 29/09)

Trong `Downloads` có 3 tài liệu khác nhau về nghiệp vụ và kỹ thuật: `SRS.md` v1.1, `context_bai_do_xe_v1.2_microservice.md` và `context_v10.docx`. Cần hỏi mentor để chốt nguồn chuẩn. Đề xuất giải pháp cho từng điểm, kèm mã yêu cầu dẫn chiếu, nằm ở `docs/quyet-dinh-nghiep-vu.md`. Các điểm sau có thể phải đổi ở khung này:

| Điểm | Khung hiện tại | SRS v1.1 |
|---|---|---|
| Broker MQTT | Plugin MQTT của RabbitMQ | Mosquitto (DP-05, HW-04); SRS không nhắc RabbitMQ |
| JWT | RS256, Identity giữ khóa riêng, service kiểm tra bằng khóa công khai (nhóm chọn ngày 29/09/2026, chờ mentor xác nhận) | RS256, Identity giữ khóa riêng, service kiểm tra bằng khóa công khai (NFR-SEC-003) |
| Bản đồ và ETA | OSRM | Google Maps Platform (DP-02) |
| Thanh toán | VNPay | VNPay Sandbox và MoMo Sandbox khi demo (DP-04) |
| Đặt chỗ | 2 loại, cọc cố định | BOOKING trả trước 100%, HOLD cọc 30% |
| Quy tắc BR | BR-01 đến BR-24 (theo v10, thiếu BR-03) | BR-01 đến BR-20; một số mã trùng số nhưng khác nghĩa |
| Route công khai của gateway | `/hubs/**`, `/api/ai/**` mở hoàn toàn | SRS không nêu route nào; chỉ có giới hạn tần suất 100 request/phút mỗi người dùng và 60 mỗi IP chưa đăng nhập (NFR-SEC-006) |

Trong lúc chờ, chỉ làm phần không phụ thuộc các điểm trên (cấu trúc theo mẫu, gRPC, hạ tầng, FE).

## 13. Lưu ý khi đổi mô hình dữ liệu

Bảng do EF Core migrations tạo, nằm trong `<Tên>.Persistence/Migrations`. Khi sửa entity hoặc cách ánh xạ, thêm một migration (cài công cụ một lần: `dotnet tool install -g dotnet-ef`):

```
dotnet ef migrations add <TenMigration> --project services/<tên>/<Tên>.Persistence --startup-project services/<tên>/<Tên>.Persistence --output-dir Migrations
```

Mỗi service có một test (`Migrations_WhenModelChanged_AreUpToDate`) báo lỗi khi mô hình thay đổi mà chưa có migration.

Database dev tạo bằng bản cũ (mỗi service một database, hoặc `EnsureCreated` không có bảng `__EFMigrationsHistory`) không dùng được với bản này. Tạo lại một lần (dữ liệu dev chỉ là dữ liệu mẫu):

```
docker compose --profile full down -v
docker compose up --build
```
