using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Rotatable
{
    [RegisterComponent]
    public class RotatableComponent : Component
    {
        public override string Name => "Rotatable";

        /// <summary>
        ///     If true, this entity can be rotated even while anchored.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool RotateWhileAnchored { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.RotateWhileAnchored, "rotateWhileAnchored", false);
        }

        private void TryRotate(IEntity user, Angle angle)
        {
            if (!RotateWhileAnchored && Owner.TryGetComponent(out IPhysicsComponent physics))
            {
                if (physics.Anchored)
                {
                    Owner.PopupMessage(user, Loc.GetString("It's stuck."));
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
                if (!ActionBlockerSystem.CanInteract(user) || (!component.RotateWhileAnchored && component.Owner.TryGetComponent(out IPhysicsComponent physics) && physics.Anchored))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.CategoryData = VerbCategories.Rotate;
                data.Text = Loc.GetString("Rotate clockwise");
                data.IconTexture = "/Textures/Interface/VerbIcons/rotate_cw.svg.96dpi.png";
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
                if (!ActionBlockerSystem.CanInteract(user) || (!component.RotateWhileAnchored && component.Owner.TryGetComponent(out IPhysicsComponent physics) && physics.Anchored))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.CategoryData = VerbCategories.Rotate;
                data.Text = Loc.GetString("Rotate counter-clockwise");
                data.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.96dpi.png";
            }

            protected override void Activate(IEntity user, RotatableComponent component)
            {
                component.TryRotate(user, Angle.FromDegrees(90));
            }
        }

    }
}
