// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Necromorphs.Deva;

[RegisterComponent]
public sealed partial class DevaComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickUtilPrison;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickUtilEnrage;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DamageTick;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DurationEnrage = 10f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DurationPrison = 10f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Range = 0.5f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeedMultiplier = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeedEnrage = 2f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float MovementSpeed = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTrappedVictim = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsEnrageState = false;

    [DataField]
    public EntProtoId DevaEnrageAction = "ActionDevaEnrage";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? DevaEnrageActionEntity;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 3 },
            { "Slash", 2 },
            { "Piercing", 4 },
            {"Structural", 10}
        }
    };

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? EatSound = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? EnrageSound = default!;
}
