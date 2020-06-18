using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Terrain3D
{
    public class TerrainController : MonoBehaviour
    {
        public bool refresh = false;
        
        [Header("Debug Settings")]
        public bool showDebug = true;
        [Space]
        public bool showMasterBounds = true;
        private bool showCells = false;
        private bool showVertices = false;
        private bool showValues = false;
        [Range(0.01f, 0.5f)]public float vertexSize = 0.05f;
    
        [Header("Terrain Settings")] 
        [Range(1, 42)]
        public int gridResolution = 8;
        public float size = 10f;
        public float threshold = 8;
    
        [Header("Renderer Settings")]
        public Material material;
        
        private Vector3 center, startPosition;
        private float cellSize;
        private int vertexResolution;
        private int gridCount, vertexCount;

        [Header("Metaball Settings")]
        [Range(1, 15)] public int metaballCount = 5;
        [Header("Size")]
        [Range(1, 5)] public float minSize = 2;
        [Range(2, 7)] public float maxSize = 2;
        [Header("Speed")]
        [Range(0.5f, 10f)] public float minSpeed = 0.5f;
        [Range(0.5f, 10f)] public float maxSpeed = 3f;
        [Range(0, 1)] public float relativeCollisionBounds = 0.9f;
        public List<Color> palette;
        
        private Vertex[] vertices;

        private TerrainMesh terrainMesh;

        private Function f;
        
        private void OnEnable()
        {
            gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            terrainMesh = gameObject.AddComponent<TerrainMesh>();
            
            InitializeMesh();
        }

        private void InitializeMesh()
        {
            center = Vector3.zero;

            startPosition = center - Vector3.one * size * 0.5f;
            
            cellSize = size / gridResolution;
            vertexResolution = gridResolution + 1;
            gridCount = gridResolution * gridResolution * gridResolution;
            vertexCount = vertexResolution * vertexResolution * vertexResolution;
            
            vertices = new Vertex[vertexCount];

            if(f == null) f = new Function(center, metaballCount, Vector3.one * size * relativeCollisionBounds, new Vector2(minSize, maxSize), new Vector2(minSpeed, maxSpeed), palette);

            for (int i = 0; i < vertexCount; i++)
            {
                Vertex v = new Vertex();
                v.k = i % vertexResolution;
                v.j = (i / vertexResolution) % vertexResolution;
                v.i = (i / (vertexResolution * vertexResolution)) % vertexResolution;
                v.position = startPosition + new Vector3(v.i * cellSize, v.j * cellSize, v.k * cellSize);
                v.value = f.Evaluate(v.position);
                v.state = v.value > threshold;
                
                vertices[i] = v;
            }
            
            if(terrainMesh != null)
            {
                terrainMesh.GenerateMesh(vertices, vertexResolution, cellSize, f, threshold);
                UpdateShaderParams();    
            }
        }

        private void UpdateShaderParams()
        {
            Vector4[] m = new Vector4[metaballCount];
            Vector4[] colors = new Vector4[metaballCount];

            for (int i = 0; i < metaballCount; i++)
            {
                Vector3 objPos = transform.TransformPoint(f.metaballs[i].center);
                m[i] = new Vector4(objPos.x, objPos.y, objPos.z, f.metaballs[i].radius);
                colors[i] = new Vector4(f.metaballs[i].color.r, f.metaballs[i].color.g, f.metaballs[i].color.b, 1);
            }
        
            Shader.SetGlobalInt("_MetaballCount", metaballCount);
            Shader.SetGlobalVectorArray("_Metaballs", m);
            Shader.SetGlobalVectorArray("_Colors", colors);
        }

        private void InitializeFunction()
        {
            f = new Function(center, metaballCount, Vector3.one * size * relativeCollisionBounds, new Vector2(minSize, maxSize), new Vector2(minSpeed, maxSpeed), palette);
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying) return;
    
            if (showMasterBounds)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(transform.TransformPoint(center), Vector3.one * size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.TransformPoint(center), Vector3.one * size * relativeCollisionBounds);
            }
    
            if (showCells)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < gridResolution; i++)
                {
                    for (int j = 0; j < gridResolution; j++)
                    {
                        for (int k = 0; k < gridResolution; k++)
                        {
                            Vector3 position = transform.TransformPoint(startPosition + new Vector3((i + 0.5f) * cellSize, (j + 0.5f) * cellSize,
                                (k + 0.5f) * cellSize));
                            Gizmos.DrawWireCube(position, Vector3.one * cellSize);
                        }
                    }
                }
            }

            if (showVertices)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    if (vertices[i].state) Gizmos.color = Color.black;
                    else Gizmos.color = Color.white;
                    
                    Gizmos.DrawSphere(transform.TransformPoint(vertices[i].position), vertexSize);
                    Handles.Label(transform.TransformPoint(vertices[i].position), vertices[i].value.ToString());
                }
            }

            if (showValues)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    Handles.Label(transform.TransformPoint(vertices[i].position), vertices[i].value.ToString());
                }
            }
        }

        private void Update()
        {
            f.MoveMetaballs();
            InitializeMesh();
        }

        private void OnValidate()
        {
            InitializeFunction();
        }

        private float InverseLerp(float a, float b, float val)
        {
            return (val - a) / (b - a);
        }
    }
    
    [System.Serializable]
    public class Vertex
    {
        public Vector3 position;
        public int i, j, k;
        public bool state;
        public float value;
    }

    public class Metaball3D
    {
        public Vector3 center;
        public float radius;
        public Vector3 velocity;
        public Color color;
    }
    
    [System.Serializable]
    public class Function
    {
        private Vector3 center;
        public Metaball3D[] metaballs;
        private Vector3 bounds;

        public Function(Vector3 _center, int nMetaballs, Vector3 _bounds, Vector2 sizeRange, Vector2 speedRange, List<Color> palette)
        {
            center = _center;
            bounds = _bounds;

            metaballs = new Metaball3D[nMetaballs];
            
            for (int i = 0; i < nMetaballs; i++)
            {
                Metaball3D m = new Metaball3D();
                m.radius = Random.Range(sizeRange.x, sizeRange.y);
                m.center = new Vector3(
                    Random.Range(-bounds.x * 0.5f + m.radius, bounds.x * 0.5f - m.radius),
                    Random.Range(-bounds.y * 0.5f + m.radius, bounds.y * 0.5f - m.radius),
                    Random.Range(-bounds.z * 0.5f + m.radius, bounds.z * 0.5f - m.radius)
                    );
                m.velocity = Random.insideUnitSphere * Random.Range(speedRange.x, speedRange.y);
                m.color = palette[Random.Range(0, palette.Count - 1)];

                metaballs[i] = m;
            }

        }

        public float Evaluate(Vector3 point)
        {
            float sum = 0;
            
            for (int i = 0; i < metaballs.Length; i++)
            {
                Metaball3D m = metaballs[i];
                
                float d = (point - m.center).magnitude;
                float r = m.radius;
                float value = r / d;
                
                sum += value;
            }

            return sum;
        }

        public Vector3 Normal(Vector3 point)
        {
            Vector3 netNormal = Vector3.zero;
            
            for (int i = 0; i < metaballs.Length; i++)
            {
                Metaball3D m = metaballs[i];

                float r = m.radius;
                float d = (point - m.center).magnitude;
                
                Vector3 normal = new Vector3();

                float value = r / d;
                normal = r * (point - m.center) / d;

                netNormal += normal * value;
            }

            netNormal = netNormal / Evaluate(point);
            
            return netNormal.normalized;
        }

        public void MoveMetaballs()
        {
            for (int i = 0; i < metaballs.Length; i++)
            {
                Vector3 p = metaballs[i].center;
                Vector3 v = metaballs[i].velocity;
                float r = metaballs[i].radius;
                
                float xMax = bounds.x * 0.5f;
                float xMin = -xMax;
                float yMax = bounds.y * 0.5f;
                float yMin = -yMax;
                float zMax = bounds.z * 0.5f;
                float zMin = -zMax;

                p = p + v * Time.deltaTime;

                /*if (Vector3.Distance(p, center) > bounds.x * 0.5f + r)
                {
                    v = -v;
                }*/
                
                if (p.x + r >= xMax)
                {
                    v.x = -v.x;
                    p.x = xMax - r;
                }
                else if (p.x - r <= xMin)
                {
                    v.x = -v.x;
                    p.x = xMin + r;
                }
                
                if (p.y + r >= yMax)
                {
                    v.y = -v.y;
                    p.y = yMax - r;
                }
                else if (p.y - r <= yMin)
                {
                    v.y = -v.y;
                    p.y = yMin + r;
                }
                
                if (p.z + r >= zMax)
                {
                    v.z = -v.z;
                    p.z = zMax - r;
                }
                else if (p.z - r <= zMin)
                {
                    v.z = -v.z;
                    p.z = zMin + r;
                }

                metaballs[i].center = p;
                metaballs[i].velocity = v;
            }
        }
    }
}
