# Quy Hoach Thiet Ke Phase 2: Cozy Polish & Heritage Progression

> **Tai lieu Quy hoach Thiet ke (Technical Design Plan) - Review**
> **Du an:** Cozy Life Sim
> **Ngay thiet ke:** 2026-05-29
> **Trang thai:** Dang danh gia (Draft / Review)
> **Ngon ngu:** Tieng Viet khong dau (Triet tieu hoan toan mojibake)

---

## I. Phan Ra Va Output Files (Plan Split Details)

De dam bao tinh de dang tich hop, toan bo Phase 2 duoc chia nho thanh 4 Ke hoach thuc thi doc lap (Implementation Plans):

| Phase | Output Plan File | Task ID Prefix | Muc Tieu & Acceptance Criteria (Tieu Chi Nghiem Thu) |
| :--- | :--- | :--- | :--- |
| **Phase 2.1** | `2026-05-29-ui-juice-polish.md` | `P2.1` | **Muc tieu:** Phan tab Shop, Sticker Tray cuon ngang, DOTween bay xu.<br>**Acceptance:** Shop chuyen tab muot ma; Tray cuon ngang hien thi Sticker. |
| **Phase 2.2** | `2026-05-29-heritage-content-fallback.md` | `P2.2` | **Muc tieu:** Bo sung data hoai niem Viet Nam that; Flat Fallback ve bang code.<br>**Acceptance:** Tu dong ve Flat color + Shadow + Outline khi thieu Art. |
| **Phase 2.3** | `2026-05-29-progression-level-unlock.md` | `P2.3` | **Muc tieu:** Cap do & XP (IProgressionService), Level Lock trong Shop, XP tu Quest.<br>**Acceptance:** Tang XP tu Quest thang cap -> Mo khoa vat pham Shop. |
| **Phase 2.4** | `2026-05-29-settings-audio.md` | `P2.4` | **Muc tieu:** Settings Popup (Audio Toggles, Player Profile), AudioService.<br>**Acceptance:** Toggle luu PlayerPrefs; Khong co nut Reset Progress. |

---

## II. Giai Quyet Cac Bai Toan Ky Thuat Cot Loi (Technical Architecture)

### 2.1. Di Dan Du Lieu Cu (Save Migration Rule) - [P1]
*   **Vi tri xu ly**: Trong ham `NormalizeSaveData()` cua `SaveService.cs`.
*   **Quy tac di dan**:
    *   Khoi tao list `StickerOwned` moi neu chua co trong file save cu.
    *   **Default Stickers (ID 1, 2)**: Tu dong cap cho nguoi choi so luong lon: `Count = 99` de thoai mai dan nhieu lan tren cac trang sach.
    *   **Sticker Dac Biet da unlock (ID 3)**: Neu list cu `UnlockedStickerIds` co chua ID 3, he thong se migrate sang `StickerOwned` moi voi `Count = 1`.
    *   **PlacedStickers hien co**: Cac sticker da dan tren sach truoc do **van duoc giu nguyen ven** va hien thi dung vi tri. So luong tren khay Sticker Tray chi hien thi so luong "chua dan" kha dung con lai trong kho de tranh bi am so luong.

### 2.2. Xung Dot Sticker API Moi (Sticker Consumable API) - [P1]
*   **Shop Service**:
    *   Doi `IShopService.TryBuySticker(int stickerId)`: Khong con unlock mot lan duy nhat. Cho phep mua lap lai nhieu lan! Moi lan mua dung se tru Coins va tang `Count` trong `StickerOwned` len 1.
    *   Thay the `IsStickerUnlocked(int stickerId)` bang `GetStickerCount(int stickerId) > 0` hoac `IsStickerAvailable(int stickerId)`.
*   **Sticker Tray UI**:
    *   StickerBook tray hien thi sticker theo danh sach cac sticker co `Count > 0`.
    *   Hien thi text so luong (vi du: `x3`, `x5`) ngay tren card sticker.

### 2.3. Quy Trinh Thu Hoi Sticker (Remove/Return Workflow) - [P2]
*   **UX Tương tac**:
    *   Khi nguoi choi nhap vao sticker da dan tren trang sach -> Kich hoat che do chinh sua (Sticker lac lu nhe).
    *   Xuat hien mot nut **"Thu hoi" (Return to Tray)** hinh thung rac nho hoac icon mui ten hoan tra o goc tren.
    *   Khi click nut "Thu hoi":
        1.  Goi `StickerBookPresenter.ReturnSticker(stickerInstance)`. Presenter goi `IMemoryService.RemovePlacedSticker(pageIndex, stickerPlacedData)` de xoa khoi danh sach da dan trong save.
        2.  Goi `IInventoryService.AddStickerCount(stickerId, 1)` de hoan lai 1 sticker vao kho.
        3.  Dung DOTween thu nho sticker ve `0` trong `0.15s` va Destroy gameObject.
        4.  Phat su kien reload de Sticker Tray tu dong cap nhat so luong moi (+1).

### 2.4. Tach Progression Service (IProgressionService) - [P2]
De giu cho `IInventoryService` luon sach va khong bi lam ban boi XP:
*   **Interface moi**: `IProgressionService.cs`
*   **Class trien khai**: `ProgressionService.cs` (Ke thua `IProgressionService`).
*   **Pham vi quan ly**: Bieu dien `PlayerLevel`, `PlayerXP`, tinh nguong thang cap, va phat su kien `OnLevelChanged` / `OnXPChanged`.
*   **Dang ky DI**: Dang ky Singleton trong `GameLifetimeScope.cs`.

### 2.5. Tinh Luy Dang Cua Phanthuong Quest (Quest Reward Idempotency) - [P2]
*   **Quy tac**: XP va Coins chi duoc trao thuong **dung 1 lan duy nhat** khi Quest hoan thanh.
*   **Co che**: He thong su dung list `CompletedQuestIds` trong `SaveData` de luu tru cac Quest da xong. Khi mot Quest dang chay dat chi tieu, no se duoc chuyen vao list nay, thuc hien cong Coin + XP lap tuc va goi `Save()`. Cac lan nap game tiep theo se chi khoi tao cac Quest chua nam trong list completed, triet tieu vinh vien rui ro lap lai phan thuong.

### 2.6. Vi Tri Cua Flag `ForceFlatUI` - [P3]
*   Flag `ForceFlatUI` se duoc dinh nghia trong ScriptableObject `UIStyleConfig.cs`:
    `public bool ForceFlatUI = false;`
*   Va mot static debug hook trong `CozyProceduralUI.cs`:
    `public static bool ForceFlatUIDebug = false;` de cho phep test nhanh trong editor ma khong can sua asset tren dia.

---

## III. Kich Ban Kiem Thu & Xac Minh Chi Tiet (Verification Checklist)

1.  **Save Migration Test (Kiem tra di dan save cu)**:
    *   Tien hanh ghi de file save cu chi chua `UnlockedStickerIds`. Nap game va verify list `StickerOwned` moi tu dong sinh ra, default sticker 1 va 2 co `Count = 99`, sticker 3 da unlock truoc do co `Count = 1`.
2.  **Sticker Consumable Loop Test (Kiem tra vong lap sticker tieu hao)**:
    *   Mua 2 Sticker ID 3 trong Shop -> Khay Sticker Tray hien thi `x2`.
    *   Keo 1 Sticker dandan len trang sach -> Verify so luong trong khay giam con `x1`.
    *   Keo tiep 1 Sticker dandan tiep -> Verify sticker bien mat khoi khay.
    *   Nhan nut "Thu hoi" 1 sticker tren trang -> Verify sticker bi xoa khoi trang va khay sticker xuat hien lai voi so luong `x1`.
3.  **Progression & Level Lock Test (Kiem tra thang cap va khoa Shop)**:
    *   Hoan thanh Quest -> Verify nhan XP chinh xac, tang level khi vuot nguong.
    *   Kiem tra Shop: Cac san pham khoa cap lap tuc mo khoa khi PlayerLevel dat yeu cau.
4.  **Audio State & Manual Controls Test (Kiem tra am thanh va settings)**:
    *   Thay doi Audio Toggles -> Load lai game -> Verify trang thai duoc khoi phuc dung tu `PlayerPrefs`.
5.  **Procedural Flat Fallback Test (Kiem tra ve Flat khi thieu Art)**:
    *   Bat `ForceFlatUIDebug = true` -> Mo Shop -> Verify visual tu dong ve Flat color + Shadow + Outline tinh te, khong crash.
