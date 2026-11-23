[English](../README.md) | [Bahasa Indonesia](README.id.md) | [ไทย](README.th.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | **Deutsch**

# Lost and Found Co. - Splitscreen-Koop-Mod

Eine lokale Splitscreen-Koop-Mod auf Basis von BepInEx 5 fuer **Lost and Found Co.** (Windows x64) mit Unterstuetzung fuer 2 bis 4 Spieler, getrennten Kameras, eigenen Mauszeigern, Eingabe-Isolation, automatischem Vollbild bei Zwischensequenzen und mehrsprachiger Mod-Oberflaeche.

> **Getestete Spielversion**: **Lost and Found Co. v1.2.2** (Steam, Windows x64). Kleinere Spiel-Updates bleiben in der Regel kompatibel, solange sich interne Kamera-, Eingabe- oder Beleuchtungsklassen nicht aendern.

## Funktionen

- **2 bis 4 lokale Spieler**:
  - **2 Spieler**: Vertikale (Links/Rechts) oder horizontale (Oben/Unten) Teilung, jederzeit umschaltbar mit `F11`.
  - **3 Spieler**: 2x2-Raster mit Wartekarte im 4. Feld.
  - **4 Spieler**: Vollstaendiges 2x2-Raster.
- **Eingabe-Isolation & gemischte Geraete**:
  - Spieler 1 nutzt standardmaessig **Maus + Tastatur**, waehrend angeschlossene Gamepads P2-P4 steuern. Mit `F7` kann Spieler 1 auf Gamepad 1 umgestellt werden.
  - Druecke `Start` auf einem beliebigen Gamepad fuer Hot-Join oder nutze `F6` fuer den P2-Tastaturtest (`IJKL`, Pfeiltasten, `U`/`O`, `Enter`).
- **Eigene Kameras & 2D-Raycast-Klicks**:
  - Jeder Spieler hat eine eigene `WorldCamera` fuer Bewegung, Zoom und Objekt-Klicks/Ziehen. Halte `Y` oder `L3` gedrueckt, um HUD und Aufgabenliste auf dem gesamten Bildschirm zu bedienen.
- **Automatisches Vollbild bei Zwischensequenzen (`AutoFullscreenOnCutscene`)**:
  - Schaltet bei Cutscenes, Schnappschuss-Szenen, Dialog-Kamerasperren oder Stockwerkwechseln voruebergehend auf Vollbild und kehrt danach automatisch in den Splitscreen zurueck.
- **URP-HDR-Bloom & SmartLighting2D**:
  - Erhaelt HDR-Bloom (`RGB111110Float`) und synchronisiert die `FunkyCode`-SmartLighting2D-Beleuchtung fuer alle aktiven Teilbilder.
- **Mehrsprachige Mod-UI**:
  - Folgt automatisch der Spielsprache (`en`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`), unterstuetzt **Bahasa Indonesia (`id`)** bei installierter indonesischer Sprachmod sowie die uebrigen I2-Sprachslots (`fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`).

## Weitere Mods von uns & Kompatibilitaet

Unsere Begleitmod **[Lost and Found Co. Indonesian Localization Mod](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)** (`LostAndFound_Indonesian.dll`, GUID `com.teteh.lostandfoundco.indonesian`) fuegt vollstaendige Bahasa-Indonesia-Uebersetzungen (16.718 Eintraege, aktualisiert bis **Patch 1.2.2**; neuere Level nach 1.2.2 erfordern ein Woerterbuch-Update) hinzu und arbeitet konfliktfrei zusammen:

- Ist Bahasa Indonesia im Spiel aktiv, wechseln Panel (`F10`), Ecken-Badges, Wartekarte und Benachrichtigungen automatisch auf Bahasa Indonesia.
- Das 2P-Layout liegt ausschliesslich auf `F11`, sodass `F9` fuer das Hot-Reloading der Uebersetzungsmod frei bleibt.
- Installer und Uninstaller (`Install_SplitScreenCoop.bat` / `Uninstall_SplitScreenCoop.bat`) lassen vorhandene BepInEx-Dateien und fremde Plugins unberuehrt.

---

## Installation

### Methode 1: Auto-Install-ZIP (Empfohlen)

1. Lade **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip`** von der [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases)-Seite herunter.
2. Entpacke den gesamten Inhalt des Archivs direkt in deinen Hauptordner von **Lost and Found Co.** (neben `Lost and Found Co.exe`).
3. Fuehre **`Install_SplitScreenCoop.bat`** per Doppelklick aus (Konsolenausgaben auf Englisch).
   - Ist BepInEx 5 noch nicht vorhanden, installiert das Skript Unity Doorstop (`winhttp.dll`), ohne bestehende Dateien zu ueberschreiben.
   - Ist BepInEx 5 bereits vorhanden, bleibt dein bestehender Loader unveraendert und es wird nur `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll` kopiert.
4. Starte **Lost and Found Co.** normal ueber Steam.

Zum Entfernen fuehre **`Uninstall_SplitScreenCoop.bat`** aus. Vor dem Loeschen wird per SHA-256 geprueft, ob Dateien von diesem Paket stammen; sind weitere Plugins installiert, bleibt BepInEx erhalten.

### Methode 2: Manuelles Kopieren der DLL

Falls **BepInEx 5.4.x (x64)** bereits eingerichtet ist:

1. Lade **`LostAndFound_SplitScreenCoop.dll`** von [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases) herunter.
2. Kopiere die Datei nach `<Spielordner>\BepInEx\plugins\LostAndFound_SplitScreenCoop.dll`.

### Methode 3: Selbst aus dem Quellcode bauen

Die vollstaendige Anleitung steht in [docs/BUILDING.md](BUILDING.md):

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -Deploy
```

---

## Steuerung & Tastenbelegung

| Taste | Aktion |
| --- | --- |
| `F8` | Splitscreen-Koop ein-/ausschalten |
| `F11` | 2P-Layout wechseln (Links/Rechts oder Oben/Unten) |
| `F7` | Spieler 1 zwischen Maus + Tastatur und Gamepad 1 umschalten |
| `F10` | Koop-Steuerungspanel oeffnen / schliessen |
| `F6` | P2-Tastaturtestmodus umschalten (`IJKL`, Pfeile, `U`/`O`, `Enter`) |

Gamepad-Steuerung (P2-P4, sowie P1 im Gamepad-1-Modus): `Left Stick` Cursor, `Right Stick` Kamera schwenken, `LT`/`LB` & `RT`/`RB` Zoom, `A` Klicken/Ziehen, `Y` oder `L3` halten fuer HUD/Aufgabenliste, `R3` Kamera zuruecksetzen, `Start` zum Beitreten.

---

## Konfiguration & Lizenz

- Konfigurationsdatei: `<Spielordner>\BepInEx\config\com.teteh.lostandfound.splitscreencoop.cfg` (u.a. mit `[UI] UiLanguage = Auto`).
- Lizenz der Mod: [MIT License](../LICENSE) (Copyright (c) 2026 AkmalHidayat). Hinweise zu gebuendelten Komponenten (BepInEx 5, HarmonyX, Unity Doorstop) stehen in [docs/THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

> **Hinweis**: Inoffizielle Community-Mod, nicht verbunden mit oder unterstuetzt durch **Bit Egg Inc.**
