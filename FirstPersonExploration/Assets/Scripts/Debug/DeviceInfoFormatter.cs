// DeviceInfoFormatter.cs
// Builds a one-time, multi-line summary of the device, GPU, graphics API, CPU, RAM, display,
// quality level, and build type. Allocates, so call it once at startup, never per frame.
using System.Text;
using UnityEngine;

public static class DeviceInfoFormatter
{
    public static string Build()
    {
        var sb = new StringBuilder(512);

        sb.Append(SystemInfo.deviceModel).Append(" | ").Append(SystemInfo.operatingSystem).AppendLine();

        sb.Append("GPU: ").Append(SystemInfo.graphicsDeviceName)
          .Append(" (").Append(SystemInfo.graphicsDeviceVendor).Append(')').AppendLine();

        sb.Append("API: ").Append(SystemInfo.graphicsDeviceType.ToString())
          .Append(" | ").Append(SystemInfo.graphicsDeviceVersion).AppendLine();

        sb.Append("VRAM ").Append(SystemInfo.graphicsMemorySize).Append(" MB | Shader level ")
          .Append(SystemInfo.graphicsShaderLevel).Append(" | Max texture ")
          .Append(SystemInfo.maxTextureSize).AppendLine();

        sb.Append("CPU: ").Append(SystemInfo.processorType).Append(" x").Append(SystemInfo.processorCount);
        if (SystemInfo.processorFrequency > 0)
        {
            sb.Append(" @ ").Append(SystemInfo.processorFrequency).Append(" MHz");
        }
        sb.Append(" | RAM ").Append(SystemInfo.systemMemorySize).Append(" MB").AppendLine();

        sb.Append("Screen ").Append(Screen.width).Append('x').Append(Screen.height);
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;
        if (refreshRate > 0.0)
        {
            sb.Append(" @ ").Append(Mathf.RoundToInt((float)refreshRate)).Append(" Hz");
        }
        sb.AppendLine();

        string[] qualityNames = QualitySettings.names;
        int qualityLevel = QualitySettings.GetQualityLevel();
        string qualityName = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "unknown";
        sb.Append("Quality: ").Append(qualityName)
          .Append(" | Target FPS ").Append(Application.targetFrameRate)
          .Append(" | vSync ").Append(QualitySettings.vSyncCount).AppendLine();

        sb.Append("Unity ").Append(Application.unityVersion)
          .Append(" | ").Append(ScriptingBackendName)
          .Append(" | ").Append(Debug.isDebugBuild ? "Development" : "Release");

        return sb.ToString();
    }

    private static string ScriptingBackendName
    {
        get
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }
    }
}
