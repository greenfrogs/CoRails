using JetBrains.Annotations;

namespace Terrain_Generation {
    [System.Serializable]
    public class Mapping {
        public MappingTiles[] tiles;
        // public MappingTiles[] tiles;
    }

    [System.Serializable]
    public class MappingTiles {
        public string name;
        public string asset;
        public bool collider;
        public string tag;
        [CanBeNull] public bool removeShadows;
    }

    // [System.Serializable]
    // public class MappingTiles : MappingTilesBase {
    //    [CanBeNull] public string north;
    //    [CanBeNull] public string east;
    //    [CanBeNull] public string south;
    //    [CanBeNull] public string west;
    //
    //     public MappingTiles Rotate(int count) {
    //         MappingTiles tile = this.Copy();
    //         for (int i = 0; i < count; i++) {
    //             string temp = tile.north;
    //             tile.north = tile.west;
    //             tile.west = tile.south;
    //             tile.south = tile.east;
    //             tile.east = temp;
    //         }
    //
    //         return tile;
    //     }
    //
    //     public MappingTiles Copy() {
    //         MappingTiles tile = new MappingTiles {
    //             name = name,
    //             asset = asset,
    //             north = north,
    //             east = east,
    //             south = south,
    //             west = west
    //         };
    //         return tile;
    //     }
    // }
}