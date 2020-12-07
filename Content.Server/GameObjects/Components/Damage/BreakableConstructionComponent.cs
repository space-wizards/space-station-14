#nullable enable
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class BreakableConstructionComponent : Component, IDestroyAct
    {
        public override string Name => "BreakableConstruction";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Node, "node", string.Empty);
        }

        public string Node { get; private set; } = string.Empty;

        async void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            if (Owner.Deleted ||
                !Owner.TryGetComponent(out ConstructionComponent? construction) ||
                string.IsNullOrEmpty(Node))
            {
                return;
            }

            await construction.ChangeNode(Node);
        }
    }
}
