namespace Terrain_Generation.Visitors.ChunkVisitors {
    public abstract class ChunkVisitor: Visitor {
        public override void Generate(Chunk[,] chunks) {
            for (int x = 0; x < chunks.GetLength(0); x++) {
                for (int y = 0; y < chunks.GetLength(1); y++) {
                    Run(chunks[x,y]);
                }
            }
        }

        public abstract void Run(Chunk chunk);
    }
}