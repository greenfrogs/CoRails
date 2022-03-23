using System.Collections.Generic;
using UnityEngine;
using Utils;
using Utils.PriorityQueue;

namespace Terrain_Generation.Visitors {
    public class PathVisitor : Visitor {
        private readonly float offshoots = 0.06f;

        public override void Generate(Chunk[,] chunks) {
            int seed = (int) RandomNumberGenerator.Instance.Generate(int.MinValue, int.MaxValue);
            Debug.Log("Path Visitor: " + seed);
            var noise = new CubicNoise(seed, 16);

            // Generate a 2d representation with noise :)
            float[,] map = new float[chunks.GetLength(0), chunks.GetLength(1)];
            for (int x = 0; x < chunks.GetLength(0); x++)
            for (int y = 0; y < chunks.GetLength(1); y++)
                if (chunks[x, y].type == BaseLayerType.Grass ||
                    chunks[x, y].properties.ContainsKey("Bridge"))
                    map[x, y] = Mathf.Abs(noise.Sample(x, y)) * 10;
                else
                    map[x, y] = 10000;

            // A* Path Finding
            var start = new Vector2Int(chunks.GetLength(0) / 2, 0);
            var end = new Vector2Int(10, chunks.GetLength(1) - 1);
            List<Vector2Int> path = AStar(map, start, end);

            for (int i = 0; i < offshoots * chunks.GetLength(1); i++) {
                Vector2Int startOffshoot = path[(int) RandomNumberGenerator.Instance.Generate(0, path.Count - 1)];
                var endOffshoot = new Vector2Int(
                    Mathf.Clamp(
                        startOffshoot.x + (int) RandomNumberGenerator.Instance.Generate(-chunks.GetLength(0) / 2,
                            chunks.GetLength(0) / 2 - 1), 0,
                        chunks.GetLength(0) - 1),
                    Mathf.Clamp(
                        startOffshoot.y + (int) RandomNumberGenerator.Instance.Generate(-chunks.GetLength(0) / 2,
                            chunks.GetLength(0) / 2 - 1), 0,
                        chunks.GetLength(1) - 1));

                List<Vector2Int> pathOffshoot = AStar(map, startOffshoot, endOffshoot);

                path.AddRange(pathOffshoot);
            }

            // Place Path
            foreach (Vector2Int p in path) {
                Chunk chunk = chunks[p.x, p.y];
                if (chunk.type == BaseLayerType.Grass) {
                    chunks[p.x, p.y].properties["Path"] = "True";

                    bool north = path.Contains(new Vector2Int(p.x, p.y + 1));
                    bool east = path.Contains(new Vector2Int(p.x + 1, p.y));
                    bool south = path.Contains(new Vector2Int(p.x, p.y - 1));
                    bool west = path.Contains(new Vector2Int(p.x - 1, p.y));

                    if (north && east && south || north && east && west || north && south && west ||
                        east && west && south)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-Centre", 0, 0));
                    else if (north && south)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path", 0, 0, Quaternion.Euler(0, 90, 0)));
                    else if (east && west)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path", 0, 0));
                    else if (north && east)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-Corner", 0, 0, Quaternion.Euler(0, 180, 0)));
                    else if (north && west)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-Corner", 0, 0, Quaternion.Euler(0, 90, 0)));
                    else if (east && south)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-Corner", 0, 0, Quaternion.Euler(0, 270, 0)));
                    else if (west && south)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-Corner", 0, 0, Quaternion.Euler(0, 0, 0)));
                    else if (north)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-End", 0, 0, Quaternion.Euler(0, 270, 0)));
                    else if (east)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-End", 0, 0, Quaternion.Euler(0, 0, 0)));
                    else if (south)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-End", 0, 0, Quaternion.Euler(0, 90, 0)));
                    else if (west)
                        chunks[p.x, p.y].AddObject(new ChunkObject("Path-End", 0, 0, Quaternion.Euler(0, 180, 0)));
                    else
                        chunks[p.x, p.y].AddObject(new ChunkObject("Debug", 0, 0));
                }
            }
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current) {
            List<Vector2Int> path = new List<Vector2Int>();
            path.Add(current);
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }

        private List<Vector2Int> AStar(float[,] map, Vector2Int start, Vector2Int end) {
            SimplePriorityQueue<Vector2Int> openSet = new SimplePriorityQueue<Vector2Int>();
            openSet.Enqueue(start, 0);

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            DictionaryWithDefault<Vector2Int, float>
                gScore = new DictionaryWithDefault<Vector2Int, float>(int.MaxValue);
            gScore[start] = 0.0f;

            DictionaryWithDefault<Vector2Int, float>
                fScore = new DictionaryWithDefault<Vector2Int, float>(int.MaxValue);
            fScore[start] = Vector2Int.Distance(start, end);

            while (openSet.Count > 0) {
                Vector2Int current = openSet.Dequeue();
                if (current == end) return ReconstructPath(cameFrom, current);

                List<Vector2Int> directions = new List<Vector2Int>();
                if (current.x > 0) directions.Add(new Vector2Int(current.x - 1, current.y));

                if (current.x < map.GetLength(0) - 1) directions.Add(new Vector2Int(current.x + 1, current.y));

                if (current.y > 0) directions.Add(new Vector2Int(current.x, current.y - 1));

                if (current.y < map.GetLength(1) - 1) directions.Add(new Vector2Int(current.x, current.y + 1));

                foreach (Vector2Int neighbour in directions) {
                    float tentativeGScore = gScore[current] + map[neighbour.x, neighbour.y];
                    if (tentativeGScore < gScore[neighbour]) {
                        cameFrom[neighbour] = current;
                        gScore[neighbour] = tentativeGScore;
                        fScore[neighbour] = tentativeGScore + Vector2Int.Distance(neighbour, end);
                        if (!openSet.Contains(neighbour)) openSet.Enqueue(neighbour, fScore[neighbour]);
                    }
                }
            }

            return null;
        }
    }
}