using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for the Entity Container that stores the victim of Lapse.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicLapsedComponent : Component
{
    [DataField]
    public EntProtoId Vfx = "EffectCosmicActionLapse";

    [DataField]
    public SoundSpecifier Sfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-lapse.ogg");
}

[Serializable, NetSerializable]
public enum CosmicLapseVisuals
{
    Visuals,
    Layer,
}
