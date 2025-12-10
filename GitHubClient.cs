using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace FikaSync;

public class GitHubClient
{
    private readonly HttpClient _client;
    
    public GitHubClient(string token)
    {
        _client = new HttpClient();
        _client.BaseAddress = new Uri("https://api.github.com");
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FikaSync", "1.0"));
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
                Logger.Info(Loc.Tr("Auth_Success", login));
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

    public (string Owner, string Repo) ExtractRepoInfo(string url)
    {
        var cleanUrl = url.Trim().TrimEnd('/').Replace(".git", "");
        var parts = cleanUrl.Split('/');
        if (parts.Length >= 2) return (parts[^2], parts[^1]);
        throw new ArgumentException(Loc.Tr("Result_Error", url));
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

            // Get existing SHA to update
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
            // Просим raw контент
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

    public async Task<(string TagName, string HtmlUrl)?> GetLatestReleaseInfo(string repoName)
    {
        try
        {
            var res = await _client.GetAsync($"/repos/{repoName}/releases/latest");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(json);
            return (node?["tag_name"]?.ToString() ?? "", node?["html_url"]?.ToString() ?? "");
        }
        catch { return null; }
    }
}