# prompts_mockup01_group_E_bamboo_fence.md

> Ghi chú:
> - Đây là file prompt text-only, KHÔNG phải lệnh tạo ảnh ở bước này.
> - Mục tiêu là chuẩn bị prompt để dùng sau.
> - Mockup hiện tại là ảnh tham chiếu duy nhất.
> - Phải giữ đúng góc nhìn / orientation gốc của từng item trong mockup.
> - Mỗi prompt chỉ dành cho 1 asset riêng.
> - Không tự đổi sang góc khác.
> - Không gộp nhiều đoạn hàng rào khác góc vào chung 1 asset.
> - Hàng rào tre trong mockup phải **tách theo góc riêng**.
> - Nếu hai đoạn hàng rào cùng loại nhưng khác góc nhìn hoặc khác chiều dài/silhouette đáng kể, phải coi là asset riêng.
> - Background sẽ xử lý ở workflow riêng sau, không trộn với nhóm item này.

---

# 1) Quy chuẩn chung cho nhóm E — Hàng rào tre

## Yêu cầu chung

- Dùng mockup gốc làm style reference và angle reference.
- Chỉ lấy đúng **1 asset** cho mỗi prompt.
- Giữ đúng **góc nhìn / hướng quay / orientation** như trong mockup.
- Không tự chuyển sang front view, side view, top view, hay isometric khác nếu không đúng với mockup.
- Giữ đúng **cozy hand-drawn Vietnamese countryside papercraft style**.
- Màu sắc ấm, pastel, mềm, sạch.
- Chất liệu phải là **hàng rào tre / gỗ mộc mạc kiểu làng quê Việt Nam**.
- Line sạch, texture giấy nhẹ, silhouette rõ.
- Có **viền trắng sticker rõ ràng** quanh toàn bộ asset.
- Asset hoàn chỉnh, không bị cắt mất phần nào.
- **Transparent background**.
- Không đổ bóng rời.
- Không có nền, không có UI, không có text, không có vật thể thừa.
- Không tự thêm cây, cỏ, ruộng, nhà, người, động vật hoặc props khác.
- Asset đặt giữa khung hình, dễ dùng cho Unity.

## Negative constraints chung

- No background
- No scene
- No grass patch
- No dirt ground
- No farm field
- No house
- No river
- No pond
- No buffalo
- No cat
- No grandma
- No haystack
- No clay jar
- No UI
- No text
- No cropped object
- No shadow on ground
- No realistic photo style
- No 3D render look
- No different camera angle
- No alternative fence orientation unless explicitly requested
- No extra decorative props tied onto the fence unless clearly part of the fence in the mockup

---

# 2) Nhóm E — Hàng rào tre theo từng góc riêng

> Quan trọng:
> - Mỗi prompt bên dưới là **một đoạn hàng rào riêng biệt**.
> - Không được gom nhiều đoạn rào khác hướng vào cùng một asset.
> - Nếu sau này cần modular hơn nữa, có thể viết thêm prompt cho cột góc / đầu rào / đoạn ngắn riêng, nhưng ở vòng này vẫn ưu tiên các đoạn silhouette rõ theo mockup.

---

## E1. FENCE_Bamboo_FrontDiagonal_Left

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the bamboo fence segment at the lower-left/front area of the farm scene. Preserve the exact original viewing angle and orientation from the mockup. This fence segment should follow the same diagonal direction and same 2.5D countryside perspective as seen in the reference. The fence should be made of simple rustic bamboo posts and cross rails, with a warm natural yellow-brown bamboo color, slightly irregular handmade construction, and a cozy hand-drawn papercraft look. Keep the warm pastel palette, clean linework, soft paper texture, and a thick clean white sticker border around the full fence segment. Show only this fence segment, centered, transparent background, no ground, no grass, no extra objects, no text, no shadow.

### Negative prompt

background, grass, rice field, house, buffalo, cat, grandma, jar, haystack, UI, text, realistic bamboo fence, straight front view, different diagonal direction, cropped fence, ground patch, shadow on floor, extra props

---

## E2. FENCE_Bamboo_MidHorizontal

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the bamboo fence segment located around the middle area near the field and buffalo. Preserve the exact original viewing angle and orientation from the mockup. This fence segment should keep the same horizontal-or-near-horizontal orientation language visible in the scene, while still matching the overall 2.5D countryside perspective. The fence should be made of rustic bamboo posts and crossbars, simple and handmade, with warm yellow-brown bamboo tones and a cozy hand-drawn papercraft texture. Keep the warm pastel palette, clean linework, and thick clean white sticker border around the whole fence segment. Show only this fence piece, centered, transparent background, no field, no buffalo, no environment, no text, no shadow.

### Negative prompt

background, grass, rice field, buffalo, house, grandma, cat, haystack, UI, text, realistic bamboo fence, wrong fence angle, front-only fence, cropped fence, ground patch, shadow on floor, extra props

---

## E3. FENCE_Bamboo_BackDiagonal

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the bamboo fence segment in the back area near the haystack and banana tree. Preserve the exact original viewing angle and orientation from the mockup. This fence segment should follow the same diagonal direction and same perspective as shown in the reference. The fence should look like a rustic Vietnamese countryside bamboo fence, with handmade bamboo poles, simple tied construction, warm natural yellow-brown color, soft paper texture, and a cozy hand-drawn papercraft style. Keep the warm pastel palette, clean linework, and thick clean white sticker border. Show only this fence segment, centered, transparent background, no banana tree, no haystack, no grass, no text, no shadow, no extra objects.

### Negative prompt

background, banana tree, haystack, grass, house, buffalo, UI, text, realistic fence photo, different diagonal direction, front view fence, cropped fence, ground patch, shadow, extra props

---

## E4. FENCE_Bamboo_ShortSegment_Left

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the short bamboo fence segment on the left side near the river area. Preserve the exact original viewing angle and orientation from the mockup. This should be a smaller fence section with a simple rustic silhouette, matching the same Vietnamese countryside bamboo construction style, warm pastel colors, clean hand-drawn papercraft texture, and thick clean white sticker border. Show only the short fence segment, centered, transparent background, no river, no grass patch, no house, no text, no shadow, no extra props.

### Negative prompt

background, river, boat, dock, grass, house, UI, text, realistic fence, different angle, cropped fence, ground patch, shadow, extra objects, oversized fence section

---

## E5. FENCE_Bamboo_Post_Corner

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the prominent bamboo fence corner post or fence-end corner element. Preserve the exact original viewing angle and orientation from the mockup. The asset should read clearly as a bamboo fence corner or end-cap element that matches the same rustic handmade Vietnamese countryside fence style seen in the scene. Keep the warm yellow-brown bamboo color, cozy hand-drawn papercraft rendering, clean linework, soft paper texture, and thick clean white sticker border. Show only the fence corner/post element, centered, transparent background, no grass, no field, no environment, no text, no shadow.

### Negative prompt

background, full long fence, grass, field, house, UI, text, realistic bamboo post, different angle, cropped post, ground patch, shadow, extra props

> Ghi chú:
> E5 hữu ích nếu sau này bạn muốn ghép rào linh hoạt hơn, nhưng vẫn không đi sâu sang workflow modular đầy đủ.

---

## E6. FENCE_Bamboo_Generic_SinglePanel_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of one generic bamboo fence panel matching the fence style in the mockup. Preserve the same overall 2.5D viewing language and countryside orientation style of the reference. The panel should be a simple rustic Vietnamese bamboo fence section with warm natural bamboo tones, handmade tied structure, soft papercraft texture, clean linework, and a thick clean white sticker border. Keep the silhouette simple and readable. Show only one fence panel, centered, transparent background, no ground, no text, no shadow, no extra objects.

### Negative prompt

background, grass, field, house, animals, UI, text, realistic fence, different angle language, cropped panel, ground patch, shadow, extra props

> Ghi chú:
> E6 là optional. Dùng khi bạn muốn có một “đoạn rào generic” cùng phong cách để bổ sung cho scene, nhưng nó không thay thế các đoạn rào cụ thể theo góc trong E1–E4.

---

# 3) Asset nên ưu tiên trong nhóm E

## Ưu tiên cao

1. `FENCE_Bamboo_FrontDiagonal_Left`
2. `FENCE_Bamboo_MidHorizontal`
3. `FENCE_Bamboo_BackDiagonal`

## Ưu tiên trung bình

4. `FENCE_Bamboo_ShortSegment_Left`
5. `FENCE_Bamboo_Post_Corner`

## Optional

6. `FENCE_Bamboo_Generic_SinglePanel_Optional`

---

# 4) Lưu ý riêng cho nhóm E

## Hàng rào phải tách theo góc riêng

Đây là nguyên tắc quan trọng nhất của nhóm E.

Ví dụ:
- Đoạn rào phía trước chéo trái = asset riêng
- Đoạn rào giữa gần ngang = asset riêng
- Đoạn rào phía sau chéo khác hướng = asset riêng
- Đoạn rào ngắn bên trái = asset riêng
- Cột góc / đầu rào = asset riêng nếu cần

Không nên:
- dùng một sprite rào rồi xoay trong Unity
- flip ngang bừa bãi
- gom 2–3 đoạn rào khác góc thành 1 ảnh

## Không cần modular quá sâu ở vòng đầu

Hiện tại bạn đã chốt:
- không đi theo Nhóm 2 modular phức tạp
- chỉ lấy item rõ silhouette

Vì vậy nhóm E ở vòng đầu chỉ cần:
- các đoạn rào rõ nhất
- góc nhìn riêng biệt
- hình dáng dễ đọc

Chưa cần tách:
- từng thanh tre ngang
- từng cọc đơn lẻ
- từng đầu dây buộc
- từng phần very-small detail

## Giữ đồng bộ với các asset khác

Fence nên đồng bộ với:
- nhà
- trâu
- mèo
- bà cụ
- cây chuối
- đống rơm

về các yếu tố:
- line weight
- độ mềm texture giấy
- độ dày viền trắng
- độ ấm màu
- phong cách hand-drawn papercraft

## Không đưa môi trường vào asset hàng rào

Prompt phải luôn tránh chuyện hàng rào bị dính:
- patch cỏ
- đất
- ruộng
- bụi cây
- ao
- mép sông
- props như lu, cây, giếng

Vì mục tiêu là asset fence sạch để dùng lại.

---

# 5) Danh sách prompt hiện có trong file này

- E1. `FENCE_Bamboo_FrontDiagonal_Left`
- E2. `FENCE_Bamboo_MidHorizontal`
- E3. `FENCE_Bamboo_BackDiagonal`
- E4. `FENCE_Bamboo_ShortSegment_Left`
- E5. `FENCE_Bamboo_Post_Corner`
- E6. `FENCE_Bamboo_Generic_SinglePanel_Optional`
