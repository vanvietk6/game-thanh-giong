# THÁNH GIÓNG — Game hành động 2.5D (Unity 6, URP)

Game hành động góc nhìn chéo (isometric 2.5D) dựa trên truyền thuyết Thánh Gióng. Người chơi điều khiển
Gióng cưỡi ngựa sắt, đánh qua 5 đợt giặc Ân để bảo vệ làng Phù Đổng. Đợt cuối là trận đấu với tướng giặc.

## Cách chạy

1. Mở project bằng Unity Hub (Unity **6000.6.6f1**).
2. Trong cửa sổ Project, mở `Assets/Scenes/ThanhGiong.unity` (nhấp đúp).
3. Bấm nút **Play** ▶.

> Toàn bộ nhân vật, bối cảnh, giao diện và âm thanh đều được **tạo bằng code** khi chạy, nên không cần
> kéo thả gì trong Editor. Bấm Play ở `SampleScene` cũng chạy được (có script tự khởi động).

- Tạo lại scene: menu **Thanh Giong → Tao lai Scene game**
- Build ra file .exe: menu **Thanh Giong → Build ban Windows** (kết quả nằm trong thư mục `Build/`)
- Chế độ tự chơi để kiểm thử: chạy `Build/ThanhGiong.exe -autotest` (AI tự điều khiển Gióng, chạy nhanh gấp 2,5 lần)
  - thêm `-god`: Gióng bất tử (để kiểm tra nhánh chiến thắng)
  - thêm `-shots <thư mục>`: tự chụp màn hình mỗi 9 giây (dùng làm hình minh họa cho báo cáo)

## Điều khiển

| Phím | Hành động |
|---|---|
| W A S D / mũi tên | Di chuyển |
| J / Chuột trái | Vung roi sắt (combo 3 đòn, đòn 3 quét rộng) |
| Space / Shift | Lướt né đòn (không bị trúng đòn khi lướt) |
| K / Chuột phải | Ngựa sắt phun lửa (tốn năng lượng Lửa, gây cháy) |
| E | Nhổ bụi tre ngà làm vũ khí trong 15 giây |
| Esc / P | Tạm dừng |
| H | Hướng dẫn |

Có hỗ trợ tay cầm (gamepad).

## Nội dung game

- **Cốt truyện**: 5 trang kể lại truyền thuyết trước khi vào trận.
- **5 đợt giặc**: lính giáo → cung thủ → lính khiên → hỗn hợp → tướng giặc Ân.
- **Gióng lớn lên**: sau mỗi đợt, Gióng to hơn, khỏe hơn, đánh xa hơn (theo chi tiết "lớn nhanh như thổi").
- **Chọn nâng cấp**: sau mỗi đợt chọn 1 trong 3 sức mạnh ngẫu nhiên (lấy từ 10 loại).
- **Nhổ tre ngà**: dựa theo chi tiết Gióng nhổ tre bên đường đánh giặc.
- **Nắm cơm làng Gióng**: rơi ra từ giặc, nhặt để hồi máu (theo chi tiết dân làng góp gạo nuôi Gióng).
- **Tướng giặc**: 3 đòn có vùng cảnh báo đỏ, dưới 50% máu thì nổi giận và gọi quân tiếp viện.
- **Kết thúc**: Gióng phi ngựa về hướng núi Sóc Sơn rồi bay lên trời.
- Kỷ lục điểm được lưu lại (PlayerPrefs).

## Kiến trúc mã nguồn (`Assets/ThanhGiong/Scripts`)

```
Core/
  GameManager.cs      Bộ não game: khởi tạo hệ thống, máy trạng thái game, điểm số, luồng chơi
  GameBootstrap.cs    Tự khởi động game khi bấm Play ở scene trống
  InputReader.cs      Đọc bàn phím/chuột/tay cầm (Input System mới)
  CameraFollow.cs     Camera 2.5D góc chéo, bám mượt, rung màn hình
  AudioManager.cs     Âm thanh và nhạc nền tổng hợp bằng code (procedural audio)
Entities/
  Character.cs        Lớp cơ sở: máu, nhận đòn, đẩy lùi, nháy sáng, di chuyển có va chạm
  Player.cs           Thánh Gióng: combo, lướt, phun lửa, nhổ tre, lớn lên, hoạt ảnh
  Enemy.cs            Lớp cơ sở giặc: máy trạng thái AI, thanh máu, cháy, chết
  EnemyTypes.cs       Lính giáo, Cung thủ, Lính khiên (kế thừa Enemy)
  BossEnemy.cs        Tướng giặc Ân: 3 kiểu đòn + 2 giai đoạn
  Projectiles.cs      Mũi tên, nắm cơm hồi máu
Systems/
  WaveManager.cs      Định nghĩa và sinh các đợt giặc
  UpgradeSystem.cs    Kho nâng cấp, chọn ngẫu nhiên 3 lựa chọn
World/
  WorldBuilder.cs     Dựng làng Phù Đổng: lũy tre, nhà tranh, cây đa, giếng, cổng làng, núi
  ModelFactory.cs     Dựng mô hình low-poly từ khối cơ bản (Gióng, ngựa sắt, giặc, nhà…)
  Arena.cs            Va chạm hình tròn tự viết + biên đấu trường
  BambooClump.cs      Bụi tre có thể nhổ, tự mọc lại
  MatLib.cs           Bảng màu + cache Material
FX/
  FXManager.cs        Hệ hạt tự viết (object pool), vùng cảnh báo, vệt chém, hitstop
UI/
  UIManager.cs        Toàn bộ giao diện uGUI dựng bằng code
```

## Các kỹ thuật nên trình bày trong báo cáo

1. **Lập trình hướng đối tượng**: `Character` → `Player` / `Enemy` → `SpearmanEnemy`, `ArcherEnemy`,
   `ShieldEnemy`, `BossEnemy` (kế thừa, đa hình qua `Configure`, `Think`, `ModifyDamage`).
2. **Máy trạng thái hữu hạn (FSM)**:
   - Trạng thái game: Menu → Story → Playing ⇄ Paused / Upgrade → GameOver / Victory.
   - AI giặc: Spawning → Chase → Windup → Attack → Recover.
3. **AI hành vi**: lính giáo áp sát; cung thủ giữ khoảng cách, đi ngang và bắn đón đầu theo vận tốc
   người chơi; lính khiên đỡ đòn theo góc tới; tướng giặc chọn đòn theo khoảng cách.
4. **Va chạm tự viết**: hình tròn với hình tròn trên mặt phẳng XZ (`Arena.Resolve`), tách đám đông (separation).
5. **Kiểm tra trúng đòn theo hình quạt**: khoảng cách + góc giữa hướng nhìn và mục tiêu (`Vector3.Angle`).
6. **Game feel**: hitstop, rung màn hình, đẩy lùi, nháy trắng, số sát thương bay lên.
7. **Tối ưu**: object pooling cho hạt và chữ bay, cache Material, static batching cho cảnh tĩnh.
8. **Procedural generation**: mô hình, âm thanh, nhạc nền (thang ngũ cung) đều tạo bằng code.
9. **Camera 2.5D**: camera phối cảnh góc 50°, xoay 45°, điều khiển theo hướng camera.

## Tài nguyên

Không dùng asset bên ngoài. Nếu muốn thay mô hình đẹp hơn, có thể dùng các bộ miễn phí giấy phép CC0:
KayKit (kaylousberg.itch.io), Kenney (kenney.nl), Quaternius (quaternius.com) và hoạt ảnh từ Mixamo.
