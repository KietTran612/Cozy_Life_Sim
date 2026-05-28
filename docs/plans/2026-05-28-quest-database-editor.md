# Kế hoạch triển khai Hệ thống Dữ liệu Nhiệm vụ & Cửa sổ Editor Tùy chỉnh (Quest Database & Editor Window)

Kế hoạch này hướng dẫn chi tiết các bước thiết lập cơ sở dữ liệu Quest dạng ScriptableObject và xây dựng một cửa sổ thiết kế dữ liệu tùy chỉnh (Quest Editor Window) chuyên nghiệp trong Unity Editor, đáp ứng các tiêu chuẩn nghiêm ngặt về phân rã Assembly, an toàn dữ liệu lưu trữ và kiểm soát dữ liệu toàn vẹn.

---

## 🏗️ Kiến trúc Đề xuất (Quest Data Architecture)

Chúng ta phân chia dữ liệu và ranh giới lắp ráp (Assembly Boundary) một cách chặt chẽ:

```text
[CozyLifeSim.Core] (Pure C# Data boundary)
  ├── QuestTemplate (Serializable class - ID, Title, TargetCount, RewardCoins, QuestType)
  ├── QuestType (Enum - WaterCrops, HarvestCrops, PetAnimal)
  └── IQuestService (Interface - ProgressQuest(QuestType, amount))

[CozyLifeSim.UI] (Unity Runtime boundary)
  ├── QuestDatabase (ScriptableObject - List<QuestTemplate>)
  └── QuestService (Concrete class - DI injected with QuestDatabase)
```

---

## 🛠️ Đề xuất thay đổi (Proposed Changes)

### 1. Thành phần Core (Core Assembly)

#### [NEW] [QuestType.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/QuestType.cs)
* Tạo enum `QuestType` để định nghĩa loại tương tác, giúp tách rời ID nhiệm vụ:
  ```csharp
  namespace CozyLifeSim.Core
  {
      public enum QuestType
      {
          WaterCrops = 0,
          HarvestCrops = 1,
          PetAnimal = 2
      }
  }
  ```

#### [NEW] [QuestTemplate.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/QuestTemplate.cs)
* Đặt cấu trúc `QuestTemplate` tĩnh (Serializable class) bên trong Core Assembly, hỗ trợ cả Constructor đầy đủ và rỗng phục vụ cơ chế sao chép/serialization:
  ```csharp
  using System;

  namespace CozyLifeSim.Core
  {
      [Serializable]
      public class QuestTemplate
      {
          public int QuestId;
          public string Title;
          public int TargetCount;
          public int RewardCoins;
          public QuestType Type;

          // Constructor rỗng bắt buộc phục vụ việc tạo thực thể động và Staging trong Editor
          public QuestTemplate() { }

          public QuestTemplate(int questId, string title, int targetCount, int rewardCoins, QuestType type)
          {
              QuestId = questId;
              Title = title;
              TargetCount = targetCount;
              RewardCoins = rewardCoins;
              Type = type;
          }
      }
  }
  ```

#### [MODIFY] [QuestData.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/QuestData.cs)
* Thêm trường `public QuestType Type;` vào lớp `QuestData` và cập nhật Constructor để lưu trữ loại nhiệm vụ lúc khởi chạy.

#### [MODIFY] [IQuestService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Core/IQuestService.cs)
* Thay thế chữ ký phương thức:
  * Cũ: `void ProgressQuest(int questId, int amount);`
  * Mới: `void ProgressQuest(QuestType type, int amount);`

---

### 2. Thành phần UI & Services (UI Assembly)

#### [NEW] [QuestDatabase.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Settings/QuestDatabase.cs)
* Tạo lớp `QuestDatabase` kế thừa từ `ScriptableObject` nằm trong UI Assembly.
* Chứa `public List<QuestTemplate> Quests = new List<QuestTemplate>();`.
* Tích hợp phương thức tự kiểm tra dữ liệu hợp lệ `ValidateDatabase(out List<string> errors)`:
  * Kiểm tra trùng ID.
  * Kiểm tra Target Count <= 0.
  * Kiểm tra Reward Coins < 0.
  * Kiểm tra Title rỗng.

#### [MODIFY] [QuestService.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Services/QuestService.cs)
* Cấu hình Constructor nhận tham số `QuestDatabase` truyền trực tiếp từ Factory của VContainer:
  ```csharp
  public QuestService(
      ISaveService saveService, 
      IInventoryService inventoryService, 
      QuestDatabase questDatabase)
  ```
  Nếu `questDatabase` bị null hoặc danh sách nhiệm vụ rỗng, tự động nạp **3 nhiệm vụ mặc định làm Fallback an toàn**:
  * Water 3 Crops (ID 1), Harvest 2 Crops (ID 2), Pet Chicken 5 times (ID 3).
* Tích hợp cơ chế **Normalize/Sanitize Save Data**:
  * Khi nạp save, tự động loại bỏ các phần tử `ActiveQuestProgress` hoặc ID hoàn thành trong `CompletedQuestIds` của các Quest ID đã bị xóa khỏi `QuestDatabase` hiện tại.
* Chuyển đổi phương thức tiến trình `ProgressQuest(QuestType type, int amount)`:
  * **Giải quyết trùng loại (Single Target Active)**: Tìm kiếm nhiệm vụ đang hoạt động **ĐẦU TIÊN** (chưa hoàn thành) khớp với loại `QuestType` để tăng tiến trình (tránh lỗi một hành động tăng tiến đồng thời cho nhiều quest cùng loại).

#### [MODIFY] [GameLifetimeScope.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/GameLifetimeScope.cs)
* Thêm trường `[SerializeField] private QuestDatabase _questDatabase;`.
* Đăng ký DI **an toàn 100%** bằng cơ chế Lambda Factory thay vì `RegisterInstance` trực tiếp (phòng tránh crash VContainer khi database null):
  ```csharp
  builder.Register<IQuestService>(resolver => new QuestService(
      resolver.Resolve<ISaveService>(),
      resolver.Resolve<IInventoryService>(),
      _questDatabase), Lifetime.Singleton);
  ```
  Việc này giúp tập trung vào Quest database, loại bỏ rủi ro về null-dependency, và hoàn toàn không làm ảnh hưởng đến cách StyleService hiện tại đang được đăng ký.

---

### 3. Presenters (UI Assembly)

#### [MODIFY] [FarmPresenter.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Presenters/FarmPresenter.cs)
* Thay thế `ProgressQuest(1, 1)` bằng `ProgressQuest(QuestType.WaterCrops, 1)`.
* Thay thế `ProgressQuest(2, 1)` bằng `ProgressQuest(QuestType.HarvestCrops, 1)`.

#### [MODIFY] [AnimalPresenter.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/Presenters/AnimalPresenter.cs)
* Thay thế `ProgressQuest(3, 1)` bằng `ProgressQuest(QuestType.PetAnimal, 1)`.

---

### 4. Thành phần Editor (Editor Assembly)

#### [NEW] [QuestEditorWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/QuestEditorWindow.cs)
* Tạo cửa sổ Editor Window tại **`Tools -> CozySim -> Quest Database Editor`**.
* Áp dụng cơ chế **Staging/Copy-on-write** chỉnh sửa an toàn:
  * Khi chọn một Quest hoặc tạo mới, toàn bộ dữ liệu sẽ được đưa vào các biến Staging (in-memory copy).
  * Việc chỉnh sửa trực tiếp chỉ diễn ra trên Staging, **hoàn toàn không chạm vào file ScriptableObject gốc**.
  * Nút **"Save to Database"** chỉ khả dụng khi `ValidateDatabase()` trả về không có lỗi. Lúc này mới chép ngược dữ liệu từ Staging vào Asset gốc, đánh dấu `EditorUtility.SetDirty` và gọi `AssetDatabase.SaveAssets()`.
  * Có nút **"Revert Changes"** để hủy bỏ toàn bộ chỉnh sửa tạm thời.
  * Hiển thị bảng cảnh báo màu đỏ/vàng trên Editor nếu dữ liệu Staging vi phạm toàn vẹn.

#### [MODIFY] [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
* Tự động quét tìm `QuestDatabase.asset` và gán vào `GameLifetimeScope`.

#### [MODIFY] [CozyLifeSimValidation.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozyLifeSimValidation.cs)
* Cập nhật các test case theo `QuestType`.
* Bổ sung **Test 5: Quest Editor Data Integrity** tự động kiểm nghiệm dữ liệu ScriptableObject trùng ID, target không hợp lệ, hoặc reward âm.

---

## 🧪 Kế hoạch Xác thực (Verification Plan)

### 1. Kiểm thử Tự động (Automated Verification)
* Chạy bộ xác thực logic cốt lõi qua Menu `Tools -> CozySim -> Run Logic Verification Tests` để đảm bảo:
  1. DI và các dịch vụ khởi tạo hoàn hảo khi database bị null hoặc rỗng.
  2. Test Case 5 bắt lỗi dữ liệu sai quy chuẩn một cách chính xác.

### 2. Kiểm thử Thủ công (Manual Verification)
1. Mở Editor Window, cố ý điền Target = `-5` hoặc ID trùng $\rightarrow$ Báo lỗi đỏ và nút Save bị khóa.
2. Tạo 3 quest có ID khác biệt (ví dụ: 10, 20, 30) gắn đúng loại `QuestType`.
3. Bấm **Setup Test Scene** rồi nhấn **Play**. Tương tác tưới nước, gieo hạt $\rightarrow$ Quest chỉ tăng tiến cho nhiệm vụ WaterCrops đầu tiên chưa hoàn thành.
