using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalUnitComponent : Component, IInteractHand, IInteractUsing
    {
        public override string Name => "DisposalUnit";

        [ViewVariables]
        private Container _container;

        public override void Initialize()
        {
            base.Initialize();
            _container = ContainerManagerComponent.Ensure<Container>(Name, Owner);
        }

        private bool TryInsert(IEntity entity)
        {
            // TODO: Click drag
            return _container.Insert(entity);
        }

        private bool TryFlush()
        {
            var snapGrid = Owner.GetComponent<SnapGridComponent>();
            var entry = snapGrid
                .GetLocal()
                .FirstOrDefault(entity => entity.HasComponent<DisposalEntryComponent>());

            if (entry == null)
            {
                return false; // TODO
            }

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();
            foreach (var entity in _container.ContainedEntities.ToList())
            {
                _container.Remove(entity);
                entryComponent.TryInsert(entity);
            }

            return true;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryFlush();
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsert(eventArgs.Using);
        }

        [Verb]
        private sealed class SelfInsertVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Jump inside");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.TryInsert(user);
            }
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalUnitComponent>
        {
            protected override void GetData(IEntity user, DisposalUnitComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Flush");
            }

            protected override void Activate(IEntity user, DisposalUnitComponent component)
            {
                component.TryFlush();
            }
        }
    }
}
