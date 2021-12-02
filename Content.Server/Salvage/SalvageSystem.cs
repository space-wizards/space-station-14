using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Server.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Salvage
{
    public class SalvageSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        [ViewVariables]
        public EntityUid PulledObject = EntityUid.Invalid;

        [ViewVariables]
        public SalvageSystemState State = SalvageSystemState.Inactive;

        [ViewVariables]
        public float StateTimer = 0.0f;

        public const float PullInTimer = 2.0f;
        public const float HoldTimer = 240.0f;
        public const float LeaveTimer = 60.0f;
        public const float AngularVelocityRangeRadians = 0.25f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<SalvageMagnetComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<SalvageMagnetComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, SalvageMagnetComponent comp, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            switch (State)
            {
                case SalvageSystemState.Inactive:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-inactive"));
                    break;
                case SalvageSystemState.PullingIn:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-pulling-in"));
                    break;
                case SalvageSystemState.Active:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-active"));
                    break;
                case SalvageSystemState.LettingGo:
                    args.PushMarkup(Loc.GetString("salvage-system-magnet-examined-letting-go"));
                    break;
            }
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            PulledObject = EntityUid.Invalid;
            State = SalvageSystemState.Inactive;
            StateTimer = 0.0f;
        }

        private void OnInteractHand(EntityUid uid, SalvageMagnetComponent sm, InteractHandEvent args)
        {
            if (args.Handled)
                return;
            args.Handled = true;
            _popupSystem.PopupEntity(CallSalvage(), uid, Filter.Entities(args.User.Uid));
        }

        private MapCoordinates GetSalvagePlacementLocation()
        {
            foreach (var (smc, tsc) in EntityManager.EntityQuery<SalvageMagnetComponent, TransformComponent>(true))
            {
                return (new EntityCoordinates(smc.OwnerUid, smc.Offset).ToMap(EntityManager));
            }
            return MapCoordinates.Nullspace;
        }

        private IEnumerable<SalvageMapPrototype> GetAllSalvageMaps()
        {
            return _prototypeManager.EnumeratePrototypes<SalvageMapPrototype>();
        }

        // String is announced
        private string SpawnSalvage()
        {
            // In case of failure
            State = SalvageSystemState.Inactive;
            StateTimer = 0.0f;

            var spl = GetSalvagePlacementLocation();
            if (spl == MapCoordinates.Nullspace)
            {
                return Loc.GetString("salvage-system-announcement-spawn-magnet-lost");
            }

            SalvageMapPrototype? map = null;

            var allSalvageMaps = GetAllSalvageMaps().ToList();

            for (var i = 0; i < allSalvageMaps.Count; i++)
            {
                map = _random.PickAndTake(allSalvageMaps);

                if (_physicsSystem.TryCollideRect(Box2.CenteredAround(spl.Position, new Vector2(map.Size * 2.0f, map.Size * 2.0f)), spl.MapId, false))
                {
                    // collided: set map to null so we don't spawn it
                    map = null;
                }
                else
                {
                    break;
                }
            }
            if (map == null)
            {
                return Loc.GetString("salvage-system-announcement-spawn-no-debris-available");
            }

            var bp = _mapLoader.LoadBlueprint(spl.MapId, map.MapPath);
            if (bp == null)
            {
                return Loc.GetString("salvage-system-announcement-spawn-debris-disintegrated");
            }

            PulledObject = bp.GridEntityId;
            EntityManager.AddComponent<SalvageComponent>(PulledObject);
            EntityManager.GetComponent<TransformComponent>(PulledObject).Coordinates = EntityCoordinates.FromMap(_mapManager, spl);
            if (EntityManager.TryGetComponent<PhysicsComponent>(PulledObject, out var phys))
            {
                phys.AngularDamping = 0.0f;
                phys.AngularVelocity = (_random.NextFloat() - 0.5f) * 2.0f * AngularVelocityRangeRadians;
            }
            // Alright, salvage magnet is active.
            State = SalvageSystemState.Active;
            return Loc.GetString("salvage-system-announcement-arrived");
        }

        public override void Update(float frameTime)
        {
            StateTimer += frameTime;
            switch (State)
            {
                case SalvageSystemState.Inactive:
                    break;
                case SalvageSystemState.PullingIn:
                    if (StateTimer >= PullInTimer)
                    {
                        string report = SpawnSalvage();
                        _chatManager.DispatchStationAnnouncement(report, Loc.GetString("salvage-system-announcement-source"));
                    }
                    break;
                case SalvageSystemState.Active:
                    // magnet power usage = base + (time² * factor)
                    // write base and factor into prototype!!!
                    // also determine if magnet is unpowered and if so auto-lose???
                    // CURRENTLY: 
                    if (StateTimer >= HoldTimer)
                    {
                        ReturnSalvage();
                    }
                    break;
                case SalvageSystemState.LettingGo:
                    if (!EntityManager.EntityExists(PulledObject))
                    {
                        State = SalvageSystemState.Inactive;
                        StateTimer = 0.0f;
                        PulledObject = EntityUid.Invalid;
                        _chatManager.DispatchStationAnnouncement(Loc.GetString("salvage-system-announcement-lost"), Loc.GetString("salvage-system-announcement-source"));
                    }
                    break;
            }
            foreach (var smc in EntityManager.EntityQuery<SalvageComponent>(true))
            {
                if (smc.Killswitch)
                {
                    smc.KillswitchTime += frameTime;
                    if (smc.KillswitchTime >= LeaveTimer)
                    {
                        EntityManager.QueueDeleteEntity(smc.OwnerUid);
                    }
                }
            }
        }

        public string CallSalvage()
        {
            // State error reports
            if (State == SalvageSystemState.LettingGo)
                return Loc.GetString("salvage-system-report-cooling-down");
            if (State != SalvageSystemState.Inactive)
                return Loc.GetString("salvage-system-report-already-active");
            // Confirm
            State = SalvageSystemState.PullingIn;
            StateTimer = 0.0f;
            return Loc.GetString("salvage-system-report-activate-success");
        }

        public string ReturnSalvage()
        {
            if (State != SalvageSystemState.Active)
                return Loc.GetString("salvage-system-report-not-active");
            // Confirm
            State = SalvageSystemState.LettingGo;
            StateTimer = 0.0f;
            // Enable killswitch, announce, report success
            if (EntityManager.TryGetComponent<SalvageComponent>(PulledObject, out var salvage))
            {
                // Schedule this to auto-delete (and ideally fly away from the station???)
                salvage.Killswitch = true;
            }
            else
            {
                // Oh no you DON'T, you aren't getting away that easily
                EntityManager.QueueDeleteEntity(PulledObject);
            }
            _chatManager.DispatchStationAnnouncement(Loc.GetString("salvage-system-announcement-losing"), Loc.GetString("salvage-system-announcement-source"));
            return Loc.GetString("salvage-system-report-deactivate-success");
        }
    }

    public enum SalvageSystemState
    {
        Inactive,
        PullingIn,
        Active,
        LettingGo
    }
}

