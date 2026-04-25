using DuckovCustomSounds.CustomEnemySounds.Context;
using UnityEngine;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// DuckovCustomSounds 对外语音扩展入口。
    /// </summary>
    public static class CustomModController
    {
        /// <summary>注册外部 Provider。</summary>
        public static bool RegisterVoicePackProvider(string modId, IVoicePackProvider provider) => ExternalRouter.Register(modId, provider);

        /// <summary>注销外部 Provider。</summary>
        public static bool UnregisterVoicePackProvider(string modId) => ExternalRouter.Unregister(modId);

        /// <summary>
        /// 查询某个 GameObject 的敌人上下文快照。
        /// </summary>
        public static bool TryGetEnemyContext(GameObject? owner, out EnemyContextData? data)
        {
            data = null;
            try
            {
                if (owner == null) return false;
                if (EnemyContextRegistry.TryGet(owner, out var ctx) && ctx != null)
                {
                    data = ExternalRouter.FromInternal(ctx);
                    return data != null && data.IsValid;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
