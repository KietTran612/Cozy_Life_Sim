# prompts_mockup01_group_G_farm_field_crops.md

> Ghi chú:
> - Đây là file prompt text-only, KHÔNG phải lệnh tạo ảnh ở bước này.
> - Mục tiêu là chuẩn bị prompt để dùng sau.
> - Mockup hiện tại là ảnh tham chiếu duy nhất.
> - Phải giữ đúng góc nhìn / orientation gốc của từng item trong mockup.
> - Mỗi prompt chỉ dành cho 1 asset riêng.
> - Không tự đổi sang góc khác.
> - Không gộp nhiều item khác chức năng vào chung 1 asset.
> - Nhóm G hơi gần background, nên chỉ tách những phần có thể xem là item/prop/crop rõ ràng.
> - Background, ground tile, nền cỏ, đường gạch, ao, nước, bờ sông sẽ xử lý ở workflow riêng sau.
> - Không tách ruộng theo kiểu modular quá sâu ở vòng này, trừ khi item đó thật sự cần cho gameplay.

---

# 1) Quy chuẩn chung cho nhóm G — Nông trại / ruộng / item liên quan trồng trọt

## Yêu cầu chung

- Dùng mockup gốc làm style reference và angle reference.
- Chỉ lấy đúng **1 asset** cho mỗi prompt.
- Giữ đúng **góc nhìn / hướng quay / orientation** như trong mockup.
- Nếu cùng loại crop/item xuất hiện ở góc khác nhau hoặc kích thước/silhouette khác rõ, phải coi là asset riêng.
- Giữ đúng **cozy hand-drawn Vietnamese countryside papercraft style**.
- Màu sắc ấm, pastel, mềm, sạch.
- Line sạch, texture giấy nhẹ, silhouette rõ.
- Có **viền trắng sticker rõ ràng** quanh toàn bộ asset nếu asset dùng như prop/sticker.
- Asset hoàn chỉnh, không bị cắt mất phần nào.
- **Transparent background**.
- Không đổ bóng rời.
- Không có nền, không có UI, không có text, không có vật thể thừa.
- Không tự thêm nhà, người, động vật, hàng rào, ao, sông, cây hoặc props khác nếu không thuộc asset chính.
- Asset đặt giữa khung hình, dễ dùng cho Unity.

## Negative constraints chung

- No background
- No full scene
- No grass ground unless the prompt specifically asks for a complete field block
- No brick path
- No pond
- No river
- No house
- No buffalo
- No cat
- No grandma
- No bamboo fence unless structurally part of the farm item
- No UI
- No readable text
- No fake letters
- No cropped object
- No shadow on ground
- No realistic photo style
- No 3D render look
- No different camera angle
- No alternative orientation unless explicitly requested

---

# 2) Nhóm G — Nông trại / ruộng / item liên quan trồng trọt

---

## G1. FARM_RiceField_Block_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the complete rice field plot as one whole farm prop. Preserve the exact original viewing angle and orientation from the mockup. The asset should show a small rectangular Vietnamese countryside rice field plot with a warm wooden or muddy border, shallow wet soil, and neat young green rice sprouts growing inside. Keep it as one complete object, not separated into modular parts. Match the cozy hand-drawn papercraft style, warm pastel palette, clean linework, soft paper texture, and thick clean white sticker border around the entire field plot. Show only the rice field plot, centered, transparent background, no sign, no buffalo, no fence, no grass background, no UI, no text, no shadow.

### Negative prompt

background, grass field, brick path, bamboo fence, buffalo, house, grandma, cat, rice field sign, UI, text, realistic rice paddy, different angle, cropped field, large landscape tile, full scene, shadow on floor, extra props

> Ghi chú:
> G1 chỉ dùng nếu bạn muốn ô ruộng là một prop lớn có thể đặt trong Unity. Nếu sau này làm Tilemap ruộng riêng, có thể bỏ G1.

---

## G2. CROP_RiceSprout_Cluster_01_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of one small cluster of young rice sprouts. Preserve the same original viewing angle and orientation language from the mockup. The crop should look like a cute Vietnamese rice seedling cluster with fresh green leaves, simple readable shape, cozy hand-drawn papercraft style, warm pastel colors, clean linework, soft paper texture, and a thick clean white sticker border if used as a standalone game item. Show only one rice sprout cluster, centered, transparent background, no soil tile, no field border, no water, no sign, no text, no shadow.

### Negative prompt

background, full rice field, soil block, water tile, wooden border, sign, UI, text, realistic rice plant, mature rice, yellow rice grain, different angle, cropped leaves, ground patch, shadow, multiple unrelated crops

---

## G3. CROP_RiceSprout_Cluster_02_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a second variant of a young rice sprout cluster. Preserve the same 2.5D countryside viewing angle and orientation language from the mockup, but make the silhouette slightly different from the first sprout cluster while keeping the same style. The crop should have fresh green leaves, a compact farm-game readable shape, cozy hand-drawn papercraft texture, warm pastel colors, clean linework, and a thick clean white sticker border if used as a standalone asset. Show only one rice sprout cluster variant, centered, transparent background, no soil tile, no full field, no sign, no text, no shadow.

### Negative prompt

background, full rice field, soil block, water tile, wooden border, sign, UI, text, realistic rice plant, mature yellow rice, different camera angle, cropped leaves, ground patch, shadow, extra crops

> Ghi chú:
> G2 và G3 là biến thể crop. Chỉ cần nếu gameplay cần nhiều cụm lúa để rải trong ruộng.

---

## G4. CROP_RiceSeedling_Row_3Q

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a short row of young rice seedlings. Preserve the same original 2.5D viewing angle and orientation language from the mockup. The asset should show several small green rice sprout clusters arranged in a short neat row, suitable for placing inside a rice field plot in Unity. Keep the cozy Vietnamese countryside papercraft style, warm pastel colors, clean linework, soft paper texture, and a thick clean white sticker border if used as a standalone crop item. Show only the short rice seedling row, centered, transparent background, no soil tile, no field border, no water, no sign, no text, no shadow.

### Negative prompt

background, full rice field, soil tile, water, wooden border, sign, UI, text, realistic rice seedlings, mature rice, different angle, cropped row, ground patch, shadow, extra objects

> Ghi chú:
> G4 hữu ích hơn G2/G3 nếu bạn muốn đặt crop theo hàng thay vì từng bụi nhỏ.

---

## G5. CROP_RiceBundle_Mature_Icon_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a harvested bundle of mature rice. Preserve the same cozy Vietnamese countryside style and 2.5D item orientation language from the mockup. The asset should show a neat tied bundle of golden ripe rice stalks, warm yellow-gold colors, cute readable silhouette, hand-drawn papercraft texture, clean linework, and a thick clean white sticker border. Show only the rice bundle item, centered, transparent background, no field, no basket, no UI frame, no text, no shadow.

### Negative prompt

background, full rice field, green seedlings, UI frame, readable text, realistic rice photo, different style, cropped bundle, ground patch, shadow, extra crops, basket

> Ghi chú:
> G5 không thấy rõ như item chính trong mockup farm screen, nhưng hợp lý nếu cần inventory/crop reward item cùng style.

---

## G6. FARM_FieldWoodBorder_3Q_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of the wooden or muddy border frame of the rice field plot. Preserve the exact original viewing angle and orientation from the mockup. The asset should show only the simple rectangular border around the small farm plot, matching the cozy Vietnamese countryside hand-drawn papercraft style, warm brown colors, clean linework, soft paper texture, and thick clean white sticker border. The center should be empty/transparent or clean enough for Unity to place crops separately. Show only the field border frame, centered, transparent background, no rice sprouts, no sign, no soil fill, no grass, no text, no shadow.

### Negative prompt

background, full rice field with crops, soil fill, water, sign, grass, buffalo, fence, UI, text, realistic wooden frame, different angle, cropped border, ground patch, shadow, extra props

> Ghi chú:
> G6 là optional và hơi nghiêng về modular. Chỉ dùng nếu sau này bạn muốn ruộng có thể thay crop riêng trong Unity.

---

## G7. FARM_MuddySoilPatch_3Q_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D game asset of a small muddy soil patch for a rice field plot. Preserve the same original 2.5D viewing angle and orientation language from the mockup. The soil patch should look soft, warm brown, slightly wet, hand-drawn, and suitable for a cozy Vietnamese countryside farm game. Keep the warm pastel palette, clean linework, soft papercraft texture, and a simple readable silhouette. Show only the muddy soil patch, centered, transparent background, no crops, no wooden border, no sign, no grass, no text, no shadow.

### Negative prompt

background, full rice field, rice sprouts, wooden border, grass tile, brick path, water pond, UI, text, realistic mud photo, different angle, cropped patch, shadow, extra props

> Ghi chú:
> G7 cũng hơi nghiêng về background/tile. Chỉ dùng nếu bạn muốn tạo patch đất riêng chứ không dùng Tilemap.

---

## G8. UI_Icon_RicePlant_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D UI icon asset of a young rice plant. Preserve the cozy hand-drawn UI style from the mockup. The icon should show a cute green rice sprout or small rice plant with a clear readable silhouette, warm pastel colors, clean linework, soft papercraft texture, and a thick clean white sticker border. Show only the rice plant icon, centered, transparent background, no UI frame, no text, no numbers, no field, no ground, no shadow.

### Negative prompt

button frame, UI text, numbers, background, full rice field, realistic rice plant, different style, cropped icon, shadow, extra crops

> Ghi chú:
> G8 thuộc UI icon nhiều hơn scene prop. Dùng nếu cần icon crop cho nhiệm vụ, kho đồ, shop hoặc bảng mục tiêu.

---

## G9. UI_Icon_SeedPacket_Rice_Optional

### Prompt

Using the provided mockup as the only visual reference, generate a single isolated 2D UI icon asset of a rice seed packet matching the cozy Vietnamese countryside UI style. Preserve the same warm pastel, hand-drawn papercraft look from the mockup. The seed packet should be a cute small paper packet with a simple rice sprout symbol, clean readable silhouette, rounded friendly shape, and thick clean white sticker border. The packet surface should have no readable text. Show only the seed packet icon, centered, transparent background, no UI frame, no numbers, no shadow, no extra objects.

### Negative prompt

background, UI frame, readable text, fake letters, realistic seed packet, different art style, cropped packet, shadow, extra crops, full field scene

> Ghi chú:
> G9 không phải item nhìn rõ trong farm screen này, nhưng hợp lý nếu cần mở rộng sang shop / inventory cùng style.

---

# 3) Asset nên ưu tiên trong nhóm G

## Ưu tiên cao

1. `FARM_RiceField_Block_3Q`
2. `CROP_RiceSprout_Cluster_01_3Q`
3. `CROP_RiceSprout_Cluster_02_3Q`
4. `CROP_RiceSeedling_Row_3Q`

## Ưu tiên trung bình

5. `CROP_RiceBundle_Mature_Icon_Optional`
6. `UI_Icon_RicePlant_Optional`

## Optional / có thể để sau

7. `FARM_FieldWoodBorder_3Q_Optional`
8. `FARM_MuddySoilPatch_3Q_Optional`
9. `UI_Icon_SeedPacket_Rice_Optional`

---

# 4) Lưu ý riêng cho nhóm G

## Nhóm G nằm giữa item và background

Ruộng, đất, nước và nền thường rất dễ bị lẫn với background. Vì vậy ở vòng này chỉ nên chọn những thứ có thể dùng như asset rõ ràng:

- cả ô ruộng nguyên khối
- cụm lúa non
- hàng lúa non
- icon cây lúa
- bó lúa thu hoạch

Chưa nên xử lý:

- nền cỏ
- đường gạch
- bờ ao
- nước ao
- sông
- nền đất toàn cảnh
- tile ruộng seamless

Các phần đó nên để workflow background/tilemap riêng.

## Không tách ruộng quá modular nếu chưa cần

Theo hướng đã chốt, hiện tại không nên tách quá sâu:

- từng thanh viền ruộng
- từng ô đất nhỏ
- từng cụm bùn
- từng giọt nước
- từng lá lúa rất nhỏ

Nếu gameplay sau này cần hệ thống trồng trọt chi tiết, có thể quay lại viết prompt modular riêng.

## Cả ô ruộng có thể xem là một item lớn

Nếu mục tiêu là dựng lại mockup nhanh trong Unity, `FARM_RiceField_Block_3Q` là lựa chọn tốt vì:

- dễ đặt vào scene
- ít phải ghép nhiều mảnh
- giữ đúng góc nhìn của mockup
- hợp với prototype và scene cố định

## Crop nên tách riêng nếu cần gameplay

Nếu crop cần thay đổi theo thời gian, nên có các asset riêng:

- seedling / mầm non
- growing / đang lớn
- mature / chín
- harvested bundle / bó thu hoạch

File này mới chuẩn bị mầm lúa và bó lúa. Các giai đoạn khác có thể viết ở batch crop riêng sau.

## Text không nên bake vào asset

Nếu ruộng có biển tên hoặc icon nhiệm vụ, nên tách:

- ruộng riêng
- bảng riêng
- icon riêng
- text render bằng Unity

Không nên bake chữ “Ruộng Lúa” vào ảnh.

---

# 5) Danh sách prompt hiện có trong file này

- G1. `FARM_RiceField_Block_3Q`
- G2. `CROP_RiceSprout_Cluster_01_3Q`
- G3. `CROP_RiceSprout_Cluster_02_3Q`
- G4. `CROP_RiceSeedling_Row_3Q`
- G5. `CROP_RiceBundle_Mature_Icon_Optional`
- G6. `FARM_FieldWoodBorder_3Q_Optional`
- G7. `FARM_MuddySoilPatch_3Q_Optional`
- G8. `UI_Icon_RicePlant_Optional`
- G9. `UI_Icon_SeedPacket_Rice_Optional`
