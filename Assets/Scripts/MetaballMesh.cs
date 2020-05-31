using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MetaballController))]
public class MetaballMesh : MonoBehaviour
{
    private Material material;

    private List<Vector3> vertices;
    private List<int> triangles;
    
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MetaballController controller;

    private Vertex[] grid;
    private int gridResolution;

    public void Start()
    {
        controller = GetComponent<MetaballController>();

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        vertices = new List<Vector3>();
        triangles = new List<int>();

        mesh = meshFilter.sharedMesh ?? new Mesh();
        mesh.name = "Metaballs";

        GenerateMesh();
    }

    private void Update() 
    {
        GenerateMesh();    
    }

    public void GenerateMesh()
    {
        grid = controller.Grid;
        material = controller.material;
        gridResolution = controller.Resolution;

        vertices.Clear();
        triangles.Clear();
        mesh.Clear();
        
        MarchingSquares();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;
    }

    private void MarchingSquares()
    {
        int N = gridResolution;
        int t = 0;

        for(int y = 0; y < N - 1; y++)
        {
            for(int x = 0; x < N - 1; x++)
            {
                int i00 = y * N + x;
                int i10 = y * N + (x + 1);
                int i01 = (y + 1) * N + x;
                int i11 = (y + 1) * N + (x + 1);

                Vertex v00 = grid[i00];
                Vertex v10 = grid[i10];
                Vertex v01 = grid[i01];
                Vertex v11 = grid[i11];
                
                int code = 0;
                code |= v00.state ? 1 : 0;
                code |= v10.state ? 2 : 0;
                code |= v11.state ? 4 : 0;
                code |= v01.state ? 8 : 0;

                switch(code)
                {
                    //Active Points = 1
                    case 1:
                    Triangulate(ref t, v00.position, EdgeY(v00), EdgeX(v00));
                    break;

                    case 2:
                    Triangulate(ref t, EdgeX(v00), EdgeY(v10), v10.position);
                    break; 

                    case 4:
                    Triangulate(ref t, EdgeY(v10), EdgeX(v01), v11.position);
                    break; 

                    case 8:
                    Triangulate(ref t, v01.position, EdgeX(v01), EdgeY(v00));
                    break;

                    //Active Points = 2
                    case 3:
                    Quadify(ref t, v00.position, v10.position, EdgeY(v10), EdgeY(v00));
                    break;

                    case 6:
                    Quadify(ref t, EdgeX(v00), v10.position, v11.position, EdgeX(v01));
                    break;

                    case 9:
                    Quadify(ref t, v00.position, EdgeX(v00), EdgeX(v01), v01.position);
                    break;

                    case 12:
                    Quadify(ref t, v01.position, EdgeY(v00), EdgeY(v10), v11.position);
                    break;

                    //Active Points = 3
                    case 7:
                    Pentagonify(ref t, EdgeY(v00), v00.position, v10.position, v11.position, EdgeX(v01));
                    break;

                    case 11:
                    Pentagonify(ref t, EdgeX(v01), v01.position, v00.position, v10.position, EdgeY(v10));
                    break;

                    case 13:
                    Pentagonify(ref t, v11.position, v01.position, v00.position, EdgeX(v00), EdgeY(v10));
                    break;

                    case 14:
                    Pentagonify(ref t, v10.position, v11.position, v01.position, EdgeY(v00), EdgeX(v00));
                    break;

                    //Special Cases (Active Points = 2)
                    case 5:
                    Triangulate(ref t, EdgeX(v01), v11.position, EdgeY(v10));
                    Triangulate(ref t, v00.position, EdgeY(v00), EdgeX(v00));
                    break;

                    case 10:
                    Triangulate(ref t, v01.position, EdgeX(v01), EdgeY(v00));
                    Triangulate(ref t, EdgeX(v00), v10.position, EdgeY(v10));
                    break;

                    //Active Points = 4
                    case 15:
                    Quadify(ref t, v00.position, v10.position, v11.position, v01.position);
                    break;

                }
            }
        }
    }

    private void Triangulate(ref int t, Vector3 a, Vector3 b, Vector3 c)
    {
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        triangles.Add(t);
        triangles.Add(t + 1);
        triangles.Add(t + 2);

        t += 3;
    }

    private void Quadify(ref int t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Triangulate(ref t, a, c, b);
        Triangulate(ref t, a, d, c);
    }

    private void Pentagonify(ref int t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        Triangulate(ref t, a, e, b);
        Triangulate(ref t, b, e, c);
        Triangulate(ref t, c, e, d);
    }

    private Vector3 EdgeX(Vertex v)
    {
        return controller.EdgeX(v);
        //return v.position + Vector3.right * v.size * 0.5f;
    }

    private Vector3 EdgeY(Vertex v)
    {
        return controller.EdgeY(v);
        //return v.position + Vector3.up * v.size * 0.5f;
    }
}
