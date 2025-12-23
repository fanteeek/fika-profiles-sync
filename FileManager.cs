using System.IO.Compression;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FikaSync;

public static class FileManager
{
    public static void ForceDeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
        }
    }

    public static string? ExtractZip(string zipPath, string extractTo)
    {
        try
        {
            ForceDeleteDirectory(extractTo);
            ZipFile.ExtractToDirectory(zipPath, extractTo);
            var extractedDirs = Directory.GetDirectories(extractTo);
            if (extractedDirs.Length > 0)
                return extractedDirs[0];
            return extractTo;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return null;
        }
    }

    public static string? Extract7z(string archivePath, string extractTo)
    {
        try
        {
            ForceDeleteDirectory(extractTo);
            Directory.CreateDirectory(extractTo);

            using (var archive = SevenZipArchive.Open(archivePath))
            {
                using (var reader = archive.ExtractAllEntries())
                {
                    reader.WriteAllToDirectory(extractTo, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }

            var extractedDirs = Directory.GetDirectories(extractTo);
            if (extractedDirs.Length == 1 && Directory.GetFiles(extractTo).Length == 0)
                return extractedDirs[0];
            return extractTo;
        }
        catch (Exception ex)
        {
            Logger.Error(Loc.Tr("Result_Error", ex.Message));
            return null;
        }
    }

    public static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);        
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    public static List<string> FindProfiles(string rootPath)
    {
        var profiles = new List<string>();
        if (!Directory.Exists(rootPath)) return profiles;

        var allFiles = Directory.GetFiles(rootPath, "*.json", SearchOption.AllDirectories);
        foreach (var file in allFiles)
        {
            profiles.Add(file);
        }

        return profiles;
    }
}