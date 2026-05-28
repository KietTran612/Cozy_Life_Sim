# Cozy Life Sim - Ke hoach thiet lap Scene kiem thu (Unity Scene Setup Plan)

Ke hoach nay huong dan chi tiet cac buoc thiet lap mot Scene kiem thu hoan chinh trong Unity Editor de van hanh, ket noi (wire-up) va chay thu nghiem 4 luong gameplay cot loi da lap trinh.

---

## 🏗️ Phan ra Cau truc Hierarchy de xuat (UGUI Hierarchy)

```text
[Main Scene]
  ├── [Directional Light] (Mac dinh)
  ├── [Main Camera] (Mac dinh)
  ├── [GameLifetimeScope] (GameObject trong) -> Gan script GameLifetimeScope
  └── [Canvas] (UGUI Canvas)
        ├── [EventSystem] (Mac dinh di kem Canvas)
        │
        ├── [UI_Root] (RectTransform - Gian toan man hinh)
        │     │
        │     ├── [Header_Panel] (Thanh thong tin tai san phia tren) -> Gan InventoryHudWidget
        │     │     ├── Coins_Text (TextMeshProUGUI) -> Gan UIStyleElement (Key: "Header_Text")
        │     │     ├── Seeds_Text (TextMeshProUGUI) -> Gan UIStyleElement (Key: "Header_Text")
        │     │     └── Crops_Text (TextMeshProUGUI) -> Gan UIStyleElement (Key: "Header_Text")
        │     │
        │     ├── [Gameplay_Area] (Khu vuc choi game o giua - Horizontal Layout)
        │     │     │
        │     │     ├── [Farm_Plot] (O dat trong cay - Gan CropWidget)
        │     │     │     ├── Crop_Visual (Image) -> Hien thi Sprite cay
        │     │     │     ├── Timer_Text (TextMeshProUGUI) -> Hien thi thoi gian/trang thai
        │     │     │     ├── Water_Button (Button) -> Tuoi nuoc
        │     │     │     ├── Plant_Button (Button) -> Gieo hat
        │     │     │     ├── Harvest_Button (Button) -> Thu hoach
        │     │     │     └── Watering_Can (Image/RectTransform) -> Binh tuoi (An mac dinh)
        │     │     │
        │     │     ├── [Animal_Pen] (Chuong ga - Gan AnimalWidget)
        │     │     │     ├── Interaction_Button (Button) -> Nut click vuot ga
        │     │     │     ├── Spawn_Root (RectTransform) -> Diem xuat hien tim bay
        │     │     │     └── Chicken_Visual (Image) -> Hinh anh con ga
        │     │     │
        │     │     └── [StickerBook_Panel] (Album nhan dan - Gan StickerBook)
        │     │           ├── Prev_Button (Button) -> Trang truoc
        │     │           ├── Next_Button (Button) -> Trang sau
        │     │           ├── Flip_Page_Indicator (RectTransform - An mac dinh) -> Hieu ung lat
        │     │           ├── [Page_0] (Gan StickerBookPage - PageIndex = 0)
        │     │           └── [Page_1] (Gan StickerBookPage - PageIndex = 1)
        │     │
        │     └── [Inventory_Tray] (Khay nhan dan phia duoi - Grid Layout)
        │           ├── [Sticker_0] (Gan CozySticker - StickerId = 1) -> Draggable Sticker Template
        │           │     ├── Shadow_Offset (RectTransform) -> Anh bong do dich lech
        │           │     ├── Visual_Image (Image) -> Anh sticker thuc te
        │           │     └── CanvasGroup (Thanh phan bat buoc)
        │           └── [Sticker_1] (Gan CozySticker - StickerId = 2) -> Draggable Sticker Template
        │                 ├── Shadow_Offset
        │                 ├── Visual_Image
        │                 └── CanvasGroup
        │
        └── [Prefabs_Holder] (Khu vuc chua Prefab mau de keo tha tham chieu - An mac dinh)
              └── [Heart_Feedback_Template] (UI Image hinh trai tim - Gan CanvasGroup)
```

---

## 🛠️ Huong dan thiet lap tung buoc bang Cozy Scene Setup Window (Editor Tool)

Moi thiet lap hien nay da duoc tu dong hoa 100% thong qua mot Editor Tool tien loi:

1. Doi Unity Editor hoan tat bien dich cac file moi.
2. Tren thanh menu cua Unity, chon: **`Tools -> CozySim -> Setup Test Scene`**.
3. Cua so **Cozy Scene Setup** se xuat hien, tu dong tim kiem file `CozyUIStyleConfig` trong du an của ban.
4. Nhan nut **`Generate Test Scene Hierarchy & Wiring`**.
5. **Ket qua**: Toan bo Hierarchy va tham chieu private giua cac Button, Text, Image va Services se duoc tu dong tao va ket noi hoan hao trong Scene chi sau 1 giay!

---

## 🧪 Kich ban kiem thu tich hop thuc te (Runtime Integration Test)

Sau khi nhan nut **Play** trong Unity Editor, hay chay tuan tu cac bai kiem tra sau de xac thuc:

### 1. Kiem tra tiem phu thuoc (Injection Test)
* **Ky vong**: Console hoan toan sach bong, khong xuat hien bat ky thong bao loi bien dich hay loi do `NullReferenceException` lien quan den VContainer hay Inject.

### 2. Luong kiem thu Nong trai (Farm Loop)
1. Bam nut **Plant Seed** tren o dat trong -> Giao dien chuyen sang hien thi hinh anh Hat gieo va hien thi chu `NEED WATER!`.
2. Bam nut **Water** -> Binh tuoi nuoc bay ra, nghieng 45 do lac lu tuoi nuoc -> Cay rung rinh nhan nuoc -> Dong chu doi sang `Growing: 5s`.
3. Cho 5 giay -> Cay lon thanh Mam -> Lon tiep thanh cay Non -> Lon tiep thanh cay Chin (Ready to harvest!).
4. Bam nut **Harvest** -> Nhan 10 Coins, o dat reset ve Dat trong (`EMPTY SOIL`).

### 3. Luong kiem thu Vat nuoi (Pet Animal)
1. Bam vuot ve con ga -> Con ga nhay lo co len xuong -> Mot bong bong tim bay xuat hien tu chan ga bay len va mo dan (fade out).
2. Kiem tra Coins trong vi tren thanh Header tu dong cap nhat tang them `+5` coins cho moi lan vuot.

### 4. Luong kiem thu Sticker & Luu tru dong thoi (Sticker Save & Restore)
1. Keo mot nhan dan tu khay chua `Inventory_Tray` (Sticker_0 hoac Sticker_1) tha vao trang `Page_0`.
2. Di chuyen trang bang nut Next/Prev -> Lat trang muot ma.
3. Nhan **Stop Play Mode** trong Unity Editor -> Nhan **Play** tro lai.
4. **Ky vong**: Nhan dan da dat xuat hien dung vi tri (X, Y) va trang da dan truoc khi tat game. Vi tien coins cua cac hanh dong vuot ga/nong trai truoc do van duoc bao toan nguyen ven.
