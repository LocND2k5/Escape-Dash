# Báo cáo dự án Escape Dash

## 1. Giới thiệu
- **Escape Dash** là game 3D endless runner 3 làn (kiểu Subway Surfers) làm bằng Unity `6000.3.9f1`, render URP 17.3.0, có cấu hình PC và Mobile.
- Đồ án nhóm môn GDC301 (tài liệu thiết kế: `GDC301_SE1984_Group1_GAME DESIGN DOCUMENT.docx`).
- Phiên bản hiện tại: 0.1.0.

**Phạm vi báo cáo:** chỉ tính hai scene chính thức là **MenuScence** (menu) và **Loc** (gameplay). Các scene `Linh`, `Tu`, `Loc 1`, `SampleScene` và `_Recovery` chỉ để test, không thuộc game.

> Lưu ý: Build Settings hiện đang liệt kê `MenuScence` và **`Loc 1`**, chưa có `Loc`. Cần sửa lại trước khi build (xem mục 7).

## 2. Luồng game
1. **MenuScence:** menu chính, nhập/đổi tên người chơi, bảng xếp hạng, thoát game.
2. **Loc:** vào chơi, chạy trên 3 làn, né vật cản, nhặt coin và power-up.
3. **Game Over / Hồi sinh:** hỏi hồi sinh bằng coin (đếm ngược 5 giây), sau đó hiện bảng Game Over với nút Chơi lại, Về menu, Bảng xếp hạng.
4. Điểm được gửi lên leaderboard online.

## 3. Luật chơi
| Mục | Chi tiết |
|---|---|
| Điều khiển | Đổi làn A/D hoặc ←/→; nhảy W/Space/↑; trượt S/↓ (bàn phím, chưa có cảm ứng) |
| Cơ chế | Map cuộn về phía người chơi, người chơi đứng yên trên trục Z |
| Tốc độ | Tăng dần theo thời gian (tối đa 20 ở `PlayerController`; `ChunkSpawner` có thông số tốc độ riêng, xem mục 7) |
| Vật cản Light | Vấp: tốc độ còn 40% rồi hồi lại trong 2 giây; miễn nhiễm 0,8 giây |
| Vật cản Heavy | Thua ngay |
| Vấp lần 2 trong 4 giây | Game Over |
| Hồi sinh | Tốn 50 coin tích lũy; reset về làn giữa và sinh đường an toàn |
| Điểm | 1 điểm/mét + 10 điểm/coin; lưu HighScore, BestDistance, TotalCoins bằng PlayerPrefs |

**Power-up** (`PowerUpManager`):
- Nam châm (10 giây): hút coin trong bán kính 8.
- Nhân đôi điểm (10 giây).
- Siêu nhảy (8 giây): nhảy cao gấp 1,8 lần.

Có thêm chu kỳ ngày đêm (`DayNightCycle`).

## 4. Kiến trúc mã (các script thực sự dùng trong 2 scene)
Các hệ thống giao tiếp qua lớp sự kiện tĩnh `GameEvents` (`OnGameOver`, `OnPlayerHit`, `OnPlayerRevived`, `OnCoinCollected`...).

**Scene MenuScence:** `MenuSceneManager`, `LeaderboardUI`, `UGSLeaderboardManager`.

**Scene Loc:**
| Script | Vai trò | Người làm |
|---|---|---|
| `PlayerController` | Đổi làn, nhảy, trượt, xử lý vấp, tốc độ | Lộc |
| `ReviveManager` | Hồi sinh, panel Game Over | Lộc |
| `ChunkSpawner` + `Chunk` | Sinh/tái sử dụng đoạn đường (pool, shuffle bag), tăng tốc | Linh |
| `ScoreManage` | Quãng đường, coin, điểm, lưu kỷ lục | Linh |
| `PowerUpManager`, `PowerUpHUD`, `CoinMagnet` | Power-up | Tú |
| `GameUIManager`, `AudioManager` | Pause, cài đặt, âm lượng, âm thanh | Khang |

**Prefab dùng trong đường:** 6 chunk (`Linh/Prefab/Chunk`), vật cản (`Obstacle`), coin (`Coin`), power-up (`PowerUpPickup`).

**Online:** Unity Gaming Services, đăng nhập ẩn danh, leaderboard ID `High_Score`.

**Package chính:** Cinemachine 3.1.7, Input System 1.19, UGS Authentication 3.3.3, UGS Leaderboards 2.2.1, ProBuilder 6.0.9.

Quy mô: khoảng 3.800 dòng C# (27 file) và 22 prefab, trong đó một phần không dùng ở hai scene chính.

## 5. Phân công và tiến độ
- 122 commit, từ 30/09/2026 đến 02/11/2026, cao điểm 13–16/10.
- Theo commit: LocND2k5 58, chubaokhang1611-beep 36, TuSanta78 20, manhlinhsinhdoi 10.
- Quy trình: mỗi người một nhánh chính và một nhánh backup; hỏi nhóm trước khi merge vào `main`.

## 6. Hiện trạng
**Đã có:** chạy 3 làn, nhảy/trượt, vật cản hai mức, coin, 3 power-up, hồi sinh, điểm và kỷ lục, menu, pause/cài đặt/âm lượng, leaderboard online, ngày đêm, âm thanh (nhạc nền và 6 SFX).

## 7. Vấn đề cần xử lý
1. **Build Settings sai scene:** đang là `MenuScence` + `Loc 1`, cần đổi thành `MenuScence` + `Loc`.
2. **Con Bò không có trong game:** `ChaserController` không được gắn vào scene `Loc` hay prefab nào. Sự kiện `OnPlayerStumbled` cũng không ai lắng nghe. Vì vậy cơ chế "Bò rượt" như mô tả trong code chưa hoạt động; chỉ còn hiệu ứng giảm tốc và luật vấp hai lần. Cần quyết định: gắn Bò vào scene hoặc bỏ khỏi thiết kế. Nếu bỏ, các đoạn code liên quan đến Bò chỉ còn là mã thừa.
3. **Hai bộ tốc độ:** `PlayerController` (8 → 20) và `ChunkSpawner` (10 → 30) đều có tốc độ riêng. `ChunkSpawner` mới là bộ thật sự di chuyển đường nên giá trị của `PlayerController` có thể gây hiểu lầm khi cân bằng. Mình chưa kiểm tra xem hai giá trị này có được đồng bộ ở đâu không.
4. **Chưa hỗ trợ cảm ứng:** dùng `Input` cũ dù có cấu hình Mobile.
5. **Dọn dự án:**
   - Xóa hoặc đưa ra nhánh riêng: scene test, `Assets/_Recovery`, `Assets/Editor` (rỗng), `TutorialInfo`.
   - Mã không dùng: `TrackManager`, `ChunkRandomizer`, `DrawLanes`, `Coin_inactive`, `ChaserController`.
   - Sửa chính tả `MenuScence`, `Obstcale_Death`.
   - Bỏ `Debug.Log` trong `Obstacle`.
6. **Bảo mật leaderboard:** điểm do client gửi nên có thể bị giả mạo; cần cấu hình đúng ID `High_Score` trên Unity Dashboard.
7. **Chưa chạy thử:** báo cáo dựa trên đọc mã và scene, chưa mở Unity để chơi thử nên chưa xác nhận hành vi thực tế.

## 8. Đề xuất bước tiếp theo
1. Sửa Build Settings, chốt scene `Loc` là bản gameplay chính thức.
2. Quyết định về con Bò (gắn vào hoặc bỏ).
3. Thêm điều khiển cảm ứng nếu nhắm tới mobile.
4. Dọn tài nguyên và mã thừa, chơi thử toàn bộ luồng Menu → Chơi → Hồi sinh → Game Over → Leaderboard.
5. Build thử và kiểm tra hiệu năng trên thiết bị.
