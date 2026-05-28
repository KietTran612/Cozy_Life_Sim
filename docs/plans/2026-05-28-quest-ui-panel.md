# Kế hoạch triển khai Hệ thống Sidebar Đa Tab (Tabbed Sidebar UI Architecture)

Kế hoạch này hướng dẫn chi tiết các bước thiết lập kiến trúc Sidebar đa tab trượt mượt mà bằng DOTween, hỗ trợ mở rộng nhiều bảng thông tin dài hạn (Quest, Stats, Achievements) trong `Cozy_Life_Sim`.

---

## 🏗️ Phân rã Cấu trúc Hierarchy đề xuất (Tabbed Sidebar Hierarchy)

Chúng ta sẽ thay thế `Quest_Panel` đơn lẻ bằng hệ thống `Sidebar_Panel` toàn diện neo ở mép phải màn hình:

```text
[UI_Root]
  ├── ...
  └── [Sidebar_Panel] (RectTransform - Neo mép phải, rộng 400px) -> Gắn CozySidebar
        │
        ├── [Tabs_Container] (Vertical Layout Group - Neo mép trái Sidebar, rộng 65px)
        │     ├── Toggle_Button (Button - Mũi tên đóng/mở trượt Sidebar)
        │     ├── Tab_Btn_0 (Button - Icon/Text "Q" mở Quest)
        │     └── Tab_Btn_1 (Button - Icon/Text "S" mở Stats)
        │
        └── [Content_Container] (RectTransform - Chiếm 335px còn lại bên phải)
              │
              ├── [Quest_Content] (Giao diện hiển thị Quest) -> Gắn QuestHudWidget
              │     ├── Quest_Title (TextMeshProUGUI - "ACTIVE QUESTS")
              │     ├── Quest_Item_0
              │     ├── Quest_Item_1
              │     └── Quest_Item_2
              │
              └── [Stats_Content] (Giao diện hiển thị Chỉ số - Demo Tab 2)
                    ├── Stats_Title (TextMeshProUGUI - "GAME STATS")
                    └── Stats_Text_0 (TextMeshProUGUI - Demo chỉ số)
```

---

## 🛠️ Đề xuất thay đổi (Proposed Changes)

### 1. Thành phần UI (UI Assembly)

#### [NEW] [CozySidebar.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/CozySidebar.cs)
* Tạo lớp `CozySidebar` để điều phối hoạt ảnh trượt và quản lý Tab.
* Khai báo các trường Serialized:
  * `RectTransform _slidingPanel` (Panel trượt).
  * `Button _toggleButton` và `TextMeshProUGUI _toggleButtonText` (Nút đóng/mở trượt).
  * `Button[] _tabButtons` (Mảng chứa các nút chuyển tab).
  * `RectTransform[] _tabContents` (Mảng chứa các Panel nội dung tương ứng).
* Tích hợp hoạt ảnh **DOTween**:
  * Khi Toggle: Trượt Panel ra sát mép phải (chỉ chừa lại 65px của dãy nút Tab để người chơi click lại).
  * Khi chọn Tab: Kích hoạt Sub-Panel tương ứng kèm hiệu ứng phóng to nhẹ (`DOScale`) tạo cảm giác mọng nước (juice). Tự động trượt mở Sidebar nếu đang đóng.
  * Tắt cờ `Raycast Target` trên ảnh nền che khuất và các text tĩnh để chuột đi xuyên qua vùng trống.

#### [MODIFY] [QuestHudWidget.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/UI/QuestHudWidget.cs)
* Tắt cờ `raycastTarget = false` trên các text nhiệm vụ để tránh cản trở chuột khi rê nhãn dán.

---

### 2. Thành phần Editor (Editor Tool & Scaffolding)

#### [MODIFY] [CozySceneSetupWindow.cs](file:///d:/soflware/Unity/Source/Cozy_Life_Sim/Assets/CozyLifeSim/Scripts/Editor/CozySceneSetupWindow.cs)
* Thay thế cơ chế sinh `Quest_Panel` cũ sang tạo dựng **`Sidebar_Panel`** hoàn chỉnh.
* Thiết lập chính xác hệ thống Layout dọc cho Tab, Layout ngang chia vùng giữa Tab và Content.
* Tự động sinh `Quest_Content` (gắn `QuestHudWidget`) và `Stats_Content` làm mẫu demo.
* Tự động gắn và liên kết toàn bộ tham chiếu của `CozySidebar` và `QuestHudWidget` thông qua `SerializedObject`.

---

## 🧪 Kế hoạch Xác thực (Verification Plan)

### 1. Kiểm thử Tự động (Automated Verification)
* Biên dịch dự án sạch 100%.

### 2. Kiểm thử Kéo thả & Trượt (Manual Verification)
1. Mở Unity Editor, chọn `Tools -> CozySim -> Setup Test Scene` để sinh tự động Sidebar Đa Tab.
2. Nhấn **Play**:
   * Xác nhận Sidebar mặc định hiển thị tab **Quests**.
   * Nhấn nút trượt `[ > ]` -> Sidebar trượt mượt mà biến mất về bên phải, chỉ để lộ thanh nút tab 65px.
   * Rê kéo nhãn dán đi qua vùng trống cũ của Sidebar $\rightarrow$ Xác nhận nhãn dán **không bị chặn chuột**, thả vào sách nhãn dán thành công.
   * Bấm Tab **`S`** (Stats) -> Sidebar tự động trượt ra và hiển thị bảng chỉ số Stats mẫu.
   * Bấm Tab **`Q`** (Quests) -> Trở lại danh sách nhiệm vụ mượt mà.
