using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Terrain3D
{
    public class TerrainMesh : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool showDebug = false;
        [Space]
        public bool showVertices = true;
        [Range(0.01f, 0.1f)] public float vertexRadius = 0.05f;
        public bool showVertexNormals = true;
        [Range(0.1f, 5f)] public float normalLength = 1;
        
        private List<Vector3> vertices;
        private List<Vector3> normals;
        private List<int> triangles;
    
        private MeshFilter meshFilter;
        private Mesh mesh;
    
        private Vertex[] grid;
        private int resolution;
        private float cellSize;

        private Function f;
        private float threshold;
    
        private void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            mesh.name = "3D Terrain";
            meshFilter.sharedMesh = mesh;
    
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            triangles = new List<int>();
        }
    
        public void GenerateMesh(Vertex[] values, int _resolution, float _cellSize, Function _f, float _threshold)
        {
            f = _f;
            
            vertices.Clear();
            normals.Clear();
            triangles.Clear();
    
            mesh.Clear();
    
            grid = values;
            resolution = _resolution;
            cellSize = _cellSize;
            threshold = _threshold;
    
            MarchingCubes();
    
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.Optimize();

            meshFilter.sharedMesh = mesh;
        }
        
        private List<Vector3> debugEdges = new List<Vector3>();
        
        private void MarchingCubes()
        {
            debugEdges.Clear();

            int t = 0;
            
            for (int k = 0; k < resolution - 1; k++)
            {
                for (int j = 0; j < resolution - 1; j++)
                {
                    for (int i = 0; i < resolution - 1; i++)
                    {
                        int mask = GetMask(i, j, k);
                        //Debug.Log(Convert.ToString(mask, 2));
                        
                        List<int> edgesIndices = Triangulation.Edges(mask);

                        int currentVertex = ((i * resolution) + j) * resolution + k;

                        List<Vector3> edges = ProcessEdges(edgesIndices, grid[currentVertex]);
                        debugEdges.AddRange(edges);
                        TriangulateEdges(edges, ref t);
                    }
                }
            }
        }

        private List<Vector3> ProcessEdges(List<int> edgeIndices, Vertex currentVertex)
        {
            List<Vector3> edgePos = new List<Vector3>();

            for (int i = 0; i < edgeIndices.Count; i++)
            {
                Vertex a, b;
                
                VertexFromEdge(edgeIndices[i], currentVertex, out a, out b);
                
                float lerpFactor = InverseLerp(a.value, b.value, threshold);
                Vector3 lerpedPos = Lerp(a.position, b.position, lerpFactor);
                
                edgePos.Add(lerpedPos);
            }
            
            return edgePos;
        }
        
        private void VertexFromEdge(int edge, Vertex currentVertex, out Vertex a, out Vertex b)
        {
            a = currentVertex;
            b = currentVertex;
            
            switch (edge)
            {
                case 0:
                    a = currentVertex;
                    b = YNeighbour(currentVertex);
                    break;
                case 1:
                    a = YNeighbour(currentVertex);
                    b = XYNeighbour(currentVertex);
                    break;
                case 2:
                    a = XNeighbour(currentVertex);
                    b = XYNeighbour(currentVertex);
                    break;
                case 3:
                    a = currentVertex;
                    b = XNeighbour(currentVertex);
                    break;
                case 4:
                    a = ZNeighbour(currentVertex);
                    b = YZNeighbour(currentVertex);
                    break;
                case 5:
                    a = YZNeighbour(currentVertex);
                    b = XYZNeighbour(currentVertex);
                    break;
                case 6:
                    a = XYZNeighbour(currentVertex);
                    b = XZNeighbour(currentVertex);
                    break;
                case 7:
                    a = XZNeighbour(currentVertex);
                    b = ZNeighbour(currentVertex);
                    break;
                case 8:
                    a = ZNeighbour(currentVertex);
                    b = currentVertex;
                    break;
                case 9:
                    a = YNeighbour(currentVertex);
                    b = YZNeighbour(currentVertex);
                    break;
                case 10:
                    a = XYNeighbour(currentVertex);
                    b = XYZNeighbour(currentVertex);
                    break;
                case 11:
                    a = XNeighbour(currentVertex);
                    b = XZNeighbour(currentVertex);
                    break;
            }
        }

        private Vertex XNeighbour(Vertex v)
        {
            return grid[((v.i + 1) * resolution + v.j) * resolution + v.k];
        }
        
        private Vertex YNeighbour(Vertex v)
        {
            return grid[((v.i) * resolution + v.j + 1) * resolution + v.k];
        }
        
        private Vertex ZNeighbour(Vertex v)
        {
            return grid[((v.i) * resolution + v.j) * resolution + v.k + 1];
        }
        
        private Vertex XYNeighbour(Vertex v)
        {
            return grid[((v.i + 1) * resolution + v.j + 1) * resolution + v.k];
        }
        
        private Vertex XZNeighbour(Vertex v)
        {
            return grid[((v.i + 1) * resolution + v.j) * resolution + v.k + 1];
        }
        
        private Vertex YZNeighbour(Vertex v)
        {
            return grid[((v.i) * resolution + v.j + 1) * resolution + v.k + 1];
        }
        
        private Vertex XYZNeighbour(Vertex v)
        {
            return grid[((v.i + 1) * resolution + v.j + 1) * resolution + v.k + 1];
        }

        private float InverseLerp(float a, float b, float value)
        {
            return (value - a) / (b - a);
        }

        private Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return a + (b - a) * t;
        }

        private void TriangulateEdges(List<Vector3> edgeConfig, ref int t)
        {
            for (int i = 0; i < edgeConfig.Count; i += 3)
            {
                Triangulate(edgeConfig[i], edgeConfig[i + 1], edgeConfig[i + 2], ref t);
            }
        }
        
        private void Triangulate(Vector3 a, Vector3 b, Vector3 c, ref int t)
        {
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            
            triangles.Add(t);
            triangles.Add(t + 1);
            triangles.Add(t + 2);
            
            normals.Add(f.Normal(a));
            normals.Add(f.Normal(b));
            normals.Add(f.Normal(c));
            
            t += 3;
        }
    
        private int GetMask(int i, int j, int k)
        {
            int mask = 0;
    
            mask |= grid[((i + 0) * resolution + (j + 0)) * resolution + (k + 0)].state ? 1 : 0; //000
            mask |= grid[((i + 0) * resolution + (j + 1)) * resolution + (k + 0)].state ? 2 : 0; //010
            mask |= grid[((i + 1) * resolution + (j + 1)) * resolution + (k + 0)].state ? 4 : 0; //110
            mask |= grid[((i + 1) * resolution + (j + 0)) * resolution + (k + 0)].state ? 8 : 0; //100
            mask |= grid[((i + 0) * resolution + (j + 0)) * resolution + (k + 1)].state ? 16 : 0; //001
            mask |= grid[((i + 0) * resolution + (j + 1)) * resolution + (k + 1)].state ? 32 : 0; //011
            mask |= grid[((i + 1) * resolution + (j + 1)) * resolution + (k + 1)].state ? 64 : 0; //111
            mask |= grid[((i + 1) * resolution + (j + 0)) * resolution + (k + 1)].state ? 128 : 0; //101
    
            return mask;
        }

        private void OnDrawGizmos()
        {
            if (!showDebug) return;
            
            if (showVertices)
            {
                Gizmos.color = Color.black;
                for (int i = 0; i < vertices.Count; i++)
                {
                    Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), vertexRadius);
                }
            }
            
            if (showVertexNormals)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < vertices.Count; i++)
                {
                    Gizmos.DrawLine(transform.TransformPoint(vertices[i]), transform.TransformPoint(vertices[i]) + transform.TransformDirection(normals[i]).normalized * normalLength);
                }
            }
        }
    }
}
