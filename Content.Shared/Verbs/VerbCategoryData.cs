#nullable enable
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Verbs
{
    /// <summary>
    /// Contains combined name and icon information for a verb category.
    /// </summary>
    public readonly struct VerbCategoryData
    {
        public VerbCategoryData(string name, SpriteSpecifier? icon)
        {
            Name = name;
            Icon = icon;
        }

        public string Name { get; }
        public SpriteSpecifier? Icon { get; }

        public static implicit operator VerbCategoryData((string name, string? icon) tuple)
        {
            var (name, icon) = tuple;

            if (icon == null)
            {
                return new VerbCategoryData(name, null);
            }

            return new VerbCategoryData(name, new SpriteSpecifier.Texture(new ResourcePath(icon)));
        }
    }
}
