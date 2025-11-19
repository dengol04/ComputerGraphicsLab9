namespace Lab9
{
    public class Polygon
    {
        public int[] Indices { get; }
        public Vector2UV[] UV { get; set; }

        public Polygon(int[] indices)
        {
            Indices = indices;
            UV = null;
        }

        public Polygon(int[] indices, Vector2UV[] uv)
        {
            Indices = indices;
            UV = uv;
        }
    }
}
