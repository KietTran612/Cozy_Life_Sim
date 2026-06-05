# prompts_mockup01_group_C_plants.md

> Ghi chú:
> - Đây là file prompt text-only, KHÔNG phải lệnh tạo ảnh ở bước này.
> - Mục tiêu là chuẩn bị prompt để dùng sau.
> - Mockup hiện tại là ảnh tham chiếu duy nhất.
> - Phải giữ đúng góc nhìn / orientation gốc của từng item trong mockup.
> - Mỗi prompt chỉ dành cho 1 asset riêng.
> - Không tự đổi sang góc khác.
> - Không gộp nhiều item vào chung 1 asset.
> - Background sẽ xử lý ở workflow riêng sau, không trộn với nhóm item này.

---

# 1) Quy chuẩn chung cho nhóm C — Cây / thực vật lớn

## Yêu cầu chung

- Dùng mockup gốc làm style reference và angle reference.
- Chỉ lấy đúng **1 asset** cho mỗi prompt.
- Giữ đúng **góc nhìn / hướng quay / orientation** như trong mockup.
- Giữ đúng **cozy hand-drawn Vietnamese countryside papercraft style**.
- Màu sắc ấm, pastel, mềm, sạch.
- Line sạch, texture giấy nhẹ.
- Có **viền trắng sticker rõ ràng** quanh toàn bộ asset nếu asset thuộc scene sticker.
- Asset hoàn chỉnh, không bị cắt mất phần nào.
- **Transparent background**.
- Không đổ bóng rời.
- Không có nền, không có UI, không có text, không có vật thể thừa.
- Không tự thêm item phụ không thuộc asset chính.
- Asset đặt giữa khung hình, dễ dùng cho Unity.

## Negative constraints chung

- No background
- No scene
- No extra props
- No UI
- No text
- No cropped object
- No shadow on ground
- No realistic photo style
- No 3D render look
- No different camera angle
- No alternative orientation unless explicitly requested
- No unrelated plants
- No ground patch unless structurally part of the plant asset

---

# 2) Nhóm C — Cây / thực vật lớn

---

## C1. TREE_Banana_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the banana tree. Preserve the exact original viewing angle and orientation from the mockup. The banana tree should have broad green leaves, a natural curved trunk cluster, and visible hanging yellow banana bunches, matching the cozy Vietnamese countryside look. Keep the same warm pastel palette, hand-drawn papercraft style, soft paper texture, clean linework, and thick clean white sticker border around the entire tree asset. Show the full banana tree only, centered, transparent background, no shadow, no text, no ground, no fence, no haystack, no surrounding props.

### Negative prompt

background, house, haystack, fence, buffalo, grandma, UI, text, realistic banana tree, tropical jungle scene, different angle, cropped leaves, cropped trunk, ground patch, shadow on floor, extra plants

---

## C2. PROP_Haystack_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the haystack. Preserve the exact original viewing angle and orientation from the mockup. The haystack should look like a cozy rural Vietnamese straw pile with a rounded cone-like silhouette, warm golden straw colors, soft hand-drawn texture, and slightly irregular natural edges. Keep the same cozy papercraft style, warm pastel palette, clean linework, and thick clean white sticker border. Show only the full haystack, centered, transparent background, no shadow, no text, no banana tree, no fence, no jar, no ground, no surrounding objects.

### Negative prompt

background, banana tree, fence, clay jar, buffalo, house, UI, text, realistic hay bale, square hay bale, different angle, cropped straw, ground patch, shadow on floor, extra farm tools

---

## C3. PLANT_Pot_TallLeaf_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the tall leafy potted plant near the front of the house. Preserve the exact original viewing angle and orientation from the mockup. The asset should include a warm terracotta clay pot with a tall green leafy plant growing upward, matching the cozy Vietnamese countryside garden style. Keep the same hand-drawn papercraft rendering, warm pastel colors, soft texture, clean linework, and thick clean white sticker border. Show only this potted plant, centered, transparent background, no shadow, no text, no house wall, no door, no ground, no other pots or flowers.

### Negative prompt

background, house, wall, door, bench, clay jar, UI, text, realistic plant photo, different angle, cropped leaves, cropped pot, ground patch, shadow on floor, multiple pots

---

## C4. PLANT_Pot_FlowerPink_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the pink flower pot near the well. Preserve the exact original viewing angle and orientation from the mockup. The asset should show a simple countryside clay pot filled with cute pink flowers and green leaves, matching the cozy hand-drawn Vietnamese village papercraft style. Keep the warm pastel palette, clean linework, soft paper texture, and thick clean white sticker border around the complete plant and pot. Show only this potted flower asset, centered, transparent background, no shadow, no text, no well, no house, no ground, no surrounding plants.

### Negative prompt

background, well, house, UI, text, realistic flowers, different flower type, different angle, cropped flowers, cropped pot, ground patch, shadow on floor, multiple pots, extra objects

---

## C5. PLANT_Pot_SmallGreen_HouseStep_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small green potted plant near the house steps. Preserve the exact original viewing angle and orientation from the mockup. The asset should show a small rustic clay pot with compact green leaves, matching the cozy hand-drawn Vietnamese countryside style. Keep the same warm pastel colors, papercraft texture, clean linework, and thick clean white sticker border. Show only this small potted plant, centered, transparent background, no shadow, no text, no house, no door, no bench, no ground, no other objects.

### Negative prompt

background, house wall, door, bench, clay jar, UI, text, realistic plant, different angle, cropped leaves, cropped pot, ground patch, shadow on floor, multiple plants, extra props

---

## C6. PLANT_LotusFlower_Pond_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the pink lotus flower from the pond. Preserve the exact original viewing angle and orientation from the mockup. The lotus should have soft pink petals, a warm gentle shape, and a cozy hand-drawn papercraft look matching the Vietnamese countryside mockup. Keep the warm pastel palette, clean linework, soft paper texture, and thick clean white sticker border if used as a standalone sticker asset. Show only the lotus flower, centered, transparent background, no water, no lily pads, no pond edge, no shadow, no text, no extra plants.

### Negative prompt

pond background, water, lily pads, rocks, UI, text, realistic lotus, different angle, cropped petals, shadow on water, extra flowers, full pond scene

---

## C7. PLANT_LilyPad_Large_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the large lily pad from the pond. Preserve the exact original viewing angle and orientation from the mockup. The lily pad should be a rounded green floating leaf with a simple hand-drawn shape, warm pastel coloring, and soft papercraft texture matching the mockup. Keep it clean and readable as a Unity decor asset. Show only one large lily pad, centered, transparent background, no water, no lotus flower, no rocks, no text, no shadow, no extra pond elements.

### Negative prompt

pond background, water, lotus, rocks, UI, text, realistic leaf, different angle, multiple lily pads, cropped leaf, water ripple background, shadow on water

---

## C8. PLANT_LilyPad_Small_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a smaller lily pad from the pond. Preserve the exact original viewing angle and orientation from the mockup. The lily pad should be smaller than the main large lily pad, with a clean rounded green shape, cozy hand-drawn papercraft texture, and warm pastel colors. Show only one small lily pad, centered, transparent background, no water, no lotus flower, no rocks, no text, no shadow, no extra pond elements.

### Negative prompt

pond background, water, lotus, rocks, UI, text, realistic leaf, different angle, multiple lily pads, cropped leaf, water ripple background, shadow on water

---

## C9. PLANT_Pot_Flower_BottomUIIcon_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D UI icon asset of the flower pot shown in the bottom menu decoration button. Preserve the same front-facing UI icon orientation from the mockup. The icon should show a cute clay flower pot with pink flowers and green leaves, matching the cozy hand-drawn papercraft UI style. Keep the warm pastel palette, clean linework, soft paper texture, and thick clean white sticker border. Show only the flower pot icon, centered, transparent background, no button frame, no text, no shadow, no extra UI elements.

### Negative prompt

button background, UI text, Trang trí text, scene background, realistic flowers, different icon angle, cropped pot, shadow, extra decorations

> Ghi chú:
> C9 thuộc UI icon nhiều hơn scene prop. Chỉ dùng nếu muốn tách icon ở menu dưới.

---

# 3) Asset nên ưu tiên trong nhóm C

## Ưu tiên cao

1. `TREE_Banana_3Q`
2. `PROP_Haystack_3Q`
3. `PLANT_Pot_TallLeaf_3Q`
4. `PLANT_Pot_FlowerPink_3Q`
5. `PLANT_Pot_SmallGreen_HouseStep_3Q`

## Ưu tiên trung bình

6. `PLANT_LotusFlower_Pond_3Q`
7. `PLANT_LilyPad_Large_3Q`
8. `PLANT_LilyPad_Small_3Q`

## Optional / UI

9. `PLANT_Pot_Flower_BottomUIIcon_Optional`

---

# 4) Lưu ý riêng cho nhóm C

## Không nên tách quá nhỏ ở vòng đầu

Các chi tiết như hoa nhỏ trên cỏ, bụi cỏ lấm tấm, cụm lá nhỏ quanh nhà, hoặc hoa nền li ti nên để sau. Chúng thuộc nhóm background decor/decal, không phải item chính ở vòng đầu.

## Không nên gộp cây với nền

Ví dụ:
- Cây chuối không kèm đống rơm.
- Đống rơm không kèm hàng rào.
- Chậu cây không kèm tường nhà.
- Hoa sen không kèm mặt nước.
- Lá sen không kèm ao.

## Về viền trắng

Các item scene chính trong mockup có viền trắng sticker rõ, nên nhóm C cũng nên có viền trắng để khớp với nhân vật, trâu, mèo, nhà và các prop lớn.

Riêng các item dùng làm decor nằm trong ao hoặc nền có thể cân nhắc không viền trắng ở giai đoạn sau, nhưng ở file prompt này vẫn ưu tiên kiểu sticker để đồng bộ với mockup chính.

---

# 5) Danh sách prompt hiện có trong file này

- C1. `TREE_Banana_3Q`
- C2. `PROP_Haystack_3Q`
- C3. `PLANT_Pot_TallLeaf_3Q`
- C4. `PLANT_Pot_FlowerPink_3Q`
- C5. `PLANT_Pot_SmallGreen_HouseStep_3Q`
- C6. `PLANT_LotusFlower_Pond_3Q`
- C7. `PLANT_LilyPad_Large_3Q`
- C8. `PLANT_LilyPad_Small_3Q`
- C9. `PLANT_Pot_Flower_BottomUIIcon_Optional`
