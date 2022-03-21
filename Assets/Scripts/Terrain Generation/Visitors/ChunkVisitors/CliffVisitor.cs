using UnityEngine;

namespace Terrain_Generation.Visitors.ChunkVisitors {
    public class CliffVisitor : ChunkVisitor {
        public override void Run(Chunk chunk) {
            if (chunk.type != BaseLayerType.Cliff) return;
            
            // TODO: if we want to be really perfect here we can use an inner corner this will remove a very small amount of overlap
            if (chunk.north.type != BaseLayerType.Cliff) {
                chunk.AddObject(new ChunkObject("Cliff-Edge", 0, -1, Quaternion.Euler(0, 180, 0)));

                // Add Corners
                if (chunk.west.type != BaseLayerType.Cliff) {
                    chunk.AddObject(new ChunkObject("Cliff-Corner", -1, -1, Quaternion.Euler(0, 180, 0)));
                }

                if (chunk.east.type != BaseLayerType.Cliff) {
                    chunk.AddObject(new ChunkObject("Cliff-Corner", 1, -1, Quaternion.Euler(0, 90, 0)));
                }
            }

            if (chunk.east.type != BaseLayerType.Cliff) {
                chunk.AddObject(new ChunkObject("Cliff-Edge", 1, 0, Quaternion.Euler(0, 90, 0)));
            }

            if (chunk.south.type != BaseLayerType.Cliff) {
                chunk.AddObject(new ChunkObject("Cliff-Edge", 0, 1, Quaternion.Euler(0, 0, 0)));

                // Add Corners
                if (chunk.west.type != BaseLayerType.Cliff) {
                    chunk.AddObject(new ChunkObject("Cliff-Corner", -1, 1, Quaternion.Euler(0, 270, 0)));
                }

                if (chunk.east.type != BaseLayerType.Cliff) {
                    chunk.AddObject(new ChunkObject("Cliff-Corner", 1, 1, Quaternion.Euler(0, 0, 0)));
                }
            }

            if (chunk.west.type != BaseLayerType.Cliff) {
                chunk.AddObject(new ChunkObject("Cliff-Edge", -1, 0, Quaternion.Euler(0, 270, 0)));
            }
        }
    }
}