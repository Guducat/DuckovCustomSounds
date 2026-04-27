using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    internal sealed class GrenadeSoundResolution
    {
        public string FilePath { get; set; } = string.Empty;
        public string SoundKey { get; set; } = string.Empty;
        public string FileBase { get; set; } = string.Empty;
        public string? TypeIdStr { get; set; }
        public ExplosionSoundSource Source { get; set; }
        public IReadOnlyList<string> Attempts { get; set; } = Array.Empty<string>();
    }

    internal static class GrenadeSoundResolver
    {
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };

        public static string GetBaseDir()
        {
            return Path.Combine(ModBehaviour.ModFolderName, "CustomGrenadeSounds");
        }

        public static bool TryResolve(
            string baseDir,
            ExplosionSoundFrame context,
            string soundKey,
            out GrenadeSoundResolution resolution,
            bool applySoundMap = true)
        {
            resolution = new GrenadeSoundResolution();
            if (string.IsNullOrWhiteSpace(baseDir) || string.IsNullOrWhiteSpace(soundKey))
            {
                return false;
            }

            var typeIdStr = context.TypeIdStr;
            var resolvedKey = applySoundMap ? GrenadeSoundMap.ResolveForReplace(typeIdStr, soundKey) : soundKey;
            var fileBase = GrenadeSoundMap.ResolveFileBase(typeIdStr, resolvedKey) ?? resolvedKey;
            var attempts = BuildAttempts(baseDir, context.Source, typeIdStr, resolvedKey, fileBase).ToList();
            var filePath = FindFirst(attempts);

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            resolution = new GrenadeSoundResolution
            {
                FilePath = filePath,
                SoundKey = resolvedKey,
                FileBase = fileBase,
                TypeIdStr = typeIdStr,
                Source = context.Source,
                Attempts = attempts,
            };
            return true;
        }

        private static IEnumerable<string> BuildAttempts(
            string baseDir,
            ExplosionSoundSource source,
            string? typeIdStr,
            string soundKey,
            string fileBase)
        {
            // CustomGrenadeSounds/grenade/<TypeID>.* -> grenade/<fileBase>.* -> grenade/<soundKey>.* -> grenade/default.*
            // CustomGrenadeSounds/<TypeID>.* -> <fileBase>.* -> <soundKey>.* -> default.* keeps legacy packs working.
            switch (source)
            {
                case ExplosionSoundSource.Grenade:
                {
                    var grenadeDir = Path.Combine(baseDir, "grenade");
                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        foreach (var path in ExpandCandidates(grenadeDir, typeIdStr)) yield return path;
                    }
                    foreach (var path in ExpandCandidates(grenadeDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(grenadeDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(grenadeDir, "default")) yield return path;

                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        foreach (var path in ExpandCandidates(baseDir, typeIdStr)) yield return path;
                    }
                    foreach (var path in ExpandCandidates(baseDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, "default")) yield return path;
                    break;
                }
                case ExplosionSoundSource.Breakable:
                {
                    var breakableDir = Path.Combine(baseDir, "breakable");
                    foreach (var path in ExpandCandidates(breakableDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(breakableDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(breakableDir, "default")) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, "default")) yield return path;
                    break;
                }
                case ExplosionSoundSource.Proxy:
                {
                    var proxyDir = Path.Combine(baseDir, "proxy");
                    foreach (var path in ExpandCandidates(proxyDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(proxyDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(proxyDir, "default")) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, "default")) yield return path;
                    break;
                }
                default:
                {
                    foreach (var path in ExpandCandidates(baseDir, fileBase)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, soundKey)) yield return path;
                    foreach (var path in ExpandCandidates(baseDir, "default")) yield return path;
                    break;
                }
            }
        }

        internal static IEnumerable<string> ExpandCandidates(string dir, params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var ext in Exts)
                {
                    yield return Path.Combine(dir, name + ext);
                }
            }
        }

        private static string? FindFirst(IReadOnlyList<string> attempts)
        {
            var exact = attempts.FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(exact))
            {
                return TryPickVariantStrict(exact);
            }

            foreach (var attempt in attempts)
            {
                try
                {
                    var dir = Path.GetDirectoryName(attempt);
                    var baseName = Path.GetFileNameWithoutExtension(attempt);
                    var variant = TryPickVariantStrictByBase(dir, baseName);
                    if (!string.IsNullOrEmpty(variant))
                    {
                        return variant;
                    }
                }
                catch { }
            }

            return null;
        }

        internal static string TryPickVariantStrict(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                var ext = Path.GetExtension(filePath);
                var nameNoExt = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(nameNoExt))
                {
                    return filePath;
                }

                var candidates = FindVariantCandidates(dir, nameNoExt, ext);
                if (candidates.Count == 0) return filePath;

                var pick = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[pick];
            }
            catch
            {
                return filePath;
            }
        }

        internal static string? TryPickVariantStrictByBase(string? dir, string? baseNameNoExt)
        {
            if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(baseNameNoExt)) return null;
            try
            {
                var candidates = new List<string>();
                foreach (var ext in Exts)
                {
                    candidates.AddRange(FindVariantCandidates(dir, baseNameNoExt, ext));
                }

                if (candidates.Count == 0) return null;
                var pick = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[pick];
            }
            catch
            {
                return null;
            }
        }

        private static List<string> FindVariantCandidates(string dir, string baseNameNoExt, string ext)
        {
            var candidates = new List<string>();
            var pattern = baseNameNoExt + "_*" + ext;
            foreach (var path in Directory.EnumerateFiles(dir, pattern))
            {
                var fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(fileNameNoExt)) continue;

                var underscoreIndex = fileNameNoExt.LastIndexOf('_');
                if (underscoreIndex <= 0) continue;
                if (!string.Equals(fileNameNoExt.Substring(0, underscoreIndex), baseNameNoExt, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suffix = fileNameNoExt.Substring(underscoreIndex + 1);
                if (suffix.Length == 0 || !suffix.All(char.IsDigit)) continue;
                if (int.TryParse(suffix, out var number) && number >= 1)
                {
                    candidates.Add(path);
                }
            }

            return candidates;
        }
    }
}
