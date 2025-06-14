using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Rotatable;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.Rotatable
{
    /// <summary>
    ///     Handles verbs for the <see cref="RotatableComponent"/> and <see cref="FlippableComponent"/> components.
    /// </summary>
    public sealed class RotatableSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<FlippableComponent, GetVerbsEvent<Verb>>(AddFlipVerb);
            SubscribeLocalEvent<RotatableComponent, GetVerbsEvent<Verb>>(AddRotateVerbs);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.RotateObjectClockwise, new PointerInputCmdHandler(HandleRotateObjectClockwise))
                .Bind(ContentKeyFunctions.RotateObjectCounterclockwise, new PointerInputCmdHandler(HandleRotateObjectCounterclockwise))
                .Bind(ContentKeyFunctions.FlipObject, new PointerInputCmdHandler(HandleFlipObject))
                .Register<RotatableSystem>();
        }

        private void AddFlipVerb(EntityUid uid, FlippableComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess
                || !args.CanInteract
                || !args.CanComplexInteract)
                return;

            // Check if the object is anchored.
            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? physics) && physics.BodyType == BodyType.Static)
                return;

            Verb verb = new()
            {
                Act = () => Flip(uid, component),
                Text = Loc.GetString("flippable-verb-get-data-text"),
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
                Priority = -3, // show flip last
                DoContactInteraction = true
            };
            args.Verbs.Add(verb);
        }

        private void AddRotateVerbs(EntityUid uid, RotatableComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanAccess
                || !args.CanInteract
                || !args.CanComplexInteract
                || Transform(uid).NoLocalRotation) // Good ol prototype inheritance, eh?
                return;

            // Check if the object is anchored, and whether we are still allowed to rotate it.
            if (!component.RotateWhileAnchored &&
                EntityManager.TryGetComponent(uid, out PhysicsComponent? physics) &&
                physics.BodyType == BodyType.Static)
                return;

            Verb resetRotation = new()
            {
                DoContactInteraction = true,
                Act = () => EntityManager.GetComponent<TransformComponent>(uid).LocalRotation = Angle.Zero,
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Text = "Reset",
                Priority = -2, // show CCW, then CW, then reset
                CloseMenu = false,
            };
            args.Verbs.Add(resetRotation);

            // rotate clockwise
            Verb rotateCW = new()
            {
                Act = () => EntityManager.GetComponent<TransformComponent>(uid).LocalRotation -= component.Increment,
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
                Priority = -1,
                CloseMenu = false, // allow for easy double rotations.
            };
            args.Verbs.Add(rotateCW);

            // rotate counter-clockwise
            Verb rotateCCW = new()
            {
                Act = () => EntityManager.GetComponent<TransformComponent>(uid).LocalRotation += component.Increment,
                Category = VerbCategory.Rotate,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png")),
                Priority = 0,
                CloseMenu = false, // allow for easy double rotations.
            };
            args.Verbs.Add(rotateCCW);
        }

        /// <summary>
        ///     Replace a flippable entity with it's flipped / mirror-symmetric entity.
        /// </summary>
        public void Flip(EntityUid uid, FlippableComponent component)
        {
            var oldTransform = EntityManager.GetComponent<TransformComponent>(uid);
            var entity = EntityManager.SpawnEntity(component.MirrorEntity, oldTransform.Coordinates);
            var newTransform = EntityManager.GetComponent<TransformComponent>(entity);
            newTransform.LocalRotation = oldTransform.LocalRotation;
            _transform.Unanchor(entity, newTransform);
            EntityManager.DeleteEntity(uid);
        }

        public bool HandleRotateObjectClockwise(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
        {
            if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
                return false;

            if (!TryComp<RotatableComponent>(entity, out var rotatableComp))
                return false;

            if (!_actionBlocker.CanInteract(player, entity)
                || !_actionBlocker.CanComplexInteract(player)
                || !_interaction.InRangeAndAccessible(player, entity))
                return false;

            // Check if the object is anchored, and whether we are still allowed to rotate it.
            if (!rotatableComp.RotateWhileAnchored && EntityManager.TryGetComponent(entity, out PhysicsComponent? physics) &&
                physics.BodyType == BodyType.Static)
            {
                _popup.PopupEntity(Loc.GetString("rotatable-component-try-rotate-stuck"), entity, player);
                return false;
            }

            Transform(entity).LocalRotation -= rotatableComp.Increment;
            return true;
        }

        public bool HandleRotateObjectCounterclockwise(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
        {
            if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
                return false;

            if (!TryComp<RotatableComponent>(entity, out var rotatableComp))
                return false;

            if (!_actionBlocker.CanInteract(player, entity)
                || !_actionBlocker.CanComplexInteract(player)
                || !_interaction.InRangeAndAccessible(player, entity))
                return false;

            // Check if the object is anchored, and whether we are still allowed to rotate it.
            if (!rotatableComp.RotateWhileAnchored && EntityManager.TryGetComponent(entity, out PhysicsComponent? physics) &&
                physics.BodyType == BodyType.Static)
            {
                _popup.PopupEntity(Loc.GetString("rotatable-component-try-rotate-stuck"), entity, player);
                return false;
            }

            Transform(entity).LocalRotation += rotatableComp.Increment;
            return true;
        }

        public bool HandleFlipObject(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
        {
            if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
                return false;

            if (!TryComp<FlippableComponent>(entity, out var flippableComp))
                return false;

            if (!_actionBlocker.CanInteract(player, entity)
                || !_actionBlocker.CanComplexInteract(player)
                || !_interaction.InRangeAndAccessible(player, entity))
                return false;

            // Check if the object is anchored.
            if (EntityManager.TryGetComponent(entity, out PhysicsComponent? physics) && physics.BodyType == BodyType.Static)
            {
                _popup.PopupEntity(Loc.GetString("flippable-component-try-flip-is-stuck"), entity, player);
                return false;
            }

            Flip(entity, flippableComp);
            return true;
        }
    }
}
