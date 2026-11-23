[English](../README.md) | [Bahasa Indonesia](README.id.md) | [ไทย](README.th.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | **日本語** | [한국어](README.ko.md) | [Deutsch](README.de.md)

# Lost and Found Co. - 画面分割ローカル協力プレイ MOD

**Lost and Found Co.**（Windows x64）向けの BepInEx 5 製ローカル画面分割協力 MOD です。最大 4 人まで 1 画面で同時プレイでき、プレイヤーごとの独立カメラ・仮想カーソル・入力デバイス分離・カットシーン時の自動フルスクリーン切替・多言語 UI に対応しています。

> **動作確認済みゲームバージョン**: **Lost and Found Co. Patch 1.2.2**（2026 年 10 月 27 日 / Steam Build `26480281` / Unity `6000.0.84f1` / Windows x64）。カメラ・入力・ライティング関連の内部クラスに変更がない限り、通常はマイナーアップデート後もそのまま動作します。

## 主な機能

- **2〜4 人のローカル画面分割**:
  - **2 人**: 左右分割または上下分割（`F11` でいつでも切替可能）。
  - **3 人**: 2x2 グリッド表示（右下枠は P4 参加待機カード）。
  - **4 人**: フル 2x2 クアッド分割。
- **入力分離とデバイス混在**:
  - 既定では P1 が **マウス + キーボード**、接続済みゲームパッドが自動で P2〜P4 に割り当てられます（`F7` でゲームパッド 1 を P1 に割り当てることも可能）。
  - ゲームパッドの `Start` ボタンで途中参加できます。キーボードのみでテストしたい場合は `F6` で P2 キーボードテスト（`IJKL` カーソル、矢印キー移動、`U`/`O` ズーム、`Enter` クリック）を切り替えられます。
- **独立カメラと 2D レイキャスト**:
  - 各プレイヤーの `WorldCamera` が独立してカメラ移動・ズーム・クリック/ドラッグ処理を行います（`Y` または `L3` 長押しで画面全体の HUD やリストを操作可能）。
- **カットシーン時の自動フルスクリーン (`AutoFullscreenOnCutscene`)**:
  - タイムライン演出、スナップショット、会話中のカメラロック、階層移動などが始まると自動でフルスクリーンへ切り替え、終了後に分割画面へ復帰します。
- **URP HDR Bloom & SmartLighting2D 対応**:
  - HDR レンダーテクスチャ（`RGB111110Float`）と `FunkyCode` SmartLighting2D のライトマップ同期により、分割時も照明効果を保持します。
- **自動多言語 UI**:
  - ゲーム内の言語設定（`en`, `th`, `zh-CN`, `zh-TW`, `ja`, `ko`, `de`、およびインドネシア語 MOD 導入時の `id`、さらに予約スロット `fr`, `es-ES`, `it`, `ru`, `sv-SE`, `tr`, `pt-BR`, `vi`）に自動追従し、言語に応じた Windows システムフォント（Yu Gothic UI / Meiryo / CJK / Thai フォント等）を適用します。

## 制作した関連 MOD と互換性

当方制作の関連 MOD **[Lost and Found Co. インドネシア語ローカライズ MOD](https://github.com/AkmalHidayat/LostAndFound-IndonesianMod)**（`LostAndFound_Indonesian.dll` / `com.teteh.lostandfoundco.indonesian`、全 16,718 項目を **Patch 1.2.2** まで翻訳対応済み。Patch 1.2.2 以降で追加される新ステージは辞書更新まで未対応）と同時に導入しても競合しないよう設計されています:

- ゲーム言語がインドネシア語に切り替わると、MOD の制御パネル（`F10`）、コーナーバッジ、待機表示、トースト通知も自動でインドネシア語に切り替わります。
- 2P レイアウト切替キーは `F11` のみに割り当てられており、辞書リロード用の `F9` と衝突しません。
- インストーラー / アンインストーラー（`Install_SplitScreenCoop.bat` / `Uninstall_SplitScreenCoop.bat`）は既存のローダーや他 MOD のファイルを上書き・削除しません。

---

## インストール方法

### 方法 1: 自動インストール ZIP（推奨）

1. [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases) から **`LostAndFound-SplitScreenCoop-v1.0.4-AutoInstall.zip`** をダウンロードします。
2. ZIP の中身を **Lost and Found Co.** のゲームフォルダ直下（`Lost and Found Co.exe` と同じ場所）へ展開します。
3. **`Install_SplitScreenCoop.bat`** をダブルクリックして実行します（案内表示は英語です）。
   - BepInEx 5 が未導入の場合は空いている Unity Doorstop プロキシ（`winhttp.dll`）を使って上書きなしでセットアップします。
   - すでに BepInEx 5 や他 MOD が入っている場合は既存環境を壊さず `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll` のみを追加します。
4. Steam から通常どおりゲームを起動します。

アンインストールは同フォルダ内の **`Uninstall_SplitScreenCoop.bat`** を実行してください。SHA-256 を検証したうえで削除し、他のプラグインが存在する場合は BepInEx を残します。

### 方法 2: 手動ドラッグ＆ドロップ用 ZIP

`.bat` スクリプトを使わず手動で配置したい場合:

1. [GitHub Releases](https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop/releases) から **`LostAndFound-SplitScreenCoop-v1.0.4-Manual.zip`** をダウンロードし、一時フォルダに展開します。
2. ゲームフォルダの状況に合わせてコピーします:
   - **まだ BepInEx が入っていない場合**: `winhttp.dll`、`doorstop_config.ini`、`BepInEx` フォルダをすべて `<ゲームフォルダ>`（`Lost and Found Co.exe` の横）へドラッグ＆ドロップします。
   - **すでに BepInEx 5 やインドネシア語 MOD を導入済みの場合**: `BepInEx\plugins\LostAndFound_SplitScreenCoop.dll` のみを `<ゲームフォルダ>\BepInEx\plugins\` へコピーします（既存の `winhttp.dll` や `doorstop_config.ini` は上書きしないでください）。

### 方法 3: ソースコードからビルド

手順の詳細は [docs/BUILDING.md](BUILDING.md) をご覧ください:

```powershell
git clone https://github.com/AkmalHidayat/LostAndFound-SplitScreenCoop.git
cd LostAndFound-SplitScreenCoop
powershell -ExecutionPolicy Bypass -File ".\scripts\build.ps1" -Deploy
```

---

## 操作方法・ホットキー

| キー | 機能 |
| --- | --- |
| `F8` | 画面分割協力プレイの ON / OFF |
| `F11` | 2 人分割レイアウト切替（左右 / 上下） |
| `F7` | P1 の操作を マウス+キーボード / ゲームパッド 1 で切替 |
| `F10` | 協力コントロールパネルの開閉 |
| `F6` | P2 キーボードテストの ON / OFF（`IJKL` カーソル、矢印移動、`U`/`O` ズーム、`Enter` クリック） |

ゲームパッド操作（P2〜P4、およびゲームパッド 1 モード時の P1）: `左スティック` カーソル移動、`右スティック` カメラ移動、`LT`/`LB` & `RT`/`RB` ズーム、`A` クリック/ドラッグ、`Y` または `L3` 長押しで HUD・タスク一覧へアクセス、`R3` カメラ位置リセット、`Start` 途中参加。

---

## 設定とライセンス

- 設定ファイル: `<ゲームフォルダ>\BepInEx\config\com.teteh.lostandfound.splitscreencoop.cfg`（`[UI]` の `UiLanguage` から言語固定も可能）。
- MOD 本体のコードは [MIT License](../LICENSE)（Copyright (c) 2026 AkmalHidayat）で提供されます。同梱される BepInEx 5 / HarmonyX / Unity Doorstop のライセンス表記は [docs/THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。

> **免責事項**: 本 MOD は非公式のコミュニティ制作物であり、**Bit Egg Inc.** とは一切関係ありません。
