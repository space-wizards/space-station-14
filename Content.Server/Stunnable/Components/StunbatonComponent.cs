using Content.Server.Stunnable.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Stunnable.Components;

[RegisterComponent, Access(typeof(StunbatonSystem))]
public sealed class StunbatonComponent : Component
{
    public bool Activated = false;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("energyPerUse")]
    public float EnergyPerUse = 350;

    [DataField("stunSound", required: true)]
    public SoundSpecifier StunSound = default!;

    [DataField("sparksSound")]
    public SoundSpecifier SparksSound = new SoundCollectionSpecifier("sparks");

    [DataField("turnOnFailSound")]
    public SoundSpecifier TurnOnFailSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
