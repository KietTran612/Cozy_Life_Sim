# Tài Liệu Mô Tả Dự Án & Quy Chuẩn Tạo Ảnh AI (Cozy Life Sim Project Spec & AI Asset Prompts)

> [!IMPORTANT]
> **DISCLAIMER:** Tài liệu này chỉ dùng để tạo asset/prompt, không phải implementation plan, không đổi branding/project name.

Tài liệu này cung cấp một mô tả toàn diện về bối cảnh, chức năng cốt lõi của dự án **Cozy Life Sim** (chủ đề di sản Việt Nam) và danh sách các tài nguyên hình ảnh (Sprites, Tileset) cần sinh bằng trí tuệ nhân tạo (AI) để hỗ trợ quá trình thiết kế mỹ thuật hoặc tái tạo/nâng cấp asset khi cần thiết. Để xem danh sách đầy đủ 54/54 asset hiện có, vui lòng tham khảo [required-assets.md](required-assets.md).

---

## 1. Quy Trình Sản Xuất Asset Cụ Thể (Asset Pipeline Specification)

Để đảm bảo chất lượng hình ảnh và tích hợp mượt mà vào Unity, toàn bộ tài nguyên đồ họa được phân thành 3 nhánh sản xuất (pipeline) chính với các quy chuẩn kỹ thuật bắt buộc dưới đây:

### Pipeline A: Gạch Địa Hình (Isometric 2.5D Tiles)
* **Mục đích:** Dùng cho lưới ô đất nền để người chơi vẽ bản đồ (Grass, Dirt, Red Brick).
* **Đặc tính kỹ thuật:** 
  * Phải ghép nối liền mạch đầy khung hình (`full-canvas seamless 2:1 tiling isometric pattern`).
  * **Kích thước output:** Theo từng dòng asset trong bảng; luôn giữ tỷ lệ 2:1 và không vượt quá `1024x1024 px`.
  * **Tuyệt đối KHÔNG CÓ VIỀN TRẮNG (`no border`)**.
  * **Tuyệt đối KHÔNG CÓ BÓNG ĐỔ (`no shadows`)**.
  * Không chứa nền đen cô lập mà họa tiết phải lấp đầy toàn bộ khung để khi ghép dạng gạch không lộ khớp nối.

### Pipeline B: Vật Thể Thế Giới & Sticker (World & UI Stickers)
* **Mục đích:** Vật phẩm trang trí, thú nuôi, cây trồng đứng trên sân vườn hoặc dán vào sổ tay Scrapbook.
* **Đặc tính kỹ thuật:**
  * **Phải có viền trắng dày nổi bật (`white border`)** bao quanh rìa đối tượng.
  * **Kích thước output:** Theo từng dòng asset trong bảng; không vượt quá `1024x1024 px`.
  * Nền xung quanh trong suốt (`transparent background`) hoặc đơn sắc tương phản để dễ tách nền sạch.
  * **Tuyệt đối KHÔNG CÓ BÓNG ĐỔ (`no shadows`)** trong ảnh AI; bóng đổ thực tế được tạo tự động bằng code trong Unity.
  * Góc nhìn chính diện phẳng (Front view) hoặc 3/4 nhẹ nhàng, tâm đặt ảnh (Pivot) cấu hình ở Bottom Center (với vật thể thế giới) hoặc Center (với sticker sổ tay).

### Pipeline C: Khung Giao Diện & Nút Bấm (UI Panels & Buttons 9-Slice)
* **Mục đích:** Khung gỗ panel lớn, nút chuyển tab trong cửa hàng tạp hóa.
* **Đặc tính kỹ thuật:**
  * **Thiết kế an toàn góc (Corner-safe):** Các hoa văn họa tiết chi tiết, đường cong chỉ vẽ ở 4 góc của khung.
  * **Kích thước output:** Theo từng dòng asset trong bảng; không vượt quá `1024x1024 px`.
  * **Vùng co giãn an toàn (Stretchable center):** Các cạnh dọc, cạnh ngang và tâm khung phải phẳng, trơn nhẵn hoặc chứa đường nét đồng nhất đơn giản để Unity có thể kéo giãn 9-slice mà không làm vỡ, méo mó họa tiết chính.
  * Có viền ngoài màu trắng hoặc viền gỗ phẳng đồng đều. KHÔNG chứa bóng đổ hay chi tiết trang trí ngẫu nhiên ở phần rìa co giãn.

---

## 2. Bối Cảnh & Phong Cách Nghệ Thuật (Aesthetic & Style)

* **Tên dự án:** Cozy Life Sim.
* **Thể loại:** Cozy Life Simulation, Trang trí sổ tay (Scrapbook), Trồng trọt nhẹ nhàng (Farming Idle), Sưu tầm (Collection).
* **Bối cảnh:** Làng quê Việt Nam hoài niệm thập niên 2000 (với ruộng lúa, giàn mía ngọt, tiếng gà kêu, quầy tạp hóa đơn sơ, và những câu chuyện xưa của Bà Ngoại).
* **Định dạng màn hình:** Màn hình ngang (Landscape-Only).
* **Mô hình dựng cảnh (Pop-up Book Approach):**
  * Nền đất dạng gạch nghiêng Isometric 2.5D vẽ tay, bên trên là các vật thể 2D phẳng dạng Sticker dựng đứng tạo cảm giác nổi sinh động.

---

## 3. Các Chức Năng Cốt Lõi Của Project (Core Features)

Dự án Cozy Life Sim hiện hoạt động với các cơ chế chính sau:

### 3.1. Hệ thống Trồng Trọt (Farming System)
* **Chức năng:** Người chơi mua hạt giống, gieo xuống các ô đất trồng (`World_Soil_Plot`), tưới nước và thu hoạch nông sản.
* **Chu kỳ sinh trưởng:** Mỗi nông sản có 3 giai đoạn:
  * **Hạt giống (Seed):** Thể hiện túi hạt giống hoặc hom mía.
  * **Cây non (Sprout):** Chồi non xanh mướt nhô lên khỏi đất.
  * **Cây chín (Mature / Harvest):** Bụi cây trưởng thành trĩu hạt/quả để thu hoạch.
* **Các nông sản di sản:** Hoa sen Việt Nam, Cây mía ngọt, Lúa nước.

### 3.2. Hệ thống Chăn Nuôi (Animal & Petting System)
* **Chức năng:** Nuôi thú tại chuồng trại (`World_Animal_Pen`). Thú cưng thở phập phồng nhẹ nhàng. Khi vuốt ve (Click), thú nhảy lên, nảy tim đỏ (`Heart Feedback`) và thưởng xu.
* **Vật nuôi di sản:** Mèo tam thể, Trâu nước Việt Nam.

### 3.3. Hệ thống Sổ Tay Trang Trí & Viết Nhật Ký (Scrapbook & Diary Notes)
* **Chức năng:** Trang trí các trang sổ giấy tập học sinh, giấy ố vàng bằng cách kéo thả, co giãn, xoay và xóa các Sticker Việt Nam hoài niệm. Người chơi tự viết nhật ký lên các mảnh giấy ghi chú dán kèm.

### 3.4. Hệ thống Nhiệm Vụ & Đối Thoại NPC (Quests & NPC Dialogue System)
* **Chức năng:** Nhận nhiệm vụ từ bảng gỗ thế giới (`World_Quest_Board`) để nhận xu, XP, sticker. Click vào Bà Ngoại hoặc Cô Ba để hiển thị bong bóng đối thoại typewriter.

### 3.5. Hệ thống Cửa Hàng Tạp Hóa (Shop System)
* **Chức năng:** Tiệm tạp hóa Cô Ba (`World_Shop_Stall`) chia làm 3 Tab: Mua hạt giống, Mua sticker trang trí, Bán nông sản thu hoạch.

---

## 4. Danh Sách Các Asset Tiêu Biểu & Trạng Thái Hiện Tại (Selected Key Assets & Prompts)

Dự án đã tích hợp đầy đủ 54/54 asset di sản Việt Nam cơ bản (được liệt kê chi tiết trong [required-assets.md](required-assets.md)). Dưới đây là bảng tổng hợp các asset tiêu biểu kèm Prompt AI gợi ý để hỗ trợ tái tạo, nâng cấp mỹ thuật hoặc tạo thêm gạch nền mới.

### 4.1. Nhóm 1: Hệ Thống Gạch Địa Hình (Isometric 2.5D Tiles - Đất Nền Mới)

*Áp dụng Pipeline A (seamless, 2:1 tile, no border, no shadows).*

| Tên Asset | Trạng Thái / Tên File Tham Chiếu | Kích Thước Đề Xuất | Mục Đích | Prompt AI Đề Xuất |
| :--- | :--- | :--- | :--- | :--- |
| **Gạch Cỏ Làng Quê** | **Mới (Cần Tạo)** | `1024x512 px` | Nền cỏ xanh mộc mạc làm nền chính sân vườn. | `Full-canvas seamless tiling 2:1 isometric tile pattern of lush green meadow grass with tiny yellow wildflowers, top-down angle, high quality continuous surface, cute hand-drawn papercraft style, soft pastel colors, no border, no shadows, mobile game texture` |
| **Đường Đất Nện** | **Mới (Cần Tạo)** | `1024x512 px` | Đường đi đất sét vàng nông thôn nối các khu vực. | `Full-canvas seamless tiling 2:1 isometric tile pattern of dry countryside dirt clay road path, top-down angle, high quality continuous surface, cute hand-drawn papercraft style, warm brown colors, no border, no shadows, mobile game texture` |
| **Sân Gạch Đỏ** | **Mới (Cần Tạo)** | `1024x512 px` | Lát sân gạch đỏ truyền thống trước nhà. | `Full-canvas seamless tiling 2:1 isometric tile pattern of rustic red brick pavement, traditional Vietnamese courtyard tiles, top-down angle, high quality continuous surface, cute hand-drawn papercraft style, soft warm red colors, no border, no shadows, mobile game texture` |

### 4.2. Nhóm 2: Vật Thể Thế Giới & Nông Nghiệp Tiêu Biểu (World Objects & Crops)

*Áp dụng Pipeline B (white border, transparent background, no shadows).*

| Tên Asset | Trạng Thái / Tên File Tham Chiếu | Kích Thước Đề Xuất | Mục Đích | Prompt AI Đề Xuất |
| :--- | :--- | :--- | :--- | :--- |
| `World_Soil_Plot.png` | Đang có sẵn trong project | `768x768 px` | Ô đất trồng rau viền gỗ. | `2D cute cartoon game asset of a neat square plot of fertile dark soil, rustic wooden garden border, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `World_Animal_Pen.png` | Đang có sẵn trong project | `768x768 px` | Hàng rào gỗ chuồng gà/mèo. | `2D cute cartoon game asset of a small cozy wooden fence animal pen with green grass floor, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `World_Quest_Board.png` | Đang có sẵn trong project | `768x768 px` | Bảng gỗ cắm đất ghi nhiệm vụ. | `2D cute cartoon game asset of a rustic wooden bulletin board standing on a post, cute small paper notes pinned on it, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `World_Shop_Stall.png` | Đang có sẵn trong project | `1024x768 px` | Quầy hàng tạp hóa bạt sọc. | `2D cute cartoon game asset of a cozy small village shop stall with a striped pastel canvas awning, wooden crates of crops on the ground, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `World_Character_Grandma_Idle.png` | Đang có sẵn trong project | `768x1024 px` | Sprite Bà Ngoại đứng ngoài sân. | `2D cute cartoon game asset of a cozy old Vietnamese grandma character, grey hair in a bun, wearing a traditional brown Ao Ba Ba, standing posture, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `World_Character_CoBa_Idle.png` | Đang có sẵn trong project | `768x1024 px` | Sprite Cô Ba đứng thế giới. | `2D cute cartoon game asset of a friendly young Vietnamese woman character, long black hair, wearing a traditional colorful Ao Dai or Ao Ba Ba, standing posture, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `World_Quest_Fence.png` | **Mới (Cần Tạo)** | `768x512 px` | Hàng rào gỗ/tre đặt đứng (World Object). | `2D cute cartoon game asset of a small rustic bamboo fence, standing posture, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Sugarcane_Seed.png` | Đang có sẵn trong project | `512x512 px` | Hom mía giống để gieo trồng. | `2D cute cartoon game asset of sugarcane seed cuttings, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Sugarcane_Sprout.png` | Đang có sẵn trong project | `512x512 px` | Mầm mía non mới nhú. | `2D cute cartoon game asset of a young sugarcane sprout, green shoots, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Sugarcane_Mature.png` | Đang có sẵn trong project | `768x768 px` | Bụi mía chín sẵn sàng thu hoạch. | `2D cute cartoon game asset of a tall mature sugarcane bundle, lush green leaves, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Rice_Seed.png` | Đang có sẵn trong project | `512x512 px` | Hạt thóc giống lúa nước. | `2D cute cartoon game asset of ripe golden rice grains, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Rice_Sprout.png` | Đang có sẵn trong project | `512x512 px` | Khóm mạ non xanh mướt. | `2D cute cartoon game asset of young green rice seedlings, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Rice_Mature.png` | Đang có sẵn trong project | `768x768 px` | Bông lúa nước trĩu hạt vàng óng. | `2D cute cartoon game asset of golden ripe rice paddy stalks heavy with grains, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Lotus_Seed.png` | Đang có sẵn trong project | `512x512 px` | Bát sen/hạt sen khô làm giống. | `2D cute cartoon game asset of dried lotus seeds in a pod, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Lotus_Sprout.png` | Đang có sẵn trong project | `512x512 px` | Lá sen non nổi trên mặt nước. | `2D cute cartoon game asset of a small green lotus leaf floating on water, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Crop_Lotus_Mature.png` | Đang có sẵn trong project | `768x768 px` | Bông hoa sen hồng nở rộ rực rỡ. | `2D cute cartoon game asset of a beautiful blooming pink lotus flower, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Animal_CalicoCat.png` | Đang có sẵn trong project | `768x768 px` | Mèo tam thể ngồi dễ thương. | `2D cute cartoon game asset of a calico cat sitting cozy, fur orange black white, round eyes, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Animal_WaterBuffalo.png` | Đang có sẵn trong project | `1024x768 px` | Trâu nước Việt Nam đứng trên cỏ. | `2D cute cartoon game asset of a friendly water buffalo, dark gray skin, curved horns, big warm eyes, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |

### 4.3. Nhóm 3: Chân Dung Hội Thoại & Sticker Tiêu Biểu (UI & Scrapbook)

*Khung UI áp dụng Pipeline C (9-slice stretch-safe). Sticker áp dụng Pipeline B (white border, no shadows).*

| Tên Asset | Trạng Thái / Tên File Tham Chiếu | Kích Thước Đề Xuất | Mục Đích | Prompt AI Đề Xuất |
| :--- | :--- | :--- | :--- | :--- |
| `NPC_Portrait_Grandma.png` | Đang có sẵn trong project | `512x512 px` | Chân dung hội thoại Bà Ngoại. | `2D cute cartoon game portrait of a cozy old Vietnamese grandma, smiling warmly, wearing a traditional brown Ao Ba Ba, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated, mobile game icon format` |
| `NPC_Portrait_CoBa.png` | Đang có sẵn trong project | `512x512 px` | Chân dung hội thoại Cô Ba. | `2D cute cartoon game portrait of a friendly young Vietnamese woman, long black hair, wearing a traditional colorful Ao Dai, smiling warmly, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated, mobile game icon format` |
| `Sticker_BanhMiCart.png` | Đang có sẵn trong project | `512x512 px` | Sticker Xe Bánh Mì dán sổ tay. | `2D cute cartoon game asset of a traditional Vietnamese Banh Mi cart, glass display with warm crispy bread inside, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Sticker_SugarcaneJuice.png` | Đang có sẵn trong project | `512x512 px` | Sticker Ly Nước Mía dán sổ tay. | `2D cute cartoon game asset of a green sugarcane juice glass with a straw and lemon slice, ice cubes, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Sticker_ConicalHat.png` | Đang có sẵn trong project | `512x512 px` | Sticker Chiếc Nón Lá dán sổ tay. | `2D cute cartoon game asset of a traditional conical straw hat, dry palm leaf texture, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Sticker_Cyclo.png` | Đang có sẵn trong project | `768x768 px` | Sticker Xe Xích Lô dán sổ tay. | `2D cute cartoon game asset of a classic Vietnamese Cyclo, black metal frame, red seat cushion, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `Sticker_StarLantern.png` | Đang có sẵn trong project | `512x512 px` | Sticker Lồng Đèn Ông Sao. | `2D cute cartoon game asset of a traditional Vietnamese star-shaped red and yellow paper lantern, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `UI_Panel_Frame_Wood.png` | Đang có sẵn trong project | `1024x1024 px` | Khung Panel gỗ co giãn (9-slice). | `2D cute cartoon game asset of a square panel board frame made of light wood with rounded corners, clean uniform borders, safe empty stretchable center for 9-slice, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated, mobile game UI format` |
| `UI_Tab_Button_Bg.png` | Đang có sẵn trong project | `512x256 px` | Nền nút bấm chuyển tab (9-slice). | `2D cute cartoon game asset of a rounded rectangular wooden tab button background, clean uniform borders, empty stretchable center, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated, mobile game icon format` |
| `UI_Button_Close_X.png` | Đang có sẵn trong project | `256x256 px` | Nút đóng chữ X gỗ tròn. | `2D cute cartoon game asset of a wooden round close button with a brown X mark, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `System_Coin_Vietnamese.png` | Đang có sẵn trong project | `256x256 px` | Đồng xu cổ lỗ vuông hiển thị tiền. | `2D cute cartoon game asset of a shiny gold coin with a square hole in the center, cozy aesthetic, white border, no shadows, transparent background, stylized vector art, isolated` |
| `UI_Quest_Stamp_Completed.png` | Đang có sẵn trong project | `512x512 px` | Nhãn đóng dấu "HOÀN THÀNH" đỏ. | `2D cute cartoon game asset of a circular red ink stamp saying "HOAN THANH" in a cozy hand-drawn font, star details, cozy aesthetic, no shadows, grunge ink texture, transparent background, isolated, mobile game UI format` |

---

## 5. Ngữ Cảnh Cho AI Tạo Ảnh (AI Image Generation Context)

AI image generators **không cần truy cập Unity project, không cần hiểu đường dẫn local, và không được yêu cầu lưu ảnh vào thư mục project**. Tài liệu này chỉ cung cấp ngữ cảnh sáng tạo, danh sách ảnh đã có, tên file tham chiếu và prompt mẫu để sinh ảnh khi cần.

1. **Vai trò của tên file trong bảng:**
   * Tên file như `World_Soil_Plot.png`, `Crop_Rice_Mature.png`, hoặc `UI_Panel_Frame_Wood.png` chỉ là **tên tham chiếu mong muốn** để developer nhận diện asset sau khi AI tạo ảnh.
   * AI chỉ cần trả về ảnh PNG độc lập hoặc kết quả tải xuống tương ứng với prompt. Developer sẽ tự import, đặt tên, cấu hình và kiểm tra trong Unity.
2. **Vai trò của kích thước đề xuất:**
   * Kích thước trong bảng là **kích thước canvas output đề xuất cho AI**, không phải đường dẫn lưu trữ hay bằng chứng rằng asset cũ đang có đúng kích thước đó.
   * Ưu tiên tạo đúng kích thước đề xuất nếu công cụ hỗ trợ. Nếu không, tạo ảnh gần tỷ lệ đó và không vượt quá `1024x1024 px`.
3. **Trạng thái asset:**
   * `Đang có sẵn trong project` nghĩa là dự án đã có asset đó, prompt chỉ dùng khi cần tái tạo hoặc nâng cấp mỹ thuật.
   * `Mới (Cần Tạo)` nghĩa là asset chưa có và có thể được ưu tiên sinh ảnh mới.
4. **Ranh giới Unity:**
   * Không yêu cầu AI lưu file vào bất kỳ thư mục Unity hoặc đường dẫn local nào.
   * Không yêu cầu AI tạo Unity `.meta` files.
   * Developer sẽ tự xử lý import, Texture Importer, pivot, 9-slice border và validation trong Unity sau khi nhận ảnh.
