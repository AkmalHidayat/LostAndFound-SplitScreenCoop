using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using I2.Loc;

namespace LostAndFoundSplitScreenCoop
{
    internal enum LocKey
    {
        LayoutVertical,
        LayoutHorizontal,
        ToastLayoutChanged,
        InputMouseKb,
        InputGamepad1,
        ToastP1InputChanged,
        ToastP2TestOn,
        ToastP2TestOff,
        ToastCoopReady,
        ToastCoopActive,
        ToastCoopJoined,
        ToastSoloMode,
        ToastOneGamepadDetected,
        StandbyTitle,
        StandbySubtitle,
        PanelTitle,
        PanelClose,
        PanelGuide,
        BadgeMouse,
        BadgeGamepad,
        BadgeKeyTest,
        SlotP1ActiveMkb,
        SlotP1WaitingGamepad,
        SlotActiveDevice,
        SlotActiveKeyTest,
        SlotEmpty,
        BtnModeActive,
        BtnModeCutscene,
        BtnModeReady,
        BtnModeOff,
        BtnLayoutVertical,
        BtnLayoutHorizontal,
        BtnP1Gamepad,
        BtnP1MouseKb,
        BtnP2TestOn,
        BtnP2TestOff
    }

    /// <summary>
    /// Provides in-game UI localization aligned with the game's active language and Indonesian Localization Mod detection.
    /// </summary>
    internal static class SplitScreenLocalization
    {
        public const string IndonesianModGuid = "com.teteh.lostandfoundco.indonesian";

        private static readonly Dictionary<string, Dictionary<LocKey, string>> Tables = BuildTables();

        private static string _currentLanguageCode = "en";
        private static bool? _indonesianModInstalled;

        public static string CurrentLanguageCode => _currentLanguageCode;

        public static bool IsIndonesianModInstalled
        {
            get
            {
                if (_indonesianModInstalled.HasValue)
                {
                    return _indonesianModInstalled.Value;
                }

                try
                {
                    _indonesianModInstalled = Chainloader.PluginInfos != null &&
                                              Chainloader.PluginInfos.ContainsKey(IndonesianModGuid);
                }
                catch (Exception)
                {
                    _indonesianModInstalled = false;
                }

                return _indonesianModInstalled.Value;
            }
        }

        public static bool RefreshCurrentLanguage(string configOverride)
        {
            string resolved = ResolveLanguageCode(configOverride);
            if (!string.Equals(resolved, _currentLanguageCode, StringComparison.Ordinal))
            {
                _currentLanguageCode = resolved;
                return true;
            }
            return false;
        }

        public static string ResolveLanguageCode(string configOverride)
        {
            if (!string.IsNullOrWhiteSpace(configOverride) &&
                !string.Equals(configOverride.Trim(), "auto", StringComparison.OrdinalIgnoreCase))
            {
                string explicitCode = NormalizeLanguageToken(configOverride);
                if (!string.IsNullOrEmpty(explicitCode) && Tables.ContainsKey(explicitCode))
                {
                    return explicitCode;
                }
            }

            try
            {
                string i2Code = NormalizeLanguageToken(LocalizationManager.CurrentLanguageCode);
                if (!string.IsNullOrEmpty(i2Code) && Tables.ContainsKey(i2Code))
                {
                    return i2Code;
                }

                string i2Name = NormalizeLanguageToken(LocalizationManager.CurrentLanguage);
                if (!string.IsNullOrEmpty(i2Name) && Tables.ContainsKey(i2Name))
                {
                    return i2Name;
                }
            }
            catch (Exception)
            {
                // I2 Localization may not be initialized during early plugin Awake.
            }

            return "en";
        }

        public static string Get(LocKey key)
        {
            if (Tables.TryGetValue(_currentLanguageCode, out Dictionary<LocKey, string> table) &&
                table.TryGetValue(key, out string value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (Tables.TryGetValue("en", out Dictionary<LocKey, string> enTable) &&
                enTable.TryGetValue(key, out string enValue))
            {
                return enValue;
            }

            return key.ToString();
        }

        public static string Format(LocKey key, params object[] args)
        {
            string pattern = Get(key);
            if (args == null || args.Length == 0)
            {
                return pattern;
            }

            try
            {
                return string.Format(pattern, args);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        public static string GetFontScriptGroup(string langCode)
        {
            switch (langCode)
            {
                case "th":
                case "zh-CN":
                case "zh-TW":
                case "ja":
                case "ko":
                    return langCode;
                default:
                    return "latin";
            }
        }

        private static string NormalizeLanguageToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            string lower = token.Trim().ToLowerInvariant().Replace('_', '-');

            if (lower == "zh-tw" || lower == "zh-hant" || lower == "zh-hk" || lower == "zh-mo" ||
                lower.Contains("繁體") || lower.Contains("繁体") || lower.Contains("traditional"))
            {
                return "zh-TW";
            }

            if (lower == "zh" || lower == "zh-cn" || lower == "zh-hans" || lower == "zh-sg" ||
                lower.Contains("简体") || lower.Contains("simplified") || lower == "chinese")
            {
                return "zh-CN";
            }

            if (lower == "id" || lower.StartsWith("id-") || lower.Contains("indonesia"))
            {
                return "id";
            }

            if (lower == "th" || lower.StartsWith("th-") || lower.Contains("ไทย") || lower == "thai")
            {
                return "th";
            }

            if (lower == "ja" || lower.StartsWith("ja-") || lower == "jp" || lower.Contains("日本語") || lower == "japanese")
            {
                return "ja";
            }

            if (lower == "ko" || lower.StartsWith("ko-") || lower == "kr" || lower.Contains("한국") || lower == "korean")
            {
                return "ko";
            }

            if (lower == "de" || lower.StartsWith("de-") || lower.Contains("deutsch") || lower == "german")
            {
                return "de";
            }

            if (lower == "fr" || lower.StartsWith("fr-") || lower.Contains("français") || lower.Contains("francais") || lower == "french")
            {
                return "fr";
            }

            if (lower == "es" || lower.StartsWith("es-") || lower.Contains("español") || lower.Contains("espanol") || lower == "spanish")
            {
                return "es-ES";
            }

            if (lower == "it" || lower.StartsWith("it-") || lower.Contains("italiano") || lower == "italian")
            {
                return "it";
            }

            if (lower == "ru" || lower.StartsWith("ru-") || lower.Contains("русский") || lower == "russian")
            {
                return "ru";
            }

            if (lower == "sv" || lower.StartsWith("sv-") || lower.Contains("svenska") || lower == "swedish")
            {
                return "sv-SE";
            }

            if (lower == "tr" || lower.StartsWith("tr-") || lower.Contains("türkçe") || lower.Contains("turkce") || lower == "turkish")
            {
                return "tr";
            }

            if (lower == "pt" || lower.StartsWith("pt-") || lower.Contains("português") || lower.Contains("portugues") || lower == "portuguese")
            {
                return "pt-BR";
            }

            if (lower == "vi" || lower.StartsWith("vi-") || lower.Contains("tiếng việt") || lower == "vietnamese")
            {
                return "vi";
            }

            if (lower == "en" || lower.StartsWith("en-") || lower == "english")
            {
                return "en";
            }

            return string.Empty;
        }

        private static Dictionary<string, Dictionary<LocKey, string>> BuildTables()
        {
            return new Dictionary<string, Dictionary<LocKey, string>>(StringComparer.Ordinal)
            {
                ["en"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertical (Left/Right)",
                    [LocKey.LayoutHorizontal] = "Horizontal (Top/Bottom)",
                    [LocKey.ToastLayoutChanged] = "2P Layout: {0}",
                    [LocKey.InputMouseKb] = "Mouse + Keyboard",
                    [LocKey.InputGamepad1] = "Gamepad 1",
                    [LocKey.ToastP1InputChanged] = "P1 Input: {0}",
                    [LocKey.ToastP2TestOn] = "P2 Keyboard Test ON (IJKL: Cursor | Arrows: Pan | U/O: Zoom | Enter: Click)",
                    [LocKey.ToastP2TestOff] = "P2 Keyboard Test OFF",
                    [LocKey.ToastCoopReady] = "Co-op Ready: Press Start on Gamepad (or F6 for P2 Keyboard Test)",
                    [LocKey.ToastCoopActive] = "Split-Screen {0}-Player Active (F8: Toggle | F11: Layout | F10: Panel)",
                    [LocKey.ToastCoopJoined] = "Split-Screen {0}-Player Active!",
                    [LocKey.ToastSoloMode] = "Solo Mode Active (Press F8 for Split-Screen)",
                    [LocKey.ToastOneGamepadDetected] = "1 Gamepad detected: Press [Start] on Gamepad to join as P2, or F8/F10",
                    [LocKey.StandbyTitle] = "P4 Slot Ready to Join",
                    [LocKey.StandbySubtitle] = "Connect Gamepad 4 and press [Start]",
                    [LocKey.PanelTitle] = "Split-Screen Co-op Panel (F10)",
                    [LocKey.PanelClose] = "Close Panel [F10]",
                    [LocKey.PanelGuide] = "Gamepad: L-Stick Cursor | R-Stick Pan | LT/RT or LB/RB Zoom\nA: Click/Drag | Hold Y or L3: Access HUD/To-Do | R3: Reset Camera",
                    [LocKey.BadgeMouse] = "{0}: Mouse",
                    [LocKey.BadgeGamepad] = "{0}: Gamepad",
                    [LocKey.BadgeKeyTest] = "{0}: Key Test",
                    [LocKey.SlotP1ActiveMkb] = "P1: Active (Mouse + Keyboard)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Waiting for Gamepad 1",
                    [LocKey.SlotActiveDevice] = "{0}: Active ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Active (Keyboard Test IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Empty (Press Start on Gamepad)",
                    [LocKey.BtnModeActive] = "Co-op: ON [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: CUTSCENE [F8]",
                    [LocKey.BtnModeReady] = "Co-op: READY [F8]",
                    [LocKey.BtnModeOff] = "Co-op: OFF [F8]",
                    [LocKey.BtnLayoutVertical] = "2P Layout: Left/Right [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P Layout: Top/Bottom [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Gamepad 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Mouse + KB [F7]",
                    [LocKey.BtnP2TestOn] = "P2 KB Test: ON [F6]",
                    [LocKey.BtnP2TestOff] = "P2 KB Test: OFF [F6]"
                },

                ["id"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertikal (Kiri/Kanan)",
                    [LocKey.LayoutHorizontal] = "Horizontal (Atas/Bawah)",
                    [LocKey.ToastLayoutChanged] = "Tata letak 2P: {0}",
                    [LocKey.InputMouseKb] = "Mouse + Keyboard",
                    [LocKey.InputGamepad1] = "Gamepad 1",
                    [LocKey.ToastP1InputChanged] = "Input P1: {0}",
                    [LocKey.ToastP2TestOn] = "Uji P2 Keyboard AKTIF (IJKL: Kursor | Panah: Geser | U/O: Zoom | Enter: Klik)",
                    [LocKey.ToastP2TestOff] = "Uji P2 Keyboard NONAKTIF",
                    [LocKey.ToastCoopReady] = "Co-op Siap: Tekan Start pada Gamepad (atau F6 untuk Uji P2 Keyboard)",
                    [LocKey.ToastCoopActive] = "Split-Screen {0} Pemain Aktif (F8: Toggle | F11: Layout | F10: Panel)",
                    [LocKey.ToastCoopJoined] = "Split-Screen {0} Pemain Aktif!",
                    [LocKey.ToastSoloMode] = "Mode Solo Aktif (Tekan F8 untuk Split-Screen)",
                    [LocKey.ToastOneGamepadDetected] = "1 Gamepad terdeteksi: Tekan [Start] di Gamepad untuk gabung P2, atau F8/F10",
                    [LocKey.StandbyTitle] = "Slot P4 Siap Bergabung",
                    [LocKey.StandbySubtitle] = "Sambungkan Gamepad ke-4 dan tekan [Start]",
                    [LocKey.PanelTitle] = "Panel Co-op Split-Screen (F10)",
                    [LocKey.PanelClose] = "Tutup Panel [F10]",
                    [LocKey.PanelGuide] = "Kontrol Gamepad: L-Stick Kursor | R-Stick Geser Kamera | LT/RT atau LB/RB Zoom\nTombol A: Klik/Drag | Tahan Y atau L3: Akses HUD/Daftar Tugas | Tekan R3: Reset Kamera",
                    [LocKey.BadgeMouse] = "{0}: Mouse",
                    [LocKey.BadgeGamepad] = "{0}: Gamepad",
                    [LocKey.BadgeKeyTest] = "{0}: Uji Tombol",
                    [LocKey.SlotP1ActiveMkb] = "P1: Aktif (Mouse + Keyboard)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Menunggu Gamepad 1",
                    [LocKey.SlotActiveDevice] = "{0}: Aktif ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Aktif (Uji Keyboard IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Kosong (Tekan Start di Gamepad)",
                    [LocKey.BtnModeActive] = "Co-op: AKTIF [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: CUTSCENE [F8]",
                    [LocKey.BtnModeReady] = "Co-op: SIAP [F8]",
                    [LocKey.BtnModeOff] = "Co-op: MATI [F8]",
                    [LocKey.BtnLayoutVertical] = "Layout 2P: Kiri/Kanan [F11]",
                    [LocKey.BtnLayoutHorizontal] = "Layout 2P: Atas/Bawah [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Gamepad 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Mouse + KB [F7]",
                    [LocKey.BtnP2TestOn] = "Uji P2 KB: AKTIF [F6]",
                    [LocKey.BtnP2TestOff] = "Uji P2 KB: MATI [F6]"
                },

                ["th"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "แนวตั้ง (ซ้าย/ขวา)",
                    [LocKey.LayoutHorizontal] = "แนวนอน (บน/ล่าง)",
                    [LocKey.ToastLayoutChanged] = "รูปแบบ 2 คน: {0}",
                    [LocKey.InputMouseKb] = "เมาส์ + คีย์บอร์ด",
                    [LocKey.InputGamepad1] = "จอยเกม 1",
                    [LocKey.ToastP1InputChanged] = "การควบคุม P1: {0}",
                    [LocKey.ToastP2TestOn] = "โหมดทดสอบคีย์บอร์ด P2 เปิด (IJKL: เคอร์เซอร์ | ลูกศร: เลื่อนกล้อง | U/O: ซูม | Enter: คลิก)",
                    [LocKey.ToastP2TestOff] = "โหมดทดสอบคีย์บอร์ด P2 ปิด",
                    [LocKey.ToastCoopReady] = "Co-op พร้อมแล้ว: กด Start บนจอย (หรือกด F6 เพื่อทดสอบคีย์บอร์ด P2)",
                    [LocKey.ToastCoopActive] = "เปิดแบ่งหน้าจอ {0} คนแล้ว (F8: เปิด/ปิด | F11: รูปแบบ | F10: แผงควบคุม)",
                    [LocKey.ToastCoopJoined] = "เปิดแบ่งหน้าจอ {0} คนแล้ว!",
                    [LocKey.ToastSoloMode] = "โหมดเล่นคนเดียว (กด F8 เพื่อเปิดแบ่งหน้าจอ)",
                    [LocKey.ToastOneGamepadDetected] = "พบจอยเกม 1 ตัว: กด [Start] บนจอยเพื่อเข้าร่วมเป็น P2 หรือกด F8/F10",
                    [LocKey.StandbyTitle] = "ช่อง P4 พร้อมเข้าร่วม",
                    [LocKey.StandbySubtitle] = "เชื่อมต่อจอยที่ 4 แล้วกด [Start]",
                    [LocKey.PanelTitle] = "แผงควบคุมแบ่งหน้าจอ Co-op (F10)",
                    [LocKey.PanelClose] = "ปิดแผงควบคุม [F10]",
                    [LocKey.PanelGuide] = "จอยเกม: L-Stick เคอร์เซอร์ | R-Stick เลื่อนกล้อง | LT/RT หรือ LB/RB ซูม\nปุ่ม A: คลิก/ลาก | กด Y หรือ L3 ค้าง: เข้าถึง HUD/รายการ | R3: รีเซ็ตกล้อง",
                    [LocKey.BadgeMouse] = "{0}: เมาส์",
                    [LocKey.BadgeGamepad] = "{0}: จอยเกม",
                    [LocKey.BadgeKeyTest] = "{0}: ทดสอบปุ่ม",
                    [LocKey.SlotP1ActiveMkb] = "P1: ใช้งานอยู่ (เมาส์ + คีย์บอร์ด)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: รอจอยเกม 1",
                    [LocKey.SlotActiveDevice] = "{0}: ใช้งานอยู่ ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: ใช้งานอยู่ (ทดสอบคีย์บอร์ด IJKL)",
                    [LocKey.SlotEmpty] = "{0}: ว่าง (กด Start บนจอยเกม)",
                    [LocKey.BtnModeActive] = "Co-op: เปิด [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: คัตซีน [F8]",
                    [LocKey.BtnModeReady] = "Co-op: พร้อม [F8]",
                    [LocKey.BtnModeOff] = "Co-op: ปิด [F8]",
                    [LocKey.BtnLayoutVertical] = "แบ่งจอ 2P: ซ้าย/ขวา [F11]",
                    [LocKey.BtnLayoutHorizontal] = "แบ่งจอ 2P: บน/ล่าง [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: จอยเกม 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: เมาส์ + คีย์บอร์ด [F7]",
                    [LocKey.BtnP2TestOn] = "ทดสอบ P2: เปิด [F6]",
                    [LocKey.BtnP2TestOff] = "ทดสอบ P2: ปิด [F6]"
                },

                ["zh-CN"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "左右分屏",
                    [LocKey.LayoutHorizontal] = "上下分屏",
                    [LocKey.ToastLayoutChanged] = "双人分屏布局: {0}",
                    [LocKey.InputMouseKb] = "鼠标 + 键盘",
                    [LocKey.InputGamepad1] = "手柄 1",
                    [LocKey.ToastP1InputChanged] = "P1 输入方式: {0}",
                    [LocKey.ToastP2TestOn] = "P2 键盘测试已开启 (IJKL: 光标 | 方向键: 移动镜头 | U/O: 缩放 | Enter: 点击)",
                    [LocKey.ToastP2TestOff] = "P2 键盘测试已关闭",
                    [LocKey.ToastCoopReady] = "合作模式就绪: 按手柄 Start 加入 (或按 F6 开启 P2 键盘测试)",
                    [LocKey.ToastCoopActive] = "{0}人分屏已开启 (F8: 开关 | F11: 布局 | F10: 面板)",
                    [LocKey.ToastCoopJoined] = "{0}人分屏已开启!",
                    [LocKey.ToastSoloMode] = "单人模式已恢复 (按 F8 开启分屏)",
                    [LocKey.ToastOneGamepadDetected] = "检测到 1 个手柄: 按手柄 [Start] 作为 P2 加入，或按 F8/F10",
                    [LocKey.StandbyTitle] = "P4 栏位等待加入",
                    [LocKey.StandbySubtitle] = "连接第 4 个手柄并按 [Start]",
                    [LocKey.PanelTitle] = "分屏合作控制面板 (F10)",
                    [LocKey.PanelClose] = "关闭面板 [F10]",
                    [LocKey.PanelGuide] = "手柄操作: 左摇杆移动光标 | 右摇杆移动镜头 | LT/RT 或 LB/RB 缩放\nA 键: 点击/拖拽 | 按住 Y 或 L3: 操作界面/任务清单 | R3: 重置镜头",
                    [LocKey.BadgeMouse] = "{0}: 鼠标",
                    [LocKey.BadgeGamepad] = "{0}: 手柄",
                    [LocKey.BadgeKeyTest] = "{0}: 键盘测试",
                    [LocKey.SlotP1ActiveMkb] = "P1: 已连接 (鼠标 + 键盘)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: 等待手柄 1",
                    [LocKey.SlotActiveDevice] = "{0}: 已连接 ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: 已连接 (键盘测试 IJKL)",
                    [LocKey.SlotEmpty] = "{0}: 空闲 (按手柄 Start 加入)",
                    [LocKey.BtnModeActive] = "合作: 开启 [F8]",
                    [LocKey.BtnModeCutscene] = "合作: 过场中 [F8]",
                    [LocKey.BtnModeReady] = "合作: 就绪 [F8]",
                    [LocKey.BtnModeOff] = "合作: 关闭 [F8]",
                    [LocKey.BtnLayoutVertical] = "2P布局: 左右 [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P布局: 上下 [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: 手柄 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: 键鼠 [F7]",
                    [LocKey.BtnP2TestOn] = "P2键盘测试: 开 [F6]",
                    [LocKey.BtnP2TestOff] = "P2键盘测试: 关 [F6]"
                },

                ["zh-TW"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "左右分割",
                    [LocKey.LayoutHorizontal] = "上下分割",
                    [LocKey.ToastLayoutChanged] = "雙人版面: {0}",
                    [LocKey.InputMouseKb] = "滑鼠 + 鍵盤",
                    [LocKey.InputGamepad1] = "控制器 1",
                    [LocKey.ToastP1InputChanged] = "P1 輸入方式: {0}",
                    [LocKey.ToastP2TestOn] = "P2 鍵盤測試已開啟 (IJKL: 游標 | 方向鍵: 移動鏡頭 | U/O: 縮放 | Enter: 點擊)",
                    [LocKey.ToastP2TestOff] = "P2 鍵盤測試已關閉",
                    [LocKey.ToastCoopReady] = "合作模式就緒: 按控制器 Start 加入 (或按 F6 開啟 P2 鍵盤測試)",
                    [LocKey.ToastCoopActive] = "{0}人分割畫面已開啟 (F8: 切換 | F11: 版面 | F10: 面板)",
                    [LocKey.ToastCoopJoined] = "{0}人分割畫面已開啟!",
                    [LocKey.ToastSoloMode] = "單人模式已恢復 (按 F8 開啟分割畫面)",
                    [LocKey.ToastOneGamepadDetected] = "偵測到 1 支控制器: 按控制器 [Start] 以 P2 加入，或按 F8/F10",
                    [LocKey.StandbyTitle] = "P4 欄位準備加入",
                    [LocKey.StandbySubtitle] = "連接第 4 支控制器並按 [Start]",
                    [LocKey.PanelTitle] = "分割畫面合作控制面板 (F10)",
                    [LocKey.PanelClose] = "關閉面板 [F10]",
                    [LocKey.PanelGuide] = "控制器: 左搖桿游標 | 右搖桿移動鏡頭 | LT/RT 或 LB/RB 縮放\nA 鍵: 點擊/拖曳 | 按住 Y 或 L3: 操作介面/待辦清單 | R3: 重設鏡頭",
                    [LocKey.BadgeMouse] = "{0}: 滑鼠",
                    [LocKey.BadgeGamepad] = "{0}: 控制器",
                    [LocKey.BadgeKeyTest] = "{0}: 鍵盤測試",
                    [LocKey.SlotP1ActiveMkb] = "P1: 已啟用 (滑鼠 + 鍵盤)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: 等待控制器 1",
                    [LocKey.SlotActiveDevice] = "{0}: 已啟用 ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: 已啟用 (鍵盤測試 IJKL)",
                    [LocKey.SlotEmpty] = "{0}: 空位 (按控制器 Start 加入)",
                    [LocKey.BtnModeActive] = "合作: 開啟 [F8]",
                    [LocKey.BtnModeCutscene] = "合作: 過場中 [F8]",
                    [LocKey.BtnModeReady] = "合作: 就緒 [F8]",
                    [LocKey.BtnModeOff] = "合作: 關閉 [F8]",
                    [LocKey.BtnLayoutVertical] = "2P版面: 左右 [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P版面: 上下 [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: 控制器 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: 鍵鼠 [F7]",
                    [LocKey.BtnP2TestOn] = "P2鍵盤測試: 開 [F6]",
                    [LocKey.BtnP2TestOff] = "P2鍵盤測試: 關 [F6]"
                },

                ["ja"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "左右分割",
                    [LocKey.LayoutHorizontal] = "上下分割",
                    [LocKey.ToastLayoutChanged] = "2人レイアウト: {0}",
                    [LocKey.InputMouseKb] = "マウス + キーボード",
                    [LocKey.InputGamepad1] = "ゲームパッド 1",
                    [LocKey.ToastP1InputChanged] = "P1 入力: {0}",
                    [LocKey.ToastP2TestOn] = "P2 キーボードテスト ON (IJKL: カーソル | 矢印: カメラ移動 | U/O: ズーム | Enter: クリック)",
                    [LocKey.ToastP2TestOff] = "P2 キーボードテスト OFF",
                    [LocKey.ToastCoopReady] = "協力プレイ待機中: ゲームパッドの Start を押す (または F6 で P2 キーテスト)",
                    [LocKey.ToastCoopActive] = "{0}人画面分割を開始しました (F8: 切替 | F11: レイアウト | F10: パネル)",
                    [LocKey.ToastCoopJoined] = "{0}人画面分割を開始しました!",
                    [LocKey.ToastSoloMode] = "ソロモードに切り替えました (F8 で画面分割)",
                    [LocKey.ToastOneGamepadDetected] = "ゲームパッドを1台検出: [Start] で P2 参加、または F8/F10",
                    [LocKey.StandbyTitle] = "P4 スロット待機中",
                    [LocKey.StandbySubtitle] = "4台目のゲームパッドを接続して [Start]",
                    [LocKey.PanelTitle] = "画面分割協力パネル (F10)",
                    [LocKey.PanelClose] = "パネルを閉じる [F10]",
                    [LocKey.PanelGuide] = "操作: 左スティック カーソル | 右スティック カメラ移動 | LT/RT または LB/RB ズーム\nAボタン: クリック/ドラッグ | Y または L3 長押し: UI/リスト操作 | R3: カメラリセット",
                    [LocKey.BadgeMouse] = "{0}: マウス",
                    [LocKey.BadgeGamepad] = "{0}: パッド",
                    [LocKey.BadgeKeyTest] = "{0}: キーテスト",
                    [LocKey.SlotP1ActiveMkb] = "P1: 有効 (マウス + キーボード)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: ゲームパッド 1 待機中",
                    [LocKey.SlotActiveDevice] = "{0}: 有効 ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: 有効 (キーボードテスト IJKL)",
                    [LocKey.SlotEmpty] = "{0}: 空き (ゲームパッドの Start で参加)",
                    [LocKey.BtnModeActive] = "協力: ON [F8]",
                    [LocKey.BtnModeCutscene] = "協力: ムービー中 [F8]",
                    [LocKey.BtnModeReady] = "協力: 待機中 [F8]",
                    [LocKey.BtnModeOff] = "協力: OFF [F8]",
                    [LocKey.BtnLayoutVertical] = "2P配置: 左右 [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P配置: 上下 [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: パッド 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: マウス+KB [F7]",
                    [LocKey.BtnP2TestOn] = "P2キーテスト: ON [F6]",
                    [LocKey.BtnP2TestOff] = "P2キーテスト: OFF [F6]"
                },

                ["ko"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "좌우 분할",
                    [LocKey.LayoutHorizontal] = "상하 분할",
                    [LocKey.ToastLayoutChanged] = "2인 화면 배치: {0}",
                    [LocKey.InputMouseKb] = "마우스 + 키보드",
                    [LocKey.InputGamepad1] = "게임패드 1",
                    [LocKey.ToastP1InputChanged] = "P1 입력: {0}",
                    [LocKey.ToastP2TestOn] = "P2 키보드 테스트 켜짐 (IJKL: 커서 | 방향키: 화면 이동 | U/O: 줌 | Enter: 클릭)",
                    [LocKey.ToastP2TestOff] = "P2 키보드 테스트 꺼짐",
                    [LocKey.ToastCoopReady] = "협동 준비 완료: 게임패드 Start 버튼 (또는 F6으로 P2 키보드 테스트)",
                    [LocKey.ToastCoopActive] = "{0}인 화면 분할 활성화 (F8: 전환 | F11: 배치 | F10: 패널)",
                    [LocKey.ToastCoopJoined] = "{0}인 화면 분할 활성화!",
                    [LocKey.ToastSoloMode] = "솔로 모드 활성화 (F8로 화면 분할)",
                    [LocKey.ToastOneGamepadDetected] = "게임패드 1개 감지됨: [Start]를 눌러 P2 참가 또는 F8/F10",
                    [LocKey.StandbyTitle] = "P4 슬롯 참가 대기",
                    [LocKey.StandbySubtitle] = "4번째 게임패드를 연결하고 [Start]를 누르세요",
                    [LocKey.PanelTitle] = "화면 분할 협동 패널 (F10)",
                    [LocKey.PanelClose] = "패널 닫기 [F10]",
                    [LocKey.PanelGuide] = "게임패드: 왼쪽 스틱 커서 | 오른쪽 스틱 화면 이동 | LT/RT 또는 LB/RB 줌\nA 버튼: 클릭/드래그 | Y 또는 L3 홀드: HUD/할 일 목록 | R3: 카메라 초기화",
                    [LocKey.BadgeMouse] = "{0}: 마우스",
                    [LocKey.BadgeGamepad] = "{0}: 패드",
                    [LocKey.BadgeKeyTest] = "{0}: 키 테스트",
                    [LocKey.SlotP1ActiveMkb] = "P1: 활성 (마우스 + 키보드)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: 게임패드 1 대기 중",
                    [LocKey.SlotActiveDevice] = "{0}: 활성 ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: 활성 (키보드 테스트 IJKL)",
                    [LocKey.SlotEmpty] = "{0}: 비어 있음 (패드 Start 누르기)",
                    [LocKey.BtnModeActive] = "협동: 켜짐 [F8]",
                    [LocKey.BtnModeCutscene] = "협동: 컷신 중 [F8]",
                    [LocKey.BtnModeReady] = "협동: 대기 [F8]",
                    [LocKey.BtnModeOff] = "협동: 꺼짐 [F8]",
                    [LocKey.BtnLayoutVertical] = "2P 배치: 좌/우 [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P 배치: 상/하 [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: 패드 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: 마우스+KB [F7]",
                    [LocKey.BtnP2TestOn] = "P2 키 테스트: ON [F6]",
                    [LocKey.BtnP2TestOff] = "P2 키 테스트: OFF [F6]"
                },

                ["de"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertikal (Links/Rechts)",
                    [LocKey.LayoutHorizontal] = "Horizontal (Oben/Unten)",
                    [LocKey.ToastLayoutChanged] = "2P-Layout: {0}",
                    [LocKey.InputMouseKb] = "Maus + Tastatur",
                    [LocKey.InputGamepad1] = "Gamepad 1",
                    [LocKey.ToastP1InputChanged] = "P1-Eingabe: {0}",
                    [LocKey.ToastP2TestOn] = "P2-Tastaturtest AN (IJKL: Cursor | Pfeile: Schwenken | U/O: Zoom | Enter: Klick)",
                    [LocKey.ToastP2TestOff] = "P2-Tastaturtest AUS",
                    [LocKey.ToastCoopReady] = "Koop bereit: Start auf Gamepad druecken (oder F6 fuer P2-Tastaturtest)",
                    [LocKey.ToastCoopActive] = "Splitscreen fuer {0} Spieler aktiv (F8: Umschalten | F11: Layout | F10: Panel)",
                    [LocKey.ToastCoopJoined] = "Splitscreen fuer {0} Spieler aktiv!",
                    [LocKey.ToastSoloMode] = "Solo-Modus aktiv (F8 fuer Splitscreen)",
                    [LocKey.ToastOneGamepadDetected] = "1 Gamepad erkannt: [Start] auf Gamepad fuer P2 druecken, oder F8/F10",
                    [LocKey.StandbyTitle] = "P4-Platz bereit zum Beitreten",
                    [LocKey.StandbySubtitle] = "4. Gamepad verbinden und [Start] druecken",
                    [LocKey.PanelTitle] = "Splitscreen-Koop-Panel (F10)",
                    [LocKey.PanelClose] = "Panel schliessen [F10]",
                    [LocKey.PanelGuide] = "Gamepad: L-Stick Cursor | R-Stick Kamera | LT/RT oder LB/RB Zoom\nA: Klick/Ziehen | Y oder L3 halten: HUD/Aufgabenliste | R3: Kamera zuruecksetzen",
                    [LocKey.BadgeMouse] = "{0}: Maus",
                    [LocKey.BadgeGamepad] = "{0}: Gamepad",
                    [LocKey.BadgeKeyTest] = "{0}: Tastentest",
                    [LocKey.SlotP1ActiveMkb] = "P1: Aktiv (Maus + Tastatur)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Warte auf Gamepad 1",
                    [LocKey.SlotActiveDevice] = "{0}: Aktiv ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Aktiv (Tastaturtest IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Frei (Start auf Gamepad druecken)",
                    [LocKey.BtnModeActive] = "Koop: AN [F8]",
                    [LocKey.BtnModeCutscene] = "Koop: ZWISCHENSEQUENZ [F8]",
                    [LocKey.BtnModeReady] = "Koop: BEREIT [F8]",
                    [LocKey.BtnModeOff] = "Koop: AUS [F8]",
                    [LocKey.BtnLayoutVertical] = "2P-Layout: Links/Rechts [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P-Layout: Oben/Unten [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Gamepad 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Maus + KB [F7]",
                    [LocKey.BtnP2TestOn] = "P2 KB-Test: AN [F6]",
                    [LocKey.BtnP2TestOff] = "P2 KB-Test: AUS [F6]"
                },

                ["fr"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertical (Gauche/Droite)",
                    [LocKey.LayoutHorizontal] = "Horizontal (Haut/Bas)",
                    [LocKey.ToastLayoutChanged] = "Disposition 2J : {0}",
                    [LocKey.InputMouseKb] = "Souris + Clavier",
                    [LocKey.InputGamepad1] = "Manette 1",
                    [LocKey.ToastP1InputChanged] = "Entree J1 : {0}",
                    [LocKey.ToastP2TestOn] = "Test clavier J2 ACTIVE (IJKL : Curseur | Fleches : Camera | U/O : Zoom | Entree : Clic)",
                    [LocKey.ToastP2TestOff] = "Test clavier J2 DESACTIVE",
                    [LocKey.ToastCoopReady] = "Coop pret : Appuyez sur Start (ou F6 pour tester J2 au clavier)",
                    [LocKey.ToastCoopActive] = "Ecran scinde {0} joueurs actif (F8 : Basculer | F11 : Disposition | F10 : Panneau)",
                    [LocKey.ToastCoopJoined] = "Ecran scinde {0} joueurs actif !",
                    [LocKey.ToastSoloMode] = "Mode Solo actif (F8 pour l'ecran scinde)",
                    [LocKey.ToastOneGamepadDetected] = "1 manette detectee : Appuyez sur [Start] pour rejoindre en J2, ou F8/F10",
                    [LocKey.StandbyTitle] = "Emplacement P4 pret",
                    [LocKey.StandbySubtitle] = "Branchez la 4e manette et appuyez sur [Start]",
                    [LocKey.PanelTitle] = "Panneau Coop Ecran Scinde (F10)",
                    [LocKey.PanelClose] = "Fermer le panneau [F10]",
                    [LocKey.PanelGuide] = "Manette : Stick G Curseur | Stick D Camera | LT/RT ou LB/RB Zoom\nA : Clic/Glisser | Maintenir Y ou L3 : HUD/Taches | R3 : Reinitialiser",
                    [LocKey.BadgeMouse] = "{0}: Souris",
                    [LocKey.BadgeGamepad] = "{0}: Manette",
                    [LocKey.BadgeKeyTest] = "{0}: Test Clavier",
                    [LocKey.SlotP1ActiveMkb] = "P1: Actif (Souris + Clavier)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Attente Manette 1",
                    [LocKey.SlotActiveDevice] = "{0}: Actif ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Actif (Test Clavier IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Libre (Appuyez sur Start)",
                    [LocKey.BtnModeActive] = "Coop: ACTIF [F8]",
                    [LocKey.BtnModeCutscene] = "Coop: CINEMATIQUE [F8]",
                    [LocKey.BtnModeReady] = "Coop: PRET [F8]",
                    [LocKey.BtnModeOff] = "Coop: INACTIF [F8]",
                    [LocKey.BtnLayoutVertical] = "2J: Gauche/Droite [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2J: Haut/Bas [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Manette 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Souris + KB [F7]",
                    [LocKey.BtnP2TestOn] = "Test J2 KB: OUI [F6]",
                    [LocKey.BtnP2TestOff] = "Test J2 KB: NON [F6]"
                },

                ["es-ES"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertical (Izquierda/Derecha)",
                    [LocKey.LayoutHorizontal] = "Horizontal (Arriba/Abajo)",
                    [LocKey.ToastLayoutChanged] = "Disposicion 2J: {0}",
                    [LocKey.InputMouseKb] = "Raton + Teclado",
                    [LocKey.InputGamepad1] = "Mando 1",
                    [LocKey.ToastP1InputChanged] = "Entrada P1: {0}",
                    [LocKey.ToastP2TestOn] = "Prueba Teclado P2 ACTIVA (IJKL: Cursor | Flechas: Mover | U/O: Zoom | Enter: Clic)",
                    [LocKey.ToastP2TestOff] = "Prueba Teclado P2 DESACTIVADA",
                    [LocKey.ToastCoopReady] = "Coop listo: Pulsa Start en el mando (o F6 para probar P2 en teclado)",
                    [LocKey.ToastCoopActive] = "Pantalla dividida {0} jugadores activa (F8: Alternar | F11: Disposicion | F10: Panel)",
                    [LocKey.ToastCoopJoined] = "Pantalla dividida {0} jugadores activa!",
                    [LocKey.ToastSoloMode] = "Modo Solo activo (Pulsa F8 para pantalla dividida)",
                    [LocKey.ToastOneGamepadDetected] = "1 mando detectado: Pulsa [Start] para unirte como P2, o F8/F10",
                    [LocKey.StandbyTitle] = "Ranura P4 lista para unirse",
                    [LocKey.StandbySubtitle] = "Conecta el 4o mando y pulsa [Start]",
                    [LocKey.PanelTitle] = "Panel Cooperativo Pantalla Dividida (F10)",
                    [LocKey.PanelClose] = "Cerrar panel [F10]",
                    [LocKey.PanelGuide] = "Mando: Stick I Cursor | Stick D Camara | LT/RT o LB/RB Zoom\nA: Clic/Arrastrar | Mantener Y o L3: HUD/Tareas | R3: Restablecer camara",
                    [LocKey.BadgeMouse] = "{0}: Raton",
                    [LocKey.BadgeGamepad] = "{0}: Mando",
                    [LocKey.BadgeKeyTest] = "{0}: Test Teclas",
                    [LocKey.SlotP1ActiveMkb] = "P1: Activo (Raton + Teclado)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Esperando Mando 1",
                    [LocKey.SlotActiveDevice] = "{0}: Activo ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Activo (Prueba Teclado IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Libre (Pulsa Start en Mando)",
                    [LocKey.BtnModeActive] = "Coop: ACTIVO [F8]",
                    [LocKey.BtnModeCutscene] = "Coop: ESCENA [F8]",
                    [LocKey.BtnModeReady] = "Coop: LISTO [F8]",
                    [LocKey.BtnModeOff] = "Coop: APAGADO [F8]",
                    [LocKey.BtnLayoutVertical] = "2J: Izq./Der. [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2J: Arriba/Abajo [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Mando 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Raton + KB [F7]",
                    [LocKey.BtnP2TestOn] = "Test P2 KB: SI [F6]",
                    [LocKey.BtnP2TestOff] = "Test P2 KB: NO [F6]"
                },

                ["it"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Verticale (Sinistra/Destra)",
                    [LocKey.LayoutHorizontal] = "Orizzontale (Sopra/Sotto)",
                    [LocKey.ToastLayoutChanged] = "Layout 2G: {0}",
                    [LocKey.InputMouseKb] = "Mouse + Tastiera",
                    [LocKey.InputGamepad1] = "Controller 1",
                    [LocKey.ToastP1InputChanged] = "Input P1: {0}",
                    [LocKey.ToastP2TestOn] = "Test Tastiera P2 ATTIVO (IJKL: Cursore | Frecce: Camera | U/O: Zoom | Invio: Clic)",
                    [LocKey.ToastP2TestOff] = "Test Tastiera P2 DISATTIVO",
                    [LocKey.ToastCoopReady] = "Co-op pronto: Premi Start sul controller (o F6 per test tastiera P2)",
                    [LocKey.ToastCoopActive] = "Schermo condiviso {0} giocatori attivo (F8: Attiva/Disattiva | F11: Layout | F10: Pannello)",
                    [LocKey.ToastCoopJoined] = "Schermo condiviso {0} giocatori attivo!",
                    [LocKey.ToastSoloMode] = "Modalita Solo attiva (Premi F8 per schermo condiviso)",
                    [LocKey.ToastOneGamepadDetected] = "1 controller rilevato: Premi [Start] per unirti come P2, o F8/F10",
                    [LocKey.StandbyTitle] = "Slot P4 pronto a unirsi",
                    [LocKey.StandbySubtitle] = "Collega il 4o controller e premi [Start]",
                    [LocKey.PanelTitle] = "Pannello Co-op Schermo Condiviso (F10)",
                    [LocKey.PanelClose] = "Chiudi pannello [F10]",
                    [LocKey.PanelGuide] = "Controller: Stick S Cursore | Stick D Camera | LT/RT o LB/RB Zoom\nA: Clic/Trascina | Tieni Y o L3: HUD/Lista | R3: Ripristina camera",
                    [LocKey.BadgeMouse] = "{0}: Mouse",
                    [LocKey.BadgeGamepad] = "{0}: Controller",
                    [LocKey.BadgeKeyTest] = "{0}: Test Tasti",
                    [LocKey.SlotP1ActiveMkb] = "P1: Attivo (Mouse + Tastiera)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: In attesa Controller 1",
                    [LocKey.SlotActiveDevice] = "{0}: Attivo ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Attivo (Test Tastiera IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Vuoto (Premi Start sul controller)",
                    [LocKey.BtnModeActive] = "Co-op: ATTIVO [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: FILMATO [F8]",
                    [LocKey.BtnModeReady] = "Co-op: PRONTO [F8]",
                    [LocKey.BtnModeOff] = "Co-op: SPENTO [F8]",
                    [LocKey.BtnLayoutVertical] = "2G: Sinistra/Destra [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2G: Sopra/Sotto [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Controller 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Mouse + KB [F7]",
                    [LocKey.BtnP2TestOn] = "Test P2 KB: SI [F6]",
                    [LocKey.BtnP2TestOff] = "Test P2 KB: NO [F6]"
                },

                ["ru"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Вертикально (Слева/Справа)",
                    [LocKey.LayoutHorizontal] = "Горизонтально (Сверху/Снизу)",
                    [LocKey.ToastLayoutChanged] = "Раскладка 2P: {0}",
                    [LocKey.InputMouseKb] = "Мышь + Клавиатура",
                    [LocKey.InputGamepad1] = "Геймпад 1",
                    [LocKey.ToastP1InputChanged] = "Ввод P1: {0}",
                    [LocKey.ToastP2TestOn] = "Тест клавиатуры P2 ВКЛ (IJKL: курсор | Стрелки: камера | U/O: масштаб | Enter: клик)",
                    [LocKey.ToastP2TestOff] = "Тест клавиатуры P2 ВЫКЛ",
                    [LocKey.ToastCoopReady] = "Кооп готов: нажмите Start на геймпаде (или F6 для теста P2)",
                    [LocKey.ToastCoopActive] = "Разделенный экран ({0} игр.) включен (F8: вкл/выкл | F11: вид | F10: панель)",
                    [LocKey.ToastCoopJoined] = "Разделенный экран ({0} игр.) включен!",
                    [LocKey.ToastSoloMode] = "Одиночный режим (F8 для разделенного экрана)",
                    [LocKey.ToastOneGamepadDetected] = "Обнаружен 1 геймпад: нажмите [Start] для входа P2 или F8/F10",
                    [LocKey.StandbyTitle] = "Слот P4 готов к входу",
                    [LocKey.StandbySubtitle] = "Подключите 4-й геймпад и нажмите [Start]",
                    [LocKey.PanelTitle] = "Панель кооператива (F10)",
                    [LocKey.PanelClose] = "Закрыть панель [F10]",
                    [LocKey.PanelGuide] = "Геймпад: L-стик курсор | R-стик камера | LT/RT или LB/RB зум\nA: клик/перетаскивание | Удерж. Y или L3: интерфейс/список | R3: сброс",
                    [LocKey.BadgeMouse] = "{0}: Мышь",
                    [LocKey.BadgeGamepad] = "{0}: Геймпад",
                    [LocKey.BadgeKeyTest] = "{0}: Клавиатура",
                    [LocKey.SlotP1ActiveMkb] = "P1: Активен (Мышь + Клавиатура)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Ожидание геймпада 1",
                    [LocKey.SlotActiveDevice] = "{0}: Активен ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Активен (Тест клавиатуры IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Свободно (Start на геймпаде)",
                    [LocKey.BtnModeActive] = "Кооп: ВКЛ [F8]",
                    [LocKey.BtnModeCutscene] = "Кооп: КАТСЦЕНА [F8]",
                    [LocKey.BtnModeReady] = "Кооп: ГОТОВ [F8]",
                    [LocKey.BtnModeOff] = "Кооп: ВЫКЛ [F8]",
                    [LocKey.BtnLayoutVertical] = "2P: Слева/Справа [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P: Сверху/Снизу [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Геймпад 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Мышь + Клав. [F7]",
                    [LocKey.BtnP2TestOn] = "Тест P2: ВКЛ [F6]",
                    [LocKey.BtnP2TestOff] = "Тест P2: ВЫКЛ [F6]"
                },

                ["sv-SE"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertikal (Vanster/Hoger)",
                    [LocKey.LayoutHorizontal] = "Horisontell (Upp/Ner)",
                    [LocKey.ToastLayoutChanged] = "2P-layout: {0}",
                    [LocKey.InputMouseKb] = "Mus + Tangentbord",
                    [LocKey.InputGamepad1] = "Handkontroll 1",
                    [LocKey.ToastP1InputChanged] = "P1-inmatning: {0}",
                    [LocKey.ToastP2TestOn] = "P2 Tangentbordstest PA (IJKL: Pekare | Pilar: Panorera | U/O: Zoom | Enter: Klick)",
                    [LocKey.ToastP2TestOff] = "P2 Tangentbordstest AV",
                    [LocKey.ToastCoopReady] = "Co-op redo: Tryck Start pa handkontroll (eller F6 for P2-test)",
                    [LocKey.ToastCoopActive] = "Delad skarm {0} spelare aktiv (F8: Vaxla | F11: Layout | F10: Panel)",
                    [LocKey.ToastCoopJoined] = "Delad skarm {0} spelare aktiv!",
                    [LocKey.ToastSoloMode] = "Sololage aktivt (Tryck F8 for delad skarm)",
                    [LocKey.ToastOneGamepadDetected] = "1 handkontroll hittad: Tryck [Start] for att ga med som P2, eller F8/F10",
                    [LocKey.StandbyTitle] = "P4-plats redo",
                    [LocKey.StandbySubtitle] = "Anslut 4:e handkontrollen och tryck [Start]",
                    [LocKey.PanelTitle] = "Delad skarm Co-op-panel (F10)",
                    [LocKey.PanelClose] = "Stang panel [F10]",
                    [LocKey.PanelGuide] = "Handkontroll: V-spak Pekare | H-spak Kamera | LT/RT eller LB/RB Zoom\nA: Klick/Dra | Hall Y eller L3: HUD/Att-gora | R3: Aterstall kamera",
                    [LocKey.BadgeMouse] = "{0}: Mus",
                    [LocKey.BadgeGamepad] = "{0}: Kontroll",
                    [LocKey.BadgeKeyTest] = "{0}: Tangenttest",
                    [LocKey.SlotP1ActiveMkb] = "P1: Aktiv (Mus + Tangentbord)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Vantar pa Handkontroll 1",
                    [LocKey.SlotActiveDevice] = "{0}: Aktiv ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Aktiv (Tangenttest IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Tom (Tryck Start pa kontroll)",
                    [LocKey.BtnModeActive] = "Co-op: PA [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: FILMKLIPP [F8]",
                    [LocKey.BtnModeReady] = "Co-op: REDO [F8]",
                    [LocKey.BtnModeOff] = "Co-op: AV [F8]",
                    [LocKey.BtnLayoutVertical] = "2P: Vanster/Hoger [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P: Upp/Ner [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Kontroll 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Mus + KB [F7]",
                    [LocKey.BtnP2TestOn] = "P2 KB-test: PA [F6]",
                    [LocKey.BtnP2TestOff] = "P2 KB-test: AV [F6]"
                },

                ["tr"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Dikey (Sol/Sag)",
                    [LocKey.LayoutHorizontal] = "Yatay (Ust/Alt)",
                    [LocKey.ToastLayoutChanged] = "2P Duzeni: {0}",
                    [LocKey.InputMouseKb] = "Fare + Klavye",
                    [LocKey.InputGamepad1] = "Oyun Kolu 1",
                    [LocKey.ToastP1InputChanged] = "P1 Girisi: {0}",
                    [LocKey.ToastP2TestOn] = "P2 Klavye Testi ACIK (IJKL: Imlec | Oklar: Kamera | U/O: Yakinlastir | Enter: Tikla)",
                    [LocKey.ToastP2TestOff] = "P2 Klavye Testi KAPALI",
                    [LocKey.ToastCoopReady] = "Esli Mod Hazir: Kolda Start'a basin (veya P2 klavye testi icin F6)",
                    [LocKey.ToastCoopActive] = "{0} Oyunculu Bolunmus Ekran Acik (F8: Ac/Kapa | F11: Duzen | F10: Panel)",
                    [LocKey.ToastCoopJoined] = "{0} Oyunculu Bolunmus Ekran Acik!",
                    [LocKey.ToastSoloMode] = "Tek Oyuncu Modu (Bolunmus ekran icin F8)",
                    [LocKey.ToastOneGamepadDetected] = "1 oyun kolu algilandi: P2 olarak katilmak icin [Start] tusuna basin veya F8/F10",
                    [LocKey.StandbyTitle] = "P4 Yuvasi Katilima Hazir",
                    [LocKey.StandbySubtitle] = "4. oyun kolunu baglayin ve [Start] tusuna basin",
                    [LocKey.PanelTitle] = "Bolunmus Ekran Esli Mod Paneli (F10)",
                    [LocKey.PanelClose] = "Paneli Kapat [F10]",
                    [LocKey.PanelGuide] = "Oyun Kolu: Sol Cubuk Imlec | Sag Cubuk Kamera | LT/RT veya LB/RB Yakinlastir\nA: Tikla/Surukle | Y veya L3 Basili: Arayuz/Gorevler | R3: Kamerayi Sifirla",
                    [LocKey.BadgeMouse] = "{0}: Fare",
                    [LocKey.BadgeGamepad] = "{0}: Oyun Kolu",
                    [LocKey.BadgeKeyTest] = "{0}: Tus Testi",
                    [LocKey.SlotP1ActiveMkb] = "P1: Etkin (Fare + Klavye)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Oyun Kolu 1 Bekleniyor",
                    [LocKey.SlotActiveDevice] = "{0}: Etkin ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Etkin (Klavye Testi IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Bos (Kolda Start'a basin)",
                    [LocKey.BtnModeActive] = "Esli: ACIK [F8]",
                    [LocKey.BtnModeCutscene] = "Esli: ARA SAHNE [F8]",
                    [LocKey.BtnModeReady] = "Esli: HAZIR [F8]",
                    [LocKey.BtnModeOff] = "Esli: KAPALI [F8]",
                    [LocKey.BtnLayoutVertical] = "2P: Sol/Sag [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P: Ust/Alt [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Kol 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Fare + KB [F7]",
                    [LocKey.BtnP2TestOn] = "P2 KB Testi: ACIK [F6]",
                    [LocKey.BtnP2TestOff] = "P2 KB Testi: KAPALI [F6]"
                },

                ["pt-BR"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Vertical (Esquerda/Direita)",
                    [LocKey.LayoutHorizontal] = "Horizontal (Cima/Baixo)",
                    [LocKey.ToastLayoutChanged] = "Layout 2J: {0}",
                    [LocKey.InputMouseKb] = "Mouse + Teclado",
                    [LocKey.InputGamepad1] = "Controle 1",
                    [LocKey.ToastP1InputChanged] = "Entrada J1: {0}",
                    [LocKey.ToastP2TestOn] = "Teste Teclado J2 ATIVO (IJKL: Cursor | Setas: Mover | U/O: Zoom | Enter: Clique)",
                    [LocKey.ToastP2TestOff] = "Teste Teclado J2 DESATIVADO",
                    [LocKey.ToastCoopReady] = "Co-op pronto: Pressione Start no controle (ou F6 para teste J2)",
                    [LocKey.ToastCoopActive] = "Tela dividida {0} jogadores ativa (F8: Alternar | F11: Layout | F10: Painel)",
                    [LocKey.ToastCoopJoined] = "Tela dividida {0} jogadores ativa!",
                    [LocKey.ToastSoloMode] = "Modo Solo ativo (Pressione F8 para tela dividida)",
                    [LocKey.ToastOneGamepadDetected] = "1 controle detectado: Pressione [Start] para entrar como J2, ou F8/F10",
                    [LocKey.StandbyTitle] = "Espaco P4 pronto para entrar",
                    [LocKey.StandbySubtitle] = "Conecte o 4o controle e pressione [Start]",
                    [LocKey.PanelTitle] = "Painel Co-op Tela Dividida (F10)",
                    [LocKey.PanelClose] = "Fechar Painel [F10]",
                    [LocKey.PanelGuide] = "Controle: Stick E Cursor | Stick D Camera | LT/RT ou LB/RB Zoom\nA: Clique/Arrastar | Segurar Y ou L3: HUD/Tarefas | R3: Resetar camera",
                    [LocKey.BadgeMouse] = "{0}: Mouse",
                    [LocKey.BadgeGamepad] = "{0}: Controle",
                    [LocKey.BadgeKeyTest] = "{0}: Teste Tecla",
                    [LocKey.SlotP1ActiveMkb] = "P1: Ativo (Mouse + Teclado)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Aguardando Controle 1",
                    [LocKey.SlotActiveDevice] = "{0}: Ativo ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Ativo (Teste Teclado IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Vazio (Pressione Start)",
                    [LocKey.BtnModeActive] = "Co-op: ATIVO [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: CENA [F8]",
                    [LocKey.BtnModeReady] = "Co-op: PRONTO [F8]",
                    [LocKey.BtnModeOff] = "Co-op: DESLIG. [F8]",
                    [LocKey.BtnLayoutVertical] = "2J: Esq./Dir. [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2J: Cima/Baixo [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Controle 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Mouse + KB [F7]",
                    [LocKey.BtnP2TestOn] = "Teste J2 KB: SIM [F6]",
                    [LocKey.BtnP2TestOff] = "Teste J2 KB: NAO [F6]"
                },

                ["vi"] = new Dictionary<LocKey, string>
                {
                    [LocKey.LayoutVertical] = "Chia doc (Trai/Phai)",
                    [LocKey.LayoutHorizontal] = "Chia ngang (Tren/Duoi)",
                    [LocKey.ToastLayoutChanged] = "Bo cuc 2 nguoi: {0}",
                    [LocKey.InputMouseKb] = "Chuot + Ban phim",
                    [LocKey.InputGamepad1] = "Tay cam 1",
                    [LocKey.ToastP1InputChanged] = "Dieu khien P1: {0}",
                    [LocKey.ToastP2TestOn] = "Thu ban phim P2 BAT (IJKL: Con tro | Mui ten: Di chuyen | U/O: Thu phong | Enter: Nhan)",
                    [LocKey.ToastP2TestOff] = "Thu ban phim P2 TAT",
                    [LocKey.ToastCoopReady] = "Co-op san sang: Nhan Start tren tay cam (hoac F6 de thu ban phim P2)",
                    [LocKey.ToastCoopActive] = "Chia man hinh {0} nguoi dang bat (F8: Bat/Tat | F11: Bo cuc | F10: Bang)",
                    [LocKey.ToastCoopJoined] = "Chia man hinh {0} nguoi dang bat!",
                    [LocKey.ToastSoloMode] = "Che do choi don (Nhan F8 de chia man hinh)",
                    [LocKey.ToastOneGamepadDetected] = "Phat hien 1 tay cam: Nhan [Start] de vao P2, hoac F8/F10",
                    [LocKey.StandbyTitle] = "Vi tri P4 san sang tham gia",
                    [LocKey.StandbySubtitle] = "Ket noi tay cam thu 4 va nhan [Start]",
                    [LocKey.PanelTitle] = "Bang dieu khien Co-op chia man hinh (F10)",
                    [LocKey.PanelClose] = "Dong bang [F10]",
                    [LocKey.PanelGuide] = "Tay cam: Can trai Con tro | Can phai Camera | LT/RT hoac LB/RB Thu phong\nNut A: Nhan/Keo | Giu Y hoac L3: Giao dien/Viec can lam | R3: Dat lai camera",
                    [LocKey.BadgeMouse] = "{0}: Chuot",
                    [LocKey.BadgeGamepad] = "{0}: Tay cam",
                    [LocKey.BadgeKeyTest] = "{0}: Thu phim",
                    [LocKey.SlotP1ActiveMkb] = "P1: Hoat dong (Chuot + Ban phim)",
                    [LocKey.SlotP1WaitingGamepad] = "P1: Dang cho Tay cam 1",
                    [LocKey.SlotActiveDevice] = "{0}: Hoat dong ({1})",
                    [LocKey.SlotActiveKeyTest] = "{0}: Hoat dong (Thu ban phim IJKL)",
                    [LocKey.SlotEmpty] = "{0}: Trong (Nhan Start tren tay cam)",
                    [LocKey.BtnModeActive] = "Co-op: BAT [F8]",
                    [LocKey.BtnModeCutscene] = "Co-op: CAT CANH [F8]",
                    [LocKey.BtnModeReady] = "Co-op: SAN SANG [F8]",
                    [LocKey.BtnModeOff] = "Co-op: TAT [F8]",
                    [LocKey.BtnLayoutVertical] = "2P: Trai/Phai [F11]",
                    [LocKey.BtnLayoutHorizontal] = "2P: Tren/Duoi [F11]",
                    [LocKey.BtnP1Gamepad] = "P1: Tay cam 1 [F7]",
                    [LocKey.BtnP1MouseKb] = "P1: Chuot + KB [F7]",
                    [LocKey.BtnP2TestOn] = "Thu P2 KB: BAT [F6]",
                    [LocKey.BtnP2TestOff] = "Thu P2 KB: TAT [F6]"
                }
            };
        }
    }
}
