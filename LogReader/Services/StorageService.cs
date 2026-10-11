using LogReader.Models;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace LogReader.Services;

public class StorageService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogReader");

    private static readonly string HistoryFile = Path.Combine(AppDataDir, "history.json");
    private static readonly string FavoritesFile = Path.Combine(AppDataDir, "favorites.json");

    public static readonly string ConnectionsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Connections.json");

    private const int MaxHistory = 10;

    public StorageService()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);
    }

    public bool ConnectionsFileExists() => File.Exists(ConnectionsFilePath);

    // --- Connections ---

    public List<ConnectionProfile> LoadConnections()
    {
        // Priority 1: Connections.json 在 EXE 同目錄
        if (File.Exists(ConnectionsFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConnectionsFilePath);
                return JsonConvert.DeserializeObject<ConnectionsConfig>(json)?.Connections ?? new();
            }
            catch { }
        }

        // Priority 2: 嵌入在 EXE 內的預設資料
        return LoadEmbeddedConnections();
    }

    private static List<ConnectionProfile> LoadEmbeddedConnections()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                               .FirstOrDefault(n => n.EndsWith("Connections.json"));
            if (name == null) return new();

            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<ConnectionsConfig>(json)?.Connections ?? new();
        }
        catch { return new(); }
    }

    // 只有檔案已存在才更新，不自動建立
    public void SaveConnections(List<ConnectionProfile> connections)
    {
        if (!File.Exists(ConnectionsFilePath)) return;
        WriteConnectionsFile(connections);
    }

    // 明確匯出（無論檔案是否存在都寫入）
    public void ExportConnections(List<ConnectionProfile> connections)
    {
        WriteConnectionsFile(connections);
    }

    private static void WriteConnectionsFile(List<ConnectionProfile> connections)
    {
        var config = new ConnectionsConfig { Connections = connections };
        File.WriteAllText(ConnectionsFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }

    // --- History ---

    public List<QueryHistory> LoadHistory()
    {
        if (!File.Exists(HistoryFile)) return new();
        try
        {
            var json = File.ReadAllText(HistoryFile);
            return JsonConvert.DeserializeObject<HistoryStore>(json)?.Items ?? new();
        }
        catch { return new(); }
    }

    public void SaveHistory(List<QueryHistory> history)
    {
        var store = new HistoryStore { Items = history.Take(MaxHistory).ToList() };
        File.WriteAllText(HistoryFile, JsonConvert.SerializeObject(store, Formatting.Indented));
    }

    public void AddHistory(QueryHistory item)
    {
        var list = LoadHistory();
        list.Insert(0, item);
        SaveHistory(list.Take(MaxHistory).ToList());
    }

    public void ClearHistory() => SaveHistory(new());

    public void DeleteHistoryItem(QueryHistory item)
    {
        var list = LoadHistory();
        list.RemoveAll(h => h.QueryTime == item.QueryTime && h.Keywords == item.Keywords);
        SaveHistory(list);
    }

    // --- Favorites ---

    public List<FavoriteQuery> LoadFavorites()
    {
        if (!File.Exists(FavoritesFile)) return new();
        try
        {
            var json = File.ReadAllText(FavoritesFile);
            return JsonConvert.DeserializeObject<FavoritesStore>(json)?.Items ?? new();
        }
        catch { return new(); }
    }

    public void SaveFavorites(List<FavoriteQuery> favorites)
    {
        var store = new FavoritesStore { Items = favorites };
        File.WriteAllText(FavoritesFile, JsonConvert.SerializeObject(store, Formatting.Indented));
    }

    public void AddFavorite(FavoriteQuery item)
    {
        var list = LoadFavorites();
        list.Add(item);
        SaveFavorites(list);
    }

    public void DeleteFavorite(FavoriteQuery item)
    {
        var list = LoadFavorites();
        list.RemoveAll(f => f.Name == item.Name && f.Keywords == item.Keywords);
        SaveFavorites(list);
    }
}
