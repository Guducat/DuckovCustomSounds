using System;
using System.Collections.Generic;
using System.Linq;
using Duckov;

namespace DuckovCustomSounds.CustomEnemySounds
{
    internal sealed class VoiceRuleEngine
    {
        private VoiceConfig _config;

        public void Reload(VoiceConfig config)
        {
            _config = config ?? new VoiceConfig();
            CESLogger.Info($"[CES:Rule] 规则已加载：{_config.Rules?.Count ?? 0} 条，默认模板: {_config.DefaultPattern}");
        }

        public bool TryRoute(EnemyContext ctx, string soundKey, AudioManager.VoiceType voiceType, out VoiceRoute route)
        {
            route = null;
            if (_config == null) return false;

            CESLogger.Debug($"[CES:Rule] ctx.team={ (ctx!=null? ctx.GetTeamNormalized() : "null") }, rank={ (ctx!=null? ctx.GetRank() : "null") }");

            // 1) rules
            if (_config.Rules != null)
            {
                foreach (var r in _config.Rules)
                {
                    string detail;
                    bool m = MatchesDetailed(r, ctx, soundKey, out detail);
                    CESLogger.Debug($"[CES:Rule] 检查规则: {Describe(r)} => match={m} ({detail})");
                    if (!m) continue;

                    var vtStr = !string.IsNullOrEmpty(r.ForceVoiceType) ? r.ForceVoiceType : voiceType.ToString();
                    var pattern = string.IsNullOrEmpty(r.FilePattern) ? _config.DefaultPattern : r.FilePattern;
                    pattern = SanitizePatternLocal(pattern);
                    CESLogger.Debug($"[CES:Rule] 使用模板: {pattern}, vt={vtStr}, soundKey={soundKey}");

                    var cands = PathBuilder.BuildCandidates(pattern, ctx, soundKey, ParseVoiceType(vtStr, voiceType), _config.Fallback.PreferredExtensions, true);
                    if (PathBuilder.TryResolveExisting(cands, _config.Debug.ValidateFileExists, out var chosen, out var tried))
                    {
                        CESLogger.Info($"[CES:Rule] 命中: {chosen}");
                        route = new VoiceRoute { UseCustom = true, FileFullPath = chosen, MatchRule = Describe(r), TriedPaths = tried };
                        return true;
                    }
                    else
                    {
                        CESLogger.Debug($"[CES:Rule] 未命中文件。尝试路径数={ (tried != null ? tried.Count : 0) }");
                    }
                    // 继续执行
                }
            }

            // 2) default pattern (with and without soundKey)
            {
                var defPattern = SanitizePatternLocal(_config.DefaultPattern);
                CESLogger.Debug($"[CES:Rule] 尝试默认模板: {defPattern}, vt={voiceType}, soundKey={soundKey}");
                var cands = PathBuilder.BuildCandidates(defPattern, ctx, soundKey, voiceType, _config.Fallback.PreferredExtensions, true);
                if (PathBuilder.TryResolveExisting(cands, _config.Debug.ValidateFileExists, out var chosen, out var tried))
                {
                    CESLogger.Info($"[CES:Rule] 默认模板命中: {chosen}");
                    route = new VoiceRoute { UseCustom = true, FileFullPath = chosen, MatchRule = "<default>", TriedPaths = tried };
                    return true;
                }
                else
                {
                    CESLogger.Debug("[CES:Rule] 默认模板未命中。");
                }
            }

            // 3) no route
            route = new VoiceRoute { UseCustom = false, FileFullPath = null, MatchRule = "<none>", TriedPaths = new List<string>() };
            return false;
        }

        private static bool Matches(VoiceRuleConfig r, EnemyContext ctx, string soundKey)
        {
            if (r == null) return false;
            if (ctx == null) return false;
            if (!string.IsNullOrEmpty(r.Team))
            {
                var want = r.Team.Trim().ToLowerInvariant();
                if (ctx.GetTeamNormalized() != want) return false;
            }
            if (!string.IsNullOrEmpty(r.IconType))
            {
                var want = r.IconType.Trim().ToLowerInvariant();
                var rank = ctx.GetRank();
                if (want == "boss" || want == "elite" || want == "normal")
                {
                    if (rank != want) return false;
                }
                else
                {
                    // Fallback: compare raw iconType string
                    if ((ctx.IconType ?? string.Empty).ToLowerInvariant().IndexOf(want, StringComparison.OrdinalIgnoreCase) < 0) return false;
                }
            }
            if (r.MinHealth.HasValue && ctx.Health < r.MinHealth.Value) return false;
            if (r.MaxHealth.HasValue && ctx.Health > r.MaxHealth.Value) return false;
            if (!string.IsNullOrEmpty(r.NameKeyContains))
            {
                var nk = ctx.NameKey ?? string.Empty;
                if (nk.IndexOf(r.NameKeyContains, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }
            if (r.SoundKeys != null && r.SoundKeys.Length > 0)
            {
                if (!r.SoundKeys.Contains(soundKey)) return false;
            }
            return true;
        }
        private static bool MatchesDetailed(VoiceRuleConfig r, EnemyContext ctx, string soundKey, out string detail)
        {
            detail = string.Empty;
            if (r == null || ctx == null)
            {
                detail = "r/ctx null";
                return false;
            }
            var parts = new List<string>();

            // Team check
            if (!string.IsNullOrEmpty(r.Team))
            {
                var wantTeam = r.Team.Trim().ToLowerInvariant();
                var gotTeam = ctx.GetTeamNormalized();
                bool ok = gotTeam == wantTeam;
                parts.Add($"team={wantTeam} got={gotTeam} => {(ok ? "ok" : "fail")}");
                if (!ok) { detail = string.Join("; ", parts); return false; }
            }

            // IconType / Rank check
            if (!string.IsNullOrEmpty(r.IconType))
            {
                var want = r.IconType.Trim().ToLowerInvariant();
                var rank = ctx.GetRank();
                bool rankMode = (want == "boss" || want == "elite" || want == "normal");
                bool ok = rankMode ? (rank == want)
                                   : ((ctx.IconType ?? string.Empty).ToLowerInvariant().IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0);
                parts.Add($"icon={r.IconType} via={(rankMode?"rank":"raw")} gotRank={rank} raw={ctx.IconType} => {(ok?"ok":"fail")}");
                if (!ok) { detail = string.Join("; ", parts); return false; }
            }

            // Health range
            if (r.MinHealth.HasValue)
            {
                bool ok = ctx.Health >= r.MinHealth.Value;
                parts.Add($"hp>={r.MinHealth.Value} got={ctx.Health} => {(ok?"ok":"fail")}");
                if (!ok) { detail = string.Join("; ", parts); return false; }
            }
            if (r.MaxHealth.HasValue)
            {
                bool ok = ctx.Health <= r.MaxHealth.Value;
                parts.Add($"hp<={r.MaxHealth.Value} got={ctx.Health} => {(ok?"ok":"fail")}");
                if (!ok) { detail = string.Join("; ", parts); return false; }
            }

            // NameKey partial
            if (!string.IsNullOrEmpty(r.NameKeyContains))
            {
                var nk = ctx.NameKey ?? string.Empty;
                bool ok = nk.IndexOf(r.NameKeyContains, StringComparison.OrdinalIgnoreCase) >= 0;
                parts.Add($"name~\"{r.NameKeyContains}\" in=\"{nk}\" => {(ok?"ok":"fail")}");
                if (!ok) { detail = string.Join("; ", parts); return false; }
            }

            // SoundKeys
            if (r.SoundKeys != null && r.SoundKeys.Length > 0)
            {
                bool ok = r.SoundKeys.Contains(soundKey);
                parts.Add($"soundKey in=[{string.Join(",", r.SoundKeys)}] got={soundKey} => {(ok?"ok":"fail")}");
                if (!ok) { detail = string.Join("; ", parts); return false; }
            }

            detail = string.Join("; ", parts);
            return true;
        }


        private static string Describe(VoiceRuleConfig r)
        {
            return $"team={r.Team}, icon={r.IconType}, hp=[{r.MinHealth},{r.MaxHealth}], name~={r.NameKeyContains}, forcedVT={r.ForceVoiceType}, pattern={r.FilePattern}";
        }

        private static AudioManager.VoiceType ParseVoiceType(string s, AudioManager.VoiceType fallback)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return fallback;
                return (AudioManager.VoiceType)Enum.Parse(typeof(AudioManager.VoiceType), s, true);
            }
            catch { return fallback; }
        }

        private static string SanitizePatternLocal(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            var t = p.Replace("{enemyType}/", string.Empty)
                     .Replace("/{enemyType}", string.Empty)
                     .Replace("{enemyType}", string.Empty);
            t = t.Replace("\\\\", "\\").Replace("//", "/");
            return t;
        }
    }
}

