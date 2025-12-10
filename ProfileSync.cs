using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace FikaSync;

public class ProfileSync
{
    private readonly Config _config;
    private readonly GitHubClient _client;
    
    private readonly HashSet<string> _pendingUploads = new();
    
    private Dictionary<string, long> _sessionStartTimestamps = new();

    public ProfileSync(Config config, GitHubClient client)
    {
        _config = config;
        _client = client;
    }

    public async Task PerformStartupSync(string owner, string repo)
    {
        string tempZip = Path.Combine(_config.BaseDir, "temp", "repo.zip");
        string extractPath = Path.Combine(_config.BaseDir, "temp", "extracted");

        try
        {
            Logger.Debug(Loc.Tr("Sync_Downloading"));
            bool downloaded = await AnsiConsole.Status().StartAsync(Loc.Tr("Sync_Downloading"), async ctx => 
                await _client.DownloadRepository(owner, repo, tempZip));

            if (!downloaded) throw new Exception(Loc.Tr("Result_Error"));

            string? contentDir = FileManager.ExtractZip(tempZip, extractPath);

            var remoteFiles = contentDir != null ? FileManager.FindProfiles(contentDir) : new List<string>();

            if (remoteFiles.Count == 0)
                Logger.Info(Loc.Tr("Sync_NoProfiles"));
            else
                Logger.Info(Loc.Tr("Sync_Found", remoteFiles.Count));

            ProcessDownloadedFiles(remoteFiles);
        }
        finally
        {
            FileManager.ForceDeleteDirectory(Path.Combine(_config.BaseDir, "temp"));
        }
    }

    public void CaptureSessionStartSnapshot()
    {
        _sessionStartTimestamps.Clear();
        if (!Directory.Exists(_config.GameProfilesPath)) return;

        foreach (var file in Directory.GetFiles(_config.GameProfilesPath, "*.json"))
        {
            string content = File.ReadAllText(file);
            _sessionStartTimestamps[Path.GetFileName(file)] = GetTimestamp(content);
        }
    }

    public async Task PerformShutdownSync(string owner, string repo)
    {
        Logger.Info(Loc.Tr("Sync_Checking"));
        
        var table = new Table();
        table.Title(Loc.Tr("Sync_Report_Title")).AddColumn(Loc.Tr("Sync_Profile_Title")).AddColumn(Loc.Tr("Sync_Reason_Title")).AddColumn(Loc.Tr("Sync_Result_Title")).Border(TableBorder.Rounded);

        if (!Directory.Exists(_config.GameProfilesPath))
        {
            Logger.Info(Loc.Tr("Sync_NoLocal"));
            return;
        }

        var localFiles = Directory.GetFiles(_config.GameProfilesPath, "*.json");
        bool hasActivity = false;
        int sentCount = 0;

        foreach (var file in localFiles)
        {
            string fileName = Path.GetFileName(file);
            string content = File.ReadAllText(file);
            long currentTs = GetTimestamp(content);

            bool shouldUpload = false;
            string reason = "";

            long startTs = _sessionStartTimestamps.ContainsKey(fileName) ? _sessionStartTimestamps[fileName] : 0;
            
            if (currentTs > startTs)
            {
                shouldUpload = true;
                reason = Loc.Tr("Reason_NewProgress");
            }
            else if (_pendingUploads.Contains(fileName))
            {
                if (await IsSafeToUploadPending(owner, repo, fileName, currentTs))
                {
                    shouldUpload = true;
                    reason = Loc.Tr("Reason_Pending");
                }
                else
                {
                    table.AddRow(fileName, Loc.Tr("Result_Conflict"), Loc.Tr("Result_RemoteNewer"));
                    continue;
                }
            }

            if (shouldUpload)
            {
                hasActivity = true;
                byte[] bytes = await File.ReadAllBytesAsync(file);
                string repoPath = $"profiles/{fileName}";

                if (await _client.UploadFile(owner, repo, repoPath, bytes))
                {
                    table.AddRow(fileName, reason, Loc.Tr("Result_Sent"));
                    sentCount++;
                }
                else
                {
                    table.AddRow(fileName, reason, Loc.Tr("Result_Error"));
                }
            }
        }

        if (hasActivity) AnsiConsole.Write(table);
        else Logger.Info(Loc.Tr("Sync_AllDone"));
    }

    private void ProcessDownloadedFiles(List<string> remoteFiles)
    {
        var table = new Table().AddColumn(Loc.Tr("Table_File")).AddColumn(Loc.Tr("Table_Status")).AddColumn(Loc.Tr("Table_Action"));
        int updated = 0;

        if (!Directory.Exists(_config.GameProfilesPath)) Directory.CreateDirectory(_config.GameProfilesPath);

        var processedFiles = new HashSet<string>();

        foreach (var remotePath in remoteFiles)
        {
            string fileName = Path.GetFileName(remotePath);
            processedFiles.Add(fileName);
            string localPath = Path.Combine(_config.GameProfilesPath, fileName);

            string remoteContent = File.ReadAllText(remotePath);
            long remoteTs = GetTimestamp(remoteContent);

            long localTs = 0;
            string localHash = "";
            if (File.Exists(localPath))
            {
                string localContent = File.ReadAllText(localPath);
                localTs = GetTimestamp(localContent);
                localHash = GetFileHash(localPath);
            }
            
            string remoteHash = GetFileHash(remotePath);

            if (localHash == remoteHash)
            {
                table.AddRow(fileName, Loc.Tr("Status_Synced"), Loc.Tr("Action_Pass"));
                continue;
            }

            if (localTs > remoteTs)
            {
                _pendingUploads.Add(fileName);
                table.AddRow(fileName, Loc.Tr("Status_LocalNewer"), Loc.Tr("Action_WillUpload"));
            }
            else
            {
                ApplyUpdate(remotePath, localPath);
                table.AddRow(fileName, Loc.Tr("Status_Update"), Loc.Tr("Action_Downloaded"));
                updated++;
            }
        }

        var localFiles = Directory.GetFiles(_config.GameProfilesPath, "*.json");
        foreach(var file in localFiles)
        {
            string name = Path.GetFileName(file);
            if (!processedFiles.Contains(name))
            {
                _pendingUploads.Add(name);
                table.AddRow(name, Loc.Tr("Status_NewLocal"), Loc.Tr("Action_WillUpload"));
            }
        }

        AnsiConsole.Write(table);
        if (updated > 0) Logger.Info(Loc.Tr("Sync_Updated_Count", updated));
    }

    private void ApplyUpdate(string source, string dest)
    {
        try
        {
            if (File.Exists(dest)) CreateBackup(dest);
            File.Copy(source, dest, true);
            
            var dt = File.GetLastWriteTime(source);
            File.SetLastWriteTime(dest, dt);
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
        }
    }

    private async Task<bool> IsSafeToUploadPending(string owner, string repo, string fileName, long localTs)
    {
        AnsiConsole.MarkupLine(Loc.Tr("Verify_Remote"), fileName);
        
        byte[]? remoteBytes = await _client.DownloadFileContent(owner, repo, $"profiles/{fileName}");
        
        if (remoteBytes == null) return true;

        try
        {
            string remoteJson = Encoding.UTF8.GetString(remoteBytes);
            long remoteTs = GetTimestamp(remoteJson);
            
            Logger.Debug($"Verify {fileName}: LocalTS={localTs}, RemoteTS={remoteTs}");

            return localTs > remoteTs;
        }
        catch
        {
            return false; 
        }
    }

    private long GetTimestamp(string jsonContent)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonContent)) return 0;
            var node = JsonNode.Parse(jsonContent);
            return node?["characters"]?["pmc"]?["Hideout"]?["sptUpdateLastRunTimestamp"]?.GetValue<long>() ?? 0;
        }
        catch { return 0; }
    }

    private string GetFileHash(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private void CreateBackup(string filePath)
    {
        try
        {
            string ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string dir = Path.Combine(_config.BaseDir, "backups", ts);
            Directory.CreateDirectory(dir);
            File.Copy(filePath, Path.Combine(dir, Path.GetFileName(filePath)));
        }
        catch { }
    }
}