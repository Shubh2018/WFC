using UnityEngine;
using System.Collections.Generic;

public static class PoissonDiskSampling
{
    public static List<Vector2> GeneratePoints(float width, float height, float r, int k)
    {
        float cellSize = r / Mathf.Sqrt(2);

        int gridWidth = Mathf.CeilToInt(width / cellSize);
        int gridHeight = Mathf.CeilToInt(height / cellSize);

        int[,] grid = new int[gridWidth, gridHeight];

        // for (int i = 0; i < gridWidth; i++)
        // {
        //     for(int j = 0; j < gridHeight; j++)
        //         grid[i,j] = -1;
        // }

        List<Vector2> samples = new List<Vector2>();
        List<int> active = new List<int>();
        
        Vector2 point = new Vector2(width / 2, height / 2);
        
        samples.Add(point);
        active.Add(0);
        
        int gx = Mathf.FloorToInt(point.x / cellSize);
        int gy = Mathf.FloorToInt(point.y / cellSize);
        grid[gx, gy] = 0;

        while (active.Count > 0)
        {
            int randomID = Random.Range(0, active.Count);
            int sampleIndex = active[randomID];
            
            Vector2 center = samples[sampleIndex];

            bool found = false;

            for (int i = 0; i < k; i++)
            {
                Vector2 candidate = GenerateRandomPointsAround(center, r);

                if (candidate.x < 0 || candidate.x >= width || candidate.y < 0 || candidate.y >= height)
                    continue;

                if (IsValid(candidate, r, cellSize, grid, samples, gridWidth, gridHeight))
                {
                    samples.Add(candidate);
                    int newIndex = samples.Count - 1;
                    
                    active.Add(newIndex);
                    
                    int candidateGx = Mathf.FloorToInt(candidate.x / cellSize);
                    int candidateGy = Mathf.FloorToInt(candidate.y / cellSize);
                    
                    grid[candidateGx, candidateGy] = newIndex;

                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                (active[randomID], active[^1]) = (active[^1], active[randomID]);

                active.RemoveAt(active.Count - 1);
            }
        }

        return samples;
    }

    private static Vector2 GenerateRandomPointsAround(Vector2 center, float r)
    {
        float radius = Random.Range(r, 2 * r);
        float angle = Random.Range(0, 2 * Mathf.PI);
        
        float x = center.x + radius * Mathf.Cos(angle);
        float y = center.y + radius * Mathf.Sin(angle);
        
        return new Vector2(x, y);
    }

    private static bool IsValid(Vector2 candidate, float r, float cellSize, int[,] grid, List<Vector2> samples, float gridWidth,
        float gridHeight)
    {
        int gx = Mathf.FloorToInt(candidate.x / cellSize);
        int gy = Mathf.FloorToInt(candidate.y / cellSize);

        for (int x = Mathf.Max(gx - 2, 0); x <= Mathf.Min(gx + 2, gridWidth - 1); x++)
        {
            for (int y = Mathf.Max(gy - 2, 0); y <= Mathf.Min(gy + 2, gridHeight - 1); y++)
            {
                int sampleIndex = grid[x, y];
                if (sampleIndex != -1)
                {
                    Vector2 point = samples[sampleIndex];

                    if (Vector2.Distance(candidate, point) <= r)
                        return false;
                }
            }
        }

        return true;
    }
}
