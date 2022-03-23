using UnityEngine;
using Utils;

namespace Terrain_Generation.Visitors.NoiseVisitors {
    public class FlowerVisitor : NoiseVisitor {
        private readonly string[] colours = {"Purple", "Yellow", "Red"};
        private readonly string[] types = {"A", "B", "C"};
        public override float number => 2f;
        public override int octave => 16;
        public override float power => 16;

        public override float offset => 0.5f;
        public override float offsetMultiply => 1f;

        protected override ChunkObject Place(Chunk chunk) {
            if (chunk.type != BaseLayerType.Grass) {
                return new ChunkObject("Lilly-" + RandomNumberGenerator.Instance.Generate(1, 2), 0, -0.05f, 0,
                    Quaternion.Euler(0, (float) RandomNumberGenerator.Instance.Generate(0, 7) * 45, 0));
                ;
            }

            string colour = colours[(int) RandomNumberGenerator.Instance.Generate(0, 2)];
            string type = types[(int) RandomNumberGenerator.Instance.Generate(0, 2)];

            if (chunk.properties.ContainsKey("Flower-Colour")) {
                colour = chunk.properties["Flower-Colour"];
            }
            else {
                chunk.properties["Flower-Colour"] = colour;
                chunk.north.properties["Flower-Colour"] = colour;
                chunk.east.properties["Flower-Colour"] = colour;
                chunk.south.properties["Flower-Colour"] = colour;
                chunk.west.properties["Flower-Colour"] = colour;
            }

            var chunkObject = new ChunkObject(
                "Flower-" + colour + "-" + type, 0, 0, 0,
                Quaternion.Euler(0, (float) RandomNumberGenerator.Instance.Generate(0, 7) * 45, 0));

            return chunkObject;
        }
    }
}