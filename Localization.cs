using System.Globalization;
using System.Linq.Expressions;

namespace FikaSync;

public static class Loc
{
    private static Dictionary<string, string> _currentStrings;

    private static readonly Dictionary<string, string> _en = new()
    {
        // app general
        {"App_Started", "Application started. Args: {0}"},
        {"Debug_Enabled", "Debug mode enabled via arguments"},
        {"Config_Loading", "Loading configuration..."},
        {"Setup_Incomplete", "Setup not complete. Exit."},
        {"Conn_GitHub", "Connecting to GitHub..."},
        {"Repo_Target", "Target repository: [blue]{0}/{1}[/]" }, // debug
        {"Offline_Mode", "[yellow]![/] Offline mode (GitHub not reachable)."},
        {"Start_Game_Question", "Start the game?"},
        {"Start_Game_NoSync", "Start the game without synchronization?"},
        {"Launch_Canceled", "[gray]Launch canceled.[/]"},
        {"Press_Enter", "Press [blue]Enter[/] to exit."},
        {"Result_Error", "[red]Error: {0}[/]"},

        // config
        {"Token_NotFound", "[yellow]![/] GitHub Token not found."},
        {"Token_Prompt", "Enter your [green]GitHub PAT[/]:"},
        {"Token_Invalid", "[white on red]×[/] The token is too short!"},
        {"Url_NotFound", "[yellow]![/] Repository URL not found."},
        {"Url_Prompt", "Enter [green]HTTPS URL[/] repository:"},
        {"Url_Invalid", "[white on red]×[/] The link must begin with https://github.com/"},
        {"Config_Saved", "[green]√[/] Settings are saved in the .env file."},
        {"Env_Error", "[white on red]×[/] Error writing .env: {0}"},
        {"Ignore_Create", "[blue].fikaignore[/] file was created!"},
        {"Ignore_Loaded", "[gray]Loaded .fikaignore: {0} rules.[/]"},
        {"Ignore_File", "[gray]Ignored file: {0}[/]"},
        {"Failed_Parse", "Failed to parse config {0}: {1}"},
        {"Detected_SPT3", "SPT folder not found. Using mod structure for profiles from version 3.11.x"},
        {"Detected_SPT4", "SPT folder found. Using mod structure for profiles from version 4.x.x"},

        // updater
        {"Update_Available", "[bold red]UPDATE AVAILABLE[/]"},
        {"Update_Body", "[yellow]New version available:[/] [green]v{0}[/]\nYour version: [gray]v{1}[/]\n\nDownload: [blue underline]{2}[/]"},
        {"Update_Latest", "[gray]The program version is up to date. (v{0})[/]"},
        {"Update_Found", "[bold yellow]New version found:[/] [green]v{0}[/]"},
        {"Update_Ask", "Do you want to update now?"},
        {"Update_Downloading", "[gray]Downloading update...[/]"},
        {"Update_Extracting", "[gray]Extracting update...[/]"},
        {"Update_Success", "[green]Update successful! The application will restart.[/]"},
        {"Update_Fail", "[red]Update failed: {0}[/]"},
        {"Update_AssetNotFound", "[red]Release asset not found in the repository![/]"},
        {"Update_Install", "[gray]Installing update...[/]"},
        {"Update_Extract_Failed", "Failed to extract update archive."},
        
        // sync
        {"Sync_Downloading", "[gray]Downloading cloud profiles...[/]"},
        {"Sync_DownloadFail", "Failed to download repository."},
        {"Sync_NoProfiles", "[yellow]![/] No profiles found in the cloud (Repository is empty)."},
        {"Sync_Found", "[bold]Profiles in cloud:[/] {0}"},
        {"Sync_Updated_Count", "[green]Updated {0} profiles from cloud.[/]"},
        {"Sync_Backup", "[green] Created backup: [/] {0}"},
        {"Sync_Backup_Failed", "Failed backup: [/] {0}"},
        {"Sync_Backup_DeletedOld", "Deleted old backup: [/] {0}"},
        {"Sync_Backup_Failed_DeletedOld", "Failed deleted old backup: [/] {0}: {1}"},
        {"Sync_Title", "[yellow]Synchronization[/]"},
        {"Sync_Checking", "[gray]Checking for changes to upload...[/]"},
        {"Sync_Report_Title", "Synchronization Report"},
        {"Sync_NoLocal", "[gray]No local profiles found.[/]"},
        {"Sync_AllDone", "[gray]Everything is synchronized.[/]"},
        {"Verify_Remote", "[gray]Verifying remote version: {0}...[/]"},

        // sync_table
        {"Table_File", "File"},
        {"Table_Status", "Status"},
        {"Table_Action", "Action"},
        {"Status_Synced", "[green]Synced[/]"},
        {"Status_LocalNewer", "[blue]Local Newer[/]"},
        {"Status_NewLocal", "[green]New Local[/]"},
        {"Status_Update", "[yellow]Update[/]"},
        {"Action_Pass", "[gray]-[/]"},
        {"Action_WillUpload", "[yellow]Will Upload Later[/]"},
        {"Action_Downloaded", "[green]Downloaded[/]"},
        {"Sync_Profile_Title", "Profile"}, 
        {"Sync_Reason_Title", "Reasone"}, 
        {"Sync_Result_Title", "Result"},
        {"Reason_NewProgress", "[green]New Progress[/]"},
        {"Reason_Pending", "[blue]Pending Sync[/]"},
        {"Result_Conflict", "[red]Conflict[/]"},
        {"Result_RemoteNewer", "Remote is newer now"},
        {"Result_Sent", "[green]Sent[/]"},

        // game
        {"Server_NotFound", "Server file not found: {0}"},
        {"Config_Found", "[gray]Configuration found:[/] {0} -> [blue]{1}:{2}[/]"},
        {"Config_Default", "[gray]Configs not found, using default:[/]{0}:{1}"},
        {"Game_Starting", "[yellow]Starting the game[/]"},
        {"Server_Starting", "[gray]Starting SPT Server...[/]"},
        {"Server_Process_Fail", "Failed to start the server process!"},
        {"Server_Waiting", "Waiting for the server to start up..."},
        {"Server_Loading", "Loading server... {0}s"},
        {"Server_Exited", "The server shut down unexpectedly!"},
        {"Server_Timeout", "[yellow]![/] Server wait timeout.[/]"},
        {"Server_Success", "[green]√[/] The server has successfully booted up [green]{0}:{1}[/]"},
        {"Launcher_Opening", "[gray]Opening Launcher...[/]"},
        {"Launcher_NotFound", "Launcher not found!"},
        {"Game_Started_Title", "The game has started"},
        {"Game_Close_Instruction", "Press [bold red]ENTER[/] in this window to close the server and synchronize the profile."},
        {"Server_Stopping", "[gray]I'm shutting down the server...[/]"},
        {"Server_Stopped", "[green]√[/] The server has been shut down."},
        
        // git
        {"Auth_Success", "[green]√[/] Authorized as: [bold]{0}[/]"},
        {"File_Sent", "[green]√[/] File sent: {0}"},
        {"Progress_Downloading", "Downloading..."},
        {"Sync_EmptyRepo", "[yellow]![/] Repository might be empty. Initializing..."},
        {"Create_Readme_Failed", "Failed to create Readme.md"},
    };

    private static readonly Dictionary<string, string> _ru = new()
    {
        // general
        {"App_Started", "Приложение запущено. Аргументы: {0}"},
        {"Debug_Enabled", "Режим отладки включен через аргументы"},
        {"Config_Loading", "Загрузка конфигурации..."},
        {"Setup_Incomplete", "Настройка не завершена. Выход."},
        {"Conn_GitHub", "Подключение к GitHub..."},
        {"Repo_Target", "Целевой репозиторий: [blue]{0}/{1}[/]" },
        {"Offline_Mode", "[yellow]![/] Офлайн режим (нет доступа к GitHub)."},
        {"Start_Game_Question", "Запустить игру?"},
        {"Start_Game_NoSync", "Запустить игру без синхронизации?"},
        {"Launch_Canceled", "[gray]Запуск отменен.[/]"},
        {"Press_Enter", "Нажмите [blue]Enter[/], чтобы выйти."},
        {"Result_Error", "Ошибка: {0}"},
        
        // config
        {"Token_NotFound", "[yellow]![/] GitHub Token не найден."},
        {"Token_Prompt", "Введите ваш [green]GitHub PAT[/]:"},
        {"Token_Invalid", "[white on red]×[/] Токен слишком короткий!"},
        {"Url_NotFound", "[yellow]![/] URL репозитория не найден."},
        {"Url_Prompt", "Введите [green]HTTPS URL[/] репозитория:"},
        {"Url_Invalid", "[white on red]×[/] Ссылка должна начинаться с https://github.com/"},
        {"Config_Saved", "[green]√[/] Настройки сохранены в .env файл."},
        {"Env_Error", "[white on red]×[/] Ошибка записи .env: {0}"},
        {"Ignore_Create", "[blue].fikaignore[/] файл был создан!"},
        {"Ignore_Loaded", "[gray]Загружен .fikaignore: {0} правил.[/]"},
        {"Ignore_File", "[gray]Игнорируется файл: {0}[/]"},
        {"Failed_Parse", "Не удалось разобрать конфигурацию {0}: {1}"},
        {"Detected_SPT3", "Папка SPT не найдена. Использование структуры модов для профилей из версии 3.11.x"},
        {"Detected_SPT4", "Папка SPT найдена. Использование структуры модов для профилей из версии 4.x.x"},

        // updater
        {"Update_Available", "[bold red]ДОСТУПНО ОБНОВЛЕНИЕ[/]"},
        {"Update_Body", "[yellow]Новая версия:[/] [green]v{0}[/]\nВаша версия: [gray]v{1}[/]\n\nСкачать: [blue underline]{2}[/]"},
        {"Update_Latest", "[gray]У вас последняя версия программы. (v{0})[/]"},
        {"Update_Found", "[bold yellow]Найдена новая версия:[/] [green]v{0}[/]"},
        {"Update_Ask", "Хотите обновиться сейчас?"},
        {"Update_Downloading", "[gray]Скачивание обновления...[/]"},
        {"Update_Extracting", "[gray]Распаковка обновления...[/]"},
        {"Update_Success", "[green]Обновление завершено! Приложение будет перезапущено.[/]"},
        {"Update_Fail", "[red]Ошибка обновления: {0}[/]"},
        {"Update_AssetNotFound", "[red]Файл обновления не найден в репозитории![/]"},
        {"Update_Install", "[gray]Установка обновления...[/]"},
        {"Update_Extract_Failed", "Не удалось извлечь архив обновлений."},

        // sync
        {"Sync_Title", "[yellow]Синхронизация[/]"},
        {"Sync_Downloading", "[gray]Загрузка профилей из облака...[/]"},
        {"Sync_DownloadFail", "Не удалось скачать репозиторий."},
        {"Sync_NoProfiles", "[yellow]![/] В облаке нет профилей (Репозиторий пуст)."},
        {"Sync_Found", "[bold]Профилей в облаке:[/] {0}"},
        {"Sync_Updated_Count", "[green]Обновлено {0} профилей из облака.[/]"},
        {"Sync_Backup", "[green] Сделана резервная копия - [/] {0}"},
        {"Sync_Checking", "[gray]Проверка изменений для отправки...[/]"},
        {"Sync_NoLocal", "[gray]Локальные профили не найдены.[/]"},
        {"Sync_AllDone", "[gray]Всё синхронизировано.[/]"},
        {"Verify_Remote", "[gray]Проверка версии на сервере: {0}...[/]"},

        // table
        {"Table_File", "Файл"},
        {"Table_Status", "Статус"},
        {"Table_Action", "Действие"},
        {"Status_Synced", "[green]Актуальный[/]"},
        {"Status_LocalNewer", "[blue]Локальный новее[/]"},
        {"Status_NewLocal", "[green]Новый локальный[/]"},
        {"Status_Update", "[yellow]Обновить[/]"},
        {"Action_WillUpload", "[yellow]Будет отправлен[/]"},
        {"Action_Downloaded", "[green]Загружен[/]"},
        {"Sync_Report_Title", "Отчет синхронизации"},
        {"Sync_Profile_Title", "Профиль"}, 
        {"Sync_Reason_Title", "Причина"},
        {"Sync_Result_Title", "Результат"},
        {"Reason_NewProgress", "[green]Новый прогресс[/]"},
        {"Reason_Pending", "[blue]Отложенная синхронизация[/]"},
        {"Result_Conflict", "[red]Конфликт[/]"},
        {"Result_RemoteNewer", "В облаке новее"},
        {"Result_Sent", "[green]Отправлен[/]"},
        
        // game
        {"Server_NotFound", "Файл сервера не найден: {0}"},
        {"Config_Found", "[gray]Конфиг найден:[/] {0} -> [blue]{1}:{2}[/]"},
        {"Config_Default", "[gray]Конфиг не найден, используем:[/]{0}:{1}"},
        {"Game_Starting", "[yellow]Запуск игры[/]"},
        {"Server_Starting", "[gray]Запуск сервера SPT...[/]"},
        {"Server_Process_Fail", "Не удалось запустить процесс сервера!"},
        {"Server_Waiting", "Ожидание запуска сервера..."},
        {"Server_Loading", "Загрузка сервера... {0}c"},
        {"Server_Exited", "Сервер неожиданно завершил работу!"},
        {"Server_Timeout", "[yellow]![/] Время ожидания сервера истекло.[/]"},
        {"Server_Success", "[green]√[/] Сервер успешно запущен [green]{0}:{1}[/]"},
        {"Launcher_Opening", "[gray]Открываем Лаунчер...[/]"},
        {"Launcher_NotFound", "Лаунчер не найден!"},
        {"Game_Started_Title", "Игра запущена"},
        {"Game_Close_Instruction", "Нажмите [bold red]ENTER[/] в этом окне, чтобы закрыть сервер и синхронизировать профиль."},
        {"Server_Stopping", "[gray]Выключаю сервер...[/]"},
        {"Server_Stopped", "[green]√[/] Сервер выключен."},
        
        // git
        {"Auth_Success", "[green]√[/] Авторизован как: [bold]{0}[/]"},
        {"File_Sent", "[green]√[/] Файл отправлен: {0}"},
        {"Progress_Downloading", "Скачивание..."},
        {"Sync_EmptyRepo", "[yellow]![/] Возможно, репозиторий пуст. Инициализация..."},
    };

    private static readonly Dictionary<string, string> _uk = new()
    {
        // app general
        {"App_Started", "Додаток запущено. Аргументи: {0}"},
        {"Debug_Enabled", "Режим налагодження увімкнено через аргументи"},
        {"Config_Loading", "Завантаження конфігурації..."},
        {"Setup_Incomplete", "Налаштування не завершено. Вихід."},
        {"Conn_GitHub", "Підключення до GitHub..."},
        {"Repo_Target", "Цільовий репозиторій: [blue]{0}/{1}[/]" }, 
        {"Offline_Mode", "[yellow]![/] Офлайн режим (немає доступу до GitHub)."},
        {"Start_Game_Question", "Запустити гру?"},
        {"Start_Game_NoSync", "Запустити гру без синхронізації?"},
        {"Launch_Canceled", "[gray]Запуск скасовано.[/]"},
        {"Press_Enter", "Натисніть [blue]Enter[/] для виходу."},
        {"Result_Error", "[red]Помилка: {0}[/]"},

        // config
        {"Token_NotFound", "[yellow]![/] GitHub Token не знайдено."},
        {"Token_Prompt", "Введіть ваш [green]GitHub PAT[/]:"},
        {"Token_Invalid", "[white on red]×[/] Токен занадто короткий!"},
        {"Url_NotFound", "[yellow]![/] URL репозиторію не знайдено."},
        {"Url_Prompt", "Введіть [green]HTTPS URL[/] репозиторію:"},
        {"Url_Invalid", "[white on red]×[/] Посилання має починатися з https://github.com/"},
        {"Config_Saved", "[green]√[/] Налаштування збережено в .env файл."},
        {"Env_Error", "[white on red]×[/] Помилка запису .env: {0}"},
        {"Ignore_Create", "[blue].fikaignore[/] файл був створений!"},
        {"Ignore_Loaded", "[gray]Завантажено .fikaignore: {0} правил.[/]"},
        {"Ignore_File", "[gray]Ігнорується файл: {0}[/]"},
        {"Failed_Parse", "Не вдалося проаналізувати конфігурацію {0}: {1}"},
        {"Detected_SPT3", "Папка SPT не знайдена. Використання структури модів для профілів з версії 3.11.x"},
        {"Detected_SPT4", "Папка SPT знайдена. Використання структури модів для профілів з версії 4.x.x"},


        // updater
        {"Update_Available", "[bold red]ДОСТУПНЕ ОНОВЛЕННЯ[/]"},
        {"Update_Body", "[yellow]Нова версія:[/] [green]v{0}[/]\nВаша версія: [gray]v{1}[/]\n\nЗавантажити: [blue underline]{2}[/]"},
        {"Update_Latest", "[gray]У вас остання версія програми. (v{0})[/]"},
        {"Update_Found", "[bold yellow]Знайдена нова версія:[/] [green]v{0}[/]"},
        {"Update_Ask", "Бажаєте оновитися зараз?"},
        {"Update_Downloading", "[gray]Завантаження оновлення...[/]"},
        {"Update_Extracting", "[gray]Вилучення оновлення...[/]"},
        {"Update_Install", "[gray]Встановлення оновлення...[/]"},
        {"Update_Success", "[green]Оновлення завершено! Додаток буде перезапущено.[/]"},
        {"Update_Fail", "[red]Помилка оновлення: {0}[/]"},
        {"Update_AssetNotFound", "[red]Файл оновлення не знайдено в репозиторії![/]"},
        {"Update_Extract_Failed", "Не вдалося розпакувати архів оновлень."},

        // sync
        {"Sync_Title", "[yellow]Синхронізація[/]"},
        {"Sync_Downloading", "[gray]Завантаження профілів з облака...[/]"},
        {"Sync_DownloadFail", "Не вдалося завантажити репозиторій."},
        {"Sync_NoProfiles", "[yellow]![/] У хмарі немає профілів (Репозиторій порожній)."},
        {"Sync_Found", "[bold]Профілів у хмарі:[/] {0}"},
        {"Sync_Updated_Count", "[green]Оновлено {0} профілів з облака.[/]"},
        {"Sync_Backup", "[green] Cтворено резервну копію - [/] {0}"},
        {"Sync_Checking", "[gray]Перевірка змін для відправки...[/]"},
        {"Sync_NoLocal", "[gray]Локальні профілі не знайдені.[/]"},
        {"Sync_AllDone", "[gray]Все синхронізовано.[/]"},
        {"Verify_Remote", "[gray]Перевірка версії на сервері: {0}...[/]"},

        // table
        {"Table_File", "Файл"},
        {"Table_Status", "Статус"},
        {"Table_Action", "Дія"},
        {"Status_Synced", "[green]Актуальний[/]"},
        {"Status_LocalNewer", "[blue]Локальний новіший[/]"},
        {"Status_NewLocal", "[green]Новий локальний[/]"},
        {"Status_Update", "[yellow]Оновити[/]"},
        {"Action_WillUpload", "[yellow]Буде надіслано[/]"},
        {"Action_Downloaded", "[green]Завантажено[/]"},
        {"Sync_Report_Title", "Звіт синхронізації"},
        {"Sync_Profile_Title", "Профіль"}, 
        {"Sync_Reason_Title", "Причина"},
        {"Sync_Result_Title", "Результат"},
        {"Reason_NewProgress", "[green]Новий прогрес[/]"},
        {"Reason_Pending", "[blue]Відкладена синхронізація[/]"},
        {"Result_Conflict", "[red]Конфлікт[/]"},
        {"Result_RemoteNewer", "У хмарі новіше"},
        {"Result_Sent", "[green]Надіслано[/]"},
        
        // game
        {"Server_NotFound", "Файл сервера не знайдено: {0}"},
        {"Config_Found", "[gray]Конфіг знайдено:[/] {0} -> [blue]{1}:{2}[/]"},
        {"Config_Default", "[gray]Конфіг не знайдено, використовуємо:[/]{0}:{1}"},
        {"Game_Starting", "[yellow]Запуск гри[/]"},
        {"Server_Starting", "[gray]Запуск сервера SPT...[/]"},
        {"Server_Process_Fail", "Не вдалося запустити процес сервера!"},
        {"Server_Waiting", "Очікування запуску сервера..."},
        {"Server_Loading", "Завантаження сервера... {0}c"},
        {"Server_Exited", "Сервер несподівано завершив роботу!"},
        {"Server_Timeout", "[yellow]![/] Час очікування сервера вичерпано.[/]"},
        {"Server_Success", "[green]√[/] Сервер успішно запущено [green]{0}:{1}[/]"},
        {"Launcher_Opening", "[gray]Відкриваємо Лаунчер...[/]"},
        {"Launcher_NotFound", "Лаунчер не знайдено!"},
        {"Game_Started_Title", "Гра запущена"},
        {"Game_Close_Instruction", "Натисніть [bold red]ENTER[/] у цьому вікні, щоб закрити сервер та синхронізувати профіль."},
        {"Server_Stopping", "[gray]Вимикаю сервер...[/]"},
        {"Server_Stopped", "[green]√[/] Сервер вимкнено."},
        
        // git
        {"Auth_Success", "[green]√[/] Авторизовано як: [bold]{0}[/]"},
        {"File_Sent", "[green]√[/] Файл надіслано: {0}"},
        {"Progress_Downloading", "Завантаження..."},
        {"Sync_EmptyRepo", "[yellow]![/] Можливо, репозиторій порожній. Ініціалізація..."},
    };

    static Loc()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();

        _currentStrings = culture switch
        {
            "ru" or "be" => _ru, 
            "uk" => _uk,         
            _ => _en 
        };
    }

    public static string Tr(string key, params object[] args)
    {   
        string template = _currentStrings.GetValueOrDefault(key) ?? _en.GetValueOrDefault(key) ?? key;

        if (args.Length == 0) return template;
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
            
        
    }
}