using System.Linq;
using UnityEngine;
using Utils;

namespace Terrain_Generation {
    public class Baselayer {
        private static readonly float power = 10.0f;
        private static readonly float[] ratios = {0.08f, 0.84f, 0.08f};

        private static readonly BaseLayerType[] types = {BaseLayerType.Water, BaseLayerType.Grass, BaseLayerType.Cliff};

        private readonly double[] bounds = {0.0, 0.0};
        private readonly CubicNoise cubicNoise;
        private int length;

        private int width;

        public Baselayer(int width, int length) {
            this.width = width;
            this.length = length;
            int seed = (int) RandomNumberGenerator.Instance.Generate(int.MinValue, int.MaxValue);
            cubicNoise = new CubicNoise(seed, 16);

            double[] samples = new double[width * length];

            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
                samples[x + y * width] = Mathf.Pow(cubicNoise.Sample(x, y), power);

            samples = samples.OrderBy(a => a).ToArray();

            int i = 0;
            int count = 0;
            int type = 0;
            double sum = ratios[0];
            while (i < samples.Length && type < bounds.Length) {
                count += 1;
                if (count > sum * (width * length)) {
                    bounds[type] = (samples[i] + samples[i + 1]) / 2;
                    type += 1;
                    sum += ratios[type];
                }

                i += 1;
            }

            Debug.Log("Bounds calculated: " + StringUtils.ToString(bounds));
        }

        public BaseLayerType Sample(int x, int y, bool pass = true) {
            float value = Mathf.Pow(cubicNoise.Sample(x, y), power);
            var type = BaseLayerType.Cliff;
            for (int i = 0; i < bounds.Length; i++)
                if (value < bounds[i]) {
                    type = types[i];
                    break;
                }

            return type;
        }

        public void PrettyPrint(int startX, int endX, int startY, int endY) {
            string output = "";
            for (int x = startX; x < endX; x++) {
                for (int y = startY; y < endY; y++) {
                    BaseLayerType type = Sample(x, y);
                    switch (type) {
                        case BaseLayerType.Grass:
                            output += 'G';
                            break;
                        case BaseLayerType.Cliff:
                            output += 'C';
                            break;
                        case BaseLayerType.Water:
                            output += 'W';
                            break;
                    }
                }

                output += '\n';
            }

            Debug.Log(output);
        }
    }

    public enum BaseLayerType {
        Grass,
        Water,
        Cliff
    }
}