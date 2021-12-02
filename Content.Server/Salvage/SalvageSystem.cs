using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
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
using Robust.Shared.Timing;

namespace Content.Server.Salvage
{
    public class SalvageSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

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
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            PulledObject = EntityUid.Invalid;
            State = SalvageSystemState.Inactive;
            StateTimer = 0.0f;
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

        private bool SpawnSalvage()
        {
            var allSalvageMaps = GetAllSalvageMaps().ToList();
            if (allSalvageMaps.Count == 0)
            {
                Logger.ErrorS("c.s.salvage", "Unable to spawn salvage: no maps!");
                return false;
            }

            var map = _random.Pick(allSalvageMaps);

            var spl = GetSalvagePlacementLocation();
            if (spl == MapCoordinates.Nullspace)
            {
                Logger.ErrorS("c.s.salvage", "Unable to spawn salvage: map coordinates bad!");
                return false;
            }

            var bp = _mapLoader.LoadBlueprint(spl.MapId, map.MapPath);
            if (bp == null)
            {
                Logger.ErrorS("c.s.salvage", "Unable to spawn salvage: blueprint yielded no grid!");
                return false;
            }

            PulledObject = bp.GridEntityId;
            EntityManager.AddComponent<SalvageComponent>(PulledObject);
            EntityManager.GetComponent<TransformComponent>(PulledObject).Coordinates = EntityCoordinates.FromMap(_mapManager, spl);
            if (EntityManager.TryGetComponent<PhysicsComponent>(PulledObject, out var phys))
            {
                phys.AngularDamping = 0.0f;
                phys.AngularVelocity = (_random.NextFloat() - 0.5f) * 2.0f * AngularVelocityRangeRadians;
            }
            return true;
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
                        string report = Loc.GetString("salvage-system-announcement-arrived");
                        if (SpawnSalvage())
                        {
                            // Done!
                            State = SalvageSystemState.Active;
                            StateTimer = 0.0f;
                        }
                        else
                        {
                            report = Loc.GetString("salvage-system-announcement-wtf");
                            // Uhoh
                            State = SalvageSystemState.Inactive;
                            StateTimer = 0.0f;
                        }
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

