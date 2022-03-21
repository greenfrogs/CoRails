using UnityEngine;
using Utils;

namespace Terrain_Generation.Visitors.NoiseVisitors {
    public class RockVisitor : NoiseVisitor {
        public override float number => 1.0f;
        public override int octave => 64;
        public override float power => 1;

        public override float offset => 0f;
        public override float offsetMultiply => 0.6f;

        protected override ChunkObject Place(Chunk chunk) {
            if (chunk.type == BaseLayerType.Water) {
                return null;
            }

            ChunkObject chunkObject = new ChunkObject("Rock-" + RandomNumberGenerator.Instance.Generate(1, 6), 0, 0, 0,
                Quaternion.Euler(0, (float)RandomNumberGenerator.Instance.Generate(0, 7) * 45f, 0));

            chunk.properties["Rock"] = "true";
            
            return chunkObject;
        }
    }
}