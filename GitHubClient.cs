using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FikaSync;

public class GitHubClient
{
    private readonly HttpClient _client;
    
    public GitHubClient(string token)
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri("https://api.github.com");
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FikaSync", "0.3.3"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public async Task<bool> TestToken()
    {
        try
        {
            var response = await _client.GetAsync("/user");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var login = JsonNode.Parse(json)?["login"]?.ToString() ?? "Unknown";
                Logger.Debug(Loc.Tr("Auth_Success", login));
                return true;
            }
            Logger.Error(Loc.Tr("Result_Error", response.StatusCode));
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return false;
        }
    }

    public async Task<bool> CreateReadme(string owner, string repo)
    {
        try
        {
            string requestUri = $"/repos/{owner}/{repo}/contents/README.md";

            string content = "# FikaSync Storage\nThis repository is used to store game profiles.";
            string base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

            var payload = new
            {
                message = "Initial commit: create README",
                content = base64Content
            };

            var json = JsonSerializer.Serialize(payload);
            
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _client.PutAsync(requestUri, httpContent);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorDetails = await response.Content.ReadAsStringAsync();
            Logger.Error(Loc.Tr("Result_Error", errorDetails));
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return false;
        }
    }

    public (string Owner, string Repo) ExtractRepoInfo(string url)
    {
        var cleanUrl = url.Trim().TrimEnd('/').Replace(".git", "");
        var parts = cleanUrl.Split('/');
        if (parts.Length < 2)
            throw new ArgumentException($"Invalid URL format: {url}");
        return (parts[^2], parts[^1]);
    }

    public async Task<bool> DownloadRepository(string owner, string repo, string savePath)
    {
        try
        {
            string url = $"/repos/{owner}/{repo}/zipball";
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return false;

            var dir = Path.GetDirectoryName(savePath);
            if (dir != null) Directory.CreateDirectory(dir);

            using var fs = new FileStream(savePath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return false;
        }
    }

    public async Task<bool> UploadFile(string owner, string repo, string filePath, byte[] content)
    {
        try
        {
            string url = $"/repos/{owner}/{repo}/contents/{filePath}";
            string base64 = Convert.ToBase64String(content);
            string? sha = null;

            try
            {
                var getRes = await _client.GetAsync(url);
                if (getRes.IsSuccessStatusCode)
                {
                    var json = await getRes.Content.ReadAsStringAsync();
                    sha = JsonNode.Parse(json)?["sha"]?.ToString();
                }
            }
            catch {}

            var body = new
            {
                message = $"Update profile {Path.GetFileName(filePath)}",
                content = base64,
                sha = sha
            };

            var response = await _client.PutAsJsonAsync(url, body);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return false;
        }
    }
    
    public async Task<byte[]?> DownloadFileContent(string owner, string repo, string filePath)
    {
        try
        {
            string url = $"/repos/{owner}/{repo}/contents/{filePath}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Clear(); 
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));

            var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode) return await res.Content.ReadAsByteArrayAsync();
            return null;
        }
        catch 
        {
            return null;
        }
    }

    public async Task<(string TagName, string DownloadUrl)?> GetLatestReleaseInfo(string repoName)
    {
        try
        {
            var res = await _client.GetAsync($"/repos/{repoName}/releases/latest");
            Logger.Debug($"GetLatestReleaseInfo: {res} | StatusCode: {res.IsSuccessStatusCode}");
            if (!res.IsSuccessStatusCode) return null;

            var node = JsonNode.Parse(await res.Content.ReadAsStringAsync());
            string tag = node?["tag_name"]?.ToString() ?? "";
            var assets = node?["assets"]?.AsArray();

            if (assets == null) return null;

            foreach (var asset in assets)
            {
                string name = asset?["name"]?.ToString() ?? "";
                string url = asset?["browser_download_url"]?.ToString() ?? "";

                if (name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    return (tag, url);
                }
            }

            return null;
        }
        catch { return null; }
    }

    public async Task<bool> DownloadAsset(string url, string savePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return false;

            using var fs = new FileStream(savePath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return false;
        }
    }
}