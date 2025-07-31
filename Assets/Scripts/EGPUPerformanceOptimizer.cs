using UnityEngine;
using UnityEngine.Rendering;

namespace RollABall.Performance
{
    /// <summary>
    /// eGPU Performance Optimizer für Roll-a-Ball
    /// Optimiert Unity-Einstellungen für dedizierte Grafikkarten
    /// </summary>
    [System.Serializable]
    public class EGPUPerformanceOptimizer : MonoBehaviour
    {
        [Header("eGPU Detection & Settings")]
        [SerializeField] private bool enableAutoOptimization = true;
        [SerializeField] private bool showGPUInfo = true;
        [SerializeField] private int targetFrameRate = 120;
        
        [Header("Graphics Quality")]
        [SerializeField] private bool forceHighQuality = true;
        [SerializeField] private bool enableVSync = false;
        [SerializeField] private int antiAliasing = 8;
        
        [Header("Debug")]
    [SerializeField] private bool showPerformanceStats = true;
    [SerializeField] private TMPro.TextMeshProUGUI statsText;
        
        private void Start()
        {
            if (enableAutoOptimization)
            {
                OptimizeForEGPU();
            }
            
            if (showGPUInfo)
            {
                LogGPUInformation();
            }
        }
        
        private void OptimizeForEGPU()
        {
            // Frame Rate optimieren
            Application.targetFrameRate = targetFrameRate;
            
            // VSync für eGPU deaktivieren (bessere Performance)
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;
            
            if (forceHighQuality)
            {
                // Höchste Qualitätsstufe setzen
                QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
                
                // Anti-Aliasing aktivieren
                QualitySettings.antiAliasing = antiAliasing;
                
                // Weitere Optimierungen
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 150f;
                QualitySettings.shadowCascades = 4;
            }
            
            // Unity Memory Management optimieren
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            
            Debug.Log("🎮 eGPU Optimierungen aktiviert!");
        }
        
        private void LogGPUInformation()
        {
            Debug.Log("=== GPU INFORMATION ===");
            Debug.Log($"🎮 Grafikkarte: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"💾 VRAM: {SystemInfo.graphicsMemorySize} MB");
            Debug.Log($"🔧 API: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"📊 Driver: {SystemInfo.graphicsDeviceVersion}");
            Debug.Log($"🖥️ Display: {Screen.currentResolution.width}x{Screen.currentResolution.height} @{Screen.currentResolution.refreshRateRatio}Hz");
            Debug.Log($"🎯 Target FPS: {Application.targetFrameRate}");
            Debug.Log($"🌟 Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            Debug.Log("========================");
        }
        
        private void Update()
        {
            if (!showPerformanceStats || statsText == null) return;

            statsText.text = $"FPS: {(1.0f / Time.deltaTime):F1}\n" +
                             $"GPU: {SystemInfo.graphicsDeviceName}\n" +
                             $"VRAM: {SystemInfo.graphicsMemorySize} MB\n" +
                             $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}";
        }
        
        [ContextMenu("Force GPU Detection")]
        public void ForceGPUDetection()
        {
            LogGPUInformation();
        }
        
        [ContextMenu("Reset to Default Quality")]
        public void ResetQuality()
        {
            QualitySettings.SetQualityLevel(2, true); // Medium quality
            Debug.Log("Quality auf Standard zurückgesetzt");
        }
    }
}
