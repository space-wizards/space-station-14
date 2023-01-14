using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.StationRecords;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIdCardConsoleComponent))]
    public sealed class IdCardConsoleComponent : SharedIdCardConsoleComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);

        private StationRecordsSystem? _recordSystem;
        private StationSystem? _stationSystem;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<AccessReaderComponent>();
            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

            _stationSystem = _entities.EntitySysManager.GetEntitySystem<StationSystem>();
            _recordSystem = _entities.EntitySysManager.GetEntitySystem<StationRecordsSystem>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not {Valid: true} player)
            {
                return;
            }

            switch (obj.Message)
            {
                case WriteToTargetIdMessage msg:
                    TryWriteToTargetId(msg.FullName, msg.JobTitle, msg.AccessList, msg.JobPrototype, player);
                    UpdateUserInterface();
                    break;
            }
        }

        /// <summary>
        /// Returns true if there is an ID in <see cref="PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
        /// </summary>
        private bool PrivilegedIdIsAuthorized()
        {
            if (!_entities.TryGetComponent(Owner, out AccessReaderComponent? reader))
            {
                return true;
            }

            var privilegedIdEntity = PrivilegedIdSlot.Item;
            var accessSystem = _entities.EntitySysManager.GetEntitySystem<AccessReaderSystem>();
            return privilegedIdEntity != null && accessSystem.IsAllowed(privilegedIdEntity.Value, reader);
        }

        /// <summary>
        /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
        /// Writes data passed from the UI into the ID stored in <see cref="TargetIdSlot"/>, if present.
        /// </summary>
        private void TryWriteToTargetId(string newFullName, string newJobTitle, List<string> newAccessList, string newJobProto, EntityUid player)
        {
            if (TargetIdSlot.Item is not {Valid: true} targetIdEntity || !PrivilegedIdIsAuthorized())
                return;

            var cardSystem = _entities.EntitySysManager.GetEntitySystem<IdCardSystem>();
            cardSystem.TryChangeFullName(targetIdEntity, newFullName, player: player);
            cardSystem.TryChangeJobTitle(targetIdEntity, newJobTitle, player: player);

            if (!newAccessList.TrueForAll(x => AccessLevels.Contains(x)))
            {
                Logger.Warning("Tried to write unknown access tag.");
                return;
            }

            var accessSystem = _entities.EntitySysManager.GetEntitySystem<AccessSystem>();
            var oldTags = accessSystem.TryGetTags(targetIdEntity) ?? new List<string>();
            oldTags = oldTags.ToList();

            if (oldTags.SequenceEqual(newAccessList))
                return;

            var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
            var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();
            accessSystem.TrySetTags(targetIdEntity, newAccessList);

            /*TODO: ECS IdCardConsoleComponent and then log on card ejection, together with the save.
            This current implementation is pretty shit as it logs 27 entries (27 lines) if someone decides to give themselves AA*/
            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{_entities.ToPrettyString(player):player} has modified {_entities.ToPrettyString(targetIdEntity):entity} with the following accesses: [{String.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");

            UpdateStationRecord(targetIdEntity, newFullName, newJobTitle, newJobProto);
        }

        private void UpdateStationRecord(EntityUid idCard, string newFullName, string newJobTitle, string newJobProto)
        {
            var station = _stationSystem?.GetOwningStation(Owner);
            if (station == null
                || _recordSystem == null
                || !_entities.TryGetComponent(idCard, out StationRecordKeyStorageComponent? keyStorage)
                || keyStorage.Key == null
                || !_recordSystem.TryGetRecord(station.Value, keyStorage.Key.Value, out GeneralStationRecord? record))
            {
                return;
            }

            record.Name = newFullName;
            record.JobTitle = newJobTitle;

            if (_prototypeManager.TryIndex(newJobProto, out JobPrototype? job))
            {
                record.JobPrototype = newJobProto;
                record.JobIcon = job.Icon;
            }

            _recordSystem.Synchronize(station.Value);
        }

        public void UpdateUserInterface()
        {
            if (!Initialized)
                return;

            IdCardConsoleBoundUserInterfaceState newState;
            // this could be prettier
            if (TargetIdSlot.Item is not {Valid: true} targetIdEntity)
            {
                var privilegedIdName = string.Empty;
                if (PrivilegedIdSlot.Item is {Valid: true} item)
                {
                    privilegedIdName = _entities.GetComponent<MetaDataComponent>(item).EntityName;
                }

                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    false,
                    null,
                    null,
                    null,
                    string.Empty,
                    privilegedIdName,
                    string.Empty);
            }
            else
            {
                var targetIdComponent = _entities.GetComponent<IdCardComponent>(targetIdEntity);
                var targetAccessComponent = _entities.GetComponent<AccessComponent>(targetIdEntity);
                var name = string.Empty;
                if (PrivilegedIdSlot.Item is {Valid: true} item)
                    name = _entities.GetComponent<MetaDataComponent>(item).EntityName;

                var station = _stationSystem?.GetOwningStation(Owner);
                var jobProto = string.Empty;
                if (_recordSystem != null
                    && station != null
                    && _entities.TryGetComponent(targetIdEntity, out StationRecordKeyStorageComponent? keyStorage)
                    && keyStorage.Key != null
                    && _recordSystem.TryGetRecord(station.Value, keyStorage.Key.Value,
                        out GeneralStationRecord? record))
                {
                    jobProto = record.JobPrototype;
                }

                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    true,
                    targetIdComponent.FullName,
                    targetIdComponent.JobTitle,
                    targetAccessComponent.Tags.ToArray(),
                    jobProto,
                    name,
                    _entities.GetComponent<MetaDataComponent>(targetIdEntity).EntityName);
            }
            UserInterface?.SetState(newState);
        }
    }
}
