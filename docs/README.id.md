[English](../README.md) | **Bahasa Indonesia** | [ไทย](README.th.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Deutsch](README.de.md)

# Lost and Found Co. - Mod Co-op Split-Screen

Mod co-op split-screen lokal berbasis BepInEx 5 untuk game **Lost and Found Co.** (Windows x64) yang mendukung 2 hingga 4 pemain dalam satu layar, lengkap dengan kamera independen, kursor virtual, isolasi input perangkat, layar penuh otomatis saat cutscene, dan antarmuka mod multi-bahasa.

## Fitur Utama

- **2 hingga 4 Pemain Lokal**:
  - **2 Pemain**: Tampilan Vertikal (Kiri/Kanan) atau Horizontal (Atas/Bawah), bisa diganti kapan saja dengan `F11`.
  - **3 Pemain**: Grid 2x2 dengan panel tunggu slot P4 yang siap bergabung.
  - **4 Pemain**: Grid 2x2 penuh untuk empat pemain.
- **Isolasi Input & Campuran Perangkat**:
  - Pemain 1 secara default memakai **Mouse + Keyboard** sementara gamepad yang terpasang otomatis mengendalikan P2-P4, atau tekan `F7` agar Gamepad 1 mengendalikan P1.
  - Tekan `Start` di gamepad mana saja untuk langsung bergabung (hot-join), atau tekan `F6` untuk menguji P2 memakai keyboard (`IJKL`, tombol panah, `U`/`O`, `Enter`).
- **Kamera & Interaksi Raycast Independen**:
  - Setiap pemain memiliki `WorldCamera` terpisah dengan geser kamera, zoom, raycast klik/drag `Physics2D`, animasi riak klik, dan akses HUD (tahan `Y` atau `L3` agar kursor bebas menjangkau HUD dan Daftar Tugas).
- **Layar Penuh Otomatis saat Cutscene (`AutoFullscreenOnCutscene`)**:
  - Beralih sementara ke tampilan layar penuh tunggal saat timeline cutscene, momen foto, kunci kamera dialog, perpindahan lantai, atau animasi kamera berlangsung, lalu otomatis kembali ke split-screen setelah selesai.
- **Dukungan Pencahayaan HDR Bloom & SmartLighting2D**:
  - Menjaga efek HDR Bloom URP (`RGB111110Float`) dan menyinkronkan peta cahaya `FunkyCode` SmartLighting2D di semua kamera pemain.
- **UI Mod Multi-Bahasa Otomatis**:
  - Otomatis mengikuti bahasa aktif game (`en`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`), **Bahasa Indonesia (`id`)** jika Mod Bahasa Indonesia terpasang, serta cadangan untuk 8 slot I2 tambahan (`fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`).
  - Memilih font sistem Windows secara dinamis agar aksara Thailand, Tionghoa, Jepang, Korea, Cyrillic, dan Vietnam tampil rapi tanpa kotak kosong.

## Kompatibilitas dengan Mod Bahasa Indonesia

Mod ini dirancang untuk berjalan berdampingan dengan **Mod Bahasa Indonesia Lost and Found Co.** (`LostAndFound_Indonesian.dll`, GUID `com.teteh.lostandfoundco.indonesian`):

- **UI Otomatis Berbahasa Indonesia**: Saat Mod Bahasa Indonesia aktif di dalam game, panel kontrol `F10`, label sudut pemain, kartu tunggu P4, dan toast notifikasi otomatis tampil dalam Bahasa Indonesia dengan istilah yang konsisten (seperti `Daftar Tugas` untuk To-Do List).
- **Bebas Konflik Tombol Pintas**: Pergantian tata letak 2 pemain hanya memakai `F11`, sehingga tombol `F9` tetap sepenuhnya bebas untuk fitur hot-reload kamus milik Mod Bahasa Indonesia.
- **Installer Aman Berdampingan**: Kedua mod memakai BepInEx 5 tanpa saling mengganggu. Skrip `Install_SplitScreenCoop.bat` maupun `Uninstall_SplitScreenCoop.bat` tidak pernah menimpa atau menghapus `LostAndFound_Indonesian.dll`, `runtime_id.json`, maupun file loader proxy yang sudah terpasang.

---

## Cara Pemasangan

Pilih metode instalasi yang sesuai dengan kebutuhan Anda.

### Metode 1: Auto-Install ZIP (Direkomendasikan)

1. Unduh **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip`** dari halaman [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases).
2. Ekstrak seluruh isi file ZIP langsung ke folder utama game **Lost and Found Co.** (di sebelah `Lost and Found Co.exe`).
   - Lokasi default Steam: `C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co`
3. Klik dua kali **`Install_SplitScreenCoop.bat`** (bahasa antarmuka skrip: Bahasa Inggris).
   - Jika BepInEx 5 belum terpasang, installer otomatis memilih slot loader proxy kosong (`winhttp.dll`, `version.dll`, atau `winmm.dll`) tanpa menimpa file yang ada, lalu memasang BepInEx 5.4.23.5 + mod ini.
   - Jika BepInEx 5 atau Mod Bahasa Indonesia sudah terpasang sebelumnya, installer membiarkan loader lama tetap utuh dan hanya menambahkan `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll`.
4. Jalankan **Lost and Found Co.** lewat Steam seperti biasa.

Untuk menghapus mod, klik dua kali **`Uninstall_SplitScreenCoop.bat`** di folder game. Uninstaller memeriksa hash SHA-256 sebelum menghapus file, dan menjaga BepInEx tetap terpasang apabila masih ada plugin lain di folder `BepInEx\plugins\`.

### Metode 2: Drag-and-Drop DLL Manual

Gunakan cara ini jika **BepInEx 5.4.x (x64)** sudah terpasang di folder game Anda:

1. Unduh **`LostAndFound_SplitScreenCoop.dll`** dari halaman [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases).
2. Salin `LostAndFound_SplitScreenCoop.dll` ke:
   ```text
   <Folder Game>\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll
   ```
3. Jalankan game lewat Steam.

### Metode 3: Build dari Source Code

Kompilasi DLL sendiri dari folder `src/` menggunakan instalasi Steam lokal Anda:

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -Deploy
```

Atau langsung dengan `.NET SDK 8.0+`:

```powershell
dotnet build .\src\LostAndFound_SplitScreenCoop.csproj -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Lost and Found Co"
```

Lihat [docs/BUILDING.md](BUILDING.md) untuk prasyarat lengkap dan pemecahan masalah build.

---

## Kontrol & Tombol Pintas

### Pintasan Keyboard

| Tombol | Fungsi |
| --- | --- |
| `F8` | Mengaktifkan / mematikan Split-Screen Co-op |
| `F11` | Mengganti tata letak 2 Pemain (Kiri/Kanan atau Atas/Bawah) |
| `F7` | Mengganti input P1 antara Mouse + Keyboard dan Gamepad 1 |
| `F10` | Membuka / menutup Panel Kontrol Co-op |
| `F6` | Mode Uji P2 Keyboard (`IJKL` kursor, Panah geser kamera, `U`/`O` zoom, `Enter` klik) |

### Kontrol Gamepad (P2-P4, dan P1 jika mode Gamepad 1 aktif)

| Input | Fungsi |
| --- | --- |
| `Left Stick` | Menggerakkan kursor pemain |
| `Right Stick` | Menggeser kamera |
| `LT` / `LB` dan `RT` / `RB` | Zoom keluar / Zoom masuk ke arah kursor |
| `Tombol A` | Klik / Seret benda |
| Tahan `Y` atau `L3` | Membuka kunci kursor ke seluruh layar untuk menekan HUD / Daftar Tugas |
| Tekan `R3` | Reset posisi kamera ke kamera P1 |
| `Start` | Bergabung langsung (hot-join) ke sesi co-op |

---

## Konfigurasi

Setelah game dijalankan satu kali, BepInEx akan membuat file konfigurasi:

```text
<Folder Game>\BepInEx\config\com.teteh.lostandfound.splitscreencoop.cfg
```

| Bagian | Kunci | Default | Keterangan |
| --- | --- | --- | --- |
| `Coop` | `AutoEnableMultiGamepad` | `true` | Aktifkan split-screen otomatis saat 2+ gamepad terhubung |
| `Coop` | `AutoFullscreenOnCutscene` | `true` | Beralih sementara ke layar penuh saat cutscene atau fokus kamera |
| `Coop` | `Player1UseGamepad` | `false` | `false`: P1 Mouse + Keyboard; `true`: P1 Gamepad 1 |
| `Coop` | `TwoPlayerLayout` | `VerticalLeftRight` | Layout bawaan 2 pemain (`VerticalLeftRight` / `HorizontalTopBottom`) |
| `UI` | `UiLanguage` | `Auto` | `Auto` mengikuti bahasa game, atau isi kode `en`, `id`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`, dll. |
| `Controls` | `GamepadCursorSpeed` | `780` | Kecepatan gerak kursor stik kiri |
| `Controls` | `GamepadCameraPanSpeed` | `1.75` | Pengali kecepatan geser kamera stik kanan |
| `Controls` | `GamepadZoomSpeed` | `18` | Kecepatan zoom trigger/bumper P2-P4 |
| `Visual` | `CursorScale` | `1.0` | Skala ukuran kursor P2-P4 |
| `Performance` | `OptimizeMainCameraCull` | `true` | Mematikan render ganda pada `Camera.main` saat split-screen aktif |

---

## Tanya Jawab (FAQ)

- **Gamepad terpasang tetapi kursor tidak bergerak**: Periksa pengaturan controller di Windows (`joy.cpl`) atau Steam Input, lalu tekan tombol `Start` di gamepad saat berada di dalam game. Buka panel `F10` untuk melihat status tiap slot.
- **Hanya punya 1 gamepad, apakah tetap bisa bermain berdua?**: Bisa. Gunakan Mouse + Keyboard untuk P1 dan biarkan Gamepad 1 menjadi P2 (`F7` tetap di mode Mouse + KB), atau tekan `F6` untuk mencoba dengan 1 keyboard.
- **Apakah mod ini aman untuk akun Steam dan file save?**: Aman. *Lost and Found Co.* adalah game petualangan puzzle lokal tanpa sistem anti-cheat, dan mod ini berjalan murni di memori tanpa mengubah file `data.unity3d` maupun data save game.

---

## Lisensi & Atribusi

- Kode sumber mod, skrip build, dan skrip installer dirilis di bawah [Lisensi MIT](../LICENSE) (Copyright (c) 2026 AkmalHidayat).
- Paket Auto-Install ZIP menyertakan **BepInEx 5**, **HarmonyX**, dan **Unity Doorstop** di bawah lisensi open-source masing-masing. Lihat [docs/THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- Kontribusi terbuka untuk perbaikan maupun penambahan terjemahan: lihat [CONTRIBUTING.md](../CONTRIBUTING.md) dan [CHANGELOG.md](../CHANGELOG.md).

> **Penafian**: Ini adalah mod komunitas tidak resmi dan tidak berafiliasi dengan atau didukung oleh **Bit Egg Inc.** selaku pengembang *Lost and Found Co.*
