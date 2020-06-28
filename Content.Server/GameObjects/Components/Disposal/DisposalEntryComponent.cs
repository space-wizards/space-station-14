using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalEntryComponent : DisposalTubeComponent
    {
        public override string Name => "DisposalEntry";

        private bool CanInsert(IEntity entity)
        {
            return entity.HasComponent<ItemComponent>() ||
                   entity.HasComponent<SpeciesComponent>();
        }

        public bool TryInsert(IEntity entity)
        {
            if (!CanInsert(entity) || !Contents.Insert(entity))
            {
                return false;
            }

            var inDisposals = entity.EnsureComponent<InDisposalsComponent>();
            inDisposals.EnterTube(this);

            return true;
        }

        protected override Direction[] ConnectableDirections()
        {
            return new[] {Owner.Transform.LocalRotation.GetDir()};
        }

        protected override IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals)
        {
            return Connected.GetValueOrDefault(ConnectableDirections()[0]);
        }
    }
}
