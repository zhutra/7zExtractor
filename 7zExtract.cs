using System;
using System.IO;
using SevenZip;
using System.Linq;
using System.Collections.Generic;

class SevenZExtract
{
    static void Main(string[] args)
    {
        SevenZipDLL();

        int toIndex = Array.FindIndex(args, arg => arg.Equals("--to", StringComparison.OrdinalIgnoreCase));
        int reIndex = Array.FindIndex(args, arg => arg.Equals("--re", StringComparison.OrdinalIgnoreCase));
        int fromIndex = Array.FindIndex(args, arg => arg.Equals("--from", StringComparison.OrdinalIgnoreCase));

        if (toIndex <= 0 || toIndex == args.Length - 1)
        {
            Console.Write("\nUsage: 7zExtract.exe <archive_file> --to <destination_folder> [--re \"new_name\"] [--from \"folder1\" or \"folder2\" or ...]");
            return;
        }

        string archivePath;
        if (args[0].StartsWith("\""))
        {
            var archiveArgs = new List<string>();
            int i = 0;
            string? archiveCurrent = args[i].TrimStart('"');

            while (i < toIndex && !archiveCurrent.EndsWith("\""))
            {
                archiveArgs.Add(archiveCurrent);
                i++;
                if (i < toIndex)
                    archiveCurrent = args[i];
            }

            if (archiveCurrent?.EndsWith("\"") == true)
            {
                archiveArgs.Add(archiveCurrent.TrimEnd('"'));
                archivePath = string.Join(" ", archiveArgs);
            }
            else
            {
                Console.Write("\nError: Unclosed quotes in archive path");
                return;
            }
        }
        else
        {
            archivePath = string.Join(" ", args.Take(toIndex));
        }

        string? current = null;
        string destinationPath;
        if (args[toIndex + 1].StartsWith("\""))
        {
            var destArgs = new List<string>();
            int i = toIndex + 1;
            current = args[i].TrimStart('"');

            while (i < args.Length && !current.EndsWith("\""))
            {
                destArgs.Add(current);
                i++;
                if (i < args.Length)
                    current = args[i];
            }

            if (current?.EndsWith("\"") == true)
            {
                destArgs.Add(current.TrimEnd('"'));
                destinationPath = string.Join(" ", destArgs);
            }
            else
            {
                Console.Write("\nError: Unclosed quotes in destination path");
                return;
            }
        }
        else if (reIndex > 0 || fromIndex > 0)
        {
            int nextFlag = (reIndex > 0 && fromIndex > 0) ? Math.Min(reIndex, fromIndex) :
                          reIndex > 0 ? reIndex : fromIndex;
            destinationPath = string.Join(" ", args.Skip(toIndex + 1).Take(nextFlag - (toIndex + 1)));
        }
        else
        {
            destinationPath = args[toIndex + 1];
        }

        string? newFolderName = null;
        if (reIndex > 0 && reIndex < args.Length - 1)
        {
            int endIndex = fromIndex > 0 ? fromIndex : args.Length;
            newFolderName = string.Join(" ", args.Skip(reIndex + 1).Take(endIndex - (reIndex + 1))).Trim('"');
        }

        List<string> fromPatterns = new List<string>();
        if (fromIndex > 0 && fromIndex < args.Length - 1)
        {
            string fullFromString = string.Join(" ", args.Skip(fromIndex + 1));
            fromPatterns = fullFromString.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(p => p.Trim().Trim('"'))
                                       .Where(p => !string.IsNullOrWhiteSpace(p))
                                       .ToList();
        }

        try
        {
            archivePath = Path.GetFullPath(archivePath);
            destinationPath = Path.GetFullPath(destinationPath);
        }
        catch (Exception ex)
        {
            Console.Write($"\nError: Could not process paths - {ex.Message}");
            return;
        }

        if (!File.Exists(archivePath))
        {
            Console.Write($"\nError: File not found - {archivePath}");
            return;
        }

        Console.Write("Extracting...");

        try
        {
            SevenZipBase.SetLibraryPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.dll"));

            using (var extractor = new SevenZipExtractor(archivePath))
            {
                string? matchedFolder = null;
                if (fromPatterns.Any())
                {
                    var archiveNames = extractor.ArchiveFileNames.ToList();
                    var topLevelFolders = archiveNames
                        .Select(name => name.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0])
                        .Where(name => archiveNames.Any(path =>
                            path.Length > name.Length &&
                            (path[name.Length] == Path.DirectorySeparatorChar || path[name.Length] == '/') &&
                            path.StartsWith(name)))
                        .Distinct();

                    foreach (var pattern in fromPatterns)
                    {
                        matchedFolder = topLevelFolders.FirstOrDefault(folder =>
                            folder.Equals(pattern, StringComparison.OrdinalIgnoreCase));

                        if (matchedFolder != null) break;
                    }
                }

                Directory.CreateDirectory(destinationPath);

                int lastPercentage = -1;

                extractor.Extracting += (sender, e) =>
                {
                    int currentPercentage = (int)e.PercentDone;
                    if (currentPercentage != lastPercentage)
                    {
                        Console.Write($"\rExtracting: {currentPercentage}%");
                        lastPercentage = currentPercentage;
                    }
                };

                if (matchedFolder != null)
                {
                    var filesToExtract = extractor.ArchiveFileNames
                        .Select((name, index) => new { name, index })
                        .Where(x => x.name.StartsWith(matchedFolder + Path.DirectorySeparatorChar))
                        .Select(x => x.index)
                        .ToArray();

                    extractor.ExtractFiles(destinationPath, filesToExtract);
                    Console.WriteLine($"\nExtracted {matchedFolder}");

                    if (newFolderName != null)
                    {
                        string originalPath = Path.Combine(destinationPath, matchedFolder);
                        string newPath = Path.Combine(destinationPath, newFolderName);

                        try
                        {
                            if (Directory.Exists(originalPath) && !matchedFolder.Equals(newFolderName, StringComparison.OrdinalIgnoreCase))
                            {
                                Directory.Move(originalPath, newPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nWarning: Could not rename folder - {ex.Message}");
                        }
                    }
                }
                else
                {
                    extractor.ExtractArchive(destinationPath);
                    Console.WriteLine("\nExtracted all files");

                    if (newFolderName != null)
                    {
                        try
                        {
                            var allEntries = Directory.GetFileSystemEntries(destinationPath);
                            var dirs = Directory.GetDirectories(destinationPath);

                            if (dirs.Length == 1 && allEntries.Length == 1)
                            {
                                string originalPath = dirs[0];
                                string finalPath = originalPath;
                                string newPath = Path.Combine(destinationPath, newFolderName);
                                string currentName = Path.GetFileName(originalPath);
                                if (!currentName.Equals(newFolderName, StringComparison.OrdinalIgnoreCase))
                                {
                                    Directory.Move(originalPath, newPath);
                                    finalPath = newPath;
                                }
                                ExtractNestedFiles(finalPath);
                            }
                            else if (allEntries.Length > 0)
                            {
                                string newFolderPath = Path.Combine(destinationPath, newFolderName);
                                Directory.CreateDirectory(newFolderPath);

                                foreach (string entry in allEntries)
                                {
                                    string entryName = Path.GetFileName(entry);
                                    string newPath = Path.Combine(newFolderPath, entryName);
                                    if (Directory.Exists(entry))
                                    {
                                        Directory.Move(entry, newPath);
                                    }
                                    else
                                    {
                                        File.Move(entry, newPath);
                                    }
                                }

                                if (dirs.Length == 0)
                                {
                                    ExtractNestedFiles(newFolderPath);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nWarning: Could not organize files - {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: Extraction failed - {ex.Message}");
        }
    }

    private static void SevenZipDLL()
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.dll");

        if (!File.Exists(dllPath))
        {
            using var resourceStream = typeof(SevenZExtract).Assembly.GetManifestResourceStream("7z.dll");
            if (resourceStream == null)
            {
                throw new Exception("Error: Could not find embedded 7z.dll");
            }

            using var fileStream = new FileStream(dllPath, FileMode.Create);
            resourceStream.CopyTo(fileStream);
        }
    }

    private static void ExtractNestedFiles(string folderPath)
    {
        var archiveExtensions = new[] { ".7z", ".zip", ".rar", ".iso", ".tar.gz" };
        var nestedArchives = Directory.GetFiles(folderPath)
            .Where(f => archiveExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        if (nestedArchives.Count > 0)
        {
            Console.WriteLine($"\nFound {nestedArchives.Count} nested archive{(nestedArchives.Count > 1 ? "s" : "")}");
        }

        foreach (string archive in nestedArchives)
        {
            try
            {
                Console.Write($"\nExtracting {Path.GetFileName(archive)}");
                using (var nestedExtractor = new SevenZipExtractor(archive))
                {
                    nestedExtractor.ExtractArchive(folderPath);
                }
                File.Delete(archive);
            }
            catch (Exception)
            {
                continue;
            }
        }
        Console.WriteLine();
    }
}
