# Roll-a-Ball OSM - WebGL Build Profile

## Configuration
- Platform: WebGL
- Scripting Backend: IL2CPP
- API Compatibility: .NET Standard 2.1
- Compression: Gzip

## OSM Features
- ✅ CORS-compatible API access
- ⚠️ Limited threading (Unity WebGL)
- ⚠️ No local file caching
- ✅ Browser-based location services

## Browser Requirements
- Modern browser (Chrome 80+, Firefox 75+)
- WebGL 2.0 support
- ~500MB RAM available
- Stable internet connection

## Limitations
- No multithreading
- Limited memory (heap size)
- Network timeouts more likely
- No persistent local storage

## Optimization
- Texture quality: Medium
- Audio quality: Compressed
- Code stripping: Aggressive
- Exception handling: None
