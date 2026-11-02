using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Character;

[RegisterComponent, ComponentProtoName("CF-Species")]
public sealed partial class CFSpeciesComponent : Component
{
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species;
}
