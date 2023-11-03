using Content.Server.Popups;
using Content.Server.Speech.Muting;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Necroobelisk.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.DoAfter;
using Content.Shared.UnitologyPowerSystem;

namespace Content.Server.Abilities.Unitolog
{
    public sealed class UnitologPowersSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
        [Dependency] private readonly TurfSystem _turf = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<UnitologPowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<UnitologPowersComponent, ObeliskActionEvent>(OnInvisibleWall);

            SubscribeLocalEvent<UnitologPowersComponent, ObeliskSpawnDoAfterEvent>(OnDoAfter);
        }

        private void OnComponentInit(EntityUid uid, UnitologPowersComponent component, ComponentInit args)
        {
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
            _actionsSystem.AddAction(uid, ref component.ObeliskActionEntity, component.ObeliskAction, uid);
        }

        /// <summary>
        /// Creates an invisible wall in a free space after some checks.
        /// </summary>
        private void OnInvisibleWall(EntityUid uid, UnitologPowersComponent component, ObeliskActionEvent args)
        {


            if (_container.IsEntityOrParentInContainer(uid))
                return;

            var victims = _lookup.GetEntitiesInRange(uid, 3f);

            foreach(var victinUID in victims)
            {
                if (EntityManager.HasComponent<HumanoidAppearanceComponent>(victinUID))
                {
                if (_mobState.IsDead(victinUID))
                {

                    BeginSpawn(uid,victinUID,component);

                    args.Handled = true;
                        return;
                }

                }
            }


            _popupSystem.PopupEntity(Loc.GetString("Вы должны принести жертву, чтобы призвать обелиск"), uid, uid);
            return;
        }


         private void BeginSpawn(EntityUid uid, EntityUid target, UnitologPowersComponent component)
        {

                    var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.Duration, new ObeliskSpawnDoAfterEvent(), uid, target: target)
                    {
                        DistanceThreshold = 3,
                        BreakOnUserMove = true
                    };



                    if (!_doAfter.TryStartDoAfter(searchDoAfter))
                        return;


        }

        private void OnDoAfter(EntityUid uid, UnitologPowersComponent component, ObeliskSpawnDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

            EntityUid target = args.Args.Target.Value;
            Spawn(component.WallPrototype, Transform(target).Coordinates);
            QueueDel(target);
            _actionsSystem.RemoveAction(uid, component.ObeliskActionEntity);

        }

    }
}
