using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.CharacterAppearance.Components;

[RegisterComponent]
public sealed partial class RandomHumanoidAppearanceComponent : Component
{
    [DataField("randomizeName")] public bool RandomizeName = true;
}
