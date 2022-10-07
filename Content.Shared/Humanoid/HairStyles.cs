namespace Content.Shared.Humanoid
{
    public static class HairStyles
    {
        public const string DefaultHairStyle = "HairBald";
        public const string DefaultFacialHairStyle = "FacialHairShaved";

        public static readonly IReadOnlyList<Color> RealisticHairColors = new List<Color>
        {
            Color.Yellow,
            Color.Black,
            Color.SandyBrown,
            Color.Brown,
            Color.Wheat,
            Color.Gray
        };
    }
}
