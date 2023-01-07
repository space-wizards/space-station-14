using System.Threading;
using Robust.Shared.Audio;
using Content.Shared.Damage;

namespace Content.Server.PlasmaCutter.Components;

[RegisterComponent]
public sealed class PlasmaCutterComponent : Component
{
    private const int DefaultAmmoCount = 10;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("maxAmmo")] public int MaxAmmo = DefaultAmmoCount;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ammo")]
    public int CurrentAmmo = DefaultAmmoCount;

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
