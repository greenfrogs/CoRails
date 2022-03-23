using UnityEngine;
using Utils;

namespace Terrain_Generation.Visitors.ChunkVisitors {
    public class BridgeVisitor : ChunkVisitor {
        private static readonly int maxLength = 5;
        private static readonly int maxDistance = 8;
        private static readonly float probability = 10.0f;

        public override void Run(Chunk chunk) {
            if (chunk.type != BaseLayerType.Water) return;
            string isStone = RandomNumberGenerator.Instance.Generate(0, 1) == 0 ? "-Stone" : "";
            if ((int) RandomNumberGenerator.Instance.Generate(0, 100) > probability) return;

            // Scan out to search for other bridges, if we find one then don't build
            if (Scan(chunk, maxDistance)) return;

            // Scan out in each direction trying to find land, prefer length if two matches found
            int[] scan = {0, 0, 0, 0};
            bool[] founds = {false, false, false, false};
            bool found = false;
            bool vertical = false;
            Chunk scanChunk;
            while (true) {
                for (int i = 0; i < 4; i++) {
                    if (founds[i]) continue;
                    scan[i] += 1;
                    scanChunk = chunk;
                    for (int j = 0; j < scan[i]; j++) {
                        scanChunk = scanChunk.ChunkDirection(i);
                        if (scanChunk == null) break;
                    }

                    if (scanChunk != null && scanChunk.type == BaseLayerType.Grass) founds[i] = true;
                }

                if (founds[0] && founds[2] || founds[1] && founds[3]) {
                    found = true;
                    if (founds[0] && founds[2]) vertical = true;

                    break;
                }

                if (scan[0] + scan[2] > maxLength - 1 && scan[1] + scan[3] > maxLength - 1) {
                    Debug.Log("Failed too far" + StringUtils.ToString(scan));
                    break;
                }
            }

            if (!found) return;

            if (scan[0] + scan[2] == 2) {
                chunk.properties["Bridge"] = "true";
                chunk.AddObject(new ChunkObject("Bridge-Short" + isStone, 0, 0,
                    vertical ? Quaternion.Euler(0, 90, 0) : Quaternion.identity));
                return;
            }

            // Don't build a long bridge directly next to land
            if (scan[0] <= 1 || scan[2] <= 1 || scan[1] <= 1 || scan[3] <= 1) return;


            if (vertical)
                for (int y = -scan[0] + 1; y < scan[2]; y++)
                    if (y == -scan[0] + 1)
                        chunk.AddObject(new ChunkObject("Bridge-Start" + isStone, 0, y, Quaternion.Euler(0, 270, 0)));
                    else if (y == scan[2] - 1)
                        chunk.AddObject(new ChunkObject("Bridge-Start" + isStone, 0, y, Quaternion.Euler(0, 90, 0)));
                    else
                        chunk.AddObject(new ChunkObject("Bridge-Centre" + isStone, 0, y, Quaternion.Euler(0, 90, 0)));
            else // As these bridges are rotated, they need to be offset by +1
                for (int x = -scan[3] + 1; x < scan[1]; x++)
                    if (x == -scan[3] + 1)
                        chunk.AddObject(new ChunkObject("Bridge-Start" + isStone, x, 0, Quaternion.Euler(0, 0, 0)));
                    else if (x == scan[1] - 1)
                        chunk.AddObject(new ChunkObject("Bridge-Start" + isStone, x, 0, Quaternion.Euler(0, 180, 0)));
                    else
                        chunk.AddObject(new ChunkObject("Bridge-Centre" + isStone, x, 0, Quaternion.Euler(0, 0, 0)));

            // Set properties in chunks with bridges
            scanChunk = chunk;
            for (int i = 0; i < (vertical ? scan[0] : scan[1]) - 1; i++)
                scanChunk = vertical ? scanChunk.north : scanChunk.east;

            for (int i = 0; i < (vertical ? scan[0] + scan[2] : scan[1] + scan[3]) - 1; i++) {
                scanChunk.properties["Bridge"] = "true";
                scanChunk = vertical ? scanChunk.south : scanChunk.west;
            }
        }

        private bool Scan(Chunk chunk, int range) {
            Chunk scanner = chunk;
            // Go to top right
            for (int i = 0; i < range / 2; i++) {
                if (scanner.north.north != null) scanner = scanner.north;

                if (scanner.west.west != null) scanner = scanner.west;
            }

            bool toggle = true;

            for (int i = 0; i < range; i++) {
                for (int j = 0; j < range; j++) {
                    if (scanner.properties.ContainsKey("Bridge")) return true;

                    if (toggle) {
                        if (scanner.east.east == null) break;
                        scanner = scanner.east;
                    }
                    else {
                        if (scanner.west.west == null) break;
                        scanner = scanner.west;
                    }
                }

                if (scanner.south.south == null) break;
                scanner = scanner.south;
                toggle = !toggle;
            }

            return false;
        }
    }
}