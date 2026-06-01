# Vietnamese Heritage Asset Sprite Integration & Prompt Design Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Trien khai tich hop cac Sprite ve tay (2D cartoon game asset) thuc te cho cac vat pham di san Viet Nam. Uu tien dung real sprites khi co file trong thu muc Heritage, dong thoi giu nguyen co che Flat Fallback lam safety/debug path an toan. Tu dong hoa viec import anh bang TextureImporter va chay dong bo nang cap (idempotent upgrade) cho cac entry database da ton tai.

**Architecture:** Bo sung helper post-processing hoac logic check trong tung Database Utility Editor: tu dong cau hinh TextureImporter cho anh moi, quet database nang cap (override) cac entry da co voi real sprite moi, va viet kiem thu tich hop sau de verify chinh xac asset tuong ung voi tung ID.

**Tech Stack:** Unity Editor AssetDatabase API, TextureImporter API, CozyLifeSim Databases.

---

## I. Yeu Cau Danh Gia Tu Nguoi Dung (User Review Required)

> [!IMPORTANT]
> **Khong "thay hoan toan" Flat Fallback:**
> Giu nguyen co che safety cua Flat Fallback (`CozyProceduralUI.cs`) va bien flag `ForceFlatUIDebug` / `UIStyleConfig.ForceFlatUI` de dam bao game chay on dinh khi thieu file hoac khi can debug, va giu vung cac kiem thu validation hien tai.
> 
> **Tu dong hoa Texture Import (Repeatable Workflow):**
> Tu dong hoa qua trinh import hinh anh thong qua API `TextureImporter` tai Editor. Khi nguoi choi hoac agent cap nhat file anh vao thu muc `Assets/CozyLifeSim/Textures/Heritage/`, he thong se tu dong cau hinh sang che do Sprite ma khong can bat ky thao tac thu cong nao tren Inspector.
>
> **Cho doi Unity sinh .meta an toan:**
> Tuyet doi khong tu tao tay file `.meta` cho anh PNG moi sinh ra. Cho Unity tu dong compile/refresh va tu dong sinh file `.meta` hop le.

---

## II. Chi Tiet Cac Thay Doi De Xuat (Proposed Changes)

### Task 1: Prompt Designs & Sinh Anh Heritage PNG

**Step 1: Sinh anh bang `generate_image`**
Chung ta se lan luot goi cong cu tao anh cho 14 tai nguyen di san Viet Nam (5 Stickers, 3 Crops (voi 3 stages) va 2 Animals). Lay prompt chi tiet tai link tuong ung trong [required-assets.md](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md):

*   **Vietnamese Heritage Stickers (5 Stickers)**:
    1.  **Xe Banh Mi (ID 4)**: [Sticker_BanhMiCart.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Sticker_BanhMiCart)
    2.  **Ly Nuoc Mia (ID 5)**: [Sticker_SugarcaneJuice.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Sticker_SugarcaneJuice)
    3.  **Chiec Non La (ID 6)**: [Sticker_ConicalHat.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Sticker_ConicalHat)
    4.  **Chiec Xich Lo (ID 7)**: [Sticker_Cyclo.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Sticker_Cyclo)
    5.  **Long Den Trung Thu (ID 8)**: [Sticker_StarLantern.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Sticker_StarLantern)

*   **Vietnamese Heritage Crops (3 Crops, 3 Stages each)**:
    6.  **Cay Mia Ngot (ID 2)**:
        *   Seed: [Crop_Sugarcane_Seed.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Sugarcane_Seed)
        *   Sprout: [Crop_Sugarcane_Sprout.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Sugarcane_Sprout)
        *   Mature/Harvest: [Crop_Sugarcane_Mature.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Sugarcane_Mature)
    7.  **Lua Nuoc (ID 3)**:
        *   Seed: [Crop_Rice_Seed.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Rice_Seed)
        *   Sprout: [Crop_Rice_Sprout.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Rice_Sprout)
        *   Mature/Harvest: [Crop_Rice_Mature.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Rice_Mature)
    8.  **Hoa Sen (ID 4)**:
        *   Seed: [Crop_Lotus_Seed.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Lotus_Seed)
        *   Sprout: [Crop_Lotus_Sprout.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Lotus_Sprout)
        *   Mature/Harvest: [Crop_Lotus_Mature.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Crop_Lotus_Mature)

*   **Vietnamese Heritage Animals (2 Animals)**:
    9.  **Meo Tam The (ID 2)**: [Animal_CalicoCat.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Animal_CalicoCat)
    10. **Trau Nuoc (ID 3)**: [Animal_WaterBuffalo.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Animal_WaterBuffalo)

*   **NPC Dialogue Portraits (2 NPC Portraits)**:
    11. **Chan Dung Ba Ngoai (NPC Grandma)**: [NPC_Portrait_Grandma.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#NPC_Portrait_Grandma)
    12. **Chan Dung Co Ba (NPC Shop Vendor)**: [NPC_Portrait_CoBa.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#NPC_Portrait_CoBa)

*   **Scrapbook Customization Assets (4 Assets)**:
    13. **Giay Nen Co Dien (Old Paper)**: [Scrapbook_Bg_OldPaper.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Scrapbook_Bg_OldPaper)
    14. **Giay Pastel Hong (Pastel Pink)**: [Scrapbook_Bg_PastelPink.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Scrapbook_Bg_PastelPink)
    15. **Giay O Ly Tap Voi (Grid Paper)**: [Scrapbook_Bg_GridPaper.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Scrapbook_Bg_GridPaper)
    16. **Giay Ghi Chu Dan (Yellow Sticky Note)**: [Scrapbook_StickyNote_Yellow.png](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/docs/plans/required-assets.md#Scrapbook_StickyNote_Yellow)

**Quy tac bat buoc**: Luu file PNG dung voi Ten asset tieng Anh neu tren vao thu muc `Assets/CozyLifeSim/Textures/Heritage/` de dam bao he thong automatic import va idempotent upgrade quet phat hien chinh xac.


---

### Task 2: Automating Sprite Import & Idempotent Database Upgrades

**Files:**
- Create: [CozyAssetImporterUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyAssetImporterUtility.cs)
- Modify: [StickerDatabaseUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/StickerDatabaseUtility.cs)
- Modify: [CropDatabaseUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CropDatabaseUtility.cs)
- Modify: [AnimalDatabaseUtility.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/AnimalDatabaseUtility.cs)

**Step 1: Viet CozyAssetImporterUtility de tu dong hoa Import settings**
*   Khi tệp PNG vừa được ghi từ ngoài Unity (bởi công cụ generate_image), tệp đó chưa tồn tại trong AssetDatabase của Unity. Do đó, việc gọi `GetAtPath` ngay lập tức sẽ trả về `null`.
*   Quy trình import chuẩn hóa bắt buộc:
    1.  Ghi tệp PNG xuống hệ thống file.
    2.  Gọi `AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate)` để Unity đăng ký tài nguyên và tạo thiết lập mặc định.
    3.  Gọi `AssetImporter.GetAtPath(assetPath) as TextureImporter` để lấy Importer và cấu hình Sprite.
    4.  Gọi `SaveAndReimport()` để Unity ghi nhận định dạng Sprite.
    5.  Sau đó gọi `AssetDatabase.LoadAssetAtPath<Sprite>(assetPath)` để load Sprite thành công.

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CozyLifeSim.Editor
{
    public static class CozyAssetImporterUtility
    {
        public static void ConfigureAsSprite(string assetPath)
        {
            // Buoc 1: Bat buoc Import truoc de Unity ghi nhan doi tuong ngoai he thong
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Buoc 2: Lay importer cau hinh sang Sprite
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    !importer.alphaIsTransparency)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif
```

**Step 2: Viet logic "Idempotent Upgrade" voi SetDirty day du**
*   *Trong `StickerDatabaseUtility.cs`, `CropDatabaseUtility.cs`, `AnimalDatabaseUtility.cs`*:
    *   Sử dụng một cờ hiệu trạng thái duy nhất: `bool changedAny = false;` đại diện cho cả việc **thêm mới (add)**, **tự sửa lỗi (repair)** và **ghi đè nâng cấp di sản (idempotent upgrade)**.
    *   Khi có file ảnh di sản tương ứng trong `Heritage` folder, ta thực hiện `CozyAssetImporterUtility.ConfigureAsSprite` trước, sau đó so sánh và gán đè Sprite mới. Nếu có sự thay đổi giá trị Sprite, ta chuyển `changedAny = true;`.
    *   **Quy tắc bắt buộc**: Chỉ khi `changedAny == true` ta mới gọi `EditorUtility.SetDirty(database)` và `AssetDatabase.SaveAssets()`. Điều này đảm bảo dữ liệu ghi đè được persist xuống đĩa cứng (disk) một cách chính xác.
*   *Quyet dinh cho Animal HeartFeedbackSprite*:
    *   `HeartFeedbackSprite` của các vật nuôi di sản (Mèo Tam Thể, Trâu Nước) sẽ **giữ nguyên là generic feedback sprite** (Sprite trái tim màu đỏ mặc định từ CuteKawaii hoặc fallback built-in). 
    *   Không sinh ảnh riêng cho tim di sản để giữ scope tinh giản và tập trung 100% vào Sprite hình dáng của con vật.

---

## III. Verification Plan (Quy Trinh Kiem Thu & Tieu Chi Nghiem Thu)

### Kiem Thu Tu Dong (Automated Tests)
1.  **Test 0: Exact Asset Path & GUID Validation**
    *   Bổ sung kiểm tra sau vào suite kiểm thử logic của `CozyLifeSimValidation.cs` để phân biệt rõ giữa "asset chưa generate" và "asset có nhưng database chưa bind":
    *   Duyệt qua từng mẫu di sản: Sticker ID 4-8, Crop ID 2-4 (tat ca stages), Animal ID 2-3.
    *   Kiểm tra sự tồn tại của file trên đĩa cứng: `bool fileExists = File.Exists(heritagePath);`
    *   **Trường hợp `fileExists == true` (Asset đã được generate)**: Assert nghiêm ngặt `AssetDatabase.GetAssetPath(sprite)` phải khớp chính xác với `heritagePath`. Nếu không khớp (ví dụ vẫn dùng CuteKawaii placeholder), kiểm thử sẽ thất bại (Fail).
    *   **Trường hợp `fileExists == false` (Asset chưa được generate)**: Cho phép sử dụng Sprite fallback mặc định, báo cáo một Expected Warning an toàn và tiếp tục bỏ qua kiểm tra đường dẫn khắt khe để giữ vững kiến trúc Fallback của game.

### Kiem Thu Thu Cong (Manual Verification)
1.  Mở Play Mode.
2.  Mở Tiệm Tạp Hóa (Shop) va khu vuon. Verify hinh anh hiển thị đúng các sprite di sản Việt Nam.
