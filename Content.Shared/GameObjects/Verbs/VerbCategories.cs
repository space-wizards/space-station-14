#nullable enable

namespace Content.Shared.GameObjects.Verbs
{
    /// <summary>
    ///     Standard verb categories.
    /// </summary>
    public static class VerbCategories
    {
        public static readonly VerbCategoryData Debug =
            ("Debug", "/Textures/Interface/VerbIcons/debug.svg.96dpi.png");

        public static readonly VerbCategoryData Rotate = ("Rotate", null);
        public static readonly VerbCategoryData Construction = ("Construction", null);
    }
}
