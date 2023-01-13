using System.Threading;
using Robust.Shared.Audio;
using Content.Shared.Damage;

namespace Content.Server.PlasmaCutter.Components;

[RegisterComponent]
public sealed class PlasmaCutterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("defaultFuelCount")] private const double DefaultFuelCount = 750;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("maxFuel")] public double MaxFuel = DefaultFuelCount;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fuel")]
    public double CurrentFuel = DefaultFuelCount;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float UseDelay = 2f;

    [DataField("sparksSound")]
    public SoundSpecifier sparksSound = new SoundCollectionSpecifier("sparks");

    [DataField("successSound")]
    public SoundSpecifier successSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    [DataField("activatedMeleeDamageBonus")]
    public DamageSpecifier ActivatedMeleeDamageBonus = new();

    public bool Activated = false;

    public CancellationTokenSource? CancelToken = null;
}
