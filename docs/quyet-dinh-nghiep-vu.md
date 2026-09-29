# Quyết định nghiệp vụ còn mở và giải pháp đề xuất

Các điểm mâu thuẫn giữa các tài liệu nguồn, kèm đề xuất để code không bị chặn trong lúc chờ mentor trả lời. Tài liệu này **chỉ đề xuất**; người quyết định là mentor hoặc nhóm (cột "Ai quyết").

## Nguồn đã đối chiếu (đọc nguyên văn ngày 29/09/2026)

| Nguồn | Ghi chú |
|---|---|
| `SRS.md` v1.1 (27/09) | Tự nhận "Source of Truth duy nhất". Trích dẫn dưới đây theo mã trong file (DP-xx, FR-xx, NFR-xx, BR-xx) |
| `context_v10.docx` (28/09) | Bản có các quyết định D1–D33 do nhóm duyệt. Trích dẫn theo mã quyết định hoặc mã BR trong bản này |
| `Ke-hoach-hop-nhat-SRS.md` | Kế hoạch hợp nhất SRS, có danh sách câu hỏi cần mentor duyệt ở mục 5 |
| `context_bai_do_xe_v1.2_microservice.md` | **Không còn trên máy**, chưa đối chiếu lại được. Mọi điểm liên quan v1.2 chờ có lại file |
| `Project.CleanArchitecture` | Template bắt buộc; chỉ liên quan đến điểm về JWT |

## Cách dùng

1. Mỗi điểm có một **mặc định trong code** để tiếp tục làm việc; mặc định này đổi được bằng cấu hình hoặc thay một adapter, không phải viết lại.
2. Khi mentor hoặc nhóm quyết, sửa file này trước (ghi ngày và người quyết), rồi mới sửa code và tài liệu khác.
3. Điểm nào chưa được duyệt thì không được ghi vào code như một quy tắc chắc chắn.

## Các điểm cần quyết

| # | Chủ đề | SRS v1.1 | context v10 | Đề xuất và mặc định trong code | Ai quyết | Ảnh hưởng code |
|---|---|---|---|---|---|---|
| Q1 | Broker MQTT cho camera/cảm biến | Mosquitto (DP-05, HW-04); SRS không nhắc RabbitMQ | Plugin MQTT của RabbitMQ (cùng một broker với sự kiện nghiệp vụ) | Cả hai đều là MQTT chuẩn. Code chỉ cần `Mqtt:Host`, `Mqtt:Username`, `Mqtt:Password` và cùng một topic contract (DP-05 yêu cầu simulator dùng cùng event contract). **Mặc định:** giữ plugin RabbitMQ (ít hơn một container, máy dev 16 GB đang chạy 12 container). Nếu mentor yêu cầu Mosquitto: thêm service `mosquitto` vào compose và đổi biến cấu hình, không đổi code (cổng trong code đang cố định 1883, cũng là cổng mặc định của Mosquitto) | Mentor | Thấp |
| Q2 | Ký JWT | RS256; access token 24 giờ, refresh token 7 ngày; đăng xuất thì thu hồi refresh token (NFR-SEC-003). Mật khẩu băm bằng bcrypt cost 12, dài ít nhất 8 ký tự gồm chữ hoa, chữ thường, số và ký tự đặc biệt (NFR-SEC-001). Đăng nhập sai 3 lần liên tiếp thì khóa tài khoản 15 phút (BR-15 của SRS) | Chỉ ghi "JWT" | **Đề xuất RS256 theo SRS:** chỉ Identity giữ khóa riêng, gateway và các service chỉ cần khóa công khai nên service khác không thể tự ký token; refresh token lưu trong DB để thu hồi. Template mẫu dùng khóa đối xứng (HS256) chỉ như ví dụ minh họa, còn `API Design Template` của login trả cả `accessToken` và `refreshToken`, khớp với SRS. Hiện code đang dùng HS256 một khóa chung. Login theo `API Design Template` chỉ có 3 lỗi 400 (thiếu userName, thiếu password, sai thông tin), nên việc khóa tài khoản sau 3 lần sai chưa làm cho tới khi có mẫu API cho nó. **Cần quyết trước khi làm Identity.** Nếu mentor chấp nhận HS256 thì giữ nguyên và chỉ đổi phần cấu hình khóa | Mentor | Trung bình (làm ngay ở Identity) |
| Q3 | Bản đồ và thời gian di chuyển | Google Maps Platform: Maps JavaScript, Geocoding, Routes (DP-02); nói rõ không dùng Mapbox trong baseline và đổi provider thì phải sửa SRS | OpenStreetMap + OSRM | Đặt việc tính khoảng cách/ETA sau một interface để đổi provider bằng cấu hình. **Mặc định:** OSM + OSRM vì dự án không có kinh phí (Google Maps Platform cần API key gắn với tài khoản thanh toán). Server OSRM công khai có giới hạn truy cập, cần cache hoặc tự chạy OSRM khi demo | Mentor | Chưa có code bản đồ nên chưa chặn việc gì |
| Q4 | Cổng thanh toán | VNPay Sandbox và MoMo Sandbox khi demo; webhook phải verify chữ ký và chống xử lý trùng (DP-04) | Thanh toán thật qua VNPay, nền tảng thu hộ cho chủ bãi; không có MoMo | Viết một adapter cổng thanh toán, làm VNPay trước. **Mặc định:** dev và demo chạy VNPay sandbox (TmnCode, HashSecret, URL nằm trong cấu hình); chuyển sang tài khoản merchant thật chỉ khi nhóm có tài khoản đó và mentor đồng ý. MoMo thêm adapter thứ hai sau (ưu tiên thấp). Việc nền tảng "thu hộ" tiền của chủ bãi có cần giấy phép hay không là câu hỏi pháp lý (mục 1.4 của v10), tài liệu này không kết luận | Nhóm và mentor | Chưa có code payment nên chưa chặn |
| Q5 | Đặt chỗ và cọc | BOOKING trả 100% phí khung giờ; HOLD cọc 30% phí ước tính, giữ 30 phút kể từ lúc thanh toán; cửa sổ thanh toán 15 phút (FR-RES-005, FR-RES-016, FR-PAY-015). Hoàn tiền: đã trả ≤ 30 phút, hoặc hủy trước giờ bắt đầu > 60 phút (BR-06) | 2 loại: giữ chỗ khi đang tới và đặt trước theo lịch (BR-22). Cọc do chủ bãi đặt, mặc định 20.000đ, cả hai loại cùng mức. Giữ chỗ hủy trong 5 phút sau khi đặt thì hoàn 100% cọc; đặt theo lịch hủy trước giờ hẹn từ 30 phút thì hoàn 100% (BR-13). Quá giờ hẹn 15 phút mà xe chưa vào cổng thì tự hủy và mất cọc. Sau khi xe vào cổng, chỗ đã xếp được giữ 10 phút (BR-15) | **Mô hình v10 làm gốc** (đã được nhóm duyệt), nhưng lưu chính sách cọc theo từng bãi dưới dạng dữ liệu: kiểu (số tiền cố định hoặc phần trăm) và giá trị; các mốc 5, 10, 15, 30 phút là tham số. Khi đó "30%" của SRS chỉ là phần trăm = 30 và "100%" là phần trăm = 100, không phải viết lại. Kế hoạch hợp nhất SRS (mục 5, câu 1) cũng ghi cọc HOLD 30% và chính sách hủy chờ mentor duyệt | Mentor | Cao khi làm Booking |
| Q6 | Mã BR trùng số nhưng khác nghĩa | BR-03 làm tròn phí đến 1.000đ; BR-05 quyết toán khi ra (`remaining = max(0, actual_fee - prepaid_amount)`); BR-06 hoàn tiền khi hủy; BR-15 khóa tài khoản sau đăng nhập sai. SRS có BR-01 đến BR-20 | BR-03 đã bỏ (miễn phí 10 phút), không đánh số lại; BR-05 nợ chỉ ở bãi bị nợ; BR-06 quy tắc ưu tiên khi nhiều nguồn báo trạng thái khác nhau (nhân viên xác nhận); BR-15 xếp chỗ khi xe vào cổng. v10 có BR-01 đến BR-24 (thiếu BR-03) | Lập một bảng đối chiếu mã một lần. Trong code và test không chỉ ghi số: ghi kèm nguồn và tên quy tắc (ví dụ `BR-05 (v10, debt only at the lot that is owed)`). Không đánh số lại v10 vì nhóm đã duyệt | Nhóm | Thấp, chỉ là cách đặt tên |
| Q7 | Khách vãng lai có được đặt chỗ không | Walk-in là "xe vào bãi mà không đặt trước" (mục thuật ngữ) | Chỉ khách đăng ký giữ chỗ hoặc đặt trước; khách vãng lai dùng dịch vụ tức thời | Kế hoạch hợp nhất SRS (mục 5, câu 4) ghi SRS của mentor cho khách vãng lai đặt chỗ. **Mặc định:** theo v10 (chỉ tài khoản đăng ký đặt chỗ), kiểm tra vai trò ở một điểm duy nhất trong Booking nên đổi được | Mentor | Trung bình khi làm Booking |
| Q8 | `AssignSlot` và `CheckDebt` | | BR-15 (xếp chỗ khi xe vào cổng) và BR-05 (nợ) | Hai lệnh gRPC này đang trả `Unimplemented` có chủ ý để không cho xe vào bằng một câu trả lời giả. Chỉ làm khi Q5 và Q6 đã chốt và có use case viết đủ (điều kiện, lỗi trả về) | Nhóm | Chờ Q5, Q6 |

Điểm về từ "gán" và "xếp chỗ" không phải mâu thuẫn nghiệp vụ: v10 dùng "xếp chỗ" (D33); `SRS.md` không dùng chữ "gán". Tên lệnh trong code vẫn là `AssignSlot`.

## Nguồn chuẩn

`SRS.md` tự nhận là nguồn duy nhất nhưng v10 mới hơn một ngày và chứa các quyết định nhóm đã duyệt; v1.2 hiện không còn file. Đề xuất: **trong lúc chờ mentor, file này là nguồn chuẩn cho việc viết code**, mỗi điểm đổi theo câu trả lời của mentor.

## Phiếu hỏi mentor (gửi một lần)

1. Q2: token ký RS256 (theo SRS) hay HS256 một khóa chung?
2. Q1: broker MQTT là Mosquitto riêng hay plugin của RabbitMQ?
3. Q3: bản đồ dùng Google Maps Platform hay OpenStreetMap + OSRM?
4. Q4: thanh toán thật hay sandbox khi demo; có cần MoMo không; việc thu hộ có vướng quy định không?
5. Q5: chính sách cọc theo mô hình nào (cố định theo bãi hay phần trăm), thời hạn giữ chỗ và mốc hoàn tiền?
6. Q7: khách vãng lai có được đặt chỗ không?
