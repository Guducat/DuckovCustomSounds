using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillSoundResolver
    {
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };

        public static IEnumerable<string> ExpandCandidates(string dir, params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                foreach (var ext in Exts)
                    yield return Path.Combine(dir, name + ext);
            }
        }

        public static string? FindSoundFile(string dir, IEnumerable<string> attempts, IEnumerable<string> fallbacks)
        {
            var attemptList = attempts?.ToArray() ?? Array.Empty<string>();
            var fallbackList = fallbacks?.ToArray() ?? Array.Empty<string>();

            var filePath = ExpandCandidates(dir, attemptList).FirstOrDefault(File.Exists);
            if (filePath == null)
                filePath = ExpandCandidates(dir, fallbackList).FirstOrDefault(File.Exists);

            return filePath == null ? null : TryPickVariantStrict(filePath);
        }

        public static string TryPickVariantStrict(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return filePath;

            try
            {
                var dir = Path.GetDirectoryName(filePath);
                var ext = Path.GetExtension(filePath);
                var nameNoExt = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(dir) ||
                    string.IsNullOrWhiteSpace(ext) ||
                    string.IsNullOrWhiteSpace(nameNoExt))
                {
                    return filePath;
                }

                var pattern = nameNoExt + "_*" + ext;
                var candidates = new List<string>();
                foreach (var path in Directory.EnumerateFiles(dir, pattern))
                {
                    var fileNoExt = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrWhiteSpace(fileNoExt))
                        continue;

                    var split = fileNoExt.LastIndexOf('_');
                    if (split <= 0)
                        continue;

                    if (!string.Equals(fileNoExt.Substring(0, split), nameNoExt, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (int.TryParse(fileNoExt.Substring(split + 1), out var n) && n >= 1)
                        candidates.Add(path);
                }

                if (candidates.Count == 0)
                    return filePath;

                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }
            catch
            {
                return filePath;
            }
        }
    }
}
