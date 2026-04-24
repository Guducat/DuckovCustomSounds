using System;
using System.Linq;
using System.Reflection;
using DuckovCustomSounds.Logging;
using UnityEngine;

// 替换为你的mod命名空间, 防止多个同名ModConfigAPI冲突
namespace DuckovCustomSounds.ModConfig {
/// <summary>
/// ModConfig 安全接口封装类 - 提供不抛异常的静态接口
/// ModConfig Safe API Wrapper Class - Provides non-throwing static interfaces
/// </summary>
public static class ModConfigAPI
{
    public static string ModConfigName = "ModConfig";

    // Ensure this match the number of ModConfig.ModBehaviour.VERSION
    // 这里确保版本号与ModConfig.ModBehaviour.VERSION匹配
    private const int ModConfigVersion = 1;

    private static string TAG = $"ModConfig_v{ModConfigVersion}";
    private static readonly ILog Log = LogManager.GetLogger("Core").ForScope("ModConfigAPI");

    private static Type modBehaviourType = null!;
    private static Type optionsManagerType = null!;
    public static bool isInitialized = false;
    private static bool versionChecked = false;
    private static bool isVersionCompatible = false;

    private static bool CheckVersionCompatibility()
    {
        if (versionChecked)
            return isVersionCompatible;

        try
        {
            FieldInfo versionField = modBehaviourType.GetField("VERSION", BindingFlags.Public | BindingFlags.Static);
            if (versionField != null && versionField.FieldType == typeof(int))
            {
                int modConfigVersion = (int)versionField.GetValue(null);
                isVersionCompatible = (modConfigVersion == ModConfigVersion);

                if (!isVersionCompatible)
                {
                    Log.Error($"{TAG}: 版本不匹配！API版本: {ModConfigVersion}, ModConfig版本: {modConfigVersion}");
                    return false;
                }

                Log.Info($"{TAG}: 版本检查通过: {ModConfigVersion}");
                versionChecked = true;
                return true;
            }
            else
            {
                Log.Warning($"{TAG}: 未找到版本信息字段，跳过版本检查");
                isVersionCompatible = true;
                versionChecked = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 版本检查失败: {ex.Message}");
            isVersionCompatible = false;
            versionChecked = true;
            return false;
        }
    }

    public static bool Initialize()
    {
        try
        {
            if (isInitialized)
                return true;

            modBehaviourType = FindTypeInAssemblies("ModConfig.ModBehaviour");
            if (modBehaviourType == null)
            {
                Log.Warning($"{TAG}: ModConfig.ModBehaviour 类型未找到，ModConfig 可能未加载");
                return false;
            }

            optionsManagerType = FindTypeInAssemblies("ModConfig.OptionsManager_Mod");
            if (optionsManagerType == null)
            {
                Log.Warning($"{TAG}: ModConfig.OptionsManager_Mod 类型未找到");
                return false;
            }

            if (!CheckVersionCompatibility())
            {
                Log.Warning($"{TAG}: ModConfig version mismatch!!!");
                return false;
            }

            string[] requiredMethods = {
                "AddDropdownList",
                "AddInputWithSlider",
                "AddOnOptionsChangedDelegate",
                "RemoveOnOptionsChangedDelegate",
            };

            foreach (string methodName in requiredMethods)
            {
                MethodInfo method = modBehaviourType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Log.Error($"{TAG}: 必要方法 {methodName} 未找到");
                    return false;
                }
            }

            isInitialized = true;
            Log.Info($"{TAG}: ModConfigAPI 初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 初始化失败: {ex.Message}");
            return false;
        }
    }

    private static Type FindTypeInAssemblies(string typeName)
    {
        try
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    if (assembly.FullName.Contains("ModConfig"))
                    {
                        Log.Info($"{TAG}: 找到 ModConfig 相关程序集: {assembly.FullName}");
                    }

                    Type type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        Log.Info($"{TAG}: 在程序集 {assembly.FullName} 中找到类型 {typeName}");
                        return type;
                    }
                }
                catch { }
            }

            Log.Warning($"{TAG}: 在所有程序集中未找到类型 {typeName}，已加载程序集数量: {assemblies.Length}");
            foreach (var assembly in assemblies.Where(a => a.FullName.Contains("ModConfig")))
            {
                Log.Info($"{TAG}: ModConfig 相关程序集: {assembly.FullName}");
            }

            return null!;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 程序集扫描失败: {ex.Message}");
            return null!;
        }
    }

    public static bool SafeAddOnOptionsChangedDelegate(Action<string> action)
    {
        if (!Initialize())
            return false;
        if (action == null)
        {
            Log.Warning($"{TAG}: 不能添加空的事件委托");
            return false;
        }
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { action });
            Log.Info($"{TAG}: 成功添加选项变更事件委托");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 添加选项变更事件委托失败: {ex.Message}");
            return false;
        }
    }

    public static bool SafeRemoveOnOptionsChangedDelegate(Action<string> action)
    {
        if (!Initialize())
            return false;
        if (action == null)
        {
            Log.Warning($"{TAG}: 不能移除空的事件委托");
            return false;
        }
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("RemoveOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { action });
            Log.Info($"{TAG}: 成功移除选项变更事件委托");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 移除选项变更事件委托失败: {ex.Message}");
            return false;
        }
    }

    public static string BuildKey(string modName, string key)
    {
        if (string.IsNullOrEmpty(modName))
            return key ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            return modName;

        string prefix = modName + "_";
        return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? key : prefix + key;
    }

    public static string BuildKey(ModConfigScope scope, string key)
    {
        return BuildKey(GetStorageName(scope), key);
    }

    public static bool IsKeyForMod(string changedKey, string modName)
    {
        if (string.IsNullOrEmpty(changedKey))
            return true;
        if (string.IsNullOrEmpty(modName))
            return false;

        return changedKey.StartsWith(modName + "_", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsKeyForMod(string changedKey, ModConfigScope scope)
    {
        return IsKeyForMod(changedKey, GetStorageName(scope));
    }

    public static bool IsKeyForOption(string changedKey, string modName, string optionKey)
    {
        if (string.IsNullOrEmpty(changedKey))
            return true;
        if (changedKey.Equals(optionKey, StringComparison.OrdinalIgnoreCase))
            return true;

        return changedKey.Equals(BuildKey(modName, optionKey), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsKeyForOption(string changedKey, ModConfigScope scope, string optionKey)
    {
        return IsKeyForOption(changedKey, GetStorageName(scope), optionKey);
    }

    public static bool SafeAddDropdownList(ModConfigScope scope, string key, string description, System.Collections.Generic.SortedDictionary<string, object> options, Type valueType, object defaultValue)
    {
        return SafeAddDropdownListCore(GetDisplayName(scope), GetStorageName(scope), key, description, options, valueType, defaultValue);
    }

    public static bool SafeAddDropdownList(string modName, string key, string description, System.Collections.Generic.SortedDictionary<string, object> options, Type valueType, object defaultValue)
    {
        return SafeAddDropdownListCore(modName, modName, key, description, options, valueType, defaultValue);
    }

    public static bool SafeAddInputWithSlider(ModConfigScope scope, string key, string description, Type valueType, object defaultValue, UnityEngine.Vector2? sliderRange = null)
    {
        return SafeAddInputWithSliderCore(GetDisplayName(scope), GetStorageName(scope), key, description, valueType, defaultValue, sliderRange);
    }

    public static bool SafeAddInputWithSlider(string modName, string key, string description, Type valueType, object defaultValue, UnityEngine.Vector2? sliderRange = null)
    {
        return SafeAddInputWithSliderCore(modName, modName, key, description, valueType, defaultValue, sliderRange);
    }

    public static bool SafeAddBoolDropdownList(ModConfigScope scope, string key, string description, bool defaultValue)
    {
        return SafeAddDropdownListCore(GetDisplayName(scope), GetStorageName(scope), key, description, BuildLocalizedBoolOptions(), typeof(bool), defaultValue);
    }

    public static bool SafeAddBoolDropdownList(string modName, string key, string description, bool defaultValue)
    {
        return SafeAddDropdownListCore(modName, modName, key, description, BuildLocalizedBoolOptions(), typeof(bool), defaultValue);
    }

    public static T SafeLoad<T>(ModConfigScope scope, string key, T defaultValue = default!)
    {
        return SafeLoad(GetStorageName(scope), key, defaultValue);
    }

    public static T SafeLoad<T>(string mod_name, string key, T defaultValue = default!)
    {
        key = BuildKey(mod_name, key);
        if (!Initialize())
            return defaultValue;
        if (string.IsNullOrEmpty(key))
        {
            Log.Warning($"{TAG}: 配置键不能为空");
            return defaultValue;
        }
        try
        {
            MethodInfo loadMethod = optionsManagerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
            if (loadMethod == null)
            {
                Log.Error($"{TAG}: 未找到 OptionsManager_Mod.Load 方法");
                return defaultValue;
            }
            MethodInfo genericLoadMethod = loadMethod.MakeGenericMethod(typeof(T));
            object? result = genericLoadMethod.Invoke(null, new object?[] { key, defaultValue });
            if (result == null)
            {
                Log.Warning($"{TAG}: 配置 {key} 返回空值，使用默认值");
                return defaultValue;
            }

            Log.Info($"{TAG}: 成功加载配置: {key} = {result}");
            return (T)result;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 加载配置失败 {key}: {ex.Message}");
            return defaultValue;
        }
    }

    public static bool SafeSave<T>(ModConfigScope scope, string key, T value)
    {
        return SafeSave(GetStorageName(scope), key, value);
    }

    public static bool SafeSave<T>(string mod_name, string key, T value)
    {
        key = BuildKey(mod_name, key);
        if (!Initialize())
            return false;
        if (string.IsNullOrEmpty(key))
        {
            Log.Warning($"{TAG}: 配置键不能为空");
            return false;
        }
        try
        {
            MethodInfo saveMethod = optionsManagerType.GetMethod("Save", BindingFlags.Public | BindingFlags.Static);
            if (saveMethod == null)
            {
                Log.Error($"{TAG}: 未找到 OptionsManager_Mod.Save 方法");
                return false;
            }
            MethodInfo genericSaveMethod = saveMethod.MakeGenericMethod(typeof(T));
            genericSaveMethod.Invoke(null, new object?[] { key, value });
            Log.Info($"{TAG}: 成功保存配置: {key} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 保存配置失败 {key}: {ex.Message}");
            return false;
        }
    }

    public static bool IsAvailable() => Initialize();

    public static string GetVersionInfo()
    {
        if (!Initialize())
            return "ModConfig 未加载 | ModConfig not loaded";
        try
        {
            FieldInfo versionField = modBehaviourType.GetField("VERSION", BindingFlags.Public | BindingFlags.Static);
            if (versionField != null && versionField.FieldType == typeof(int))
            {
                int modConfigVersion = (int)versionField.GetValue(null);
                string compatibility = (modConfigVersion == ModConfigVersion) ? "兼容" : "不兼容";
                return $"ModConfig v{modConfigVersion} (API v{ModConfigVersion}, {compatibility})";
            }
            PropertyInfo versionProperty = modBehaviourType.GetProperty("VERSION", BindingFlags.Public | BindingFlags.Static);
            if (versionProperty != null)
            {
                object versionValue = versionProperty.GetValue(null);
                return versionValue?.ToString() ?? "未知版本 | Unknown version";
            }
            return "ModConfig 已加载（版本信息不可用） | ModConfig loaded (version info unavailable)";
        }
        catch
        {
            return "ModConfig 已加载（版本检查失败） | ModConfig loaded (version check failed)";
        }
    }

    public static bool IsVersionCompatible()
    {
        if (!Initialize())
            return false;
        return isVersionCompatible;
    }

    private static string GetDisplayName(ModConfigScope scope)
    {
        return scope?.DisplayName ?? string.Empty;
    }

    private static string GetStorageName(ModConfigScope scope)
    {
        return scope?.StorageName ?? string.Empty;
    }

    private static bool SafeAddDropdownListCore(string displayName, string storageName, string key, string description, System.Collections.Generic.SortedDictionary<string, object> options, Type valueType, object defaultValue)
    {
        key = BuildKey(storageName, key);
        if (!Initialize())
            return false;
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddDropdownList", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { displayName, key, description, options, valueType, defaultValue });
            Log.Info($"{TAG}: 成功添加下拉列表: {displayName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 添加下拉列表失败 {displayName}.{key}: {ex.Message}");
            return false;
        }
    }

    private static bool SafeAddInputWithSliderCore(string displayName, string storageName, string key, string description, Type valueType, object defaultValue, UnityEngine.Vector2? sliderRange = null)
    {
        key = BuildKey(storageName, key);
        if (!Initialize())
            return false;
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddInputWithSlider", BindingFlags.Public | BindingFlags.Static);
            object?[] parameters = sliderRange.HasValue
                ? new object?[] { displayName, key, description, valueType, defaultValue, sliderRange.Value }
                : new object?[] { displayName, key, description, valueType, defaultValue, null };
            method.Invoke(null, parameters);
            Log.Info($"{TAG}: 成功添加滑条输入框: {displayName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{TAG}: 添加滑条输入框失败 {displayName}.{key}: {ex.Message}");
            return false;
        }
    }

    private static System.Collections.Generic.SortedDictionary<string, object> BuildLocalizedBoolOptions()
    {
        return new System.Collections.Generic.SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["启用"] = true,
            ["禁用"] = false,
        };
    }
}
}
