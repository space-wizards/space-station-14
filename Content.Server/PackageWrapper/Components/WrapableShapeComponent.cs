namespace Content.Server.PackageWrapper.Components
{
    [RegisterComponent]
    public class WrapableShapeComponent : Component
    {
        public sealed override string Name => "WrapType";

        [DataField("wrapIn")]
        public string WrapType { get; } = string.Empty;
    }
}
