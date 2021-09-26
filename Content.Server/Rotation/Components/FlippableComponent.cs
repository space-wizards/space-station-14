using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Rotation.Components
{
    [RegisterComponent]
    public class FlippableComponent : Component
    {
        public override string Name => "Flippable";


        private string? _entity => _internalEntity ?? Owner.Prototype?.ID;

        [DataField("entity")]
        private string? _internalEntity;

        private void TryFlip(IEntity user)
        {
            if (Owner.TryGetComponent(out IPhysBody? physics) &&
                physics.BodyType == BodyType.Static)
            {
                Owner.PopupMessage(user, Loc.GetString("flippable-component-try-flip-is-stuck"));
                return;
            }

            if (_entity == null)
            {
                return;
            }

            Owner.EntityManager.SpawnEntity(_entity, Owner.Transform.Coordinates);
            Owner.Delete();
        }

        [Verb]
        private sealed class FlippableVerb : Verb<FlippableComponent>
        {
            protected override void GetData(IEntity user, FlippableComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("flippable-verb-get-data-text");
            }

            protected override void Activate(IEntity user, FlippableComponent component)
            {
                component.TryFlip(user);
            }
        }
    }
}
