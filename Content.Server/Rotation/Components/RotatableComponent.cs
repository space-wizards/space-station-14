using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
using Content.Shared.Rotatable;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Rotation.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRotatableComponent))]
    public class RotatableComponent : SharedRotatableComponent
    {
        private void TryRotate(IEntity user, Angle angle)
        {
            if (!RotateWhileAnchored && Owner.TryGetComponent(out IPhysBody? physics))
            {
                if (physics.BodyType == BodyType.Static)
                {
                    Owner.PopupMessage(user, Loc.GetString("rotatable-component-try-rotate-stuck"));
                    return;
                }
            }

            Owner.Transform.LocalRotation += angle;
        }

        [Verb]
        public sealed class RotateVerb : Verb<RotatableComponent>
        {
            protected override void GetData(IEntity user, RotatableComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || (!component.RotateWhileAnchored && component.Owner.TryGetComponent(out IPhysBody? physics) && physics.BodyType == BodyType.Static))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.CategoryData = VerbCategories.Rotate;
                data.Text = Loc.GetString("rotate-verb-get-data-text");
                data.IconTexture = "/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, RotatableComponent component)
            {
                component.TryRotate(user, Angle.FromDegrees(-90));
            }
        }

        [Verb]
        public sealed class RotateCounterVerb : Verb<RotatableComponent>
        {
            protected override void GetData(IEntity user, RotatableComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) || (!component.RotateWhileAnchored && component.Owner.TryGetComponent(out IPhysBody? physics) && physics.BodyType == BodyType.Static))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.CategoryData = VerbCategories.Rotate;
                data.Text = Loc.GetString("rotate-counter-verb-get-data-text");
                data.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, RotatableComponent component)
            {
                component.TryRotate(user, Angle.FromDegrees(90));
            }
        }

    }
}
