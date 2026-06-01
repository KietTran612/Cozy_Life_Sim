# Danh Sach Cac Tai Nguyen Hinh Anh (Required Sprite Assets)

Tai lieu nay liet ke toan bo cac hinh anh (Sprite) can co trong du an **Cozy Life Sim**, phan chia ro rang giua cac Tai nguyen He thong, Tai nguyen Nong san & Sticker mac dinh (su dung tu `CuteKawaiiGUIPack` lam placeholder/fallback), va cac Tai nguyen Chu de **Hoai Niem Viet Nam (Heritage)** co day du Prompt tao anh va ten tieng Anh cho tung asset.

---

## Quy Chuan Tao Anh (Sprite Art Standard)
De dam bao toan bo asset co cung phong cach, chat lieu net vẽ va do bao hoa, moi prompt deu phai bao gom cac tu khoa bat buoc sau:
`"2D cute cartoon game asset of [item], cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format."`

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
Moi cay trong can **4 trang thai hinh anh rieng biet**: Hat giong (Seed), Cay con (Sprout), Cay chin (Mature), va Cay khi thu hoach (Harvest). Trong do `Harvest` mac dinh dung chung anh voi `Mature` hoac co the dung sprite rieng.

### Cay Trong Hien Tai (Default / Placeholders)
*   **White Acorn (ID: 1)**
    *   *Seed:* `Icons/Plants/Acorn-256.png`
    *   *Sprout:* `Icons/Plants/Sapling-256.png`
    *   *Mature:* `Icons/Flowers/Flower-Tulip-Red-256.png`
    *   *Harvest:* `Icons/Flowers/Flower-Tulip-Red-256.png`

### Cay Trong Hoai Niem Viet Nam (Vietnamese Heritage Crops - Cay Moi)
Tat ca sprite nong san se duoc luu tai: `Assets/CozyLifeSim/Textures/Heritage/`

*   **Cay Mia Ngot (ID: 2)**
    *   **<a id="Crop_Sugarcane_Seed"></a>Seed Asset Name**: `Crop_Sugarcane_Seed.png`
        *   *Prompt:* `2D cute cartoon game asset of sugarcane seed cuttings, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **<a id="Crop_Sugarcane_Sprout"></a>Sprout Asset Name**: `Crop_Sugarcane_Sprout.png`
        *   *Prompt:* `2D cute cartoon game asset of a young sugarcane sprout, green shoots, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **<a id="Crop_Sugarcane_Mature"></a>Mature Asset Name**: `Crop_Sugarcane_Mature.png`
        *   *Prompt:* `2D cute cartoon game asset of a tall mature sugarcane bundle, lush green leaves, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **Harvest Asset Name**: `Crop_Sugarcane_Mature.png` *(Dung chung voi Mature)*

*   **Lua Nuoc (ID: 3)**
    *   **<a id="Crop_Rice_Seed"></a>Seed Asset Name**: `Crop_Rice_Seed.png`
        *   *Prompt:* `2D cute cartoon game asset of ripe golden rice grains, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **<a id="Crop_Rice_Sprout"></a>Sprout Asset Name**: `Crop_Rice_Sprout.png`
        *   *Prompt:* `2D cute cartoon game asset of young green rice seedlings, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **<a id="Crop_Rice_Mature"></a>Mature Asset Name**: `Crop_Rice_Mature.png`
        *   *Prompt:* `2D cute cartoon game asset of golden ripe rice paddy stalks heavy with grains, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **Harvest Asset Name**: `Crop_Rice_Mature.png` *(Dung chung voi Mature)*

*   **Hoa Sen (ID: 4)**
    *   **<a id="Crop_Lotus_Seed"></a>Seed Asset Name**: `Crop_Lotus_Seed.png`
        *   *Prompt:* `2D cute cartoon game asset of dried lotus seeds in a pod, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **<a id="Crop_Lotus_Sprout"></a>Sprout Asset Name**: `Crop_Lotus_Sprout.png`
        *   *Prompt:* `2D cute cartoon game asset of a small green lotus leaf floating on water, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **<a id="Crop_Lotus_Mature"></a>Mature Asset Name**: `Crop_Lotus_Mature.png`
        *   *Prompt:* `2D cute cartoon game asset of a beautiful blooming pink lotus flower, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
    *   **Harvest Asset Name**: `Crop_Lotus_Mature.png` *(Dung chung voi Mature)*

---

## 3. Tai Nguyen Dong Vat (Animal Sprites)
Moi loai thu nuoi can 1 hinh anh goc (Sprite) hien thi tren pen. Heart feedback van dung sprite generic mat dinh de don gian hoa art asset.

### Dong Vat Hien Tai (Default / Placeholders)
*   **Breathing Chicken (ID: 1)**
    *   *Chicken Sprite:* `Icons/Animals/Chicken-White-256.png`
    *   *Feedback Heart:* `Icons/Hearts/Heart-Red-256.png`

### Dong Vat Hoai Niem Viet Nam (Vietnamese Heritage Animals - Vat Nuoi Moi)
Moi loai se duoc luu tai: `Assets/CozyLifeSim/Textures/Heritage/`

*   **Meo Tam The (ID: 2)**
    *   **<a id="Animal_CalicoCat"></a>Asset Name**: `Animal_CalicoCat.png`
        *   *Prompt:* `2D cute cartoon game asset of a calico cat sitting cozy, orange black and white fur, cute round eyes, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
*   **Trau Nuoc (ID: 3)**
    *   **<a id="Animal_WaterBuffalo"></a>Asset Name**: `Animal_WaterBuffalo.png`
        *   *Prompt:* `2D cute cartoon game asset of a friendly water buffalo, dark gray skin, curved horns, big warm eyes, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`

---

## 4. Tai Nguyen Nhan Dan (Sticker Sprites)
Moi sticker can **1 anh goc co mau** thuc te. Anh shadow sprite (bong do) se duoc tu dong suy ra tu kenh alpha cua anh mau tai runtime/bootstrap hoac dung chung anh goc voi do mo xam de giu gin su tinh gon cho bo art.

### Stickers Hien Tai (Default / Placeholders)
*   **Bunny Pink (ID: 1):** `Icons/Animals/Bunny-Pink-256.png`
*   **Bear (ID: 2):** `Icons/Animals/Bear-256.png`
*   **Chicken White (ID: 3):** `Icons/Animals/Chicken-White-256.png`

### Stickers Hoai Niem Viet Nam (Vietnamese Heritage Stickers - Sticker Moi)
Moi sticker se duoc luu tai: `Assets/CozyLifeSim/Textures/Heritage/`

*   **Xe Banh Mi (ID: 4)**
    *   **<a id="Sticker_BanhMiCart"></a>Asset Name**: `Sticker_BanhMiCart.png`
        *   *Prompt:* `2D cute cartoon game asset of a traditional Vietnamese Banh Mi cart, glass display with warm crispy bread inside, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
*   **Ly Nuoc Mia (ID: 5)**
    *   **<a id="Sticker_SugarcaneJuice"></a>Asset Name**: `Sticker_SugarcaneJuice.png`
        *   *Prompt:* `2D cute cartoon game asset of a green sugarcane juice glass with a straw and lemon slice, ice cubes, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
*   **Chiec Non La (ID: 6)**
    *   **<a id="Sticker_ConicalHat"></a>Asset Name**: `Sticker_ConicalHat.png`
        *   *Prompt:* `2D cute cartoon game asset of a traditional conical straw hat, dry palm leaf texture, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
*   **Chiec Xich Lo (ID: 7)**
    *   **<a id="Sticker_Cyclo"></a>Asset Name**: `Sticker_Cyclo.png`
        *   *Prompt:* `2D cute cartoon game asset of a classic Vietnamese Cyclo, black metal frame, red seat cushion, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
*   **Long Den Trung Thu (ID: 8)**
    *   **<a id="Sticker_StarLantern"></a>Asset Name**: `Sticker_StarLantern.png`
        *   *Prompt:* `2D cute cartoon game asset of a traditional Vietnamese star-shaped red and yellow paper lantern, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`

---

## 5. Cac Bieu Tuong Tab Shop (Shop Tab Icons)
Dung de phan chia danh muc san pham trong Tiem Tap Hoa (Shop).

*   **Tab Hat Giong (Seeds):** Icon tui hat giong (Placeholder: `Icons/Farming/Seed-Bag-256.png`).
*   **Tab Nhan Dan (Stickers):** Icon album/sticker (Placeholder: `Icons/Tools/Paintbrush-256.png`).
*   **Tab Nong San (Crops):** Icon gio dung hoa qua/nong san (Placeholder: `Icons/Farming/Basket-Harvest-256.png`).

---

## 6. Chan Dung Hoi Thoai NPC (NPC Dialogue Portraits)
Luu tru chan dung cac nhan vat truyen tai cau chuyen co tich hoai niem trong game tai: `Assets/CozyLifeSim/Textures/Heritage/`

*   **Chan Dung Ba Ngoai (NPC Grandma)**
    *   **<a id="NPC_Portrait_Grandma"></a>Asset Name**: `NPC_Portrait_Grandma.png`
        *   *Prompt:* `2D cute cartoon game portrait of a cozy old Vietnamese grandma, grey hair in a bun, smiling warmly, wearing a traditional brown Ao Ba Ba, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`
*   **Chan Dung Co Ba (NPC Shop Vendor)**
    *   **<a id="NPC_Portrait_CoBa"></a>Asset Name**: `NPC_Portrait_CoBa.png`
        *   *Prompt:* `2D cute cartoon game portrait of a friendly young Vietnamese woman, long black hair, wearing a traditional colorful Ao Dai or Ao Ba Ba, smiling warmly, cozy aesthetic, white border, soft pastel colors, no shadows, transparent background, stylized vector art, isolated, mobile game icon format`

---

## 7. Tai Nguyen Trang Trí So Tay (Scrapbook Customization Assets)
Luu tru cac chat lieu giay nen va card ghi chu de nguoi choi tu do trang tri tai: `Assets/CozyLifeSim/Textures/Heritage/`

*   **Giay Nen Co Dien (Old Paper Background)**
    *   **<a id="Scrapbook_Bg_OldPaper"></a>Asset Name**: `Scrapbook_Bg_OldPaper.png`
        *   *Prompt:* `2D cute cartoon game asset of a seamless vintage old yellowish blank paper texture, cozy aesthetic, soft pastel colors, no shadows, mobile game background format`
*   **Giay Pastel Hong (Pastel Pink Background)**
    *   **<a id="Scrapbook_Bg_PastelPink"></a>Asset Name**: `Scrapbook_Bg_PastelPink.png`
        *   *Prompt:* `2D cute cartoon game asset of a seamless cozy pastel pink blank paper texture, cozy aesthetic, soft pastel colors, no shadows, mobile game background format`
*   **Giay O Ly Tap Voi (Grid Paper Background)**
    *   **<a id="Scrapbook_Bg_GridPaper"></a>Asset Name**: `Scrapbook_Bg_GridPaper.png`
        *   *Prompt:* `2D cute cartoon game asset of a seamless cozy school grid paper texture with light blue lines, cozy aesthetic, soft pastel colors, no shadows, mobile game background format`
*   **Giay Ghi Chu Dan (Yellow Sticky Note Card)**
    *   **<a id="Scrapbook_StickyNote_Yellow"></a>Asset Name**: `Scrapbook_StickyNote_Yellow.png`
        *   *Prompt:* `2D cute cartoon game asset of a yellow square sticky note paper, cozy aesthetic, white border, soft pastel colors, subtle shadow, transparent background, isolated, mobile game icon format`

