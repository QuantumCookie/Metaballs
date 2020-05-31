using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MetaballController : MonoBehaviour
{
    private Metaball[] metaballs;

    [Range(10, 150)] public float size = 10f;
    [Range(5, 200)] public int resolution = 8;
    private Vertex[] grid;

    public int Resolution => resolution;
    public Vertex[] Grid => grid;

    private float cellSize;

    [Header("Metaball Settings")]
    [Range(2, 50)] public int metaballCount = 5;
    [Range(1, 10)] public float maxVelocity = 3f;
    [Range(1, 30)] public float minSize = 5f;
    [Range(1, 30)] public float maxSize = 15f;

    [Header("Mesh Settings")]
    public Material material;

    private void Awake() 
    {
        Initialize();
    }

    private void Initialize()
    {
        cellSize = size / resolution;
        
        Camera.main.transform.position = new Vector3(size * 0.5f, size * 0.5f, -10);
        Camera.main.orthographicSize = size * 0.5f;

        grid = new Vertex[resolution * resolution];

        GenerateMetaballs();

        for (int y = 0, i = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++, i++)
            {
                grid[i] = new Vertex();
                grid[i].position = new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                grid[i].size = cellSize;
                grid[i].x = x;
                grid[i].y = y;
                grid[i].state = CheckMetaballProximity(grid[i].position);
            }
        }

        RefreshGrid();
    }

    private void GenerateMetaballs()
    {
        metaballs = new Metaball[metaballCount];

        for(int i = 0; i < metaballCount; i++)
        {
            Metaball m = new Metaball();
            m.position = new Vector3();
            m.position.x = Random.Range(0, size);
            m.position.y = Random.Range(0, size);
            m.radius = Random.Range(Mathf.Min(minSize, maxSize), Mathf.Max(minSize, maxSize));
            m.velocity = Random.insideUnitCircle * maxVelocity;

            ClampVelocity(m);

            metaballs[i] = m;
        }
    }

    private bool CheckMetaballProximity(Vector3 position)
    {
        return Function(position) > 1f;
    }

    public float Function(Vector3 position)
    {
        float sum = 0;

        for (int i = 0; i < metaballs.Length; i++)
        {
            float sqrDistance = (position - transform.InverseTransformPoint(metaballs[i].position)).sqrMagnitude;
            float sqrRadius = metaballs[i].radius * metaballs[i].radius;

            if (sqrDistance < 0.001) sqrDistance = 1;
            sum += sqrRadius / sqrDistance;
        }

        return sum;
    }

    private void Update()
    {
        MoveMetaballs();
        RefreshGrid();
    }

    private void OnValidate()
    {
        Initialize();
    }

    private void MoveMetaballs()
    {
        for(int i = 0; i < metaballCount; i++)
        {
            Metaball m = metaballs[i];
            m.position += m.velocity * Time.deltaTime;
            ClampVelocity(m);    
        }
    }

    private void ClampVelocity(Metaball m)
    {
        if(m.position.x + m.radius >= size)
        {
            m.velocity.x = -m.velocity.x;
            m.position.x = size - m.radius;
        }
        else if(m.position.x - m.radius <= 0)
        {
            m.velocity.x = -m.velocity.x;
            m.position.x = m.radius;
        }

        if (m.position.y + m.radius >= size)
        {
            m.velocity.y = -m.velocity.y;
            m.position.y = size - m.radius;
        }
        else if (m.position.y - m.radius <= 0)
        {
            m.velocity.y = -m.velocity.y;
            m.position.y = m.radius;
        }
    }

    private void RefreshGrid()
    {
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i].state = CheckMetaballProximity(grid[i].position);
        }
    }

    public Vector3 EdgeX(Vertex a)
    {
        Vertex b = grid[a.y * resolution + (a.x + 1)];

        Vector3 result = b.position;

        float sumA = Function(a.position);
        float sumB = Function(b.position);

        result += (a.position - b.position) * ((1f - sumB) / (sumA - sumB));

        return result;
    }

    public Vector3 EdgeY(Vertex a)
    {
        Vertex b = grid[(a.y + 1) * resolution + a.x];

        Vector3 result = b.position;

        float sumA = Function(a.position);
        float sumB = Function(b.position);

        result += (a.position - b.position) * ((1f - sumB) / (sumA - sumB));

        return result;
    }
}

public class Vertex
{
    public Vector3 position;
    public int x, y;
    public float size;
    public bool state;
}