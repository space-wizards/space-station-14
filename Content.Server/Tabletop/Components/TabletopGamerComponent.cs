namespace Content.Server.Tabletop.Components;

/// <summary>
/// Component for marking an entity as currently playing a tabletop.
/// </summary>
[RegisterComponent, Access(typeof(TabletopSystem))]
public sealed partial class TabletopGamerComponent : Component
{
    [DataField]
    public EntityUid Tabletop = EntityUid.Invalid;
}
