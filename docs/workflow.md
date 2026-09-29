# Quy trình làm việc của nhóm

Theo "Gitlab Guide" của mentor. Code, pull request và CI nằm trên GitHub, organization `FA26-OJT-SmartParking`, gồm 2 repo: `parking-system-be` (backend) và `parking-system-fe` (frontend). GitLab của FSoft Academy dùng để báo cáo công việc bằng Issue Board.

File này dùng chung cho cả 2 repo; repo FE dẫn link về đây.

## 1. GitLab Issue Board (báo cáo)

- **Label là story point.** Tạo 6 label `1`, `2`, `3`, `5`, `8`, `13` (Manage → Labels → New label). Điểm càng cao thì việc càng tốn công.
- **Milestone là sprint.** Tạo `Sprint 1` đến `Sprint 6`, mỗi sprint 2 tuần, có ngày bắt đầu và kết thúc (Plan → Milestones → New milestone).
- **Issue là user story.** Tạo ở Plan → Issues → New issue. Bắt buộc có Title, Assignee, Label (story point) và Milestone (sprint).
- **Task con:** mở issue → Add → tạo task cho từng phần việc (backend, frontend, test…).
- Mỗi PR trên GitHub dán link vào issue tương ứng. Issue đóng khi mọi task con xong và PR đã merge vào `develop`.

## 2. Nhánh

| Nhánh | Tạo từ | Merge vào | Dùng khi |
|---|---|---|---|
| `main` | — | — | Chỉ nhận merge từ `release/sprint_x` đã test; gắn tag `sprint-x` |
| `develop` | `main` | `release/sprint_x` | Tích hợp hằng ngày |
| `features/Implementation_<UserStory>` | `develop` | `develop` | Code một user story |
| `features/Design_<UserStory>` | `develop` | `develop` | Thiết kế: API (backend) hoặc Figma (frontend) |
| `hotfix/Bug_<UserStory>` | `main` | `main` và `develop` | Sửa lỗi nghiêm trọng |
| `release/sprint_x` | `develop` | `main` (và `develop` nếu có sửa) | Chuẩn bị demo sprint x, kèm release note |

Tên user story viết tiếng Anh, không dấu, dạng PascalCase. Ví dụ: `features/Implementation_BookParkingSpot`, `features/Design_BookParkingSpot`, `hotfix/Bug_DepositRefund`, `release/sprint_1`.

## 3. Commit

- Viết tiếng Anh theo Conventional Commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`, `ci:`, `build:`.
- Mỗi commit chỉ một thay đổi. Không commit kiểu "Update" hay "Fix bug" mà không nói sửa gì.
- Ví dụ: `feat: add reservation deposit via VNPay`, `fix: release held slot 10 minutes after gate entry`.
- Không commit `.env`, khóa VNPay, mật khẩu.

## 4. Pull request

- Tiêu đề có tiền tố: `[Feature] ...`, `[Fix] ...`, `[Refactor] ...`, `[Design] ...`.
- Nội dung dùng mẫu có sẵn trong `.github/pull_request_template.md` (Implementation). PR thiết kế thì thêm `?template=design.md` vào URL khi tạo PR.
- Điều kiện merge vào `develop`: CI xanh, ít nhất 1 người review, có link issue GitLab.
- Bảo vệ nhánh trên GitHub (chủ repo bật): Settings → Branches → thêm rule cho `main` và `develop`, chọn **Require a pull request before merging** (1 approval) và **Require status checks to pass**.

## 5. Làm việc với 2 repo

- Mỗi repo có `main`, `develop`, quy tắc nhánh ở mục 2 và CI riêng.
- User story chỉ sửa một phía (chỉ BE hoặc chỉ FE): làm ở repo đó.
- User story sửa cả BE và FE: tạo nhánh **cùng tên** ở cả 2 repo, ví dụ `features/Implementation_BookParkingSpot`. Mỗi repo một PR, dán cả 2 link vào issue GitLab. Merge PR của BE trước để API có sẵn, rồi mới merge PR của FE.
- Nhánh thiết kế `features/Design_<UserStory>`: ở repo BE cho thiết kế API (OpenAPI), ở repo FE cho Figma.
- Chạy FE trên máy: bật BE bằng Docker Compose (xem `docs/huong-dan-setup-microservices.md`), FE gọi gateway ở `http://localhost:8088`.

## 6. Release cuối sprint

1. Tạo `release/sprint_x` từ `develop` ở cả 2 repo; từ lúc này chỉ sửa lỗi trên nhánh release.
2. Viết release note `docs/releases/sprint_x.md`: user story đã xong, lỗi còn biết.
3. Demo với mentor từ nhánh release.
4. Ở từng repo: merge `release/sprint_x` vào `main`, gắn tag `sprint-x`. Nếu trên nhánh release có sửa lỗi thì merge ngược vào `develop`.

## 7. Tài liệu mỗi sprint (theo mẫu của mentor)

- **Sprint Planning** (docx): thời gian, mục tiêu, backlog (story point, ưu tiên, người làm, trạng thái), task breakdown, Definition of Done, sprint review, link retrospective.
- **UAT test case** (xlsx): trang bìa, báo cáo tổng, danh sách chức năng, mỗi chức năng một sheet (ID, mô tả, các bước, kết quả mong đợi, kết quả, ngày test, người test).
- **Team Retrospective** (xlsx): mỗi sprint một sheet (tên, vai trò, ưu điểm, hạn chế, cần sửa).
