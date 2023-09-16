using Content.Shared.Construction;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SetWiresPanelSecurity : IGraphAction
{
    [DataField("examine")]
    public string Examine = string.Empty;

    [DataField("wiresAccessible")]
    public bool WiresAccessible = true;

    [DataField("weldingAllowed")]
    public bool WeldingAllowed = true;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent(uid, out WiresPanelSecurityComponent? _))
        {
            var ev = new WiresPanelSecurityEvent(Examine, WiresAccessible, WeldingAllowed);
            entityManager.EventBus.RaiseLocalEvent(uid, ev);
        }
    }
}
