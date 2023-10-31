// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AdmemeEvents;

/// <summary>
/// Компонент добавляет возможность видеть указанный StatusIcon энтити,
/// при условии что у наблюдателя тоже есть этот компонент с одинаковым RoleGroupKey.
/// Используется чтобы ивентовые рольки видели друг-друга.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EventRoleComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public ProtoId<StatusIconPrototype> StatusIcon;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string RoleGroupKey;
}
