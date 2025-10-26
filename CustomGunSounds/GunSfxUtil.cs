using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using FMOD;
using FMODUnity;

namespace DuckovCustomSounds.CustomGunSounds
{
    internal static class GunSfxUtil
    {
        private const float CleanupMaxSeconds = 6f;
        private static string BaseDir => Path.Combine(ModBehaviour.ModFolderName, "CustomGunSounds");


        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };
        private static IEnumerable<string> ExpandCandidates(params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var ext in Exts)
                {
                    yield return Path.Combine(BaseDir, name + ext);
                }
            }
        }

        // Public entry for Shoot (keeps legacy behavior, incl. _mute)
        public static bool TryOverrideShoot(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Shoot/";
            if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string soundKey = eventName.Substring(prefix.Length);
            if (string.IsNullOrWhiteSpace(soundKey)) return false;

            // Gun context
            var (gun, typeIdStr) = GetGunAndTypeId(gameObject);

            // Silenced handling (by key or gun property)
            bool silenced = false;
            try { if (gun != null) silenced = gun.Silenced; } catch { }
            if (soundKey.EndsWith("_mute", StringComparison.OrdinalIgnoreCase)) silenced = true;

            // Build candidate list
            var attempts = new List<string>();
            string legacySoundKeyPath = Path.Combine(BaseDir, soundKey + ".mp3");
            if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
            {
                if (silenced) attempts.Add(Path.Combine(BaseDir, typeIdStr + "_mute.mp3"));
                attempts.Add(Path.Combine(BaseDir, typeIdStr + ".mp3"));
                attempts.Add(legacySoundKeyPath);
            }
            else
            {
                attempts.Add(legacySoundKeyPath);
            }

            string[] fallbacks = new[] { Path.Combine(BaseDir, "default.mp3") };

            return TryPlayFromCandidates(ref __result, eventName, gameObject, attempts, fallbacks, typeIdStr, soundKey, tag: "GunShoot");
        }

        // Public entry for Reload (fine-grained start/end)
        public static bool TryOverrideReload(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Reload/";
            if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string soundKey = eventName.Substring(prefix.Length); // e.g., "mag_start" or "mag_end"
            if (string.IsNullOrWhiteSpace(soundKey)) return false;

            bool isStart = soundKey.EndsWith("_start", StringComparison.OrdinalIgnoreCase);
            bool isEnd   = soundKey.EndsWith("_end",   StringComparison.OrdinalIgnoreCase);

            // Gun context
            var (gun, typeIdStr) = GetGunAndTypeId(gameObject);

            // Build fine-grained candidates
            var attempts = new List<string>();
            if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
            {
                if (isStart)
                {
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload_start.mp3"));
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload.mp3")); // generic reload fallback for start
                }
                else if (isEnd)
                {
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload_end.mp3"));
                }
                else
                {
                    // Unknown suffix, treat as generic reload
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload.mp3"));
                }
            }

            // Always include the raw soundKey (e.g., mag_start/mp3, mag_end.mp3)
            attempts.Add(Path.Combine(BaseDir, soundKey + ".mp3"));

            // Default fallbacks according to phase
            List<string> fallbacks = new List<string>();
            if (isStart)
            {
                fallbacks.Add(Path.Combine(BaseDir, "default_reload_start.mp3"));
                fallbacks.Add(Path.Combine(BaseDir, "default_reload.mp3"));
            }
            else if (isEnd)
            {
                fallbacks.Add(Path.Combine(BaseDir, "default_reload_end.mp3"));
            }
            else
            {
                // Unknown suffix: provide generic reload default as well
                fallbacks.Add(Path.Combine(BaseDir, "default_reload.mp3"));
            }
            // Final common default
            fallbacks.Add(Path.Combine(BaseDir, "default.mp3"));

            return TryPlayFromCandidates(ref __result, eventName, gameObject, attempts, fallbacks, typeIdStr, soundKey, tag: "GunReload");
        }

        // Postfix versions: preserve original Studio Event lifecycle (mute it) and play custom Core sound
        public static void PostfixOverrideShoot_Safe(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Shoot/";
            try
            {
                if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(prefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                var (gun, typeIdStr) = GetGunAndTypeId(gameObject);
                bool silenced = false;
                try { if (gun != null) silenced = gun.Silenced; } catch { }
                if (soundKey.EndsWith("_mute", StringComparison.OrdinalIgnoreCase)) silenced = true;

                var attempts = new List<string>();
                if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                {
                    if (silenced) attempts.AddRange(ExpandCandidates(typeIdStr + "_mute"));
                    attempts.AddRange(ExpandCandidates(typeIdStr));
                    attempts.AddRange(ExpandCandidates(soundKey));
                }
                else
                {
                    attempts.AddRange(ExpandCandidates(soundKey));
                }

                var fallbacks = ExpandCandidates("default").ToList();

                // Resolve file
                string filePath = attempts.FirstOrDefault(File.Exists) ?? fallbacks.FirstOrDefault(File.Exists);

                string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                    GunLogger.Debug($"[GunShoot] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");
                else
                    GunLogger.Debug($"[GunShoot] 未捕获到 ItemAgent_Gun 组件，查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");

                if (filePath == null) return; // no override

                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[GunShoot] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

                // Mute original Studio event if any
                try { if (__result.HasValue) { var ev = __result.Value; ev.setVolume(0f); } } catch { }

                // Derive 3D min/max distance from original event when possible
                float min = 1f, max = 50f;
                try
                {
                    if (__result.HasValue && __result.Value.isValid())
                    {
                        if (__result.Value.getDescription(out FMOD.Studio.EventDescription desc) == RESULT.OK)
                        {
                            try { desc.getMinMaxDistance(out min, out max); } catch { }
                        }
                    }
                }
                catch { }

                // Play custom core sound (choose mode by file type)
                string fullPath = filePath;
                try { fullPath = System.IO.Path.GetFullPath(filePath); } catch { }
                var mode = ComputeModeForFile(fullPath) | MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(fullPath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GunLogger.Info($"[GunShoot] createSound 失败({r1})，放弃自定义音效");
                    return;
                }
                try { sound.set3DMinMaxDistance(min, max); } catch { }

                ChannelGroup group = AcquireSfxChannelGroup();
                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    GunLogger.Info($"[GunShoot] playSound 失败({r2})");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return;
                }

                Vector3 pos = Vector3.zero;
                try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                try { channel.set3DMinMaxDistance(min, max); } catch { }
                try { channel.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                try { channel.setPaused(false); } catch { }

                GunLogger.Debug($"[GunShoot] 覆盖播放 {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");
                try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, CleanupMaxSeconds)); } catch { }
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunShoot] Postfix 覆盖异常: {ex.Message}");
            }
        }

        public static void PostfixOverrideReload_Safe(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Reload/";
            try
            {
                if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(prefix.Length); // e.g., mag_start or mag_end
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                bool isStart = soundKey.EndsWith("_start", StringComparison.OrdinalIgnoreCase);
                bool isEnd   = soundKey.EndsWith("_end",   StringComparison.OrdinalIgnoreCase);

                var (gun, typeIdStr) = GetGunAndTypeId(gameObject);

                var attempts = new List<string>();
                if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                {
                    if (isStart)
                    {
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload_start"));
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload"));
                    }
                    else if (isEnd)
                    {
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload_end"));
                    }
                    else
                    {
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload"));
                    }
                }
                attempts.AddRange(ExpandCandidates(soundKey));

                var fallbacks = new List<string>();
                if (isStart)
                {
                    fallbacks.AddRange(ExpandCandidates("default_reload_start"));
                    fallbacks.AddRange(ExpandCandidates("default_reload"));
                }
                else if (isEnd)
                {
                    fallbacks.AddRange(ExpandCandidates("default_reload_end"));
                }
                else
                {
                    fallbacks.AddRange(ExpandCandidates("default_reload"));
                }
                fallbacks.AddRange(ExpandCandidates("default"));

                string filePath = attempts.FirstOrDefault(File.Exists) ?? fallbacks.FirstOrDefault(File.Exists);

                string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                    GunLogger.Debug($"[GunReload] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");
                else
                    GunLogger.Debug($"[GunReload] 未捕获到 ItemAgent_Gun 组件，查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");

                if (filePath == null) return;

                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[GunReload] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

                // Mute original Studio event
                try { if (__result.HasValue) { var ev = __result.Value; ev.setVolume(0f); } } catch { }

                // Derive 3D min/max distance
                float min = 1f, max = 50f;
                try
                {
                    if (__result.HasValue && __result.Value.isValid())
                    {
                        if (__result.Value.getDescription(out FMOD.Studio.EventDescription desc) == RESULT.OK)
                        {
                            try { desc.getMinMaxDistance(out min, out max); } catch { }
                        }
                    }
                }
                catch { }

                // Play custom core sound (choose mode by file type)
                string fullPath = filePath;
                try { fullPath = System.IO.Path.GetFullPath(filePath); } catch { }
                var mode = ComputeModeForFile(fullPath) | MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(fullPath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GunLogger.Info($"[GunReload] createSound 失败({r1})，放弃自定义音效");
                    return;
                }
                try { sound.set3DMinMaxDistance(min, max); } catch { }

                ChannelGroup group = AcquireSfxChannelGroup();
                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    GunLogger.Info($"[GunReload] playSound 失败({r2})");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return;
                }

                Vector3 pos = Vector3.zero;
                try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                try { channel.set3DMinMaxDistance(min, max); } catch { }
                try { channel.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                try { channel.setPaused(false); } catch { }

                GunLogger.Debug($"[GunReload] 覆盖播放 {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");
                try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, CleanupMaxSeconds)); } catch { }
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunReload] Postfix 覆盖异常: {ex.Message}");
            }
        }

        private static (ItemAgent_Gun gun, string typeIdStr) GetGunAndTypeId(GameObject go)
        {
            ItemAgent_Gun gun = null;
            try
            {
                if (go != null)
                    gun = go.GetComponent<ItemAgent_Gun>() ?? go.GetComponentInParent<ItemAgent_Gun>();
            }
            catch { }

            string typeIdStr = string.Empty;
            try { if (gun?.Item != null) typeIdStr = gun.Item.TypeID.ToString(); } catch { }
            return (gun, typeIdStr);
        }

        private static bool TryPlayFromCandidates(
            ref FMOD.Studio.EventInstance? __result,
            string eventName,
            GameObject gameObject,
            List<string> attempts,
            IEnumerable<string> fallbackCandidates,
            string typeIdStr,
            string soundKey,
            string tag)
        {
            // Resolve final file path
            string filePath = attempts.FirstOrDefault(File.Exists);
            if (filePath == null)
                filePath = fallbackCandidates.FirstOrDefault(File.Exists);

            string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());

            if (!string.IsNullOrWhiteSpace(typeIdStr))
                GunLogger.Debug($"[{tag}] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");
            else
                GunLogger.Debug($"[{tag}] 未捕获到 ItemAgent_Gun 组件，查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");

            if (filePath == null)
            {
                // No custom file found → pass through original event
                return false;
            }

            try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[{tag}] FMOD 未初始化，放行原始事件: {eventName}"); return false; } } catch { }

            // Create 3D sound
            var mode = MODE.CREATESAMPLE | MODE._3D | MODE.LOOP_OFF;
            var r1 = RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
            if (r1 != RESULT.OK || !sound.hasHandle())
            {
                GunLogger.Info($"[{tag}] createSound 失败({r1})，放行原始事件: {eventName}");
                return false;
            }

            try { sound.set3DMinMaxDistance(1f, 50f); } catch { }

            // Route to SFX group (fallbacks to Master)
            ChannelGroup group = AcquireSfxChannelGroup();

            var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
            if (r2 != RESULT.OK || !channel.hasHandle())
            {
                GunLogger.Info($"[{tag}] playSound 失败({r2})，放行原始事件: {eventName}");
                try { if (sound.hasHandle()) sound.release(); } catch { }
                return false;
            }

            Vector3 pos = Vector3.zero;

            try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
            var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
            var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
            try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
            try { channel.setPaused(false); } catch { }

            GunLogger.Debug($"[{tag}] 替换 {eventName} → {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");

            try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, CleanupMaxSeconds)); } catch { }

            __result = new FMOD.Studio.EventInstance?();
            return true; // swallow original event
        }

        private static ChannelGroup AcquireSfxChannelGroup()
        {
            ChannelGroup group = default;

            // 1) Prefer fresh bus each call to avoid stale cached pointers
            try
            {
                var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");
                if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle())
                {
                    try { ModBehaviour.SfxGroup = cg; } catch { }
                    return cg;
                }
            }
            catch { }

            // 2) Try alternative bus path
            try
            {
                var sfxBusAlt = RuntimeManager.GetBus("bus:/SFX");
                if (sfxBusAlt.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle())
                {
                    try { ModBehaviour.SfxGroup = cg2; } catch { }
                    return cg2;
                }
            }
            catch { }

            // 3) Fallback to cached only if nothing else worked
            try { if (ModBehaviour.SfxGroup.hasHandle()) return ModBehaviour.SfxGroup; } catch { }

            // 4) Last resort: route to Master
            try { RuntimeManager.CoreSystem.getMasterChannelGroup(out group); } catch { }
            return group;
        }

        // Postfix: 静音原 Studio 事件并用 Core 播放自定义音频（射击）
        public static void PostfixOverrideShoot(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Shoot/";
            if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

            string soundKey = eventName.Substring(prefix.Length);
            if (string.IsNullOrWhiteSpace(soundKey)) return;

            var (gun, typeIdStr) = GetGunAndTypeId(gameObject);


            bool silenced = false;
            try { if (gun != null) silenced = gun.Silenced; } catch { }
            if (soundKey.EndsWith("_mute", StringComparison.OrdinalIgnoreCase)) silenced = true;

            var attempts = new List<string>();
            string legacySoundKeyPath = Path.Combine(BaseDir, soundKey + ".mp3");
            if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
            {
                if (silenced) attempts.Add(Path.Combine(BaseDir, typeIdStr + "_mute.mp3"));
                attempts.Add(Path.Combine(BaseDir, typeIdStr + ".mp3"));
                attempts.Add(legacySoundKeyPath);
            }
            else
            {
                attempts.Add(legacySoundKeyPath);
            }

            string[] fallbacks = new[] { Path.Combine(BaseDir, "default.mp3") };

            TryPlayFromCandidates_Postfix(ref __result, eventName, gameObject, attempts, fallbacks, typeIdStr, soundKey, tag: "GunShoot");
        }

        // Postfix: 静音原 Studio 事件并用 Core 播放自定义音频（换弹）
        public static void PostfixOverrideReload(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Reload/";
            if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

            string soundKey = eventName.Substring(prefix.Length); // e.g., "mag_start" or "mag_end"
            if (string.IsNullOrWhiteSpace(soundKey)) return;

            bool isStart = soundKey.EndsWith("_start", StringComparison.OrdinalIgnoreCase);
            bool isEnd   = soundKey.EndsWith("_end",   StringComparison.OrdinalIgnoreCase);

            var (gun, typeIdStr) = GetGunAndTypeId(gameObject);

            var attempts = new List<string>();
            if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
            {
                if (isStart)
                {
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload_start.mp3"));
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload.mp3")); // generic reload fallback for start
                }
                else if (isEnd)
                {
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload_end.mp3"));
                }
                else
                {
                    attempts.Add(Path.Combine(BaseDir, typeIdStr + "_reload.mp3"));
                }
            }

            // Always include the raw soundKey (e.g., mag_start.mp3, mag_end.mp3)
            attempts.Add(Path.Combine(BaseDir, soundKey + ".mp3"));

            // Default fallbacks according to phase
            List<string> fallbacks = new List<string>();
            if (isStart)
            {
                fallbacks.Add(Path.Combine(BaseDir, "default_reload_start.mp3"));
                fallbacks.Add(Path.Combine(BaseDir, "default_reload.mp3"));
            }
            else if (isEnd)
            {
                fallbacks.Add(Path.Combine(BaseDir, "default_reload_end.mp3"));
            }
            else
            {
                fallbacks.Add(Path.Combine(BaseDir, "default_reload.mp3"));
            }
            fallbacks.Add(Path.Combine(BaseDir, "default.mp3"));

            TryPlayFromCandidates_Postfix(ref __result, eventName, gameObject, attempts, fallbacks, typeIdStr, soundKey, tag: "GunReload");
        }

        private static bool TryPlayFromCandidates_Postfix(
            ref FMOD.Studio.EventInstance? __result,
            string eventName,
            GameObject gameObject,
            List<string> attempts,
            IEnumerable<string> fallbackCandidates,
            string typeIdStr,
            string soundKey,
            string tag)
        {
            // Resolve final file path
            string filePath = attempts.FirstOrDefault(File.Exists);
            if (filePath == null)
                filePath = fallbackCandidates.FirstOrDefault(File.Exists);

            string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());

            if (!string.IsNullOrWhiteSpace(typeIdStr))
                GunLogger.Debug($"[{tag}] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");
            else
                GunLogger.Debug($"[{tag}] 未捕获到 ItemAgent_Gun 组件，查找顺序: {chain}{(filePath!=null? ", 最终使用: "+filePath : ", 最终使用: 未找到自定义文件")}");

            if (filePath == null)
            {
                // No custom file found → pass through original event
                return false;
            }

            try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[{tag}] FMOD 未初始化，放行原始事件: {eventName}"); return false; } } catch { }

            // Silence original Studio event if exists
            try
            {
                if (__result.HasValue)
                {
                    var ev = __result.Value;
                    try { ev.setVolume(0f); } catch { }
                }
            }
            catch { }

            // Create 3D sound
            var mode = MODE.CREATESAMPLE | MODE._3D | MODE.LOOP_OFF;
            var r1 = RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
            if (r1 != RESULT.OK || !sound.hasHandle())
            {
                GunLogger.Info($"[{tag}] createSound 失败({r1})，放行原始事件: {eventName}");
                return false;
            }

            try { sound.set3DMinMaxDistance(1f, 50f); } catch { }

            // Route to SFX group (fallbacks to Master)
            ChannelGroup group = AcquireSfxChannelGroup();

            var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
            if (r2 != RESULT.OK || !channel.hasHandle())
            {


                GunLogger.Info($"[{tag}] playSound 失败({r2})，放行原始事件: {eventName}");
                try { if (sound.hasHandle()) sound.release(); } catch { }
                return false;
            }

            Vector3 pos = Vector3.zero;
            try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
            var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
            var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
            try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
            try { channel.setPaused(false); } catch { }

            GunLogger.Debug($"[{tag}] 替换 {eventName} → {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");

            try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, CleanupMaxSeconds)); } catch { }

            return true;
        }

        private static MODE ComputeModeForFile(string path)
        {
            try
            {
                string ext = (System.IO.Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
                if (ext == ".mp3" || ext == ".ogg" || ext == ".oga")
                    return MODE.CREATESTREAM;
            }
            catch { }
            return MODE.CREATESAMPLE;
        }

        private static IEnumerator Cleanup(Sound sound, Channel channel, float maxSec)
        {
            float end = Time.realtimeSinceStartup + Mathf.Max(1f, maxSec);
            try
            {
                while (Time.realtimeSinceStartup < end)
                {
                    bool playing = false;
                    try { if (channel.hasHandle()) channel.isPlaying(out playing); } catch { }
                    if (!playing) break;
                    yield return new WaitForSeconds(0.05f);
                }
            }
            finally
            {
                try { if (channel.hasHandle()) channel.stop(); } catch { }
                try { if (sound.hasHandle()) sound.release(); } catch { }
            }
        }
    }
}

