using Content.Server.Wires;
using Content.Shared.Construction;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ChangeWiresPanelSecurityLevel : IGraphAction
{
    [DataField("level")]
    [ValidatePrototypeId<WiresPanelSecurityLevelPrototype>]
    public string WiresPanelSecurityLevelID = "Level0";

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (WiresPanelSecurityLevelID == null)
            return;

        if (entityManager.TryGetComponent(uid, out WiresPanelComponent? wiresPanel)
            && entityManager.TrySystem(out WiresSystem? wiresSystem))
        {
            wiresSystem.SetWiresPanelSecurityData(uid, wiresPanel, WiresPanelSecurityLevelID);
        }
    }
}
