using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared.ComponentTable;

/// <summary>
/// Applies components from entities selected from the table on init.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedComponentTableSystem))]
public sealed partial class ComponentTableComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}
