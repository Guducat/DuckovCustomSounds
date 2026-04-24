using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DuckovCustomSounds.CustomBGM.Core
{
    internal static class AudioFileExtensions
    {
        public static readonly string[] MusicExtensions =
        {
            ".mp3",
            ".wav",
            ".ogg",
            ".oga",
            ".flac",
            ".aif",
            ".aiff",
            ".mp2",
            ".m4a",
            ".mp4",
            ".wma",
            ".asf",
            ".fsb",
            ".it",
            ".mid",
            ".midi",
            ".mod",
            ".s3m",
            ".xm"
        };

        private static readonly object CacheLock = new object();
        private static readonly HashSet<string> ExtensionSet = new HashSet<string>(MusicExtensions, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> ExtensionPriority = BuildExtensionPriority();
        private static readonly Dictionary<string, DirectoryIndex> Cache = new Dictionary<string, DirectoryIndex>(StringComparer.OrdinalIgnoreCase);

        public static string? FindMusicFile(string folder, string baseName)
        {
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(baseName))
                return null;

            var index = GetDirectoryIndex(folder);
            return index.TryGetByBaseName(baseName);
        }

        public static string? FindFirstMusicFile(string folder, params string[] baseNames)
        {
            if (baseNames == null || baseNames.Length == 0)
                return null;

            foreach (var baseName in baseNames)
            {
                var path = FindMusicFile(folder, baseName);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            return null;
        }

        public static string[] GetMusicFiles(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return Array.Empty<string>();

            var index = GetDirectoryIndex(folder);
            return index.AllFiles.ToArray();
        }

        public static void ClearCache(string? folder = null)
        {
            lock (CacheLock)
            {
                if (string.IsNullOrWhiteSpace(folder))
                {
                    Cache.Clear();
                    return;
                }

                Cache.Remove(NormalizeFolder(folder));
            }
        }

        private static DirectoryIndex GetDirectoryIndex(string folder)
        {
            var key = NormalizeFolder(folder);
            lock (CacheLock)
            {
                if (!Cache.TryGetValue(key, out var index))
                {
                    index = DirectoryIndex.Build(key);
                    Cache[key] = index;
                }

                return index;
            }
        }

        private static string NormalizeFolder(string folder)
        {
            try
            {
                return Path.GetFullPath(folder);
            }
            catch
            {
                return folder;
            }
        }

        private static Dictionary<string, int> BuildExtensionPriority()
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < MusicExtensions.Length; i++)
            {
                result[MusicExtensions[i]] = i;
            }

            return result;
        }

        private sealed class DirectoryIndex
        {
            private readonly Dictionary<string, string> bestFileByBaseName;

            private DirectoryIndex(List<string> allFiles, Dictionary<string, string> bestFileByBaseName)
            {
                AllFiles = allFiles;
                this.bestFileByBaseName = bestFileByBaseName;
            }

            public List<string> AllFiles { get; }

            public static DirectoryIndex Build(string folder)
            {
                var allFiles = new List<string>();
                var candidatesByBaseName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                if (Directory.Exists(folder))
                {
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
                        {
                            var extension = Path.GetExtension(file);
                            if (!ExtensionSet.Contains(extension))
                                continue;

                            allFiles.Add(file);

                            var baseName = Path.GetFileNameWithoutExtension(file);
                            if (string.IsNullOrEmpty(baseName))
                                continue;

                            if (!candidatesByBaseName.TryGetValue(baseName, out var candidates))
                            {
                                candidates = new List<string>();
                                candidatesByBaseName[baseName] = candidates;
                            }

                            candidates.Add(file);
                        }
                    }
                    catch
                    {
                    }
                }

                allFiles.Sort(CompareFilePriority);

                var bestFileByBaseName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in candidatesByBaseName)
                {
                    entry.Value.Sort(CompareFilePriority);
                    bestFileByBaseName[entry.Key] = entry.Value[0];
                }

                return new DirectoryIndex(allFiles, bestFileByBaseName);
            }

            public string? TryGetByBaseName(string baseName)
            {
                var normalized = Path.GetFileNameWithoutExtension(baseName);
                if (string.IsNullOrWhiteSpace(normalized))
                    return null;

                return bestFileByBaseName.TryGetValue(normalized, out var path) ? path : null;
            }

            private static int CompareFilePriority(string left, string right)
            {
                var priority = GetExtensionPriority(left).CompareTo(GetExtensionPriority(right));
                if (priority != 0)
                    return priority;

                return string.Compare(Path.GetFileName(left), Path.GetFileName(right), StringComparison.OrdinalIgnoreCase);
            }

            private static int GetExtensionPriority(string file)
            {
                var extension = Path.GetExtension(file);
                return ExtensionPriority.TryGetValue(extension, out var priority) ? priority : int.MaxValue;
            }
        }
    }
}
