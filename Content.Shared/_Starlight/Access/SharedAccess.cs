using Content.Shared.Access;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Access;

[Serializable, NetSerializable]
public sealed class AccessGroupSelectedMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<AccessGroupPrototype> SelectedGroup;

    public AccessGroupSelectedMessage(ProtoId<AccessGroupPrototype> selectedGroup) => SelectedGroup = selectedGroup;
}