using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Magic.Events;

public sealed partial class SpawnItemInHandEvent : InstantActionEvent
{
    /// <summary>
    /// Item to create with the action
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Spawned = string.Empty;
}
