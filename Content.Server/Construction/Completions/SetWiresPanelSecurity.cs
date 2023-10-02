using Content.Shared.Construction;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

/// <summary>
///     This graph action is used to set values on entities with the <see cref="WiresPanelSecurityComponent"/>
/// </summary>

[UsedImplicitly]
[DataDefinition]
public sealed partial class SetWiresPanelSecurity : IGraphAction
{
    /// <summary>
    ///     Sets the Examine field on the entity's <see cref="WiresPanelSecurityComponent"/>
    /// </summary>
    [DataField("examine")]
    public string Examine = string.Empty;

    /// <summary>
    ///     Sets the WiresAccessible field on the entity's <see cref="WiresPanelSecurityComponent"/>
    /// </summary>
    [DataField("wiresAccessible")]
    public bool WiresAccessible = true;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent(uid, out WiresPanelSecurityComponent? _))
        {
            var ev = new WiresPanelSecurityEvent(Examine, WiresAccessible);
            entityManager.EventBus.RaiseLocalEvent(uid, ev);
        }
    }
}
