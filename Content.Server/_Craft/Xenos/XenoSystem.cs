using Content.Server.Coordinates.Helpers;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Doors.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map;

namespace Content.Server.Abilities.Xeno
{
    public sealed class XenoSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = null!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<XenoQueenComponent, ComponentInit>(OnQueenComponentInit);
            SubscribeLocalEvent<XenoQueenComponent, XenoBirthActionEvent>(OnXenoBirth);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        private void OnQueenComponentInit(EntityUid uid, XenoQueenComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.XenoBirthAction, uid);
        }
        private void OnXenoBirth(EntityUid uid, XenoQueenComponent component, XenoBirthActionEvent args)
        {
            if (!component.Enabled) return;
            var xform = Transform(uid);
            var coords = xform.Coordinates.SnapToGrid();
            foreach (var entity in coords.GetEntitiesInTile())
            {
                PhysicsComponent? physics = null; // We use this to check if it's impassable
                if (((Resolve(entity, ref physics, false) && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0) // Is it impassable?
                    && !(TryComp<DoorComponent>(entity, out var door) && door.State != DoorState.Closed))) // Is it a door that's open and so not actually impassable?
                {
                    _popupSystem.PopupEntity(Loc.GetString("xeno-queen-birth-failed"), uid, uid);
                    return;
                }
            }
            _popupSystem.PopupEntity(Loc.GetString("xeno-queen-birth-popup", ("xeno", uid)), uid);
            Spawn(_random.Pick(component.SpawnPrototypes), coords);
            args.Handled = true;
        }
    }
    public sealed class XenoBirthActionEvent : InstantActionEvent { }
}
