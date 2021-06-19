using System.Collections.Generic;
using Content.Server.Body.Surgery.Tool;
using Content.Server.DoAfter;
using Content.Server.Notification;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;
using Content.Shared.Body.Surgery.UI;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Surgery
{
    public class SurgerySystem : SharedSurgerySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private DoAfterSystem _doAfter = default!;

        public override void Initialize()
        {
            base.Initialize();

            _doAfter = Get<DoAfterSystem>();

            SubscribeLocalEvent<SurgeryDrapesComponent, ComponentStartup>(OnDrapesStartup);
            SubscribeLocalEvent<SurgeryDrapesComponent, AfterInteractEvent>(OnDrapesAfterInteract);

            SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnToolAfterInteract);
        }

        private void OnDrapesStartup(EntityUid uid, SurgeryDrapesComponent drapes, ComponentStartup args)
        {
            var ui = drapes.UserInterface;

            if (ui != null)
            {
                ui.OnReceiveMessage += msg => OnSurgeryDrapesUIMessage(drapes, msg);
            }
        }

        // TODO SURGERY: Add surgery for dismembered limbs
        private void OnDrapesAfterInteract(EntityUid uid, SurgeryDrapesComponent drapes, AfterInteractEvent args)
        {
            var target = args.Target;
            if (target == null)
            {
                return;
            }

            var user = args.User;
            if (user.TryGetComponent(out SurgeonComponent? surgeon) &&
                surgeon.Target != null &&
                IsPerformingSurgeryOn(surgeon, surgeon.Target))
            {
                if (surgeon.Target.SurgeryTags.Count == 0 &&
                    StopSurgery(surgeon))
                {
                    DoDrapesCancelPopups(drapes, surgeon, target);
                }

                args.Handled = true;
                return;
            }

            if (!user.TryGetComponent(out ActorComponent? actor) ||
                !target.TryGetComponent(out SharedBodyComponent? body))
            {
                return;
            }

            drapes.UserInterface?.Open(actor.PlayerSession);
            UpdateDrapesUI(drapes, body);

            args.Handled = true;
        }

        // TODO SURGERY: Add surgery for dismembered limbs
        // TODO async void alert someone please make do after not async
        private async void OnToolAfterInteract(EntityUid uid, SurgeryToolComponent tool, AfterInteractEvent args)
        {
            if (tool.Behavior == null)
            {
                return;
            }

            if (!args.User.TryGetComponent(out SurgeonComponent? surgeon))
            {
                return;
            }

            if (surgeon.Target == null)
            {
                return;
            }

            var target = args.Target;
            if (target == null)
            {
                return;
            }

            // If we are not performing surgery on a grape
            if (surgeon.Target.Owner != target)
            {
                // It might be on a body instead
                if (target.TryGetComponent(out SharedBodyComponent? body) &&
                    body.HasPart(surgeon.Target.Owner))
                {
                    target = surgeon.Target.Owner;
                }
                else
                {
                    return;
                }
            }

            if (!target.TryGetComponent(out SurgeryTargetComponent? surgeryTarget))
            {
                return;
            }

            if (!tool.Behavior.CanPerform(surgeon, surgeryTarget))
            {
                tool.Behavior.OnPerformFail(surgeon, surgeryTarget);
                return;
            }

            if (tool.Delay <= 0)
            {
                ToolPerform(tool, surgeon, surgeryTarget);
                return;
            }

            tool.Behavior.OnPerformDelayBegin(surgeon, surgeryTarget);

            var cancelToken = surgeon.SurgeryCancellation?.Token ?? default;
            var result = await _doAfter.DoAfter(new DoAfterEventArgs(surgeon.Owner, tool.Delay, cancelToken, target)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });

            if (result == DoAfterStatus.Finished)
            {
                ToolPerform(tool, surgeon, surgeryTarget);
            }

            args.Handled = true;
        }

        private void OnSurgeryDrapesUIMessage(SurgeryDrapesComponent drapes, ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case SurgeryOpPartSelectUIMsg msg:
                    if (!drapes.Owner.EntityManager.TryGetEntity(msg.Part, out var targetEntity))
                    {
                        Sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with an invalid target entity id: {msg.Part}");
                        return;
                    }

                    if (!targetEntity.TryGetComponent(out SurgeryTargetComponent? target))
                    {
                        Sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with an entity that has no {nameof(SurgeryTargetComponent)}: {targetEntity}");
                        return;
                    }

                    var surgeon = message.Session.AttachedEntity?.EnsureComponent<SurgeonComponent>();

                    if (surgeon == null)
                    {
                        Sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with no attached entity of their own.");
                        return;
                    }

                    if (!_prototypeManager.TryIndex<SurgeryOperationPrototype>(msg.OperationId, out var operation))
                    {
                        Sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with an invalid {nameof(SurgeryOperationPrototype)} id: {msg.OperationId}");
                        return;
                    }

                    // TODO SURGERY: Make each surgeon "know" a set of surgeries that they may perform
                    if (operation.Hidden)
                    {
                        Sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} that tried to start a hidden {nameof(SurgeryOperationPrototype)} with id: {msg.OperationId}");
                        return;
                    }

                    if (IsPerformingSurgeryOn(surgeon, target))
                    {
                        Sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} to a start a {msg.OperationId} operation while already performing a {target.Operation?.ID} on {target.Owner}");
                        return;
                    }

                    TryUseDrapes(drapes, surgeon, target, operation);
                    break;
            }
        }

        public bool TryUseDrapes(
            SurgeryDrapesComponent drapes,
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation)
        {
            if (TryStartSurgery(surgeon, target, operation))
            {
                DoDrapesStartPopups(drapes, surgeon, target.Owner, operation);
                return true;
            }

            return false;
        }

        private void DoDrapesStartPopups(
            SurgeryDrapesComponent drapes,
            SurgeonComponent surgeon,
            IEntity target,
            SurgeryOperationPrototype operation)
        {
            if (IsPerformingSurgeryOnSelf(surgeon))
            {
                if (target.TryGetComponent(out SharedBodyPartComponent? part) &&
                    part.Body != null)
                {
                    var id = "surgery-prepare-start-self-surgeon-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("zone", target),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-self-outsider-popup";
                    part.Body.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("part", target),
                        ("procedure", operation.Name)),
                        except: part.Body.Owner);
                }
                else
                {
                    var id = "surgery-prepare-start-self-no-zone-surgeon-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-self-no-zone-outsider-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner)));
                }
            }
            else
            {
                if (IsReceivingSurgeryOnPart(target, out var part, out var body))
                {
                    var id = "surgery-prepare-start-surgeon-popup";
                    body.Owner.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("target", body.Owner),
                        ("zone", part.Owner),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-target-popup";
                    surgeon.Owner.PopupMessage(body.Owner, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("zone", part.Owner)));

                    id = "surgery-prepare-start-outsider-popup";
                    surgeon.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("target", body.Owner),
                        ("zone", target)),
                        except: body.Owner);
                }
                else
                {
                    var id = "surgery-prepare-start-no-zone-surgeon-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("target", target),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-no-zone-target-popup";
                    surgeon.Owner.PopupMessage(target, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner)));

                    id = "surgery-prepare-start-no-zone-outsider-popup";
                    surgeon.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("target", target)),
                        except: target);
                }
            }
        }

        private void DoDrapesCancelPopups(SurgeryDrapesComponent drapes, SurgeonComponent surgeon, IEntity target)
        {
            if (IsPerformingSurgeryOnSelf(surgeon))
            {
                if (target.TryGetComponent(out SharedBodyPartComponent? part) &&
                    part.Body != null)
                {
                    var id = "surgery-prepare-cancel-self-surgeon-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("zone", target)));

                    id = "surgery-prepare-cancel-self-outsider-popup";
                    part.Body.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("part", target)),
                        except: part.Body.Owner);
                }
                else
                {
                    var id = "surgery-prepare-cancel-self-no-zone-surgeon-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner)));

                    id = "surgery-prepare-cancel-self-no-zone-outsider-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner)));
                }
            }
            else
            {
                if (IsReceivingSurgeryOnPart(target, out var part, out var body))
                {
                    var id = "surgery-prepare-cancel-surgeon-popup";
                    body.Owner.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("target", body.Owner),
                        ("zone", part.Owner)));

                    id = "surgery-prepare-cancel-target-popup";
                    surgeon.Owner.PopupMessage(body.Owner, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("zone", part.Owner)));

                    id = "surgery-prepare-cancel-outsider-popup";
                    surgeon.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("target", body.Owner),
                        ("zone", target)),
                        except: body.Owner);
                }
                else
                {
                    var id = "surgery-prepare-cancel-no-zone-surgeon-popup";
                    target.PopupMessage(surgeon.Owner, Loc.GetString(id,
                        ("item", drapes.Owner),
                        ("target", target)));

                    id = "surgery-prepare-cancel-no-zone-target-popup";
                    surgeon.Owner.PopupMessage(target, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner)));

                    id = "surgery-prepare-cancel-no-zone-outsider-popup";
                    surgeon.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", drapes.Owner),
                        ("target", target)),
                        except: target);
                }
            }
        }

        private void UpdateDrapesUI(SurgeryDrapesComponent drapes, SharedBodyComponent body)
        {
            var ui = drapes.UserInterface;
            if (ui == null)
            {
                return;
            }

            var parts = new List<EntityUid>();

            foreach (var (part, _) in body.Parts)
            {
                if (part.Owner.TryGetComponent(out SurgeryTargetComponent? surgery))
                {
                    parts.Add(surgery.Owner.Uid);
                }
            }

            var state = new SurgeryUIState(parts.ToArray());
            ui.SetState(state);
        }

        public override void DoBeginPopups(SurgeonComponent surgeon, IEntity target, string id)
        {
            base.DoBeginPopups(surgeon, target, id);

            var targetReceiver = GetPopupReceiver(target);
            DoOutsiderBeginPopup(surgeon.Owner, targetReceiver, target, id);
        }

        public void DoOutsiderBeginPopup(IEntity surgeon, IEntity? target, IEntity part, string id)
        {
            string msg;

            if (target == null)
            {
                var locId = $"surgery-step-{id}-begin-no-zone-outsider-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("part", part));
            }
            else if (surgeon == target)
            {
                var locId = $"surgery-step-{id}-begin-self-outsider-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-begin-outsider-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }

            surgeon.PopupMessageOtherClients(msg, _playerManager, except: target ?? surgeon);
        }

        public override void DoSuccessPopups(SurgeonComponent surgeon, IEntity target, string id)
        {
            base.DoSuccessPopups(surgeon, target, id);

            var bodyOwner = target.GetComponentOrNull<SharedBodyPartComponent>()?.Body?.Owner;
            DoOutsiderSuccessPopup(surgeon.Owner, bodyOwner, target, id);
        }

        public void DoOutsiderSuccessPopup(IEntity surgeon, IEntity? target, IEntity part, string id)
        {
            string msg;

            if (target == null)
            {
                var locId = $"surgery-step-{id}-success-no-zone-outsider-popup";
                msg =  _loc.GetString(locId, ("user", surgeon), ("part", part));
            }
            else if (surgeon == target)
            {
                var locId = $"surgery-step-{id}-success-self-outsider-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-success-outsider-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }

            surgeon.PopupMessageOtherClients(msg, _playerManager, except: target ?? surgeon);
        }

        private void ToolPerform(SurgeryToolComponent tool, SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (tool.Behavior == null)
            {
                return;
            }

            if (tool.Behavior.Perform(surgeon, target))
            {
                tool.Behavior.OnPerformSuccess(surgeon, target);
            }
            else
            {
                tool.Behavior.OnPerformFail(surgeon, target);
            }
        }
    }
}
