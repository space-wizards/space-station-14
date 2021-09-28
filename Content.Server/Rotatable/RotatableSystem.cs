using Content.Shared.Popups;
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
            SubscribeLocalEvent<FlippableComponent, GetOtherVerbsEvent>(AddFlipVerb);
            SubscribeLocalEvent<RotatableComponent, GetOtherVerbsEvent>(AddRotateVerbs);
        }

        private void AddFlipVerb(EntityUid uid, FlippableComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract || component.MirrorEntity == null)
                return;

            Verb verb = new();
            verb.Act = () => TryFlip(component, args.User);
            verb.Text = Loc.GetString("flippable-verb-get-data-text");
            // TODO VERB ICONS Add Uno reverse card style icon?
            args.Verbs.Add(verb);
        }

        private void AddRotateVerbs(EntityUid uid, RotatableComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Check if the object is anchored, and whether we are still allowed to rotate it.
            if (!component.RotateWhileAnchored &&
                component.Owner.TryGetComponent(out IPhysBody? physics) &&
                physics.BodyType == BodyType.Static)
                return;

            // rotate clockwise
            Verb rotateCW = new();
            rotateCW.Act = () => component.Owner.Transform.LocalRotation += Angle.FromDegrees(-90);
            rotateCW.Category = VerbCategory.Rotate;
            rotateCW.IconTexture =  "/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png";
            rotateCW.Priority = -2; // show CCW, then CW
            rotateCW.CloseMenu = false; // allow for easy double rotations.
            args.Verbs.Add(rotateCW);

            // rotate counter-clockwise
            Verb rotateCCW = new();
            rotateCCW.Act = () => component.Owner.Transform.LocalRotation += Angle.FromDegrees(90);
            rotateCCW.Category = VerbCategory.Rotate;
            rotateCCW.IconTexture = "/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png";
            rotateCCW.Priority = -1;
            rotateCCW.CloseMenu = false; // allow for easy double rotations.
            args.Verbs.Add(rotateCCW);
        }

        /// <summary>
        ///     Replace a flippable entity with it's flipped / mirror-symmetric entity.
        /// </summary>
        public static void TryFlip(FlippableComponent component, IEntity user)
        {
            // TODO FLIPPABLE Currently an entity needs to be un-anchored when flipping. But the newly spawned entity
            // defaults to being anchored (and spawns under floor tiles). Fix this?
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
