using System;
using System.Text;
using Vortice.Vulkan;

namespace VulkanTest;

public static class NativeExtensions
{
    public static unsafe string GetLayerName(this VkLayerProperties properties)
        => VkStringInterop.ConvertToManaged(properties.layerName) ?? throw new InvalidOperationException();

    public static unsafe string GetDeviceName(this VkPhysicalDeviceProperties properties)
        => VkStringInterop.ConvertToManaged(properties.deviceName) ?? throw new InvalidOperationException();

    public static unsafe string GetExtensionName(this VkExtensionProperties properties)
        => VkStringInterop.ConvertToManaged(properties.extensionName) ?? throw new InvalidOperationException();

    public static string GetStringFromUtf8Buffer(this ReadOnlySpan<byte> stringBuffer)
        => Encoding.UTF8.GetString(stringBuffer);

    public static VkUtf8ReadOnlyString ToVkUtf8ReadOnlyString(this string stringBuffer)
        => new((ReadOnlySpan<byte>)Encoding.UTF8.GetBytes(stringBuffer));

    // public static void CheckResult(this VkResult result, string message = "Vulkan operation failed")
    // {
    //     if (result != VkResult.Success)
    //         throw new VulkanException(message, result);
    // }
}