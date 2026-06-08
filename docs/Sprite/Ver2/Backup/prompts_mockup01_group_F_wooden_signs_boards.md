# prompts_mockup01_group_F_wooden_signs_boards.md

> Ghi chú:
> - Đây là file prompt text-only, KHÔNG phải lệnh tạo ảnh ở bước này.
> - Mục tiêu là chuẩn bị prompt để dùng sau.
> - Mockup hiện tại là ảnh tham chiếu duy nhất.
> - Phải giữ đúng góc nhìn / orientation gốc của từng item trong mockup.
> - Mỗi prompt chỉ dành cho 1 asset riêng.
> - Không tự đổi sang góc khác.
> - Không gộp nhiều bảng khác góc / khác chức năng vào chung 1 asset.
> - Bảng có chữ trong mockup nên ưu tiên tạo bản **blank / không chữ**, để Unity render text riêng.
> - Background sẽ xử lý ở workflow riêng sau, không trộn với nhóm item này.

---

# 1) Quy chuẩn chung cho nhóm F — Bảng gỗ / bảng nhiệm vụ / biển tên

## Yêu cầu chung

- Dùng mockup gốc làm style reference và angle reference.
- Chỉ lấy đúng **1 asset** cho mỗi prompt.
- Giữ đúng **góc nhìn / hướng quay / orientation** như trong mockup.
- Nếu cùng loại bảng xuất hiện ở góc khác nhau, phải coi là asset riêng.
- Giữ đúng **cozy hand-drawn Vietnamese countryside papercraft style**.
- Màu gỗ ấm, pastel, mềm, sạch.
- Chất liệu nên là **gỗ mộc / tre / bảng làng quê Việt Nam**.
- Line sạch, texture giấy nhẹ, silhouette rõ.
- Có **viền trắng sticker rõ ràng** quanh toàn bộ asset nếu là item scene.
- Asset hoàn chỉnh, không bị cắt mất phần nào.
- **Transparent background**.
- Không đổ bóng rời.
- Không có nền, không có UI dư, không có vật thể thừa.
- Không bake chữ vào asset, trừ khi prompt có yêu cầu giữ biểu tượng nhỏ không phải text.
- Asset đặt giữa khung hình, dễ dùng cho Unity.

## Text rule

Với tất cả bảng gỗ / biển tên / bảng nhiệm vụ:

- Ưu tiên: **blank wooden sign / no readable text**
- Text như “Ruộng Lúa”, “Mục tiêu”, “Quà Ngày” nên để Unity render riêng.
- Nếu cần icon trang trí như bó lúa, nơ, hoa nhỏ, có thể giữ nếu nó là phần nhận diện của bảng.
- Không tạo chữ giả, chữ méo, ký tự lung tung.

## Negative constraints chung

- No background
- No scene
- No grass patch
- No ground
- No house unless structurally part of the sign
- No character
- No animal
- No UI text baked into the asset
- No readable text
- No fake letters
- No cropped object
- No shadow on ground
- No realistic photo style
- No 3D render look
- No different camera angle
- No alternative sign orientation unless explicitly requested
- No extra props attached unless clearly part of the sign design

---

# 2) Nhóm F — Bảng gỗ / bảng nhiệm vụ / biển tên

---

## F1. SIGN_RiceField_3Q_Blank

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small wooden rice field sign from the farm area. Preserve the exact original viewing angle and orientation from the mockup. The sign should look like a rustic Vietnamese countryside wooden sign standing on small posts, matching the sign that labels the rice field in the reference, but with a clean blank writing area and no readable text. Keep the warm brown wood, cozy hand-drawn papercraft style, soft paper texture, clean linework, and thick clean white sticker border around the entire sign. Show only the wooden sign asset, centered, transparent background, no ground, no rice field, no grass, no text, no shadow, no extra objects.

### Negative prompt

background, rice field, grass, crops, buffalo, fence, UI, readable text, fake letters, Ruộng Lúa text, realistic wooden sign, different angle, cropped sign, ground patch, shadow on floor, extra props

---

## F2. SIGN_RiceField_3Q_WithRiceIcon_Blank

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the wooden rice field sign from the farm area, preserving the exact original viewing angle and orientation from the mockup. The asset should be a rustic wooden sign on small posts with a small cute rice plant or rice bundle icon decoration, but the main text area must remain blank with no readable text. Match the cozy Vietnamese countryside papercraft style, warm pastel colors, clean linework, soft paper texture, and thick clean white sticker border. Show only the sign, centered, transparent background, no field, no grass, no text, no shadow, no extra objects.

### Negative prompt

background, rice field, grass, crops, buffalo, UI, readable text, fake letters, Ruộng Lúa text, realistic sign, different angle, cropped sign, ground patch, shadow, extra props

> Ghi chú:
> F2 là biến thể nếu bạn muốn giữ icon lúa trên biển. Nếu muốn Unity quản lý cả icon, dùng F1 thay vì F2.

---

## F3. SIGN_GoalBoard_Right_3Q_Blank

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the wooden goal board from the lower-right side of the scene. Preserve the exact original viewing angle and orientation from the mockup. The board should look like a cozy rustic wooden task board or goal board, with a warm brown frame, rounded handmade shape, simple decorative top detail, and a large blank writing area for Unity text. Keep the same cozy hand-drawn Vietnamese countryside papercraft style, warm pastel palette, clean linework, soft paper texture, and thick clean white sticker border around the entire board. Show only the goal board, centered, transparent background, no readable text, no UI numbers, no icons unless they are simple decorative board elements, no ground, no shadow, no extra objects.

### Negative prompt

background, grass, pond, UI text, readable text, fake letters, Mục tiêu text, progress numbers, realistic wooden board, different angle, cropped board, ground patch, shadow on floor, extra scene props

---

## F4. SIGN_DailyGift_RightTop_Blank

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small daily gift wooden sign from the upper-right side of the scene. Preserve the exact original viewing angle and orientation from the mockup. The sign should feel like a cute Vietnamese countryside UI/scene sign, made of warm wood with a cozy handmade shape and a small gift or reward decoration if visible in the reference, but the main text area must be blank with no readable text. Keep the warm pastel colors, clean hand-drawn linework, soft papercraft texture, and thick clean white sticker border. Show only this daily gift sign, centered, transparent background, no UI panel behind it, no text, no numbers, no shadow, no extra objects.

### Negative prompt

background, UI panel, readable text, fake letters, Quà Ngày text, timer numbers, realistic sign, different angle, cropped sign, ground patch, shadow, extra props, notification badge

---

## F5. SIGN_HouseWoodenTextBoard_Blank

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small wooden sign board mounted on the main house. Preserve the exact original viewing angle and orientation from the mockup. The sign should look like a warm rustic wooden nameplate or hanging sign, matching the cozy Vietnamese countryside house style, but with a clean blank surface and no readable text. Keep the hand-drawn papercraft style, warm pastel wood color, clean linework, soft paper texture, and thick clean white sticker border. Show only the wooden sign board, centered, transparent background, no house wall, no roof, no door, no text, no shadow, no extra objects.

### Negative prompt

background, house wall, roof, door, readable text, fake letters, realistic sign, different angle, cropped sign, shadow, extra wall decorations, UI

> Ghi chú:
> F5 chỉ nên dùng nếu bạn muốn bảng tên nhà là asset tách riêng. Nếu asset nhà chính đã giữ luôn bảng gắn trên nhà, có thể bỏ F5.

---

## F6. SIGN_SmallWoodenPost_Generic_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a small generic wooden post sign matching the wooden signs in the mockup. Preserve the same original 2.5D countryside viewing angle and orientation language from the reference. The sign should be a simple rustic Vietnamese village sign on one or two wooden posts, with a blank writing area, warm brown wood, rounded handmade edges, clean linework, soft papercraft texture, and a thick clean white sticker border. Show only the small blank wooden post sign, centered, transparent background, no text, no ground, no grass, no shadow, no extra objects.

### Negative prompt

background, grass, field, house, UI, readable text, fake letters, realistic sign, different angle, cropped sign, ground patch, shadow, extra props

> Ghi chú:
> F6 là generic sign để dùng lại trong scene nếu cần nhiều biển báo nhỏ. Không thay thế các bảng cụ thể F1-F4.

---

## F7. UI_BoardPanel_Wooden_Blank_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D UI asset of a blank wooden board panel matching the cozy wooden board style from the reference. This should be a front-readable UI board with a warm wooden frame, softly rounded corners, handmade papercraft texture, clean empty center area for Unity text, and no readable text. Preserve the cozy Vietnamese countryside UI style from the mockup while keeping the asset clean and reusable. Transparent background, centered, no icons, no numbers, no scene objects, no shadow.

### Negative prompt

scene background, grass, house, readable text, fake letters, icons, progress numbers, realistic wood photo, different UI style, cropped panel, shadow, busy center, extra decorations

> Ghi chú:
> F7 thuộc UI hơn là scene prop. Dùng nếu bạn muốn có bảng gỗ rỗng cho nhiệm vụ / popup / thông báo.

---

## F8. UI_SmallLabelTag_Wooden_Blank_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D UI asset of a small blank wooden label tag matching the cozy UI labels in the mockup. The label should be a warm brown wooden tag with rounded corners, clean empty center, soft papercraft texture, clean linework, and no readable text. Keep the asset simple, reusable, and suitable for Unity UI. Transparent background, centered, no icon, no numbers, no shadow, no extra objects.

### Negative prompt

background, scene objects, readable text, fake letters, icon, realistic wood photo, different style, cropped label, shadow, busy decoration

> Ghi chú:
> F8 là optional cho các nhãn nhỏ trong UI. Không bắt buộc ở batch scene đầu tiên.

---

# 3) Asset nên ưu tiên trong nhóm F

## Ưu tiên cao

1. `SIGN_RiceField_3Q_Blank`
2. `SIGN_GoalBoard_Right_3Q_Blank`
3. `SIGN_DailyGift_RightTop_Blank`

## Ưu tiên trung bình

4. `SIGN_RiceField_3Q_WithRiceIcon_Blank`
5. `SIGN_HouseWoodenTextBoard_Blank`
6. `SIGN_SmallWoodenPost_Generic_3Q`

## Optional / UI

7. `UI_BoardPanel_Wooden_Blank_Optional`
8. `UI_SmallLabelTag_Wooden_Blank_Optional`

---

# 4) Lưu ý riêng cho nhóm F

## Không bake text vào asset

Các bảng trong mockup có chữ, nhưng khi đưa vào Unity nên để chữ render riêng bằng TextMeshPro hoặc UI Text.

Ví dụ:
- “Ruộng Lúa” không nên bake vào asset.
- “Mục tiêu” không nên bake vào asset.
- “Quà Ngày” không nên bake vào asset.
- Số tiến độ / thời gian / phần thưởng không nên bake vào asset.

Prompt nên dùng:
- blank wooden sign
- no readable text
- no fake letters
- clean empty writing area for Unity text

## Cùng loại nhưng khác góc phải tách riêng

Ví dụ:
- Biển ruộng lúa đang nằm theo góc 2.5D của scene.
- Bảng mục tiêu bên phải có hướng/góc UI-scene khác.
- Bảng quà ngày ở phía trên là dạng UI/scene sign riêng.
- Bảng gắn tường nhà có góc bám theo mặt nhà.

Không nên dùng một bảng rồi xoay/scale bừa trong Unity vì dễ sai phối cảnh.

## Không trộn bảng với background

Bảng phải sạch:
- không dính cỏ
- không dính đất
- không dính ao
- không dính nhà
- không dính ruộng
- không dính UI text

## Giữ vùng trống đủ rộng

Với các bảng dùng để hiển thị nội dung trong game, prompt nên nhấn mạnh:
- large blank writing area
- clean empty center
- readable surface
- no busy decoration in the center

## Có thể giữ icon nhỏ nếu cần

Một số bảng có thể có icon nhận diện:
- bảng ruộng có icon lúa
- bảng quà có icon quà
- bảng nhiệm vụ có icon checklist

Tuy nhiên nếu muốn hệ thống UI linh hoạt, nên tách:
- bảng rỗng riêng
- icon riêng
- text riêng trong Unity

---

# 5) Danh sách prompt hiện có trong file này

- F1. `SIGN_RiceField_3Q_Blank`
- F2. `SIGN_RiceField_3Q_WithRiceIcon_Blank`
- F3. `SIGN_GoalBoard_Right_3Q_Blank`
- F4. `SIGN_DailyGift_RightTop_Blank`
- F5. `SIGN_HouseWoodenTextBoard_Blank`
- F6. `SIGN_SmallWoodenPost_Generic_3Q`
- F7. `UI_BoardPanel_Wooden_Blank_Optional`
- F8. `UI_SmallLabelTag_Wooden_Blank_Optional`
