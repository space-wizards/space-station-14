using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class CanBackflipComponent : Component
{
    [DataField]
    public EntProtoId BackflipAction = "ActionBackflip";

    [DataField]
    public EntityUid? BackflipActionEntity;

    [DataField]
    public string ClappaSfx = "";
}

public sealed partial class BackflipActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed class DoABackFlipEvent(NetEntity actioner, string sfxSource) : EntityEventArgs
{
    public NetEntity Actioner = actioner;
    public string SfxSource = sfxSource;
}
