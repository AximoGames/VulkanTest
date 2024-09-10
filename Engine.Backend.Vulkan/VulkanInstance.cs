using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanInstance : IDisposable
{
    private readonly WindowManager _windowManager;
    private static IEnumerable<string>? _suppressDebugMessages;
    private static readonly string[] _requestedValidationLayers = new[] { "VK_LAYER_KHRONOS_validation" };

    public VkInstance Instance;
    private VkDebugUtilsMessengerEXT _debugMessenger = VkDebugUtilsMessengerEXT.Null;

    public VulkanInstance(string applicationName, bool enableValidation, WindowManager windowManager, IEnumerable<string>? suppressDebugMessages)
    {
        _windowManager = windowManager;
        _suppressDebugMessages = suppressDebugMessages;
        CreateInstance(applicationName, enableValidation);
    }

    private void CreateInstance(string applicationName, bool enableValidation)
    {
        var appInfo = new VkApplicationInfo
        {
            pApplicationName = applicationName.ToVkUtf8ReadOnlyString(),
            applicationVersion = new VkVersion(1, 0, 0),
            pEngineName = "Vortice".ToVkUtf8ReadOnlyString(),
            engineVersion = new VkVersion(1, 0, 0),
            apiVersion = vkEnumerateInstanceVersion()
        };

        List<string> instanceExtensions = new List<string>();
        instanceExtensions.AddRange(_windowManager.GetRequiredInstanceExtensions());

        List<string> instanceLayers = new List<string>();
        if (enableValidation)
            FindValidationLayers(instanceLayers);

        if (instanceLayers.Count > 0)
            instanceExtensions.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME.GetStringFromUtf8Buffer());

        using var vkInstanceExtensions = new VkStringArray(instanceExtensions);

        var instanceCreateInfo = new VkInstanceCreateInfo
        {
            pApplicationInfo = &appInfo,
            enabledExtensionCount = vkInstanceExtensions.Length,
            ppEnabledExtensionNames = vkInstanceExtensions
        };

        using var vkLayerNames = new VkStringArray(instanceLayers);
        if (instanceLayers.Count > 0)
        {
            instanceCreateInfo.enabledLayerCount = vkLayerNames.Length;
            instanceCreateInfo.ppEnabledLayerNames = vkLayerNames;
        }

        var debugUtilsCreateInfo = new VkDebugUtilsMessengerCreateInfoEXT
        {
            messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning,
            messageType = VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance | VkDebugUtilsMessageTypeFlagsEXT.General,
            pfnUserCallback = &DebugMessengerCallback
        };

        if (instanceLayers.Count > 0)
        {
            instanceCreateInfo.pNext = &debugUtilsCreateInfo;
        }

        VkResult result = vkCreateInstance(&instanceCreateInfo, null, out Instance);
        result.CheckResult("Failed to create Vulkan instance");

        vkLoadInstance(Instance);

        if (instanceLayers.Count > 0)
        {
            vkCreateDebugUtilsMessengerEXT(Instance, &debugUtilsCreateInfo, null, out _debugMessenger).CheckResult();
        }

        LogInstanceInfo(appInfo, instanceLayers, instanceExtensions);
    }

    private void LogInstanceInfo(VkApplicationInfo appInfo, List<string> instanceLayers, List<string> instanceExtensions)
    {
        Log.Info($"Created VkInstance with version: {appInfo.apiVersion.Major}.{appInfo.apiVersion.Minor}.{appInfo.apiVersion.Patch}");
        if (instanceLayers.Count > 0)
        {
            foreach (var layer in instanceLayers)
            {
                Log.Info($"Instance layer '{layer}'");
            }
        }

        foreach (string extension in instanceExtensions)
        {
            Log.Info($"Instance extension '{extension}'");
        }

        GetAvailableExtensions();
    }

    private void GetAvailableExtensions()
    {
        uint count;
        var result = vkEnumerateInstanceExtensionProperties(&count, null);
        if (result != VkResult.Success)
            throw new Exception($"Failed to enumerate instance extensions, {result}");

        var properties = new VkExtensionProperties[count];
        result = vkEnumerateInstanceExtensionProperties(properties);
        if (result != VkResult.Success)
            throw new Exception($"Failed to enumerate instance extensions, {result}");

        Log.Verbose($"Found {count} instance extensions:");
        foreach (var prop in properties)
        {
            var name = prop.GetExtensionName();
            Log.Verbose(name);
        }
    }

    private static void FindValidationLayers(List<string> appendTo)
    {
        ReadOnlySpan<VkLayerProperties> availableLayers = vkEnumerateInstanceLayerProperties();

        for (int j = 0; j < availableLayers.Length; j++)
        {
            var name = availableLayers[j].GetLayerName();
            Log.Info($"Found Layer: {name}");
        }

        for (int i = 0; i < _requestedValidationLayers.Length; i++)
        {
            bool hasLayer = false;
            for (int j = 0; j < availableLayers.Length; j++)
            {
                if (_requestedValidationLayers[i] == availableLayers[j].GetLayerName())
                {
                    hasLayer = true;
                    break;
                }
            }

            if (hasLayer)
            {
                appendTo.Add(_requestedValidationLayers[i]);
            }
            else
            {
                Log.Warn($"Validation layer '{_requestedValidationLayers[i]}' not found.");
            }
        }
    }

    [UnmanagedCallersOnly]
    private static uint DebugMessengerCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        string? message = VkStringInterop.ConvertToManaged(pCallbackData->pMessage);
        string? messageIdName = VkStringInterop.ConvertToManaged(pCallbackData->pMessageIdName);
        if (_suppressDebugMessages != null && _suppressDebugMessages.Contains(messageIdName))
            return VK_FALSE;

        string prefix = messageTypes.HasFlag(VkDebugUtilsMessageTypeFlagsEXT.Validation) ? "[Vulkan]: Validation: " : "[Vulkan]: ";

        switch (messageSeverity)
        {
            case VkDebugUtilsMessageSeverityFlagsEXT.Error:
                Log.Error($"{prefix}{messageSeverity} - {message}");
                if (messageTypes == VkDebugUtilsMessageTypeFlagsEXT.Validation)
                    throw new Exception(message);

                break;
            case VkDebugUtilsMessageSeverityFlagsEXT.Warning:
                Log.Warn($"{prefix}{messageSeverity} - {message}");
                break;
            case VkDebugUtilsMessageSeverityFlagsEXT.Info:
                Log.Info($"{prefix}{messageSeverity} - {message}");
                break;
            case VkDebugUtilsMessageSeverityFlagsEXT.Verbose:
                Log.Verbose($"{prefix}{messageSeverity} - {message}");
                break;
        }

        return VK_FALSE;
    }

    public void Dispose()
    {
        if (_debugMessenger != VkDebugUtilsMessengerEXT.Null)
        {
            vkDestroyDebugUtilsMessengerEXT(Instance, _debugMessenger, null);
        }

        if (Instance != VkInstance.Null)
        {
            vkDestroyInstance(Instance, null);
        }
    }
}