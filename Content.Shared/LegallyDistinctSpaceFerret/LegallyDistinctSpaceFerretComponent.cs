using System.Threading;
using Content.Shared.Actions;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.LegallyDistinctSpaceFerret;

public sealed partial class BackflipActionEvent : InstantActionEvent
{
}

public sealed partial class EepyActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed class DoABackFlipEvent(NetEntity backflipper, string sfxSource) : EntityEventArgs
{
    public NetEntity Backflipper = backflipper;
    public string SfxSource = sfxSource;
}

[Serializable, NetSerializable]
public sealed class GoEepyEvent(NetEntity eepyier) : EntityEventArgs
{
    public NetEntity Eepier = eepyier;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class LegallyDistinctSpaceFerretComponent : Component
{
    [DataField]
    public EntProtoId BackflipAction = "ActionBackflip";

    [DataField]
    public EntityUid? BackflipActionEntity;

    [DataField]
    public EntProtoId EepyAction = "ActionEepy";

    [DataField]
    public EntityUid? EepyActionEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TimeUntilHibernate = TimeSpan.FromMinutes(10);

    [DataField]
    public string OutOfRangeMessage;

    [DataField]
    public string NotEnoughNutrientsMessage;

    [DataField]
    public string YouWinMessage;

    [DataField]
    public string RoleIntroSfx;

    [DataField]
    public string RoleOutroSfx;

    [DataField]
    public string ClappaSfx;

    [DataField]
    public ProtoId<AntagPrototype> AntagProtoId = "LegallyDistinctSpaceFerret";

    [DataField]
    public float BrainRotAuraRadius = 4.0f;

    public CancellationTokenSource BrainrotEffectCanceller;
}
