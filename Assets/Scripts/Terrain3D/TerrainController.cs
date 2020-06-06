using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain3D
{
    public class TerrainController : MonoBehaviour
    {
        public bool refresh = false;
        
        [Header("Debug Settings")]
        public bool showDebug = true;
        public bool showMasterBounds = true;
        public bool showCells = true;
        public bool showVertices = true;
        [Range(0.01f, 0.5f)]public float vertexSize = 0.05f;
    
        [Header("Terrain Settings")] 
        [Range(1, 40)]
        public int resolution = 8;
    
        public float size = 10f;
    
        private Vector3 center;
        private float cellSize;
        private int power3;

        private Vertex[] vertices;

        private TerrainMesh terrainMesh;
        
        private void Start()
        {
            gameObject.AddComponent<MeshFilter>();
            terrainMesh = gameObject.AddComponent<TerrainMesh>();
            RefreshMesh();
        }

        private void RefreshMesh()
        {
            center = transform.position +
                     Vector3.up * size * 0.5f +
                     Vector3.right * size * 0.5f +
                     Vector3.forward * size * 0.5f;
    
            cellSize = size / resolution;
            power3 = resolution * resolution * resolution;
            
            vertices = new Vertex[power3];

            for (int i = 0; i < power3; i++)
            {
                Vertex v = new Vertex();
                v.k = i % resolution;
                v.j = (i / resolution) % resolution;
                v.i = (i / (resolution * resolution)) % resolution;
                v.position = new Vector3(v.i * cellSize, v.j * cellSize,
                    v.k * cellSize);
                v.state = Calc(v.position);

                vertices[i] = v;
            }
            
            terrainMesh.GenerateMesh(vertices, resolution, cellSize);
        }

        private bool Calc(Vector3 pos)
        {
            return Random.value > 0.5f;
            float rr = (center - pos).sqrMagnitude;
            
            return rr < size * size * 0.25f;
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebug) return;
    
            if (showMasterBounds)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(center, Vector3.one * size);
            }
    
            if (showCells)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        for (int k = 0; k < resolution; k++)
                        {
                            Vector3 position = new Vector3((i + 0.5f) * cellSize, (j + 0.5f) * cellSize,
                                (k + 0.5f) * cellSize);
                            Gizmos.DrawWireCube(position, Vector3.one * cellSize * 0.95f);
                        }
                    }
                }
            }

            if (showVertices)
            {
                for (int i = 0; i < power3; i++)
                {
                    if (vertices[i].state) Gizmos.color = Color.black;
                    else Gizmos.color = Color.white;
                    
                    Gizmos.DrawSphere(vertices[i].position, vertexSize);
                }
            }
        }

        private void OnValidate()
        {
            if (refresh)
            {
                refresh = false;
                RefreshMesh();
            }
        }
    }
    
    public class Vertex
    {
        public Vector3 position;
        public int i, j, k;
        public bool state;
        public float value;
    }
}
