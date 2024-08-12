using Content.Server.Access.Components;
using Content.Server.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.StatusIcon;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Access.Systems
{
    public sealed class AgentIDCardSystem : SharedAgentIdCardSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IdCardSystem _cardSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AgentIDCardComponent, AfterInteractEvent>(OnAfterInteract);
            // BUI
            SubscribeLocalEvent<AgentIDCardComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardNameChangedMessage>(OnNameChanged);
            SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardJobChangedMessage>(OnJobChanged);
            SubscribeLocalEvent<AgentIDCardComponent, AgentIDCardJobIconChangedMessage>(OnJobIconChanged);
        }

        private void OnAfterInteract(EntityUid uid, AgentIDCardComponent component, AfterInteractEvent args)
        {
            if (!TryComp<AccessComponent>(args.Target, out var targetAccess) || !HasComp<IdCardComponent>(args.Target) || args.Target == null)
                return;

            if (!TryComp<AccessComponent>(uid, out var access) || !HasComp<IdCardComponent>(uid))
                return;

            var beforeLength = access.Tags.Count;
            access.Tags.UnionWith(targetAccess.Tags);
            var addedLength = access.Tags.Count - beforeLength;

            if (addedLength == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("agent-id-no-new", ("card", args.Target)), args.Target.Value, args.User);
                return;
            }

            Dirty(uid, access);

            if (addedLength == 1)
            {
                _popupSystem.PopupEntity(Loc.GetString("agent-id-new-1", ("card", args.Target)), args.Target.Value, args.User);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("agent-id-new", ("number", addedLength), ("card", args.Target)), args.Target.Value, args.User);
        }

        private void AfterUIOpen(EntityUid uid, AgentIDCardComponent component, AfterActivatableUIOpenEvent args)
        {
            if (!_uiSystem.HasUi(uid, AgentIDCardUiKey.Key))
                return;

            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            var state = new AgentIDCardBoundUserInterfaceState(idCard.FullName ?? "", idCard.JobTitle ?? "", idCard.JobIcon);
            _uiSystem.SetUiState(uid, AgentIDCardUiKey.Key, state);
        }

        private void OnJobChanged(EntityUid uid, AgentIDCardComponent comp, AgentIDCardJobChangedMessage args)
        {
            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            _cardSystem.TryChangeJobTitle(uid, args.Job, idCard);
        }

        private void OnNameChanged(EntityUid uid, AgentIDCardComponent comp, AgentIDCardNameChangedMessage args)
        {
            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            _cardSystem.TryChangeFullName(uid, args.Name, idCard);
        }

        private void OnJobIconChanged(EntityUid uid, AgentIDCardComponent comp, AgentIDCardJobIconChangedMessage args)
        {
            if (!TryComp<IdCardComponent>(uid, out var idCard))
                return;

            if (!_prototypeManager.TryIndex(args.JobIconId, out var jobIcon))
                return;

            _cardSystem.TryChangeJobIcon(uid, jobIcon, idCard);

            if (TryFindJobProtoFromIcon(jobIcon, out var job))
                _cardSystem.TryChangeJobDepartment(uid, job, idCard);
        }

        private bool TryFindJobProtoFromIcon(JobIconPrototype jobIcon, [NotNullWhen(true)] out JobPrototype? job)
        {
            foreach (var jobPrototype in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (jobPrototype.Icon == jobIcon.ID)
                {
                    job = jobPrototype;
                    return true;
                }
            }

            job = null;
            return false;
        }
    }
}
