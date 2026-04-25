using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// 图标资源加载与查找。
    /// 参考 CFKillFeedback：支持 kill/kill2/…/kill8、headshot/headshot_gold、grenade_kill、melee_kill。
    /// </summary>
    internal static class KillIconLibrary
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> LoadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            Reload();
        }

        public static void Reload()
        {
            _initialized = true;
            Sprites.Clear();
            LoadedPaths.Clear();

            if (!KillFeedbackConfig.UseIcons)
                return;

            string basePath = KillFeedbackConfig.IconFolder;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                KillFeedbackLogger.Warning("[KF] IconFolder 未配置，跳过图标加载");
                return;
            }

            // 支持绝对/相对路径
            string absolutePath = Path.IsPathRooted(basePath)
                ? basePath
                : Path.Combine(ModBehaviour.ModFolderName, basePath);

            if (!Directory.Exists(absolutePath))
            {
                KillFeedbackLogger.Warning($"[KF] 图标目录不存在: {absolutePath}");
                return;
            }

            string[] extensions = (KillFeedbackConfig.IconExtensions != null && KillFeedbackConfig.IconExtensions.Length > 0)
                ? KillFeedbackConfig.IconExtensions
                : new[] { ".png", ".jpg", ".jpeg", ".webp" };

            foreach (string file in Directory.GetFiles(absolutePath))
            {
                string ext = Path.GetExtension(file);
                if (string.IsNullOrEmpty(ext) || !extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    continue;

                string key = Path.GetFileNameWithoutExtension(file);
                try
                {
                    var texture = LoadTexture(file);
                    if (texture == null) continue;

                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    Sprites[key] = sprite;
                    LoadedPaths.Add(file);
                    if (KillFeedbackLogger.IsDebugEnabled)
                        KillFeedbackLogger.Debug($"[KF] 已加载图标: {key} ({texture.width}x{texture.height})");
                }
                catch (Exception ex)
                {
                    KillFeedbackLogger.Warning($"[KF] 读取图标失败: {file} ({ex.Message})");
                }
            }
        }

        public static Sprite Resolve(KillEventContext context)
        {
            if (!KillFeedbackConfig.UseIcons)
                return null;

            if (!_initialized)
                Initialize();

            if (context.IsHeadshot)
            {
                if (context.IsGoldenHeadshot && TryGet("headshot_gold", out var gold))
                    return gold;
                if (TryGet("headshot", out var headshot))
                    return headshot;
            }

            if (context.IsMelee && TryGet("melee_kill", out var melee))
                return melee;

            if (context.IsExplosion && TryGet("grenade_kill", out var grenade))
                return grenade;

            if (context.Streak <= 1)
            {
                TryGet("kill", out var kill);
                return kill;
            }

            int streak = Mathf.Clamp(context.Streak, 2, 8);
            if (TryGet($"kill{streak}", out var comboSprite))
                return comboSprite;

            if (TryGet("kill6", out var fallback))
                return fallback;

            TryGet("kill", out var baseKill);
            return baseKill;
        }

        public static bool HasAnyIcon => Sprites.Count > 0;

        private static Texture2D LoadTexture(string file)
        {
            try
            {
                byte[] data = File.ReadAllBytes(file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
                if (!texture.LoadImage(data, markNonReadable: false))
                {
                    UnityEngine.Object.Destroy(texture);
                    KillFeedbackLogger.Warning($"[KF] 解析图像失败: {file}");
                    return null;
                }
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 加载图标异常: {file} ({ex.Message})");
                return null;
            }
        }

        private static bool TryGet(string key, out Sprite sprite)
        {
            if (Sprites.TryGetValue(key, out sprite))
                return true;

            // 兼容大小写/下划线
            if (Sprites.TryGetValue(key.ToLowerInvariant(), out sprite))
                return true;

            sprite = null;
            return false;
        }
    }
}
