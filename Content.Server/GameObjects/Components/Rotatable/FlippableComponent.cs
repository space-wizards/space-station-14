#nullable enable
using Content.Server.Interfaces;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Rotatable
{
    [RegisterComponent]
    public class FlippableComponent : Component
    {
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

        public override string Name => "Flippable";

        private string? _entity;

        private void TryFlip(IEntity user)
        {
            if (Owner.TryGetComponent(out ICollidableComponent? collidable) &&
                collidable.Anchored)
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user, Loc.GetString("It's stuck."));
                return;
            }

            if (_entity == null)
            {
                return;
            }

            Owner.EntityManager.SpawnEntity(_entity, Owner.Transform.GridPosition);
            Owner.Delete();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _entity, "entity", Owner.Prototype?.ID);
        }

        [Verb]
        private sealed class FlippableVerb : Verb<FlippableComponent>
        {
            protected override void GetData(IEntity user, FlippableComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Flip");
            }

            protected override void Activate(IEntity user, FlippableComponent component)
            {
                component.TryFlip(user);
            }
        }
    }
}
