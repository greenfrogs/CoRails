using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Terrain_Generation {
    public class Chunk {
        public Chunk east;

        public Chunk north;
        private readonly List<ChunkObject> objects;
        public Dictionary<string, string> properties;
        public Chunk south;
        public BaseLayerType type;
        public Chunk west;

        public Chunk(BaseLayerType type) {
            this.type = type;
            objects = new List<ChunkObject>();
            properties = new Dictionary<string, string>();
        }

        public void AddObject(ChunkObject o) {
            objects.Add(o);
        }

        public void Render(int x, int y) {
            GameObject tileObject = NatureFactory.Spawn(type.ToString(), x, 0, y, Quaternion.identity);

            // todo: add teleport raycast obstruction detection, add onPearlLand check of tag
            foreach (ChunkObject o in objects) {
                GameObject chunkGameObject = NatureFactory.Spawn(o.name, x + o.x, o.z, y + o.y, o.r,
                    tileObject.GetComponent<Transform>());
            }

            if (tileObject != null) {
                var chunkProperties = tileObject.AddComponent<ChunkProperties>();
                chunkProperties.properties = StringUtils.ToString(properties);
            }
        }

        public Chunk ChunkDirection(int direction) {
            return (direction % 4) switch {
                0 => north,
                1 => east,
                2 => south,
                3 => west,
                _ => this
            };
        }
    }

    public class ChunkObject {
        public string name;
        public Quaternion r;
        public float x;
        public float y;
        public float z;

        public ChunkObject(string name, float x, float y) {
            this.name = name;
            this.x = x;
            this.y = y;
            z = 0;
        }

        public ChunkObject(string name, float x, float y, Quaternion r) {
            this.name = name;
            this.x = x;
            this.y = y;
            z = 0;
            this.r = r;
        }

        public ChunkObject(string name, float x, float y, float z, Quaternion r) {
            this.name = name;
            this.x = x;
            this.y = y;
            this.z = y;
            this.r = r;
        }
    }

    public class ChunkProperties : MonoBehaviour {
        [Multiline] public string properties;
    }
}