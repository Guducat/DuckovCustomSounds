using System;
using System.Linq;
using System.Reflection;
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

    private static Type modBehaviourType;
    private static Type optionsManagerType;
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
                    Debug.LogError($"[{TAG}] 版本不匹配！API版本: {ModConfigVersion}, ModConfig版本: {modConfigVersion}");
                    return false;
                }

                Debug.Log($"[{TAG}] 版本检查通过: {ModConfigVersion}");
                versionChecked = true;
                return true;
            }
            else
            {
                Debug.LogWarning($"[{TAG}] 未找到版本信息字段，跳过版本检查");
                isVersionCompatible = true;
                versionChecked = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 版本检查失败: {ex.Message}");
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
                Debug.LogWarning($"[{TAG}] ModConfig.ModBehaviour 类型未找到，ModConfig 可能未加载");
                return false;
            }

            optionsManagerType = FindTypeInAssemblies("ModConfig.OptionsManager_Mod");
            if (optionsManagerType == null)
            {
                Debug.LogWarning($"[{TAG}] ModConfig.OptionsManager_Mod 类型未找到");
                return false;
            }

            if (!CheckVersionCompatibility())
            {
                Debug.LogWarning($"[{TAG}] ModConfig version mismatch!!!");
                return false;
            }

            string[] requiredMethods = {
                "AddDropdownList",
                "AddInputWithSlider",
                "AddBoolDropdownList",
                "AddOnOptionsChangedDelegate",
                "RemoveOnOptionsChangedDelegate",
            };

            foreach (string methodName in requiredMethods)
            {
                MethodInfo method = modBehaviourType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError($"[{TAG}] 必要方法 {methodName} 未找到");
                    return false;
                }
            }

            isInitialized = true;
            Debug.Log($"[{TAG}] ModConfigAPI 初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 初始化失败: {ex.Message}");
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
                        Debug.Log($"[{TAG}] 找到 ModConfig 相关程序集: {assembly.FullName}");
                    }

                    Type type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        Debug.Log($"[{TAG}] 在程序集 {assembly.FullName} 中找到类型 {typeName}");
                        return type;
                    }
                }
                catch { }
            }

            Debug.LogWarning($"[{TAG}] 在所有程序集中未找到类型 {typeName}，已加载程序集数量: {assemblies.Length}");
            foreach (var assembly in assemblies.Where(a => a.FullName.Contains("ModConfig")))
            {
                Debug.Log($"[{TAG}] ModConfig 相关程序集: {assembly.FullName}");
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 程序集扫描失败: {ex.Message}");
            return null;
        }
    }

    public static bool SafeAddOnOptionsChangedDelegate(Action<string> action)
    {
        if (!Initialize())
            return false;
        if (action == null)
        {
            Debug.LogWarning($"[{TAG}] 不能添加空的事件委托");
            return false;
        }
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { action });
            Debug.Log($"[{TAG}] 成功添加选项变更事件委托");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加选项变更事件委托失败: {ex.Message}");
            return false;
        }
    }

    public static bool SafeRemoveOnOptionsChangedDelegate(Action<string> action)
    {
        if (!Initialize())
            return false;
        if (action == null)
        {
            Debug.LogWarning($"[{TAG}] 不能移除空的事件委托");
            return false;
        }
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("RemoveOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { action });
            Debug.Log($"[{TAG}] 成功移除选项变更事件委托");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 移除选项变更事件委托失败: {ex.Message}");
            return false;
        }
    }

    public static bool SafeAddDropdownList(string modName, string key, string description, System.Collections.Generic.SortedDictionary<string, object> options, Type valueType, object defaultValue)
    {
        key = $"{modName}_{key}";
        if (!Initialize())
            return false;
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddDropdownList", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { modName, key, description, options, valueType, defaultValue });
            Debug.Log($"[{TAG}] 成功添加下拉列表: {modName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加下拉列表失败 {modName}.{key}: {ex.Message}");
            return false;
        }
    }

    public static bool SafeAddInputWithSlider(string modName, string key, string description, Type valueType, object defaultValue, UnityEngine.Vector2? sliderRange = null)
    {
        key = $"{modName}_{key}";
        if (!Initialize())
            return false;
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddInputWithSlider", BindingFlags.Public | BindingFlags.Static);
            object[] parameters = sliderRange.HasValue ?
                new object[] { modName, key, description, valueType, defaultValue, sliderRange.Value } :
                new object[] { modName, key, description, valueType, defaultValue, null };
            method.Invoke(null, parameters);
            Debug.Log($"[{TAG}] 成功添加滑条输入框: {modName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加滑条输入框失败 {modName}.{key}: {ex.Message}");
            return false;
        }
    }

    public static bool SafeAddBoolDropdownList(string modName, string key, string description, bool defaultValue)
    {
        key = $"{modName}_{key}";
        if (!Initialize())
            return false;
        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddBoolDropdownList", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { modName, key, description, defaultValue });
            Debug.Log($"[{TAG}] 成功添加布尔下拉列表: {modName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加布尔下拉列表失败 {modName}.{key}: {ex.Message}");
            return false;
        }
    }

    public static T SafeLoad<T>(string mod_name, string key, T defaultValue = default(T))
    {
        key = $"{mod_name}_{key}";
        if (!Initialize())
            return defaultValue;
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[{TAG}] 配置键不能为空");
            return defaultValue;
        }
        try
        {
            MethodInfo loadMethod = optionsManagerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
            if (loadMethod == null)
            {
                Debug.LogError($"[{TAG}] 未找到 OptionsManager_Mod.Load 方法");
                return defaultValue;
            }
            MethodInfo genericLoadMethod = loadMethod.MakeGenericMethod(typeof(T));
            object result = genericLoadMethod.Invoke(null, new object[] { key, defaultValue });
            Debug.Log($"[{TAG}] 成功加载配置: {key} = {result}");
            return (T)result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 加载配置失败 {key}: {ex.Message}");
            return defaultValue;
        }
    }

    public static bool SafeSave<T>(string mod_name, string key, T value)
    {
        key = $"{mod_name}_{key}";
        if (!Initialize())
            return false;
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[{TAG}] 配置键不能为空");
            return false;
        }
        try
        {
            MethodInfo saveMethod = optionsManagerType.GetMethod("Save", BindingFlags.Public | BindingFlags.Static);
            if (saveMethod == null)
            {
                Debug.LogError($"[{TAG}] 未找到 OptionsManager_Mod.Save 方法");
                return false;
            }
            MethodInfo genericSaveMethod = saveMethod.MakeGenericMethod(typeof(T));
            genericSaveMethod.Invoke(null, new object[] { key, value });
            Debug.Log($"[{TAG}] 成功保存配置: {key} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 保存配置失败 {key}: {ex.Message}");
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
}
}

