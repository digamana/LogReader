# LogReader

> 🤖 本專案由 AI（Claude）協助開發  
> 🤖 This project was developed with AI assistance (Claude by Anthropic)

---

## ⚠️ Windows SmartScreen 警告 / SmartScreen Warning

**中文：** 由於本程式尚未購買程式碼簽署憑證，Windows 可能顯示「無法辨識的應用程式」警告。程式本身安全無虞，原始碼已完整公開。略過方式：點擊「**更多資訊**」→「**仍要執行**」。

**English:** This application has not been code-signed with a trusted certificate. Windows SmartScreen may show an "unrecognized app" warning. The application is safe to run and the full source code is publicly available here. To bypass: click **"More info"** → **"Run anyway"**.

---

## 中文說明

### 簡介

**LogReader** 是一套以 WPF（.NET 6）開發的桌面工具，專為開發人員與維運人員設計，提供高效率的 Log 查詢與分析功能。支援本機與遠端（SSH/SFTP）兩種來源，並具備多分頁、關鍵字篩選、歷史紀錄、我的最愛等實用功能。

### 系統需求

- Windows 10 / 11（64-bit）
- [.NET 6 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)（若使用非 self-contained 發布版本）

### 功能一覽

| 功能 | 說明 |
|------|------|
| 本機路徑讀取 | 瀏覽並選擇本機資料夾，自動掃描 `.log` / `.txt` 檔案 |
| 遠端 Log 下載 | 透過 SSH + SFTP 連接遠端主機，下載指定日期的 Log 檔 |
| 日期範圍篩選 | 設定開始／結束日期，預設為今天 |
| 關鍵字查詢 | 每行輸入一個關鍵字，支援「包含任一（OR）」與「包含所有（AND）」兩種模式 |
| 二次篩選 | 在已查詢的結果中再次過濾，類似 Notepad++ 的 Find in Results |
| 多分頁查詢 | 可同時開啟多個查詢分頁，各自獨立設定條件與查看結果 |
| 查詢歷史紀錄 | 自動儲存最近 10 筆查詢，可一鍵套用或選擇性清除 |
| 我的最愛 | 將常用的查詢條件命名儲存，隨時快速套用 |
| 匯出 TXT | 將查詢結果輸出至 `Downloads` 資料夾（.txt 格式） |
| 連線管理 | 新增、複製、刪除遠端 SSH/SFTP 連線，支援 Test / Prod / Stage / PreProd 環境分類 |
| 內嵌連線設定 | 連線設定預設嵌入於 EXE 中；若 EXE 同目錄存在 `Connections.json`，則優先讀取外部檔案 |
| 進度條顯示 | 下載遠端 Log 及本機掃描時，即時顯示進度（BackgroundWorker，不凍結畫面） |
| 單一 EXE 發布 | 使用 .NET 6 PublishSingleFile，輸出為單一 `.exe` 檔案，不需額外 DLL |

### 遠端連線說明

- 透過 SSH.NET（SFTP）進行連線與檔案下載
- 下載的 Log 暫存於 `C:\temp\LogReader\{連線名稱}_{時間戳記}\`
- 若 `C:\temp` 不存在，程式會自動建立
- 下載完成後，需另外按「開始查詢」按鈕才會進行分析（下載與查詢為獨立操作）

### Connections.json 機制

```
優先順序：外部 Connections.json（EXE 同目錄） > EXE 內嵌預設值
```

- 程式內建預設連線設定（編譯時嵌入）
- 可在「連線管理」視窗點選「**產生 Connections.json**」按鈕，將目前連線匯出至外部檔案
- 匯出後，每次儲存連線設定都會自動同步至此檔案
- 若無外部 `Connections.json`，關閉視窗不會自動產生

### 連線設定格式（Connections.json）

```json
{
  "Connections": [
    {
      "Name": "DEV Server",
      "Type": "Test",
      "ProjName": "MyProject",
      "Memo": "備註說明",
      "Host": "192.168.1.100",
      "SshPort": 22,
      "Username": "admin",
      "Password": "yourpassword",
      "RemoteLogPath": "/var/log/app/"
    }
  ]
}
```

### 資料儲存位置

| 資料 | 位置 |
|------|------|
| 查詢歷史紀錄 | `%AppData%\LogReader\history.json` |
| 我的最愛 | `%AppData%\LogReader\favorites.json` |
| 連線設定（外部） | EXE 同目錄下的 `Connections.json` |

### 建置與發布

```bash
# 建置
dotnet build -c Release

# 發布為單一 EXE（需目標機器安裝 .NET 6 Desktop Runtime）
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 發布為完全獨立 EXE（不需安裝 Runtime，但檔案較大）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### NuGet 套件

| 套件 | 版本 | 用途 |
|------|------|------|
| [SSH.NET](https://github.com/sshnet/SSH.NET) | 2026.0.0 | SSH / SFTP 遠端連線與檔案下載 |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.4 | JSON 序列化（連線設定、歷史、最愛） |
| [Syroot.Windows.IO.KnownFolders](https://github.com/Syroot/KnownFolders) | 1.2.3 | 取得 Windows 已知資料夾路徑（Downloads） |

### 專案架構

```
LogReader/
├── Models/              # 資料模型（ConnectionProfile、LogEntry 等）
├── ViewModels/          # MVVM ViewModel（MainViewModel、QueryTabViewModel）
├── Views/               # WPF XAML 畫面
│   ├── QueryTabView           # 主查詢頁面（UserControl）
│   ├── ConnectionManagerWindow  # 連線管理視窗
│   ├── HistoryWindow          # 歷史紀錄視窗
│   ├── FavoritesWindow        # 我的最愛視窗
│   └── AddFavoriteDialog      # 加入最愛對話框
├── Services/            # 商業邏輯
│   ├── StorageService         # JSON 讀寫（歷史、最愛、連線）
│   ├── LogSearchService       # 本機 Log 掃描與關鍵字搜尋
│   └── SshFtpService          # SSH/SFTP 遠端下載
├── Helpers/             # 工具類別（RelayCommand、InverseBoolConverter）
└── Connections.json     # 預設連線設定（嵌入於 EXE）
```

---

## English Description

### Overview

**LogReader** is a WPF desktop application built with .NET 6, designed for developers and operations teams to efficiently query and analyze log files. It supports both local and remote (SSH/SFTP) log sources, with features including multi-tab queries, keyword filtering, query history, and favorites.

### System Requirements

- Windows 10 / 11 (64-bit)
- [.NET 6 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (if using the framework-dependent build)

### Features

| Feature | Description |
|---------|-------------|
| Local file reading | Browse and select a local folder; automatically scans for `.log` / `.txt` files |
| Remote log download | Connect via SSH + SFTP to download log files filtered by date |
| Date range filter | Set start/end dates; defaults to today |
| Keyword search | One keyword per line; supports **Any keyword (OR)** and **All keywords (AND)** modes |
| Second-level filter | Filter results further without re-querying, similar to Notepad++ "Find in Results" |
| Multi-tab queries | Open multiple independent query tabs simultaneously |
| Query history | Automatically saves the last 10 queries; one-click reuse or selective deletion |
| Favorites | Save named query presets for quick reuse |
| Export to TXT | Export results to the `Downloads` folder in `.txt` format |
| Connection manager | Add, copy, and delete SSH/SFTP connections with environment type tagging |
| Embedded connection config | Default connections are embedded in the EXE; external `Connections.json` takes priority if present |
| Progress bar | Real-time progress during remote download and local file scanning (BackgroundWorker, non-blocking UI) |
| Single-file publish | Published as a single `.exe` using .NET 6 `PublishSingleFile` |

### Remote Connection Notes

- Uses SSH.NET (SFTP) for connection and file transfer
- Downloaded logs are cached to `C:\temp\LogReader\{ConnectionName}_{Timestamp}\`
- `C:\temp` is created automatically if it does not exist
- Downloading and querying are **separate operations** — download first, then click "Start Search"

### Connections.json Behavior

```
Priority: External Connections.json (same folder as EXE) > Embedded default
```

- Default connections are embedded in the EXE at compile time
- Click **"Generate Connections.json"** in the Connection Manager to export current connections to an external file
- Once the file exists, saving any connection automatically syncs to it
- Closing the window without exporting will **not** auto-create the file

### Connection Config Format

```json
{
  "Connections": [
    {
      "Name": "DEV Server",
      "Type": "Test",
      "ProjName": "MyProject",
      "Memo": "Optional notes",
      "Host": "192.168.1.100",
      "SshPort": 22,
      "Username": "admin",
      "Password": "yourpassword",
      "RemoteLogPath": "/var/log/app/"
    }
  ]
}
```

### Data Storage Locations

| Data | Location |
|------|----------|
| Query history | `%AppData%\LogReader\history.json` |
| Favorites | `%AppData%\LogReader\favorites.json` |
| External connection config | `Connections.json` next to the EXE |

### Build & Publish

```bash
# Build
dotnet build -c Release

# Publish as single EXE (requires .NET 6 Desktop Runtime on target machine)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Publish as fully self-contained EXE (no Runtime required, larger file size)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| [SSH.NET](https://github.com/sshnet/SSH.NET) | 2026.0.0 | SSH / SFTP remote connection and file download |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) | 13.0.4 | JSON serialization for connections, history, and favorites |
| [Syroot.Windows.IO.KnownFolders](https://github.com/Syroot/KnownFolders) | 1.2.3 | Resolve Windows known folder paths (e.g. Downloads) |

### Project Structure

```
LogReader/
├── Models/              # Data models (ConnectionProfile, LogEntry, etc.)
├── ViewModels/          # MVVM ViewModels (MainViewModel, QueryTabViewModel)
├── Views/               # WPF XAML views
│   ├── QueryTabView           # Main query tab (UserControl)
│   ├── ConnectionManagerWindow  # SSH/SFTP connection manager
│   ├── HistoryWindow          # Query history window
│   ├── FavoritesWindow        # Favorites window
│   └── AddFavoriteDialog      # Add-to-favorites dialog
├── Services/            # Business logic
│   ├── StorageService         # JSON persistence (history, favorites, connections)
│   ├── LogSearchService       # Local log scanning and keyword search
│   └── SshFtpService          # SSH/SFTP remote download
├── Helpers/             # Utilities (RelayCommand, InverseBoolConverter)
└── Connections.json     # Default connection config (embedded in EXE)
```

---

## AI Development Note

本專案由開發者與 **Claude（Anthropic）** AI 協作完成，包含架構設計、程式碼生成、除錯與功能迭代。  
This project was built collaboratively with **Claude (Anthropic)** AI, covering architecture design, code generation, debugging, and iterative feature development.

---

## License

See [LICENSE.txt](LICENSE.txt)
