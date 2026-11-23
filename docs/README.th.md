[English](../README.md) | [Bahasa Indonesia](README.id.md) | **ไทย** | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Deutsch](README.de.md)

# Lost and Found Co. - ม็อดเล่นหลายคนแบบแบ่งหน้าจอ (Split-Screen Co-op)

ม็อดสำหรับ BepInEx 5 เพื่อให้เกม **Lost and Found Co.** (Windows x64) เล่นพร้อมกันในเครื่องเดียวได้ 2 ถึง 4 คนแบบแบ่งหน้าจอ พร้อมกล้องอิสระ เคอร์เซอร์แยกตามผู้เล่น การแยกอุปกรณ์ควบคุม สลับเต็มจออัตโนมัติระหว่างคัตซีน และรองรับหน้าต่าง UI หลายภาษา

> **เวอร์ชันเกมที่ทดสอบแล้ว**: **Lost and Found Co. Patch 1.2.2** (27 ต.ค. 2026, Steam Build `26480281`, Unity `6000.0.84f1`, Windows x64) โดยทั่วไปแพตช์ย่อยยังคงใช้ร่วมกันได้ แต่หากมีด่านหรือข้อความใหม่หลัง Patch 1.2.2 ม็อดภาษาอินโดนีเซียของเราจะต้องรออัปเดตพจนานุกรมเพิ่มเติมจึงจะแปลข้อความใหม่ได้ครบ

## ความสามารถหลัก

- **เล่นพร้อมกัน 2 ถึง 4 คน**:
  - **2 คน**: แบ่งแนวตั้ง (ซ้าย/ขวา) หรือแนวนอน (บน/ล่าง) กด `F11` เพื่อสลับได้ตลอดเวลา
  - **3 คน**: ตาราง 2x2 พร้อมช่องสแตนด์บายสำหรับ P4
  - **4 คน**: ตาราง 2x2 เต็มทั้ง 4 ช่อง
- **แยกอุปกรณ์ควบคุมอิสระ**:
  - ค่าเริ่มต้น P1 ใช้ **เมาส์ + คีย์บอร์ด** ส่วนจอยเกมที่เชื่อมต่อจะคุม P2-P4 หรือกด `F7` เพื่อสลับให้จอย 1 คุม P1
  - กดปุ่ม `Start` บนจอยเกมเพื่อเข้าร่วมทันที หรือกด `F6` เพื่อทดสอบ P2 ด้วยคีย์บอร์ด (`IJKL`, ปุ่มลูกศร, `U`/`O`, `Enter`)
- **กล้องและ Raycast อิสระต่อผู้เล่น**:
  - แต่ละช่องมี `WorldCamera` ของตัวเองสำหรับเลื่อนกล้อง ซูม และคลิก/ลากวัตถุ 2D แยกกัน (กด `Y` หรือ `L3` ค้างเพื่อปลดล็อกเคอร์เซอร์ไปกดปุ่ม HUD และรายการภารกิจได้ทั่วจอ)
- **สลับเต็มจออัตโนมัติในคัตซีน (`AutoFullscreenOnCutscene`)**:
  - คืนหน้าจอเป็นแบบเต็มจอชั่วคราวเมื่อมีคัตซีน เหตุการณ์ถ่ายภาพ ล็อกกล้องบทสนทนา หรือเปลี่ยนชั้น แล้วกลับมาแบ่งหน้าจอให้อัตโนมัติหลังจบ
- **รองรับ URP HDR Bloom และแสง 2D (`SmartLighting2D`)**:
  - ใช้ `RGB111110Float` เพื่อรักษาแสง HDR และซิงก์แผนที่แสงของ `FunkyCode` ให้ครบทุกกล้องผู้เล่น
- **UI ของม็อดเปลี่ยนภาษาอัตโนมัติตามเกม**:
  - รองรับภาษาทางการของเกม (`en`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`), **Bahasa Indonesia (`id`)** เมื่อติดตั้งม็อดภาษาอินโดนีเซีย และภาษาเพิ่มเติมในระบบ I2 ของเกม (`fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`) พร้อมเลือกฟอนต์ระบบของ Windows (`Leelawadee UI` / CJK) อัตโนมัติ

## ม็อดอื่นของเราและการใช้งานร่วมกัน

นอกจากนี้เรายังพัฒนาม็อดแปลภาษาเสริม **[ม็อดภาษาอินโดนีเซีย Lost and Found Co.](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)** (`LostAndFound_Indonesian.dll` / `com.teteh.lostandfoundco.indonesian`, อัปเดตคำแปล 16,718 รายการถึง **Patch 1.2.2** หากมีด่านใหม่หลังแพตช์ 1.2.2 จะต้องรออัปเดตพจนานุกรมเพิ่มเติม) ซึ่งทำงานร่วมกันได้โดยไม่ทับซ้อนกัน:

- เมื่อเกมเปิดใช้ภาษาอินโดนีเซีย แผงควบคุม `F10` ป้ายมุมจอ และข้อความแจ้งเตือนของม็อดจะสลับเป็นภาษาอินโดนีเซียให้อัตโนมัติ
- ใช้ปุ่ม `F11` แทน `F9` สำหรับสลับรูปแบบหน้าจอ 2 คน เพื่อเว้น `F9` ไว้สำหรับโหลดพจนานุกรมใหม่ของม็อดแปลภาษา
- ตัวติดตั้ง `Install_SplitScreenCoop.bat` และตัวถอนติดตั้ง `Uninstall_SplitScreenCoop.bat` จะไม่เขียนทับหรือลบไฟล์ของม็อดอื่นที่ติดตั้งอยู่ก่อนแล้ว

---

## วิธีติดตั้ง

### วิธีที่ 1: ติดตั้งอัตโนมัติด้วยไฟล์ ZIP (แนะนำ)

1. ดาวน์โหลด **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip`** จากหน้า [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases)
2. แตกไฟล์ทั้งหมดลงในโฟลเดอร์หลักของเกม **Lost and Found Co.** (ข้างไฟล์ `Lost and Found Co.exe`)
   - ตำแหน่งมาตรฐานของ Steam: `C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co`
3. ดับเบิลคลิก **`Install_SplitScreenCoop.bat`** (ข้อความในตัวติดตั้งเป็นภาษาอังกฤษ)
   - หากยังไม่มี BepInEx 5 ตัวติดตั้งจะใช้ Unity Doorstop (`winhttp.dll`) หากว่างอยู่ และติดตั้ง BepInEx 5.4.23.5 พร้อมม็อดให้ทันทีโดยไม่เขียนทับไฟล์เดิม
   - หากมี BepInEx 5 อยู่แล้ว ตัวติดตั้งจะไม่แก้ไฟล์ loader เดิม และคัดลอกเฉพาะ `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll`
4. เปิดเกม **Lost and Found Co.** ผ่าน Steam ตามปกติ

หากต้องการถอนติดตั้ง ให้ดับเบิลคลิก **`Uninstall_SplitScreenCoop.bat`** ระบบจะตรวจแฮช SHA-256 ก่อนลบไฟล์ และคง BepInEx ไว้หากยังมีปลั๊กอินอื่นใช้งานอยู่

### วิธีที่ 2: ไฟล์ ZIP แบบลากวางด้วยตนเอง (Manual Drag-and-Drop)

1. ดาวน์โหลด **`LostAndFound-SplitScreenCoop-v1.0.4-Manual.zip`** จากหน้า [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases) แล้วแตกไฟล์ไว้ในโฟลเดอร์ชั่วคราว
2. เลือกวางไฟล์ตามสถานะโฟลเดอร์เกมของคุณ:
   - **โฟลเดอร์เกมใหม่ (ยังไม่มี BepInEx)**: ลากวาง `winhttp.dll`, `doorstop_config.ini` และโฟลเดอร์ `BepInEx` ลงในโฟลเดอร์หลักของเกม (ข้างไฟล์ `Lost and Found Co.exe`)
   - **มี BepInEx 5 หรือม็อดภาษาอินโดนีเซียอยู่แล้ว**: ลากวางเฉพาะ `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll` ลงใน `<โฟลเดอร์เกม>\BepInEx\plugins\` (ห้ามเขียนทับ `winhttp.dll` หรือ `doorstop_config.ini` เดิม)
3. เปิดเกมผ่าน Steam

### วิธีที่ 3: คอมไพล์จากซอร์สโค้ด

ดูขั้นตอนละเอียดได้ที่ [docs/BUILDING.md](BUILDING.md)

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -Deploy
```

---

## ปุ่มลัดและการควบคุม

| ปุ่ม | คำสั่ง |
| --- | --- |
| `F8` | เปิด / ปิดโหมดแบ่งหน้าจอ Co-op |
| `F11` | สลับรูปแบบแบ่งหน้าจอ 2 คน (ซ้าย/ขวา หรือ บน/ล่าง) |
| `F7` | สลับการควบคุม P1 ระหว่าง เมาส์+คีย์บอร์ด และ จอยเกม 1 |
| `F10` | เปิด / ปิดแผงควบคุม Co-op |
| `F6` | เปิด / ปิดโหมดทดสอบคีย์บอร์ดสำหรับ P2 (`IJKL`, ลูกศร, `U`/`O`, `Enter`) |

การควบคุมจอยเกม (P2-P4 และ P1 เมื่อเปิดโหมดจอยเกม): `Left Stick` เลื่อนเคอร์เซอร์, `Right Stick` เลื่อนกล้อง, `LT`/`LB` และ `RT`/`RB` ซูมเข้า/ออก, `A` คลิก/ลาก, กด `Y` หรือ `L3` ค้างเพื่อเข้าถึง HUD/รายการภารกิจ, กด `R3` รีเซ็ตกล้อง, กด `Start` เพื่อเข้าร่วมเกม

---

## การตั้งค่าและลิขสิทธิ์

- ไฟล์ตั้งค่าอยู่ที่ `<โฟลเดอร์เกม>\BepInEx\config\com.teteh.lostandfound.splitscreencoop.cfg` (สามารถปรับ `UiLanguage` จาก `Auto` เป็นรหัสภาษาที่ต้องการได้)
- โค้ดม็อดเผยแพร่ภายใต้ [MIT License](../LICENSE) (Copyright (c) 2026 AkmalHidayat) ส่วนไลบรารี BepInEx 5 / HarmonyX / Doorstop อยู่ใน [docs/THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)

> **ข้อจำกัดความรับผิดชอบ**: ม็อดนี้จัดทำโดยชุมชนผู้เล่น ไม่ได้เกี่ยวข้องหรือได้รับการรับรองจาก **Bit Egg Inc.** ผู้พัฒนาเกม *Lost and Found Co.*
