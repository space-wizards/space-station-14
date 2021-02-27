#nullable enable
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    /// <summary>
    ///     The basic player mover with footsteps and grabbing
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IMobMoverComponent))]
    public class SharedPlayerMobMoverComponent : Component, IMobMoverComponent, ICollideSpecial
    {
        public override string Name => "PlayerMobMover";
        public override uint? NetID => ContentNetIDs.PLAYER_MOB_MOVER;

        // TODO: COMPSTATE
        [ViewVariables(VVAccess.ReadWrite)]
        public EntityCoordinates LastPosition { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float StepSoundDistance { get; set; }
        [ViewVariables(VVAccess.ReadWrite)]
        public float GrabRange { get; set; }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<SharedPlayerInputMoverComponent>();
        }

        bool ICollideSpecial.PreventCollide(IPhysBody collidedWith)
        {
            // Don't collide with other mobs
            // TODO: unless they have combat mode on
            return collidedWith.Entity.HasComponent<IBody>() &&
                   (!Owner.TryGetComponent(out SharedCombatModeComponent? ownerCombat) || !ownerCombat.IsInCombatMode) &&
                    (!collidedWith.Entity.TryGetComponent(out SharedCombatModeComponent? otherCombat) || !otherCombat.IsInCombatMode);
        }
    }
}
