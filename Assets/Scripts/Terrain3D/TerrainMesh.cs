using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain3D
{
    public class TerrainMesh : MonoBehaviour
    {
        private List<Vector3> vertices;
        private List<Vector3> normals;
        private List<int> triangles;
    
        private MeshFilter meshFilter;
        private Mesh mesh;
    
        private Vertex[] grid;
        private int resolution;
        private float cellSize;
    
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
    
        public void GenerateMesh(Vertex[] values, int _resolution, float _cellSize)
        {
            vertices.Clear();
            normals.Clear();
            triangles.Clear();
    
            mesh.Clear();
    
            grid = values;
            resolution = _resolution;
            cellSize = _cellSize;
    
            MarchingCubes();
    
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
        }
    private List<Vector3> edges = new List<Vector3>();
        private void MarchingCubes()
        {
            edges.Clear();
            for (int i = 0; i < resolution - 1; i++)
            {
                for (int j = 0; j < resolution - 1; j++)
                {
                    for (int k = 0; k < resolution - 1; k++)
                    {
                        int mask = GetMask(i, j, k);
                        //Debug.Log(Convert.ToString(mask, 2));
                        edges = Triangulation.Edges(mask);
                    }
                }
            }
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
            Gizmos.color = Color.red;
            foreach (Vector3 edge in edges)
            {
                Gizmos.DrawSphere(edge * cellSize, 0.1f);
            }
        }
    }
}
