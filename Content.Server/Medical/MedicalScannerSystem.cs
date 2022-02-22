using Content.Server.Medical.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MedicalScannerComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
            SubscribeLocalEvent<MedicalScannerComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        }

        private void AddInsertOtherVerb(EntityUid uid, MedicalScannerComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (args.Using == null ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.IsOccupied ||
                !component.CanInsert(args.Using.Value))
                return;

            InteractionVerb verb = new();
            verb.Act = () => component.InsertBody(args.Using.Value);
            verb.Category = VerbCategory.Insert;
            verb.Text = EntityManager.GetComponent<MetaDataComponent>(args.Using.Value).EntityName;
            args.Verbs.Add(verb);
        }

        private void AddAlternativeVerbs(EntityUid uid, MedicalScannerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Eject verb
            if (component.IsOccupied)
            {
                AlternativeVerb verb = new();
                verb.Act = () => component.EjectBody();
                verb.Category = VerbCategory.Eject;
                verb.Text = Loc.GetString("medical-scanner-verb-noun-occupant");
                args.Verbs.Add(verb);
            }

            // Self-insert verb
            if (!component.IsOccupied &&
                component.CanInsert(args.User) &&
                _actionBlockerSystem.CanMove(args.User))
            {
                AlternativeVerb verb = new();
                verb.Act = () => component.InsertBody(args.User);
                verb.Text = Loc.GetString("medical-scanner-verb-enter");
                // TODO VERN ICON
                // TODO VERB CATEGORY
                // create a verb category for "enter"?
                // See also, disposal unit.  Also maybe add verbs for entering lockers/body bags?
                args.Verbs.Add(verb);
            }
        }

        private void OnRelayMovement(EntityUid uid, MedicalScannerComponent component, RelayMovementEntityEvent args)
        {
            if (_blocker.CanInteract(args.Entity, null))
            {
                if (_gameTiming.CurTime <
                    component.LastInternalOpenAttempt + MedicalScannerComponent.InternalOpenAttemptDelay)
                {
                    return;
                }

                component.LastInternalOpenAttempt = _gameTiming.CurTime;
                component.EjectBody();
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<MedicalScannerComponent>())
            {
                comp.Update(frameTime);
            }
        }
    }
}
