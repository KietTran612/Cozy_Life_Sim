# Danh Sach Cac Tai Nguyen Hinh Anh (Required Sprite Assets)

Tai lieu nay liet ke toan bo cac hinh anh (Sprite) can co trong du an **Cozy Life Sim**, phan chia ro rang giua cac Tai nguyen He thong, Tai nguyen Nong san & Sticker mac dinh (su dung tu `CuteKawaiiGUIPack` lam placeholder/fallback), va cac Tai nguyen Chu de **Hoai Niem Viet Nam (Heritage)** se duoc ban thiet ke sau nay.

---

## 1. Tai Nguyen He Thong (System Sprites)
Day la cac hinh anh bo tro cho giao dien, hieu ung tuong tac hoac bieu tuong cong cu co ban trong game.

| Ten Tai Nguyen | Muc Dich Su Dung | Sprite Goc / Placeholder (`CuteKawaiiGUIPack`) | Vi Tri Sprite Lua Chon Thay The |
| :--- | :--- | :--- | :--- |
| **Watering Can** | Icon binh tuoi nuoc trong luong tuoi lua | `Icons/Farming/Watering-Can-Pink-256.png` | Anh binh tuoi nhom, tre hoac dat nung truyen thong Viet Nam. |
| **Heart Feedback** | Hieu ung tim bay len khi vuot ve thu cung | `Icons/Hearts/Heart-Red-256.png` | Trai tim dang cute, pastel nhe nhang. |
| **Coin Icon** | Icon dong xu hien thi tren Header/Shop | `Icons/Coins/Coin-Gold-256.png` | Dong xu co truyen thong Viet Nam (co lo vuong o giua). |

---

## 2. Tai Nguyen Nong San & Cay Trong (Crop Sprites)
Moi cay trong can **4 trang thai hinh anh rieng biet**: Hat giong (Seed), Cay con (Sprout), Cay chin (Mature), va Cay khi thu hoach (Harvest).

### Cay Trong Hien Tai (Default / Placeholders)
*   **White Acorn (ID: 1)**
    *   *Seed:* `Icons/Plants/Acorn-256.png`
    *   *Sprout:* `Icons/Plants/Sapling-256.png`
    *   *Mature:* `Icons/Flowers/Flower-Tulip-Red-256.png`
    *   *Harvest:* `Icons/Flowers/Flower-Tulip-Red-256.png`

### Cay Trong Hoai Niem Viet Nam (Vietnamese Heritage Crops - Cay Moi)
*   **Cay Mia Ngot (ID: 2)**
    *   *Seed:* Sprite hat giong/khuc mia giong (Placeholder: `Icons/Plants/Seed-Pink-256.png`).
    *   *Sprout:* Sprite mam mia non (Placeholder: `Icons/Plants/Sprout-256.png`).
    *   *Mature:* Sprite bui mia cao xanh ngot (Placeholder: `Icons/Plants/Bamboo-256.png` hoac tuong duong).
    *   *Harvest:* Sprite bo mia da chat khuc hoac khuc mia.
*   **Lua Nuoc (ID: 3)**
    *   *Seed:* Sprite hat lua vang bong (Placeholder: `Icons/Plants/Seed-256.png`).
    *   *Sprout:* Sprite ma non xanh muot (Placeholder: `Icons/Plants/Grass-256.png`).
    *   *Mature:* Sprite khom lua chin vang triu hat (Placeholder: `Icons/Plants/Wheat-256.png`).
    *   *Harvest:* Sprite bong lua vang.
*   **Hoa Sen (ID: 4)**
    *   *Seed:* Sprite hat sen kho (Placeholder: `Icons/Plants/Seed-Blue-256.png`).
    *   *Sprout:* Sprite la sen non tren mat nuoc (Placeholder: `Icons/Plants/Leaf-Ivy-256.png`).
    *   *Mature:* Sprite doa hoa sen hong no ruc ro (Placeholder: `Icons/Flowers/Flower-Lotus-256.png` hoac `Flower-Tulip-Pink-256.png`).
    *   *Harvest:* Sprite bong sen / hat sen xanh.

---

## 3. Tai Nguyen Dong Vat (Animal Sprites)
Moi loai thu nuoi can 1 hinh anh goc (Sprite) hien thi tren pen va bieu tuong phan hoi.

### Dong Vat Hien Tai (Default / Placeholders)
*   **Breathing Chicken (ID: 1)**
    *   *Chicken Sprite:* `Icons/Animals/Chicken-White-256.png`
    *   *Feedback Heart:* `Icons/Hearts/Heart-Red-256.png`

### Dong Vat Hoai Niem Viet Nam (Vietnamese Heritage Animals - Vat Nuoi Moi)
*   **Meo Tam The (ID: 2)**
    *   *Cat Sprite:* Chu meo tam the cute, dang ngoi hoac nam cuon tron (Placeholder: `Icons/Animals/Cat-Orange-256.png`).
*   **Trau Nuoc (ID: 3)**
    *   *Buffalo Sprite:* Chu trau nuoc Viet Nam sung cong de thuong (Placeholder: `Icons/Animals/Bear-256.png` - to lon, ngam mau den/xam dam).

---

## 4. Tai Nguyen Nhan Dan (Sticker Sprites)
Moi sticker can **1 anh goc co mau** va **1 anh bong do (Shadow Sprite)** de tao chieu sau 2D Pseudo-3D tren trang sticker book.

### Stickers Hien Tai (Default / Placeholders)
*   **Bunny Pink (ID: 1):** `Icons/Animals/Bunny-Pink-256.png`
*   **Bear (ID: 2):** `Icons/Animals/Bear-256.png`
*   **Chicken White (ID: 3):** `Icons/Animals/Chicken-White-256.png`

### Stickers Hoai Niem Viet Nam (Vietnamese Heritage Stickers - Sticker Moi)
*   **Xe Banh Mi (ID: 4):**
    *   *Sprite:* Xe banh mi o pho co voi tu kinh, banh mi vang gion.
    *   *Shadow Sprite:* Bong khoi xe banh mi (phuc vu Pseudo-3D).
    *   *Placeholder:* `Icons/Foods/Bread-256.png`.
*   **Ly Nuoc Mia (ID: 5):**
    *   *Sprite:* Ly nuoc mia xanh mat mat co lat chanh/quat va ong hut.
    *   *Shadow Sprite:* Bong ly nuoc mia.
    *   *Placeholder:* `Icons/Foods/Drink-Soda-Blue-256.png`.
*   **Chiec Non La (ID: 6):**
    *   *Sprite:* Chiec non la Viet Nam truyen thong mau la kho tu nhien.
    *   *Shadow Sprite:* Bong non la.
    *   *Placeholder:* `Icons/Equipments/Helmet-Straw-256.png` hoac `Icons/Plants/Acorn-256.png`.
*   **Chiec Xich Lo (ID: 7):**
    *   *Sprite:* Chiec xich lo co dien kieu Ha Noi / Sai Gon.
    *   *Shadow Sprite:* Bong chiec xich lo.
    *   *Placeholder:* `Icons/Equipments/Wagon-256.png`.
*   **Long Den Trung Thu (ID: 8):**
    *   *Sprite:* Long den ong sao truyen thong mau do vang lap lanh.
    *   *Shadow Sprite:* Bong long den.
    *   *Placeholder:* `Icons/Lights/Lantern-Red-256.png`.

---

## 5. Cac Bieu Tuong Tab Shop (Shop Tab Icons)
Dung de phan chia danh muc san pham trong Tiem Tap Hoa (Shop).

*   **Tab Hat Giong (Seeds):** Icon tui hat giong (Placeholder: `Icons/Farming/Seed-Bag-256.png`).
*   **Tab Nhan Dan (Stickers):** Icon album/sticker (Placeholder: `Icons/Tools/Paintbrush-256.png`).
*   **Tab Nong San (Crops):** Icon gio dung hoa qua/nong san (Placeholder: `Icons/Farming/Basket-Harvest-256.png`).
