namespace Content.Shared.GameObjects
{
    public readonly struct VerbCategoryData
    {
        public VerbCategoryData(string name, string icon)
        {
            Name = name;
            Icon = icon;
        }

        public string Name { get; }
        public string Icon { get; }

        public static implicit operator VerbCategoryData((string name, string icon) tuple)
        {
            return new VerbCategoryData(tuple.name, tuple.icon);
        }
    }
}
