using System.Collections.Generic;
using UnityEngine;

public static class GridPathfinder2D
{
    private static readonly Vector3Int[] Directions =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(-1, -1, 0)
    };

    public static bool FindPath(HashSet<Vector3Int> walkable, Vector3Int start, Vector3Int goal, List<Vector3Int> path)
    {
        path.Clear();

        if (walkable == null || !walkable.Contains(start) || !walkable.Contains(goal))
        {
            return false;
        }

        if (start == goal)
        {
            return true;
        }

        List<Vector3Int> open = new List<Vector3Int> { start };
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float> { [start] = 0f };
        Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float> { [start] = Heuristic(start, goal) };

        while (open.Count > 0)
        {
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (fScore[open[i]] < fScore[open[bestIndex]])
                {
                    bestIndex = i;
                }
            }

            Vector3Int current = open[bestIndex];
            if (current == goal)
            {
                Reconstruct(cameFrom, current, start, path);
                return true;
            }

            open.RemoveAt(bestIndex);

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector3Int direction = Directions[i];
                Vector3Int neighbor = current + direction;

                if (!walkable.Contains(neighbor))
                {
                    continue;
                }

                bool diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal &&
                    (!walkable.Contains(current + new Vector3Int(direction.x, 0, 0)) ||
                     !walkable.Contains(current + new Vector3Int(0, direction.y, 0))))
                {
                    continue;
                }

                float stepCost = diagonal ? 1.41421356f : 1f;
                float tentativeG = gScore[current] + stepCost;

                if (gScore.TryGetValue(neighbor, out float existingG) && tentativeG >= existingG)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                if (!open.Contains(neighbor))
                {
                    open.Add(neighbor);
                }
            }
        }

        return false;
    }

    private static void Reconstruct(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current, Vector3Int start, List<Vector3Int> path)
    {
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
    }

    private static float Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dy + (1.41421356f - 2f) * Mathf.Min(dx, dy);
    }
}
