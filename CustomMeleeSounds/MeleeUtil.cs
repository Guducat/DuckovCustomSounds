using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Duckov;

namespace DuckovCustomSounds.CustomMeleeSounds
{
    /// <summary>
    /// 近战音效工具类
    /// </summary>
    internal static class MeleeUtil
    {
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };

        /// <summary>
        /// 扩展候选文件列表
        /// </summary>
        public static IEnumerable<string> ExpandCandidates(string dir, params string[] namesNoExt)
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

        /// <summary>
        /// 获取近战武器组件和 TypeID
        /// </summary>
        public static (ItemAgent_MeleeWeapon melee, string typeIdStr) GetMeleeAndTypeId(GameObject gameObject)
        {
            ItemAgent_MeleeWeapon melee = null;
            string typeIdStr = string.Empty;

            try
            {
                if (gameObject != null)
                {
                    var cmc = gameObject.GetComponent<CharacterMainControl>() ?? gameObject.GetComponentInParent<CharacterMainControl>();
                    if (cmc != null)
                    {
                        try { melee = cmc.GetMeleeWeapon(); } catch { }
                    }
                }
            }
            catch { }

            if (melee == null)
            {
                try { melee = gameObject?.GetComponent<ItemAgent_MeleeWeapon>(); } catch { }
            }
            if (melee == null)
            {
                try { melee = gameObject?.GetComponentInParent<ItemAgent_MeleeWeapon>(); } catch { }
            }
            if (melee == null)
            {
                try { melee = gameObject?.GetComponentInChildren<ItemAgent_MeleeWeapon>(); } catch { }
            }

            try { if (melee?.Item != null) typeIdStr = melee.Item.TypeID.ToString(); } catch { }

            return (melee, typeIdStr);
        }

        /// <summary>
        /// 查找音效文件
        /// </summary>
        public static string FindSoundFile(string dir, List<string> attempts, List<string> fallbacks)
        {
            string filePath = attempts.FirstOrDefault(File.Exists);
            if (filePath == null)
            {
                filePath = fallbacks.FirstOrDefault(File.Exists);
            }
            return filePath;
        }

        /// <summary>
        /// 若存在与 filePath 同名的 "_1"、"_2" 等后缀文件（同扩展名），随机选取其一；否则返回原路径
        /// </summary>
        public static string TryPickVariant(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                string ext = Path.GetExtension(filePath);
                string nameNoExt = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(nameNoExt))
                    return filePath;

                string searchPattern = nameNoExt + "_*" + ext;
                var candidates = new List<string>();
                foreach (var p in Directory.EnumerateFiles(dir, searchPattern))
                {
                    var file = Path.GetFileName(p);
                    if (file == null) continue;
                    int usIdx = file.LastIndexOf('_');
                    int dotIdx = file.LastIndexOf('.');
                    if (usIdx <= 0 || dotIdx <= usIdx + 1) continue;
                    var numSpan = file.Substring(usIdx + 1, dotIdx - (usIdx + 1));
                    if (int.TryParse(numSpan, out var n) && n >= 1)
                    {
                        candidates.Add(Path.Combine(dir, file));
                    }
                }

                if (candidates.Count == 0) return filePath;

                int pick = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[pick];
            }
            catch { return filePath; }
        }
    }
}
