using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Spectre.Console;

namespace FikaSync;

public class Updater
{
    private readonly GitHubClient _client;
    private readonly Config _config;
    
    private const string UpdateRepo = "fanteeek/fika-profiles-sync";

    public Updater(GitHubClient client, Config config)
    {
        _client = client;
        _config = config;
    }

    public async Task CheckForUpdates()
    {
        try
        {
            CleanupOldFiles();

            if (!Version.TryParse(_config.AppVersion, out Version? currentVersion))
                currentVersion = new Version(0,0,0);
            
            var releaseInfo = await _client.GetLatestReleaseInfo(UpdateRepo);
            if (releaseInfo == null) return; 

            string tagName = releaseInfo.Value.TagName.TrimStart('v');
            string downloadUrl = releaseInfo.Value.DownloadUrl;

            if (Version.TryParse(tagName, out Version? latestVersion))
            {
                if (latestVersion > currentVersion)
                {
                    Logger.Info(Loc.Tr("Update_Found", latestVersion));

                    if (AnsiConsole.Confirm(Loc.Tr("Update_Ask"), defaultValue: true))
                        await PerformUpdate(downloadUrl);
                }
                else
                {
                    Logger.Info(Loc.Tr("Update_Latest", currentVersion));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Update_Fail", ex.Message));
        }
    }

    private async Task PerformUpdate(string url)
    {
        string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        if (string.IsNullOrEmpty(currentExe)) return;
        string tempFile = Path.Combine(_config.BaseDir, "update");

        try
        {
            // download
            await AnsiConsole.Status()
                .StartAsync(Loc.Tr("Update_Downloading"), async ctx =>
                {
                    bool success = await _client.DownloadAsset(url, tempFile);
                    if (!success) throw new Exception("Download failed");
                });
            
            // install
            Logger.Info(Loc.Tr("Update_Extracting"));

            string oldExe = currentExe + ".old";
            if (File.Exists(oldExe)) File.Delete(oldExe);
            File.Move(currentExe, oldExe);

            File.Move(tempFile, currentExe);
            Logger.Info(Loc.Tr("Update_Success"));

            await Task.Delay(1500);
            AnsiConsole.Clear();

            Process.Start(currentExe);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            string oldExe = currentExe + ".old";
            if (File.Exists(oldExe) && !File.Exists(currentExe))
                File.Move(oldExe, currentExe);
            Logger.Error(Loc.Tr("Update_Fail", ex.Message));
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    private void CleanupOldFiles()
    {
        try
        {
            string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            string oldExe = currentExe + ".old";
            if (File.Exists(oldExe)) File.Delete(oldExe);
        }catch {}
    }
}