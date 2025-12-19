using Spectre.Console;
using FikaSync;

class Program
{
    static async Task Main(string[] args)
    {
        //debug
        SetupLogger(args);
        var config = new Config();

        PrintHeader(config);
        config.EnsureConfiguration();

        if (!config.IsValid())
        {
            Logger.Error(Loc.Tr("Setup_Incomplete"));
            Console.ReadLine();
            return;
        }

        Logger.Info(Loc.Tr("Config_Loading"));
        Logger.Debug($"Working folder: [blue]{config.BaseDir}[/]");
        Logger.Debug($"Path to profiles: [blue]{config.GameProfilesPath}[/]");
        Logger.Debug($"GitHub Token: [blue]{(string.IsNullOrEmpty(config.GithubToken) ? "[red]Not found[/]" : $"[blue]{config.GithubToken}[/]")}[/]");
        Logger.Debug($"GitHub URL: [blue]{(string.IsNullOrEmpty(config.RepoUrl) ? "[red]Not found[/]" : $"[blue]{config.RepoUrl}[/]")}[/]");

        // git
        Logger.Info(Loc.Tr("Conn_GitHub"));
        var github = new GitHubClient(config.GithubToken);

        var updater = new Updater(github, config);
        await updater.CheckForUpdates();

        bool isAuthSuccess = await github.TestToken();

        // sync
        var syncer = new ProfileSync(config, github);
        bool shouldLaunch;
        
        string owner = "", repo = "";

        if (isAuthSuccess)
        {
            try
            {
                (owner, repo) = github.ExtractRepoInfo(config.RepoUrl);
                Logger.Debug(Loc.Tr("Repo_Target", owner, repo));

                await syncer.PerformStartupSync(owner, repo);

                shouldLaunch = true;
            }
            catch (Exception ex)
            {
                Logger.Error(Loc.Tr("Result_Error", ex.Message));
                shouldLaunch = AnsiConsole.Confirm(Loc.Tr("Start_Game_NoSync"), defaultValue: true);
            }
        }
        else
        {
            Logger.Info(Loc.Tr("Offline_Mode"));
            shouldLaunch = AnsiConsole.Confirm(Loc.Tr("Start_Game_NoSync"), defaultValue: true);
        }

        //game
        if (shouldLaunch)
        {
            syncer.CaptureSessionStartSnapshot();

            var launcher = new GameLauncher(config);
            var gamePlayedSuccessfully = await launcher.LaunchAndMonitor();

            if (isAuthSuccess && gamePlayedSuccessfully)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule(Loc.Tr("Sync_Title")));

                try 
                 {
                    await syncer.PerformShutdownSync(owner, repo);
                 }
                 catch (Exception ex)
                 {
                    Logger.Error(Loc.Tr("Result_Error", ex));
                 }
            }
        }
        else
            Logger.Info(Loc.Tr("Server_Exited"));

        Console.WriteLine();
        Logger.Info(Loc.Tr("Press_Enter"));
        Console.ReadLine();
    }

    static void SetupLogger(string[] args)
    {
        if (args.Contains("-d") || args.Contains("--debug")) Logger.Enable();
    }

    static void PrintHeader(Config config)
    {
        string v = Logger.IsDebugEnabled ? "(DEBUG)" : "";
        Logger.Info($"[white on teal] FikaSync v{config.AppVersion}{v} \n[/]");
    }
}