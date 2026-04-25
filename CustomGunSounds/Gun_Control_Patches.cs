using HarmonyLib;
using Duckov; // ItemAgent_Gun
using System;

namespace DuckovCustomSounds.CustomGunSounds
{
    /// <summary>
    /// 枪械换弹控制补丁（停止/取消换弹）
    /// </summary>
    [HarmonyPatch(typeof(ItemAgent_Gun))]
    public static class ItemAgent_Gun_StopReloadSound_Patch
    {
        [HarmonyPatch("StopReloadSound")]
        [HarmonyPostfix]
        public static void Postfix(ItemAgent_Gun __instance)
        {
            try { GunUtil.StopCustomReloadFor(__instance); }
            catch (Exception ex) { GunLogger.Warning($"[GunReload] StopReloadSound 拦截处理异常: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(ItemAgent_Gun))]
    public static class ItemAgent_Gun_CancleReload_Patch
    {
        [HarmonyPatch("CancleReload")]
        [HarmonyPostfix]
        public static void Postfix(ItemAgent_Gun __instance)
        {
            try { GunUtil.StopCustomReloadFor(__instance); }
            catch (Exception ex) { GunLogger.Warning($"[GunReload] CancleReload 拦截处理异常: {ex.Message}"); }
        }
    }
}
