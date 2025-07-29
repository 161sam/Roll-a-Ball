using UnityEngine;

namespace RollABall.Map
{
    /// <summary>
    /// Utility class for efficient mesh creation without temporary GameObjects
    /// Optimized for use in mesh batching systems
    /// </summary>
    public static class MeshUtilities
    {
        // Cached meshes for performance
        private static Mesh _cachedCubeMesh;
        private static Mesh _cachedPlaneMesh;
        private static Mesh _cachedQuadMesh;
        
        /// <summary>
        /// Get a cached cube mesh for batching
        /// </summary>
        public static Mesh GetCubeMesh()
        {
            if (_cachedCubeMesh == null)
            {
                _cachedCubeMesh = CreateCubeMesh();
            }
            return _cachedCubeMesh;
        }
        
        /// <summary>
        /// Get a cached plane mesh for batching
        /// </summary>
        public static Mesh GetPlaneMesh()
        {
            if (_cachedPlaneMesh == null)
            {
                _cachedPlaneMesh = CreatePlaneMesh();
            }
            return _cachedPlaneMesh;
        }
        
        /// <summary>
        /// Get a cached quad mesh for batching
        /// </summary>
        public static Mesh GetQuadMesh()
        {
            if (_cachedQuadMesh == null)
            {
                _cachedQuadMesh = CreateQuadMesh();
            }
            return _cachedQuadMesh;
        }
        
        /// <summary>
        /// Create optimized cube mesh without temporary GameObjects
        /// </summary>
        private static Mesh CreateCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "OptimizedCube";
            
            // Vertices for a unit cube
            Vector3[] vertices = new Vector3[24]
            {
                // Front face
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                
                // Back face
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                
                // Left face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                
                // Right face
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                
                // Top face
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                
                // Bottom face
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
            };
            
            // Triangles (2 triangles per face, 6 faces)
            int[] triangles = new int[36]
            {
                // Front face
                0, 1, 2, 0, 2, 3,
                // Back face
                4, 5, 6, 4, 6, 7,
                // Left face
                8, 9, 10, 8, 10, 11,
                // Right face
                12, 13, 14, 12, 14, 15,
                // Top face
                16, 17, 18, 16, 18, 19,
                // Bottom face
                20, 21, 22, 20, 22, 23
            };
            
            // Normals
            Vector3[] normals = new Vector3[24]
            {
                // Front face
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                // Back face
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                // Left face
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                // Right face
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                // Top face
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                // Bottom face
                Vector3.down, Vector3.down, Vector3.down, Vector3.down
            };
            
            // UVs
            Vector2[] uvs = new Vector2[24]
            {
                // Front face
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Back face
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Left face
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Right face
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Top face
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // Bottom face
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uvs;
            
            mesh.RecalculateBounds();
            return mesh;
        }
        
        /// <summary>
        /// Create optimized plane mesh without temporary GameObjects
        /// </summary>
        private static Mesh CreatePlaneMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "OptimizedPlane";
            
            // Vertices for a unit plane (10x10 Unity units like default plane)
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-5f, 0f, -5f),
                new Vector3(5f, 0f, -5f),
                new Vector3(5f, 0f, 5f),
                new Vector3(-5f, 0f, 5f)
            };
            
            // Triangles
            int[] triangles = new int[6]
            {
                0, 1, 2,
                0, 2, 3
            };
            
            // Normals
            Vector3[] normals = new Vector3[4]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            };
            
            // UVs
            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uvs;
            
            mesh.RecalculateBounds();
            return mesh;
        }
        
        /// <summary>
        /// Create optimized quad mesh for areas
        /// </summary>
        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "OptimizedQuad";
            
            // Vertices for a unit quad (1x1)
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };
            
            // Triangles
            int[] triangles = new int[6]
            {
                0, 1, 2,
                0, 2, 3
            };
            
            // Normals
            Vector3[] normals = new Vector3[4]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            };
            
            // UVs
            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uvs;
            
            mesh.RecalculateBounds();
            return mesh;
        }
        
        /// <summary>
        /// Create a road segment mesh with specific dimensions
        /// </summary>
        public static CombineInstance CreateRoadSegmentMesh(Vector3 start, Vector3 end, float width, float height = 0.1f)
        {
            Vector3 center = (start + end) / 2f;
            Vector3 direction = (end - start).normalized;
            float length = Vector3.Distance(start, end);
            
            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 scale = new Vector3(width, height, length);
            
            Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, scale);
            
            return new CombineInstance
            {
                mesh = GetCubeMesh(),
                transform = matrix
            };
        }
        
        /// <summary>
        /// Create a building mesh with specific dimensions and rotation
        /// </summary>
        public static CombineInstance CreateBuildingMesh(Vector3 center, Vector3 size, Quaternion rotation)
        {
            center.y = size.y / 2f; // Position building base on ground
            
            Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, size);
            
            return new CombineInstance
            {
                mesh = GetCubeMesh(),
                transform = matrix
            };
        }
        
        /// <summary>
        /// Create an area mesh with specific dimensions
        /// </summary>
        public static CombineInstance CreateAreaMesh(Vector3 center, Vector3 size)
        {
            center.y = 0.01f; // Slightly above ground to prevent z-fighting
            
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, size);
            
            return new CombineInstance
            {
                mesh = GetQuadMesh(),
                transform = matrix
            };
        }
        
        /// <summary>
        /// Clear cached meshes (call when changing scenes or cleaning up)
        /// </summary>
        public static void ClearCache()
        {
            if (_cachedCubeMesh != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_cachedCubeMesh);
                else
                    Object.DestroyImmediate(_cachedCubeMesh);
                _cachedCubeMesh = null;
            }
            
            if (_cachedPlaneMesh != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_cachedPlaneMesh);
                else
                    Object.DestroyImmediate(_cachedPlaneMesh);
                _cachedPlaneMesh = null;
            }
            
            if (_cachedQuadMesh != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_cachedQuadMesh);
                else
                    Object.DestroyImmediate(_cachedQuadMesh);
                _cachedQuadMesh = null;
            }
        }
    }
}
