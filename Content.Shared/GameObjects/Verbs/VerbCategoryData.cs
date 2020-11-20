using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Verbs
{
    /// <summary>
    /// Contains combined name and icon information for a verb category.
    /// </summary>
    public readonly struct VerbCategoryData
    {
        public VerbCategoryData(string name, SpriteSpecifier icon)
        {
            Name = name;
            Icon = icon;
        }

        public string Name { get; }
        public SpriteSpecifier Icon { get; }

        public static implicit operator VerbCategoryData((string name, string icon) tuple)
        {
            return new(tuple.name, tuple.icon == null ? null : new SpriteSpecifier.Texture(new ResourcePath(tuple.icon)));
        }
    }
}
