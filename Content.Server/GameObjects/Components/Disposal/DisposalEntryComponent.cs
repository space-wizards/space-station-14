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
            if (!CanInsert(entity))
            {
                return false;
            }

            var disposable = entity.EnsureComponent<InDisposalsComponent>();

            Contents.Insert(disposable.Owner);
            disposable.EnterTube(this);

            return true;
        }

        protected override Direction[] ConnectableDirections()
        {
            return new[] {Owner.Transform.LocalRotation.GetDir()};
        }

        public override IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals)
        {
            return Connectors.GetValueOrDefault(ConnectableDirections()[0]);
        }
    }
}
