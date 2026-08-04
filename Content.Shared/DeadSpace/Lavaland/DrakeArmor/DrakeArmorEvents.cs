// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Lavaland.DrakeArmor;

public sealed partial class DrakeArmorTransformActionEvent : InstantActionEvent
{
    [DataField]
    public float DrakeChance = 0.5f;

    [DataField]
    public ProtoId<Polymorph.PolymorphPrototype> DrakePolymorph = "LavalandDrakeArmorDrake";

    [DataField]
    public ProtoId<Polymorph.PolymorphPrototype> SkeletonPolymorph = "LavalandDrakeArmorSkeleton";

    [DataField]
    public HashSet<string> BlockedSpecies = new()
    {
        "IPC",
        "Diona",
        "Xenomorph",
        "SlimePerson",
    };
    [DataField]
    public TimeSpan SkeletonStunDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public DamageSpecifier RepeatedSkeletonDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", FixedPoint2.New(60) },
        },
    };
}

public sealed partial class DrakeFireBreathActionEvent : WorldTargetActionEvent
{
    [DataField]
    public int Range = 6;

    [DataField]
    public EntProtoId FirePrototype = "LavalandAshDrakeFire";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/fireball.ogg");

    [DataField]
    public TimeSpan StepDelay = TimeSpan.FromSeconds(0.075);
}

public sealed partial class DrakeFireRainActionEvent : WorldTargetActionEvent
{
    [DataField]
    public int Radius = 2;

    [DataField]
    public EntProtoId FirePrototype = "LavalandAshDrakeFire";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/fleshtostone.ogg");

    [DataField]
    public EntProtoId TargetPrototype = "LavalandAshDrakeTarget";

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.9);
}

public sealed partial class DrakeSwoopActionEvent : WorldTargetActionEvent
{
    [DataField]
    public float MaxRange = 8f;

    [DataField]
    public float Radius = 1.75f;

    [DataField]
    public TimeSpan Windup = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan Recover = TimeSpan.FromSeconds(0.35);

    [DataField]
    public TimeSpan StepDelay = TimeSpan.FromSeconds(0.085);

    [DataField]
    public SoundSpecifier WindupSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/fireball.ogg");

    [DataField]
    public SoundSpecifier ImpactSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/meteorimpact.ogg");

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", FixedPoint2.New(25) },
            { "Heat", FixedPoint2.New(15) },
        },
    };
}
