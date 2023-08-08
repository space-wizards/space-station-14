using Content.Server.Wires;
using Content.Shared.Construction;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ChangeWiresPanelSecurityLevel : IGraphAction
    {
        [DataField("level")]
        public string WiresPanelSecurityLevelID { get; private set; } = default!;

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
}
