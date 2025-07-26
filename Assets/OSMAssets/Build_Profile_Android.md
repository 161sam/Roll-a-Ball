# Roll-a-Ball OSM - Android Build Profile

## Configuration
- Platform: Android
- Scripting Backend: IL2CPP
- API Compatibility: .NET Standard 2.1
- Min SDK: Android 7.0 (API 24)
- Target SDK: Latest

## OSM Features  
- ✅ Internet permission required
- ✅ Location services (future GPS)
- ✅ Optimized for mobile performance
- ⚠️ Limited simultaneous API calls

## Permissions Required
- android.permission.INTERNET
- android.permission.ACCESS_NETWORK_STATE
- android.permission.ACCESS_FINE_LOCATION (future)

## Optimization Settings
- Texture compression: ASTC
- Audio compression: Vorbis
- Scripting: IL2CPP + ARM64
- Graphics: OpenGL ES 3.0+

## Build Notes
- APK size: ~50-100MB (depends on assets)
- RAM usage: 200-500MB (depends on map size)
- Network: Required for initial map loading
