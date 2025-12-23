using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// A* pathfinding algorithm implementation.
/// 
/// The algorithm finds the best path from a start location to a target location
/// on a 2D grid map, taking into account terrain costs and obstacles.
/// </summary>
public class AStar
{
    /// <summary>
    /// Find a path from start to goal.
    /// </summary>
    /// <returns>Path as LinkedList of AStarNode, or null if no path found</returns>
    public static LinkedList<AStarNode>? Search(
        int startX, int startY,
        int goalX, int goalY,
        int width, int height,
        Func<int, int, bool> isWalkable)
    {
        if (!isWalkable(goalX, goalY))
            return null;

        var openSet = new PriorityQueue<AStarNode, int>();
        var visited = new Dictionary<int, AStarNode>();
        
        int Key(int x, int y) => y * width + x;
        int Heuristic(int x, int y) => Math.Abs(x - goalX) + Math.Abs(y - goalY);

        var start = new AStarNode(startX, startY, 0, Heuristic(startX, startY));
        openSet.Enqueue(start, start.F);
        visited[Key(startX, startY)] = start;

        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { -1, 0, 1, 0 };

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current.X == goalX && current.Y == goalY)
                return BuildPath(current);

            for (int i = 0; i < 4; i++)
            {
                int nx = current.X + dx[i];
                int ny = current.Y + dy[i];

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                if (!isWalkable(nx, ny))
                    continue;

                int key = Key(nx, ny);
                int newG = current.G + 1;

                if (visited.TryGetValue(key, out var existing) && existing.G <= newG)
                    continue;

                var neighbor = new AStarNode(nx, ny, newG, Heuristic(nx, ny), current);
                visited[key] = neighbor;
                openSet.Enqueue(neighbor, neighbor.F);
            }
        }

        return null;
    }

    private static LinkedList<AStarNode> BuildPath(AStarNode goal)
    {
        var path = new LinkedList<AStarNode>();
        var node = goal;

        while (node != null)
        {
            path.AddFirst(node);
            node = node.Parent;
        }

        return path;
    }
}
