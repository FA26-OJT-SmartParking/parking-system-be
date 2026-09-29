# Quy trình làm việc của nhóm

Nguồn: "Git Lab & Pull Request Guide" v1.0 của mentor (HauNK, 08/2025; file `Gitlab Guide.pdf` trong `docs/mentor/` của thư mục tổng). Các mục 1–4 chỉ chép lại những gì guide viết, kèm số mục của guide. Mục 5 liệt kê các điểm nhóm tự chọn, guide **không** quy định. Mục 6 mô tả các mẫu tài liệu mentor gửi kèm.

File này dùng chung cho cả 2 repo; repo FE dẫn link về đây.

## 1. GitLab Issue Board (guide mục 2)

- **Label** (2.1): dùng để đánh dấu công sức của một việc, gồm các mức 1, 2, 3, 5, 8, 13 điểm; điểm càng cao càng tốn công. Tạo ở Manage → Labels → New label, điền số vào Title.
- **Milestone** (2.2): mỗi milestone là một sprint, có ngày bắt đầu và ngày kết thúc. Tạo ở Plan → Milestones → New milestone.
- **Issue** (2.3): là một user story; muốn xong issue phải xong mọi task con của nó. Tạo ở Plan → Issues → New issue. Trường bắt buộc: Title (tên user story), Assignee (người chịu trách nhiệm chính), Labels (một trong các label đã tạo), Milestones (sprint).
- **Task con** (2.4): chọn issue cần chia nhỏ, bấm Add, nhập thông tin rồi Create Task.

## 2. Nhánh (guide mục 3.1, 3.2)

- **main**: chỉ nhận nhánh đã test kỹ và được chấp nhận, thường là nhánh Release.
- **develop**: nhận nhánh Feature và Hotfix.
- **Feature**: phát triển tính năng mới, xong thì merge vào develop. Một tính năng nên có 2 nhánh: `features/Implementation_UserStoryName` (viết code) và `features/Design_UserStoryName` (tạo hoặc cập nhật thiết kế). Nhánh design của Front-End cần có Figma Design; của Back-End cần có API Design.
- **Hotfix**: sửa lỗi nghiêm trọng, đặt tên `hotfix/Bug_UserStoryName`; test local xong thì merge vào main và develop.
- **Release**: chuẩn bị tính năng để phát hành, tạo từ develop, dùng trong Sprint Demo, tên `release/sprint_x` (x là số sprint, ví dụ `release/sprint_1`). Mỗi nhánh release phải có release note.

## 3. Commit (guide mục 3.3)

- Đủ rõ để hiểu thay đổi gì.
- Tránh commit "Update", "Fix bug" mà không biết cập nhật hay sửa cái gì.
- Tập trung vào một thay đổi; mỗi commit chỉ một loại việc (Add Feature, Fix Bug, Refactor…).
- Tránh gộp nhiều thay đổi không liên quan vào một commit.

## 4. Pull request (guide mục 4)

Gộp một tính năng mới của sprint luôn cần pull request. PR phải theo đúng mẫu của guide, copy nguyên văn, chỉ viết lại phần **Change Description** theo tính năng của PR.

- Mẫu Implementation (guide 4.1): `.github/pull_request_template.md` (cũng có ở `.github/PULL_REQUEST_TEMPLATE/implementation.md`).
- Mẫu Design (guide 4.2): `.github/PULL_REQUEST_TEMPLATE/design.md`; khi tạo PR trên GitHub thêm `?template=design.md` vào URL.

## 5. Nhóm tự chọn, ngoài guide (cần nhóm hoặc mentor xác nhận)

| Điểm | Đang áp dụng | Nguồn |
|---|---|---|
| Nơi chứa code, PR, CI | GitHub, organization `FA26-OJT-SmartParking`, 2 repo `parking-system-be` và `parking-system-fe`; GitLab chỉ để báo cáo bằng Issue Board | Quyết định của nhóm |
| Ngôn ngữ commit | Tiếng Anh | Quyết định của nhóm |
| Tiền tố `feat:`, `fix:`, `docs:`… trước commit | Có dùng (kiểu Conventional Commits) | Đề xuất, guide không yêu cầu |
| Tag khi merge release vào main | Chưa chốt; hình minh họa trong guide có tag kiểu `1.0`, `1.0.1`, `1.1.0` | Chưa có quy ước |
| Vị trí release note | Chưa chốt (guide chỉ nói phải có release note) | Chưa có quy ước |
| Số sprint, độ dài sprint | Chưa chốt; mẫu Sprint Planning của mentor chỉ là ví dụ của nhóm khác (2 tuần) | Chưa có |
| Bảo vệ nhánh `main`, `develop` trên GitHub, số người review | Chưa bật | Đề xuất, guide không yêu cầu |
| Nhánh cùng tên ở BE và FE cho user story sửa cả hai phía, thứ tự merge | Chưa chốt | Đề xuất |
| CI (GitHub Actions) | Guide chỉ ghi dòng "CI/CD pipeline passes successfully" trong Definition of Done, không quy định pipeline. Các workflow trong `.github/workflows/` là thiết kế của nhóm | Đề xuất; đến 29/09/2026 chưa chạy lần nào trên GitHub |

## 6. Tài liệu mỗi sprint (mẫu mentor gửi kèm)

Ba file mẫu nằm ở `docs/mentor/` của thư mục tổng. Các file này là ví dụ của nhóm khác, dùng làm khuôn.

- **Sprint Planning** (`Project Team - Sprint x Planning.docx`): I Sprint Duration, II Sprint Goals, III Sprint Backlog (No, User Story, Story Points, Priority, Assigner, Status), IV Task Breakdown, V Definition of Done, VI Sprint Review, VII Sprint Retrospective (link Google Sheet).
- **UAT test case** (`TestTeam_UAT.xlsx`): sheet Cover, Test Report, Test Case List, rồi mỗi chức năng một sheet đặt tên dạng `Vai trò_Chức năng`. Cột của sheet chức năng: ID, Test Case Description, Test Case Procedure, Expected Results, Inter-test case Dependence, Result, Test date, Tester, Number of tests, Passed, % Passed, Note.
- **Team Retrospective** (`Team_Retrospective.xlsx`): mỗi sprint một sheet; cột Tên, Vai trò, Ưu điểm, Hạn chế, Cần chỉnh sửa, Ghi chú.
