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
        public bool showCells = true;
        public bool showVertices = true;
        [Range(0.01f, 0.5f)]public float vertexSize = 0.05f;
    
        [Header("Terrain Settings")] 
        [Range(1, 40)]
        public int gridResolution = 8;
        public float size = 10f;
        [Range(-4, 4)] public float threshold = 2;
    
        [Header("Renderer Settings")]
        public Material material;
        
        private Vector3 center, startPosition;
        private float cellSize;
        private int vertexResolution;
        private int gridCount, vertexCount;

        private Vertex[] vertices;

        private TerrainMesh terrainMesh;

        private Function f;
        
        private void Awake()
        {
            gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            terrainMesh = gameObject.AddComponent<TerrainMesh>();
            RefreshMesh();
        }

        private void RefreshMesh()
        {
            if(terrainMesh == null) return;
            
            center = Vector3.zero;

            startPosition = center - Vector3.one * size * 0.5f;
            
            cellSize = size / gridResolution;
            vertexResolution = gridResolution + 1;
            gridCount = gridResolution * gridResolution * gridResolution;
            vertexCount = vertexResolution * vertexResolution * vertexResolution;
            
            vertices = new Vertex[vertexCount];
            
            f = new Function(center, size, 5);

            for (int i = 0; i < vertexCount; i++)
            {
                Vertex v = new Vertex();
                v.k = i % vertexResolution;
                v.j = (i / vertexResolution) % vertexResolution;
                v.i = (i / (vertexResolution * vertexResolution)) % vertexResolution;
                v.position = startPosition + new Vector3(v.i * cellSize, v.j * cellSize, v.k * cellSize);
                v.value = f.Evaluate(v.position);
                v.state = v.value < threshold;
                
                vertices[i] = v;
            }
        }
        
        private void OnDrawGizmos()
        {
            f.OnDrawGizmos();
            if (!showDebug) return;
    
            if (showMasterBounds)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(transform.TransformPoint(center), Vector3.one * size);
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
                    //Handles.Label(transform.TransformPoint(vertices[i].position), vertices[i].value.ToString());
                }
            }
        }

        private void Update()
        {
            //f.UpdateMetaballs();
            
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].value = f.Evaluate(vertices[i].position);
                vertices[i].state = vertices[i].value > 1f;
            }
            
            terrainMesh.GenerateMesh(vertices, vertexResolution, cellSize, f, threshold);
        }

        private void OnValidate()
        {
            if(Application.isPlaying)
            {
                RefreshMesh();
            }
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

    /*public class Function
    {
        private Vector3 center;

        public Function(Vector3 _center)
        {
            center = _center;
        }

        public float Evaluate(Vector3 point)
        {
            //return Perlin.Noise(100 * point);
            //return 4 * Mathf.Sin(10 * (point.y - center.y) * Mathf.PI * 0.5f);
            return (center - point).magnitude;
        }

        public Vector3 Normal(Vector3 point)
        {
            //return Vector3.up;//Mathf.Cos(point.z - center.z);
            return (point - center).normalized;
        }
    }*/
    
    public class Function
    {
        private Vector3 center;
        private Metaball[] metaballs;
        private float size;

        public Function(Vector3 _center, float _size, int metaballCount)
        {
            center = _center;
            size = _size;
            
            metaballs = new Metaball[metaballCount];

            for (int i = 0; i < metaballCount; i++)
            {
                metaballs[i] = new Metaball(Vector3.zero, Random.Range(1f, 5f), Random.insideUnitSphere * 1f);
            }
        }

        private Vector3 RandomPosition()
        {
            return new Vector3(
                Random.Range(-size * 0.5f, size * 0.5f),
                Random.Range(-size * 0.5f, size * 0.5f),
                Random.Range(-size * 0.5f, size * 0.5f)
                );
        }

        public void UpdateMetaballs()
        {
            foreach (Metaball m in metaballs)
            {
                UpdateMetaballPos(m);
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            foreach (Metaball metaball in metaballs)
            {
                Gizmos.DrawSphere(metaball.position, metaball.radius);
            }
        }

        private void UpdateMetaballPos(Metaball m)
        {
            m.position += m.velocity * Time.deltaTime;

            if (m.position.x + m.radius > size * 0.5f)
            {
                m.position.x = size * 0.5f - m.radius;
                m.velocity.x = -m.velocity.x;
            }
            else if (m.position.x < -size * 0.5f)
            {
                m.position.x = -size * 0.5f + m.radius;
                m.velocity.x = -m.velocity.x;
            }
            
            if (m.position.y + m.radius > size * 0.5f)
            {
                m.position.y = size * 0.5f - m.radius;
                m.velocity.y = -m.velocity.y;
            }
            else if (m.position.y < -size * 0.5f)
            {
                m.position.y = -size * 0.5f + m.radius;
                m.velocity.y = -m.velocity.y;
            }
            
            if (m.position.z + m.radius > size * 0.5f)
            {
                m.position.z = size * 0.5f - m.radius;
                m.velocity.z = -m.velocity.z;
            }
            else if (m.position.z < -size * 0.5f)
            {
                m.position.z = -size * 0.5f + m.radius;
                m.velocity.z = -m.velocity.z;
            }
        }
        
        public float Evaluate(Vector3 point)
        {
            float sum = 0;

            foreach (Metaball m in metaballs)
            {
                float dist = (point - m.position).sqrMagnitude;
                if (dist <= 0) dist++;
                sum += (m.radius * m.radius) / dist;    
            }

            return sum;
            //return (center - point).magnitude;
        }

        public Vector3 Normal(Vector3 point)
        {
            Vector3 normal = Vector3.zero;

            foreach (Metaball m in metaballs)
            {
                normal += (point - m.position).normalized;
            }

            return normal.normalized;
            //return (point - center).normalized;
        }
    }

    public class Metaball
    {
        public Vector3 position;
        public float radius;
        public Vector3 velocity;

        public Metaball(Vector3 pos, float r, Vector3 vel)
        {
            position = pos;
            radius = r;
            velocity = vel;
        }
    }
}
