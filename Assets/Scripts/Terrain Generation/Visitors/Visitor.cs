namespace Terrain_Generation.Visitors {
    public abstract class Visitor {
        public abstract void Generate(Chunk[,] chunks);
    }
}