using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicBreachComponent : Component
{
    [DataField] public EntityUid? LinkedBreach;

    [DataField] public EntProtoId TeleportVfx = "CosmicLapseAbilityVfx";

    [DataField] public SoundSpecifier TeleportSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/Abilities/ability-lapse.ogg");
}
