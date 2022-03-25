using System;
using Terrain_Generation.Visitors;
using Terrain_Generation.Visitors.ChunkVisitors;
using Terrain_Generation.Visitors.NoiseVisitors;
using Trains;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Terrain_Generation {
    public class Terrain : MonoBehaviour {
        public TrackManagerSnake trackManager;
        
        public int width;
        public int length;

        public int initState;
        /*
         * initState == 0: Awake() calls Generate() and Generate() picks a random initState
         * initState >  0: Awake() calls Generate() with initState and Generate() uses that to generate terrain
         * initState <  0: Awake() doesn't call Generate(), some other piece of code is expected to call Generate()
         */

        private Chunk[,] chunks;


        public void Awake() {
            if (initState >= 0) Generate(initState);
        }

        private static void CleanTerrain() {
            if (!(FindObjectsOfType(typeof(GameObject)) is GameObject[] gameObjects)) return; // gets all GameObjects

            foreach (GameObject gameObject in gameObjects)
                if (gameObject.name.StartsWith("gt_"))
                    Destroy(gameObject);
        }

        public int Generate(int generateInitState) {
            CleanTerrain();
            trackManager.Clear();
            trackManager.GenerateStart();
            Random.InitState((int) DateTime.Now.Ticks);

            generateInitState = generateInitState > 0
                ? generateInitState
                : Random.Range(0, int.MaxValue);

            Debug.Log($"Generating new terrain using seed {generateInitState}");
            RandomNumberGenerator.Instance.sgenrand((ulong) generateInitState);

            chunks = new Chunk[width, length];

            var baseLayer = new Baselayer(width, length);
            baseLayer.PrettyPrint(0, width, 0, length);

            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
                chunks[x, y] = new Chunk(baseLayer.Sample(x, y));

            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++) {
                Chunk chunk = chunks[x, y];
                chunk.north = y == 0 ? new Chunk(baseLayer.Sample(x, y - 1)) : chunks[x, y - 1];
                chunk.east = x == width - 1 ? new Chunk(baseLayer.Sample(x + 1, y)) : chunks[x + 1, y];
                chunk.south = y == length - 1 ? new Chunk(baseLayer.Sample(x, y + 1)) : chunks[x, y + 1];
                chunk.west = x == 0 ? new Chunk(baseLayer.Sample(x - 1, y)) : chunks[x - 1, y];
            }

            // Order is essential here
            Visitor[] visitors = new Visitor[] {
                new CliffVisitor(), new BridgeVisitor(),
                new PathVisitor(),
                new RockVisitor(), new TreeVisitor(), new FlowerVisitor(), new BushVisitor()
            };

            foreach (Visitor v in visitors) v.Generate(chunks);

            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
                chunks[x, y].Render(x, y);
                
            trackManager.GenerateEnd();
            
            Debug.LogWarning("For privacy concerns, please check all peers see this same number: " +
                             RandomNumberGenerator.Instance.Generate(0, 1000));
            
            return generateInitState;
        }
    }
}