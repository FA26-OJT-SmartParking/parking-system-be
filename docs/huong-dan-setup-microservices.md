# Hướng dẫn setup microservices

Hệ thống tìm & quản lý bãi đỗ xe thông minh (3D + AI), dự án OJT 3 tháng. Kiến trúc theo quyết định D1 trong tài liệu bối cảnh (`context_v10.docx`): microservices, REST + RabbitMQ, 1 PostgreSQL mỗi service 1 database, Docker Compose.

## 1. Tổng quan

```
Trình duyệt (React + Three.js, một ứng dụng duy nhất)
        │  REST + SignalR (WebSocket)
        ▼
API Gateway (YARP) ── kiểm tra JWT, CORS, định tuyến
        │  REST
        ├── identity      (.NET)   → identity_db
        ├── parking       (.NET)   → parking_db       + SignalR hub
        ├── booking       (.NET)   → booking_db
        ├── payment       (.NET)   → payment_db       ──► VNPay
        ├── notification  (.NET)   → notification_db  ──► SMTP
        └── ai            (Python) → ai_db            ──► OSRM
Sự kiện giữa các service: RabbitMQ + MassTransit
Camera AI / trình giả lập ──MQTT──► RabbitMQ (plugin MQTT) ──► parking (sau này cả booking)
Dùng chung: PostgreSQL (1 container), Redis, Aspire Dashboard (log, trace, metrics)
```

## 2. Frontend không cần tách

Chỉ backend chia thành microservices. Frontend giữ **một** ứng dụng React + Three.js, chỉ gọi gateway, nên backend chia bao nhiêu service cũng không ảnh hưởng.

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
| `/api/booking/**` | booking | Có |
| `/api/payment/vnpay/ipn` | payment | Không, payment tự kiểm tra chữ ký VNPay |
| `/api/payment/**` | payment | Có |
| `/api/notifications/**` | notification | Có |
| `/api/ai/**` | ai | Không |

Gateway bỏ tiền tố `/api/<service>` trước khi chuyển tiếp, ví dụ `/api/ai/events/recent` tới ai thành `/events/recent`. CORS cho phép `http://localhost:5173` (frontend lúc dev).

### 4.2 REST nội bộ (không qua gateway, chỉ trong mạng Docker)

| Gọi | Để làm gì |
|---|---|
| booking → parking `POST /internal/lots/{lotId}/assign-slot` | Xếp chỗ khi xe có đặt trước vào cổng (BR-15) |
| booking → payment `GET /internal/debts?plate=...&lotId=...` | Kiểm tra nợ trước khi cho xe vào (BR-05) |

Khi làm các lời gọi này, thêm gói `Microsoft.Extensions.Http.Resilience` để có retry và timeout.

### 4.3 Sự kiện qua RabbitMQ

Khai báo trong `contracts/ParkingSystem.Contracts/Events.cs`.

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

parking đọc bằng thư viện MQTTnet (`services/parking/Parking.Api/MqttSlotListener.cs`). Mỗi tin nhắn được chuyển thành sự kiện `SlotStatusChanged` và đẩy lên sơ đồ 3D qua SignalR (`slotStatusChanged`).

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
parking-system/
├─ contracts/ParkingSystem.Contracts/        # sự kiện dùng chung
├─ shared/ParkingSystem.ServiceDefaults/     # cấu hình chung cho mọi service .NET
├─ gateway/Gateway/                          # YARP
├─ services/
│  ├─ identity|parking|booking|payment|notification/
│  │  ├─ <Tên>.Api/                          # ASP.NET Core
│  │  ├─ <Tên>.Tests/                        # xUnit
│  │  └─ Dockerfile                          # build context là gốc repo
│  └─ ai/                                    # Python FastAPI
├─ edge/camera-simulator/                    # Python, gửi MQTT giả lập
├─ frontend/                                 # nhóm FE dựng React + Three.js
├─ deploy/                                   # docker-compose.yml, init-db.sh, enabled_plugins
├─ docs/                                     # tài liệu này, quy trình nhóm
├─ .github/                                  # workflow CI, mẫu pull request
├─ Directory.Build.props                     # net10.0, nullable, implicit usings
├─ Directory.Packages.props                  # phiên bản gói NuGet ở một chỗ
├─ nuget.config
├─ ParkingSystem.slnx
└─ .env.example
```

## 7. Chạy trên máy

1. Cài Docker Desktop. Nếu ổ C: ít chỗ, vào Settings → Resources → Advanced → Disk image location để chuyển dữ liệu sang ổ khác.
2. Tại thư mục `deploy`, chép `../.env.example` thành `.env` rồi điền giá trị (`JWT_SIGNING_KEY` dài ít nhất 32 ký tự).
3. `docker compose up --build`. Thêm `--profile sim` để chạy trình giả lập camera.
4. Kiểm tra:
   - Gateway: `http://localhost:8080/health`
   - Aspire Dashboard (log, trace): `http://localhost:18888`
   - RabbitMQ: `http://localhost:15672`
   - Luồng mẫu camera → parking → ai: `http://localhost:8080/api/ai/events/recent`

Chạy một service .NET ngoài Docker để debug: bật hạ tầng bằng compose, rồi đặt `ConnectionStrings__Db`, `Jwt__SigningKey`, `RabbitMq__*` bằng biến môi trường hoặc `dotnet user-secrets`. Service không đọc được `Jwt:SigningKey` thì dừng ngay khi khởi động.

## 8. Cấu hình chung (`shared/ParkingSystem.ServiceDefaults`)

Mỗi service .NET chỉ cần vài dòng trong `Program.cs`:

- `builder.AddServiceDefaults("<tên>")`: Serilog; OpenTelemetry cho trace và metrics; log, trace, metrics gửi về Aspire Dashboard khi có `OTEL_EXPORTER_OTLP_ENDPOINT`; health check; JWT.
- `builder.AddMessaging<TDb>()`: MassTransit + RabbitMQ, outbox và inbox trên DbContext của service. Consumer đặt trong project của service được đăng ký tự động.
- `app.UseServiceDefaults()`: log request, xác thực, phân quyền, endpoint `/health`.
- `app.EnsureDatabaseCreated<TDb>()`: tự tạo bảng khi chạy Development. Khi service có bảng nghiệp vụ thật thì chuyển sang EF Core migrations.

Phiên bản gói ghi ở `Directory.Packages.props`. MassTransit giữ bản 8 (mã nguồn mở); từ bản 9 là bản thương mại.

## 9. Quy ước chung

- .NET 10 (LTS), PostgreSQL 17, RabbitMQ 4. Python trong container là 3.12; máy dev có 3.10 trở lên cũng chạy test được.
- Tiền lưu bằng số nguyên (đồng). Thời gian lưu theo UTC, hiển thị theo giờ Việt Nam; quy tắc 22:00 (BR-04) tính theo giờ Việt Nam.
- JWT do identity ký (HS256, khóa ở `.env`); gateway và từng service đều kiểm tra.
- Không commit khóa thật: `.env` nằm trong `.gitignore`, chỉ commit `.env.example`. Khóa VNPay chỉ có ở payment.
- Gói NuGet mặc định lưu ở ổ C:. Muốn chuyển sang ổ khác thì đặt biến môi trường `NUGET_PACKAGES`, ví dụ `F:\dev\nuget-packages`.
- Git, commit, pull request: xem `docs/workflow.md`.

## 10. Kiểm thử và CI

- Unit test: `dotnet test ParkingSystem.slnx`; ai: `cd services/ai`, `pip install -r requirements-dev.txt`, `pytest`.
- Integration: chạy service với PostgreSQL và RabbitMQ thật (Testcontainers) khi bắt đầu có nghiệp vụ.
- Saga: cọc thất bại, hết hạn chờ cọc, IPN của VNPay đến trễ hoặc đến 2 lần.
- CI: mỗi service một workflow trong `.github/workflows/`, chỉ chạy khi thư mục của service (hoặc `contracts/`, `shared/`) thay đổi; chạy trên push vào `main`, `develop` và trên mọi pull request.
- CD chưa cần: lúc demo thì triển khai tay trên máy ảo Azure for Students bằng `git pull` rồi `docker compose up -d --build`.

## 11. Lưu ý

- **RAM:** cả hệ thống khoảng 12 container, nên có 16 GB. Máy 8 GB thì chỉ bật service đang làm.
- **IPN của VNPay cần URL công khai:** địa chỉ `https://<tên miền>/api/payment/vnpay/ipn` khai báo trong trang quản trị merchant của VNPay. Lúc dev dùng tunnel (Cloudflare Tunnel hoặc ngrok).
- **OSRM:** máy chủ demo công khai có giới hạn truy cập. Gọi nhiều thì cache kết quả hoặc tự chạy OSRM bằng Docker.
- **Máy ảo demo công khai:** chỉ mở cổng của gateway (8080, hoặc 80/443 qua reverse proxy). Không mở 18888 (Aspire Dashboard đang tắt đăng nhập), 15672 (RabbitMQ) và 5432 (PostgreSQL) ra Internet.
- **Kubernetes:** để giai đoạn sau.
