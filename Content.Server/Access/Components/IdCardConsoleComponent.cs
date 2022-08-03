using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Robust.Server.GameObjects;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIdCardConsoleComponent))]
    public sealed class IdCardConsoleComponent : SharedIdCardConsoleComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);

        private List<string>? AccessChangesToLog;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<AccessReaderComponent>();
            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

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
                    TryWriteToTargetId(msg.FullName, msg.JobTitle, msg.AccessList);
                    UpdateUserInterface();
                    break;
                case LogChangesToIdCardMessage msg:
                    LogChangesSinceWindowOpened(obj.Session.AttachedEntity.Value);
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
            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            return privilegedIdEntity != null && accessSystem.IsAllowed(privilegedIdEntity.Value, reader);
        }

        /// <summary>
        /// Called when the "Submit" button in the UI gets pressed.
        /// Writes data passed from the UI into the ID stored in <see cref="TargetIdSlot"/>, if present.
        /// </summary>
        private void TryWriteToTargetId(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            if (TargetIdSlot.Item is not {Valid: true} targetIdEntity || !PrivilegedIdIsAuthorized())
                return;

            var cardSystem = EntitySystem.Get<IdCardSystem>();
            cardSystem.TryChangeFullName(targetIdEntity, newFullName);
            cardSystem.TryChangeJobTitle(targetIdEntity, newJobTitle);

            if (!newAccessList.TrueForAll(x => AccessLevels.Contains(x)))
            {
                Logger.Warning("Tried to write unknown access tag.");
                return;
            }
            // For admin logging in LogChangesSinceWindowOpened()
            AccessChangesToLog = newAccessList;

            var accessSystem = EntitySystem.Get<AccessSystem>();
            accessSystem.TrySetTags(targetIdEntity, newAccessList);

        }
        /// <summary>
        /// Called when the window is closed to save only the last change made to the ID card, as to avoid log spamming.
        /// </summary>
        private void LogChangesSinceWindowOpened(EntityUid player)
        {
            if (AccessChangesToLog == null)
                return;

            if (TargetIdSlot.Item is not {Valid: true} targetIdEntity)
                return;

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{_entities.ToPrettyString(player):player} has modified ID Card ({_entities.ToPrettyString(targetIdEntity):entity} with these accesses ({string.Join(", ", AccessChangesToLog)})");
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
                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    true,
                    targetIdComponent.FullName,
                    targetIdComponent.JobTitle,
                    targetAccessComponent.Tags.ToArray(),
                    name,
                    _entities.GetComponent<MetaDataComponent>(targetIdEntity).EntityName);
            }
            UserInterface?.SetState(newState);
        }
    }
}
