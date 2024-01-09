using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireComponent : Component
{
    public readonly string BloodContainer = "blood@vampire";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool FangsExtended = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier SpaceDamage = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SpaceDamageInterval = TimeSpan.FromSeconds(1);

    public double NextSpaceDamageTick = 0f;

    public EntityUid? HomeCoffin = default!;

    //Abilities
    public HashSet<VampirePower> UnlockedPowers = new();

    //Blood Drinking
    public TimeSpan BloodDrainDelay = TimeSpan.FromSeconds(2);
    public readonly SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");


    public FrozenDictionary<VampirePower, VampirePowerDef> VampirePowers = new Dictionary<VampirePower, VampirePowerDef>
    {
        { VampirePower.ToggleFangs, new("ActionToggleFangs", null, null, 0, 0) },
        { VampirePower.Glare, new("ActionVampireGlare", null, null, 0, 0) },
        { VampirePower.DeathsEmbrace, new(null, "Smoke", new SoundPathSpecifier("/Audio/Effects/explosionsmallfar.ogg"), 100, 200) },
        { VampirePower.DrinkBlood, new(null, null, new SoundPathSpecifier("/Audio/Items/drink.ogg"), 0, -30) } //Activation cost is how much we drain from the victim
    }.ToFrozenDictionary();
}

public sealed class VampirePowerDef
{
    public readonly string? ActionPrototype;
    public readonly string? ActivationEffect;
    public readonly SoundSpecifier? ActivationSound;
    public readonly int ActivationCost;
    public readonly int UnlockCost;
    public EntityUid? Action;

    public VampirePowerDef(string? actionPrototype, string? activationEffect, SoundSpecifier? activationSound, int unlockCost, int activationCost)
    {
        this.ActionPrototype = actionPrototype;
        this.ActivationEffect = activationEffect;
        this.ActivationSound = activationSound;
        this.UnlockCost = unlockCost;
        this.ActivationCost = activationCost;
    }
}

[Serializable, NetSerializable]
public enum VampirePower : Byte
{
    ToggleFangs,
    Glare,
    DeathsEmbrace,
    DrinkBlood
}
