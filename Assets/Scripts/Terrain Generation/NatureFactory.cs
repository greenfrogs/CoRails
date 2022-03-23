using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain_Generation {
    public class NatureFactory {
        private static readonly Dictionary<string, GameObject> assets;
        private static readonly Dictionary<string, MappingTiles> mappings;

        static NatureFactory() {
            Debug.Log("Running Nature Factory");

            GameObject[] files = Resources.LoadAll<GameObject>("Nature");
            assets = new Dictionary<string, GameObject>(files.Length);

            foreach (GameObject file in files) assets.Add(file.name, file);

            Debug.Log("Loading JSON Mapping File");
            var jsonFile = Resources.Load<TextAsset>("Nature/mapping");
            var mapping = JsonUtility.FromJson<Mapping>(jsonFile.text);
            mappings = new Dictionary<string, MappingTiles>(mapping.tiles.Length);
            foreach (MappingTiles tile in mapping.tiles) mappings.Add(tile.name, tile);
        }

        public static GameObject Spawn(string name, float x, float y, float z, Quaternion rotation,
            Transform parent = null) {
            string assetName = "";

            if (mappings.ContainsKey(name)) {
                assetName = mappings[name].asset;
            }
            else {
                Debug.LogWarning("Failed to find object with " + name + " in mappings.");
                return null;
            }

            if (assets.ContainsKey(assetName)) {
                GameObject gameObject = parent != null
                    ? Object.Instantiate(assets[assetName], new Vector3(x, y, z), rotation, parent)
                    : Object.Instantiate(assets[assetName], new Vector3(x, y, z), rotation);
                gameObject.name = $"gt_{assetName}#{x}:{y}:{z}";

                if (mappings[name].collider) gameObject.AddComponent<MeshCollider>();

                if (mappings[name].tag != "") gameObject.tag = mappings[name].tag;

                if (mappings[name].removeShadows)
                    gameObject.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

                return gameObject;
            }

            Debug.LogError("Terrain with " + assetName + " could not be found and spawned.");

            return null;
        }
    }
}