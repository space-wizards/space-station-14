using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing
{
    // from SimpleStation 14
    [RegisterComponent]
    public sealed class ClothingGrantComponentComponent : Component
    {
        [DataField("component", required: true)]
        [AlwaysPushInheritance]
        public EntityPrototype.ComponentRegistry Components { get; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsActive = false;
    }
}
