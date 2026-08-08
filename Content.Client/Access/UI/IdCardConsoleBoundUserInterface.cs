using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Content.Shared.DeadSpace.PersonnelRecords; // DS14
using Content.Shared.DeadSpace.PersonnelRecords.Components; // DS14
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    public sealed class IdCardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfgManager = default!;
        private readonly SharedIdCardConsoleSystem _idCardConsoleSystem = default!;

        private IdCardConsoleWindow? _window;

        // CCVar.
        private int _maxNameLength;
        private int _maxIdJobLength;

        public IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _idCardConsoleSystem = EntMan.System<SharedIdCardConsoleSystem>();

            _maxNameLength =_cfgManager.GetCVar(CCVars.MaxNameLength);
            _maxIdJobLength = _cfgManager.GetCVar(CCVars.MaxIdJobLength);
        }

        protected override void Open()
        {
            base.Open();
            List<ProtoId<AccessLevelPrototype>> accessLevels;
            // DS14-start
            List<ProtoId<AccessLevelPrototype>> basicAccessLevels;
            List<ProtoId<AccessLevelPrototype>> extendedAccessLevels;
            bool isTaipan = false; // DS14
            // DS14-end

            if (EntMan.TryGetComponent<IdCardConsoleComponent>(Owner, out var idCard))
            {
                accessLevels = idCard.AccessLevels;
                // DS14-start
                basicAccessLevels = idCard.BasicAccessLevels;
                extendedAccessLevels = idCard.ExtendedAccessLevels;
                isTaipan = idCard.IsTaipan; // DS14
                // DS14-end
            }
            else
            {
                accessLevels = new List<ProtoId<AccessLevelPrototype>>();
                // DS14-start
                basicAccessLevels = new List<ProtoId<AccessLevelPrototype>>();
                extendedAccessLevels = new List<ProtoId<AccessLevelPrototype>>();
                // DS14-end
                _idCardConsoleSystem.Log.Error($"No IdCardConsole component found for {EntMan.ToPrettyString(Owner)}!");
            }

            var hasDismissal = EntMan.HasComponent<PersonnelDismissalComponent>(Owner); // DS14

            _window = new IdCardConsoleWindow(this, _prototypeManager, accessLevels, basicAccessLevels, extendedAccessLevels, isTaipan, hasDismissal) // DS14
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName
            };

            _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
            _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));
            _window.DismissButton.OnPressed += _ => SendMessage(new PersonnelDismissMessage()); // DS14

            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (IdCardConsoleBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void SubmitData(string newFullName, string newJobTitle, List<ProtoId<AccessLevelPrototype>> newAccessList, ProtoId<JobPrototype> newJobPrototype)
        {
            if (newFullName.Length > _maxNameLength)
                newFullName = newFullName[.._maxNameLength];

            if (newJobTitle.Length > _maxIdJobLength)
                newJobTitle = newJobTitle[.._maxIdJobLength];

            SendMessage(new WriteToTargetIdMessage(
                newFullName,
                newJobTitle,
                newAccessList,
                newJobPrototype));
        }
    }
}
