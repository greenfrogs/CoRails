using UnityEngine;
using Utils;

namespace Terrain_Generation.Visitors.NoiseVisitors {
    public class BushVisitor : NoiseVisitor {
        public override float number => 2f;
        public override int octave => 32;
        public override float power => 12;

        public override float offset => 0.5f;
        public override float offsetMultiply => 1f;


        protected override ChunkObject Place(Chunk chunk) {
            string name = "Bush-" + RandomNumberGenerator.Instance.Generate(1, 6);
            float y = 0f;

            if (chunk.type == BaseLayerType.Water) {
                name = "Lilly-" + RandomNumberGenerator.Instance.Generate(1, 2);
                y = -0.05f;
            }

            var chunkObject = new ChunkObject(name, 0, y, 0,
                Quaternion.Euler(0, (float) RandomNumberGenerator.Instance.Generate(0, 7) * 45, 0));

            return chunkObject;
        }
    }
}