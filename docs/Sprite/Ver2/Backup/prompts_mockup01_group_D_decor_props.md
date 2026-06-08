# prompts_mockup01_group_D_decor_props.md

> Ghi chú:
> - Đây là file prompt text-only, KHÔNG phải lệnh tạo ảnh ở bước này.
> - Mục tiêu là chuẩn bị prompt để dùng sau.
> - Mockup hiện tại là ảnh tham chiếu duy nhất.
> - Phải giữ đúng góc nhìn / orientation gốc của từng item trong mockup.
> - Mỗi prompt chỉ dành cho 1 asset riêng.
> - Không tự đổi sang góc khác.
> - Không gộp nhiều item vào chung 1 asset.
> - Background sẽ xử lý ở workflow riêng sau, không trộn với nhóm item này.
> - Nhóm D chỉ lấy các vật dụng trang trí / đồ sinh hoạt có silhouette tương đối rõ.
> - Các chậu cây có cây/hoa đã nằm trong nhóm C; nhóm D ưu tiên lu, ghế, đồ treo, đồ trang trí rời.

---

# 1) Quy chuẩn chung cho nhóm D — Lu / chậu / vật dụng trang trí

## Yêu cầu chung

- Dùng mockup gốc làm style reference và angle reference.
- Chỉ lấy đúng **1 asset** cho mỗi prompt.
- Giữ đúng **góc nhìn / hướng quay / orientation** như trong mockup.
- Nếu cùng loại item xuất hiện ở góc khác nhau, phải coi là asset riêng.
- Giữ đúng **cozy hand-drawn Vietnamese countryside papercraft style**.
- Màu sắc ấm, pastel, mềm, sạch.
- Line sạch, texture giấy nhẹ.
- Có **viền trắng sticker rõ ràng** quanh toàn bộ asset.
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
- No readable text
- No cropped object
- No shadow on ground
- No realistic photo style
- No 3D render look
- No different camera angle
- No alternative orientation unless explicitly requested
- No ground patch
- No surrounding house wall unless the object is structurally attached
- No plants unless the prompt specifically asks for a plant pot

---

# 2) Nhóm D — Lu / chậu / vật dụng trang trí

---

## D1. PROP_ClayJar_Left_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small clay water jar on the left side of the farm area. Preserve the exact original viewing angle and orientation from the mockup. The jar should look like a rustic Vietnamese countryside clay jar with a warm brown ceramic surface, rounded body, simple handmade shape, and cozy hand-drawn papercraft texture. Keep the same warm pastel palette, clean linework, and thick clean white sticker border around the entire jar. Show only this single clay jar, centered, transparent background, no shadow, no text, no fence, no grass, no house, no surrounding props.

### Negative prompt

background, fence, grass, house, plants, UI, text, realistic ceramic jar, different angle, front-only view, cropped jar, ground patch, shadow on floor, multiple jars, extra props

---

## D2. PROP_ClayJar_Right_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the larger clay water jar near the right side of the main house. Preserve the exact original viewing angle and orientation from the mockup. The jar should look like a large rustic Vietnamese clay water jar with a rounded body, warm reddish-brown ceramic color, simple lid or rim detail if visible in the mockup, and a cozy handmade countryside feel. Keep the same hand-drawn papercraft style, warm pastel colors, clean linework, soft texture, and thick clean white sticker border. Show only this large clay jar, centered, transparent background, no shadow, no text, no house wall, no bench, no plants, no ground, no other objects.

### Negative prompt

background, house wall, bench, potted plant, grandma, UI, text, realistic clay pot, different angle, front-only view, cropped jar, ground patch, shadow on floor, multiple jars, extra props

---

## D3. PROP_Bench_Wooden_Right_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small wooden bench in front of the main house. Preserve the exact original viewing angle and orientation from the mockup. The bench should look like a simple rustic Vietnamese countryside wooden bench, with warm brown wood, hand-drawn slightly rounded edges, and cozy papercraft texture. Keep the same warm pastel palette, clean linework, and thick clean white sticker border around the whole bench. Show only the bench, centered, transparent background, no shadow, no text, no house wall, no clay jar, no plants, no ground, no extra props.

### Negative prompt

background, house, wall, clay jar, plant, grandma, UI, text, realistic bench, different angle, front-only view, cropped legs, ground patch, shadow on floor, extra objects

---

## D4. PROP_HangingBirdCage_Front

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small hanging bird cage under the house roof. Preserve the exact original viewing angle and orientation from the mockup. The cage should look like a cute rustic Vietnamese bamboo or wooden bird cage, small and decorative, with a simple rounded hanging shape and warm natural colors. Keep the same cozy hand-drawn papercraft style, warm pastel palette, clean linework, and thick clean white sticker border. Show only the hanging bird cage, centered, transparent background, no shadow, no text, no house roof, no wall, no hook unless part of the cage, no extra objects.

### Negative prompt

background, house roof, house wall, lantern, UI, text, realistic bird cage, bird inside cage, different angle, cropped cage, shadow, extra hanging objects, scene elements

---

## D5. PROP_HangingBasket_RightWall

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the hanging woven basket or rustic wall basket on the right side of the house. Preserve the exact original viewing angle and orientation from the mockup. The basket should look handmade from bamboo or straw, with a warm tan-brown color, simple village craft details, and a cozy hand-drawn papercraft texture. Keep the same warm pastel colors, clean linework, and thick clean white sticker border. Show only this hanging basket object, centered, transparent background, no shadow, no text, no house wall, no door, no plants, no extra props.

### Negative prompt

background, house wall, door, window, UI, text, realistic basket photo, different angle, cropped basket, ground patch, shadow, extra wall decorations, multiple baskets

---

## D6. PROP_DriedChili_Hanging

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small hanging dried chili or dried vegetable decoration on the house wall. Preserve the exact original viewing angle and orientation from the mockup. The decoration should look like a rustic Vietnamese countryside hanging bundle with warm red-orange dried peppers or vegetables tied together, simple handmade shape, cozy papercraft texture, and clean readable silhouette. Keep the same warm pastel palette, hand-drawn linework, and thick clean white sticker border. Show only this hanging dried chili decoration, centered, transparent background, no shadow, no text, no house wall, no window, no extra objects.

### Negative prompt

background, house wall, window, basket, UI, text, realistic chili photo, different angle, cropped bundle, shadow, extra wall decorations, multiple unrelated vegetables

---

## D7. PROP_WallCharm_Generic

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the small rustic wall charm or hanging decorative object from the main house. Preserve the exact original viewing angle and orientation from the mockup. The decoration should feel like a simple Vietnamese countryside handmade wall ornament, with warm wood or fabric details, cozy pastel colors, clean hand-drawn linework, and soft papercraft texture. Add a thick clean white sticker border around the complete object. Show only this small wall decoration, centered, transparent background, no shadow, no readable text, no house wall, no window, no extra objects.

### Negative prompt

background, house wall, readable sign text, UI, realistic ornament, different angle, cropped object, shadow, extra decorations, multiple objects

> Ghi chú:
> D7 chỉ nên dùng nếu bạn muốn tách các decor nhỏ trên tường nhà thành asset riêng. Nếu nhà chính đã được giữ nguyên khối, có thể bỏ D7.

---

## D8. PROP_EmptyClayPot_Generic_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a small empty rustic clay pot matching the pots in the mockup. Preserve the same original 2.5D countryside viewing angle and orientation language from the mockup. The pot should be warm terracotta brown, handmade, slightly rounded, cute, and simple, matching the cozy Vietnamese village papercraft style. Keep clean linework, soft paper texture, warm pastel colors, and a thick clean white sticker border. Show only the empty clay pot, centered, transparent background, no shadow, no text, no plant, no ground, no extra props.

### Negative prompt

background, plant inside pot, flowers, house, UI, text, realistic pottery, different angle, cropped pot, ground patch, shadow, multiple pots, extra props

> Ghi chú:
> D8 là optional. Trong mockup, nhiều chậu có cây/hoa nên đã được đưa vào nhóm C. D8 chỉ dùng nếu cần một chậu rỗng làm decor riêng.

---

## D9. PROP_SmallWoodenStool_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a small rustic wooden stool matching the cozy Vietnamese countryside style of the scene. Preserve the same original 2.5D viewing angle and orientation language from the mockup. The stool should be simple, handmade, warm brown, slightly rounded, and hand-drawn with soft papercraft texture. Add a thick clean white sticker border around the complete stool. Show only the stool, centered, transparent background, no shadow, no text, no house wall, no bench, no jar, no ground, no extra props.

### Negative prompt

background, house, bench, clay jar, UI, text, realistic stool, different angle, cropped legs, ground patch, shadow on floor, extra props

> Ghi chú:
> D9 là optional, chỉ dùng nếu bạn cần thêm decor ngồi/đặt đồ. Nếu mockup không có stool đủ rõ, nên bỏ qua ở vòng đầu.

---

## D10. PROP_SmallDecorBasket_Generic_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a small rustic woven basket matching the decorative baskets in the mockup. Preserve the same original 2.5D viewing angle and countryside orientation language from the mockup. The basket should be handmade from bamboo or straw, warm tan-brown, cute, simple, and readable as a small village decor item. Keep the cozy hand-drawn papercraft style, clean linework, warm pastel colors, and thick clean white sticker border. Show only the basket, centered, transparent background, no shadow, no text, no house wall, no vegetables, no plants, no ground, no extra objects.

### Negative prompt

background, house wall, vegetables, plants, UI, text, realistic basket photo, different angle, cropped basket, ground patch, shadow, multiple baskets, extra props

> Ghi chú:
> D10 là optional nếu cần nhiều vật dụng trang trí kiểu làng quê. Không bắt buộc trong batch đầu.

---

# 3) Asset nên ưu tiên trong nhóm D

## Ưu tiên cao

1. `PROP_ClayJar_Left_3Q`
2. `PROP_ClayJar_Right_3Q`
3. `PROP_Bench_Wooden_Right_3Q`
4. `PROP_HangingBirdCage_Front`
5. `PROP_HangingBasket_RightWall`
6. `PROP_DriedChili_Hanging`

## Ưu tiên trung bình

7. `PROP_WallCharm_Generic`

## Optional / chỉ làm nếu cần thêm decor

8. `PROP_EmptyClayPot_Generic_Optional`
9. `PROP_SmallWoodenStool_Optional`
10. `PROP_SmallDecorBasket_Generic_Optional`

---

# 4) Lưu ý riêng cho nhóm D

## Không gộp đồ trang trí với nhà

Các vật như lu, ghế, lồng chim, giỏ treo, chùm ớt nên được tạo riêng nếu bạn muốn có asset library linh hoạt. Nhưng nếu sau này bạn quyết định nhà chính là một asset nguyên khối có sẵn decor gắn tường, thì có thể bỏ qua các decor nhỏ như D4-D7.

## Cùng loại nhưng khác góc phải tách riêng

Ví dụ:
- Lu nước bên trái và lu nước bên phải không nên dùng chung một sprite nếu khác kích thước/góc nhìn.
- Giỏ treo tường và giỏ đặt đất nên là asset khác nhau.
- Ghế dài và ghế đẩu nếu có, không nên gom chung.

## Không nên tách decor quá nhỏ nếu chưa cần

Những chi tiết rất nhỏ dính vào tường nhà có thể để lại trong asset nhà chính ở vòng đầu. Chỉ tách nếu bạn muốn chúng có thể bật/tắt, đặt lại, hoặc dùng ở scene khác.

## Text không nên bake vào asset

Nếu có bảng, lịch, charm hoặc vật trang trí có chữ, nên yêu cầu:
- no readable text
- blank surface
- Unity will render text separately

---

# 5) Danh sách prompt hiện có trong file này

- D1. `PROP_ClayJar_Left_3Q`
- D2. `PROP_ClayJar_Right_3Q`
- D3. `PROP_Bench_Wooden_Right_3Q`
- D4. `PROP_HangingBirdCage_Front`
- D5. `PROP_HangingBasket_RightWall`
- D6. `PROP_DriedChili_Hanging`
- D7. `PROP_WallCharm_Generic`
- D8. `PROP_EmptyClayPot_Generic_Optional`
- D9. `PROP_SmallWoodenStool_Optional`
- D10. `PROP_SmallDecorBasket_Generic_Optional`
