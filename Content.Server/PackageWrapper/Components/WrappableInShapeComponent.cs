using Robust.Shared.Serialization;

namespace Content.Server.PackageWrapper.Components
{
    [RegisterComponent]
    public class WrappableInShapeComponent : Component
    {
        [ViewVariables]
        [DataField("wrapIn")]
        public string WrapType { get; } = string.Empty;
    }
}
