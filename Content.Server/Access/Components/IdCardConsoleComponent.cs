using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.UserInterface;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIdCardConsoleComponent))]
    public sealed class IdCardConsoleComponent : SharedIdCardConsoleComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);

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
            return privilegedIdEntity != null && accessSystem.IsAllowed(reader, privilegedIdEntity.Value);
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

            if (!newAccessList.TrueForAll(x => _prototypeManager.HasIndex<AccessLevelPrototype>(x)))
            {
                Logger.Warning("Tried to write unknown access tag.");
                return;
            }

            var accessSystem = EntitySystem.Get<AccessSystem>();
            accessSystem.TrySetTags(targetIdEntity, newAccessList);
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
