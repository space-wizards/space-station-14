using Content.Server.Wires;
using Content.Shared.Construction;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ChangeWiresAccess : IGraphAction
    {
        [DataField("wiresPanelCovering")]
        public string? WiresPanelCovering { get; private set; }

        [DataField("wiresPanelCoveringWelded")]
        public bool WiresPanelCoveringWelded { get; private set; } = false;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (entityManager.TryGetComponent(uid, out WiresPanelComponent? wiresPanel))
            {
                if (entityManager.TrySystem(out WiresSystem? wires))
                {
                    wires.SetPanelData(uid, wiresPanel, WiresPanelCovering, WiresPanelCoveringWelded);

                    if (WiresPanelCovering != null)
                    {
                        wires.CloseAllUserInterfaces(uid);
                    }
                }
            }
        }
    }
}
