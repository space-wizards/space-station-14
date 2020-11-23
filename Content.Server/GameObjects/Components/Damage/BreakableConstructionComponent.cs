#nullable enable
using System;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class BreakableConstructionComponent : RuinableComponent
    {
        private ActSystem _actSystem = default!;

        public override string Name => "BreakableConstruction";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Node, "node", string.Empty);
        }

        public override void Initialize()
        {
            base.Initialize();

            _actSystem = EntitySystem.Get<ActSystem>();
        }

        public string Node { get; private set; } = string.Empty;

        protected override async void DestructionBehavior()
        {
            if (Owner.Deleted || !Owner.TryGetComponent(out ConstructionComponent? construction) || string.IsNullOrEmpty(Node)) return;

            _actSystem.HandleBreakage(Owner);

            await construction.ChangeNode(Node);
        }
    }
}
