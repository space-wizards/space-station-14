using Content.Shared.Notification.Managers;
using Content.Shared.Rotatable;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Rotatable
{
    /// <summary>
    ///     Handles verbs for the <see cref="RotatableComponent"/> and <see cref="FlippableComponent"/> components.
    /// </summary>
    public class RotatableSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<FlippableComponent, AssembleVerbsEvent>(AddFlipVerb);
            SubscribeLocalEvent<RotatableComponent, AssembleVerbsEvent>(AddRotateVerbs);
        }

        private void AddFlipVerb(EntityUid uid, FlippableComponent component, AssembleVerbsEvent args)
        {
            if (!args.Types.HasFlag(VerbTypes.Other))
                return;

            if (!args.DefaultInRangeUnobstructed || args.Hands == null)
                return;

            if (component.MirrorEntity == null)
                return;

            Verb verb = new("flip");
            verb.Act = () => TryFlip(component, args.User);
            verb.Text = Loc.GetString("flippable-verb-get-data-text");
            // TODO VERB ICONS Add Uno reverse card style icon?
            args.Verbs.Add(verb);
        }

        private void AddRotateVerbs(EntityUid uid, RotatableComponent component, AssembleVerbsEvent args)
        {
            if (!args.Types.HasFlag(VerbTypes.Other))
                return;

            if (!args.DefaultInRangeUnobstructed || args.Hands == null)
                return;

            // Check if the object is anchored, and whether we are still allowed to rotate it.
            if (!component.RotateWhileAnchored && component.Owner.TryGetComponent(out IPhysBody? physics) && physics.BodyType == BodyType.Static)
                return;

            // rotate clockwise
            Verb rotateCW = new("rotatecw");
            rotateCW.Act = () => component.Owner.Transform.LocalRotation += Angle.FromDegrees(-90);
            rotateCW.Category = VerbCategories.Rotate;
            rotateCW.IconTexture =  "/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png";
            rotateCW.Priority = -2;
            args.Verbs.Add(rotateCW);

            // rotate counter-clockwise
            Verb rotateCCW = new("rotateccw");
            rotateCCW.Act = () => component.Owner.Transform.LocalRotation += Angle.FromDegrees(90);
            rotateCCW.Category = VerbCategories.Rotate;
            rotateCCW.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png";
            rotateCCW.Priority = -1;
            args.Verbs.Add(rotateCCW);
        }

        /// <summary>
        ///     Replace a flippable entity with it's flipped / mirror-symmetric entity.
        /// </summary>
        public static void TryFlip(FlippableComponent component, IEntity user)
        {
            // TODO FLIPPABLE Currently an entity needs t0 be un-anchored when flipping, but the newly spawned in entity
            // defaults to being flipped (and spawns under floor tiles). Fix this?
            if (component.Owner.TryGetComponent(out IPhysBody? physics) &&
                physics.BodyType == BodyType.Static)
            {
                component.Owner.PopupMessage(user, Loc.GetString("flippable-component-try-flip-is-stuck"));
                return;
            }

            component.Owner.EntityManager.SpawnEntity(component.MirrorEntity, component.Owner.Transform.Coordinates);
            component.Owner.Delete();
        }
    }
}
