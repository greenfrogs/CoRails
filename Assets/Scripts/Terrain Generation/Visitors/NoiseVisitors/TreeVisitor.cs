using UnityEngine;
using Utils;

namespace Terrain_Generation.Visitors.NoiseVisitors {
    public class TreeVisitor : NoiseVisitor {
        public override float number => 4f;
        public override int octave => 100;
        public override float power => 4;
        public override float offset => 0.5f;
        public override float offsetMultiply => 1f;


        protected override ChunkObject Place(Chunk chunk) {
            if (chunk.type == BaseLayerType.Water || chunk.properties.ContainsKey("Rock")) {
                return null;
            }

            ChunkObject chunkObject = new ChunkObject("Tree-" +  RandomNumberGenerator.Instance.Generate(1, 11), 0, 0, 0,
                Quaternion.Euler(0, (float)RandomNumberGenerator.Instance.Generate(0, 7) * 45, 0));

            if (chunk.type == BaseLayerType.Cliff) {
                chunkObject.z = 1;
            }
            
            return chunkObject;
        }
    }
}