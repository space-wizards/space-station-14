using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CosmicCult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MalignRiftSpawnRuleComponent : Component
{
    [DataField] public SoundSpecifier Tier2Sound = new SoundPathSpecifier("/Audio/Cosmic/tier2.ogg");
}
