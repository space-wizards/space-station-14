using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Operation;
using Content.Shared.Body.Surgery.Operation.Messages;
using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Surgeon.Messages;
using Content.Shared.Body.Surgery.Target;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Surgery
{
    [UsedImplicitly]
    public class SharedSurgerySystem : EntitySystem
    {
        public const string SurgeryLogId = "surgery";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;

        protected ISawmill Sawmill { get; private set; } = default!;

        public override void Initialize()
        {
            base.Initialize();

            Sawmill = Logger.GetSawmill(SurgeryLogId);

            ValidateOperations();

            SubscribeLocalEvent<SurgeryTargetComponent, ComponentRemove>(OnTargetComponentRemoved);

            SubscribeLocalEvent<SurgeonComponent, SurgeonStartedOperation>(OnSurgeonStartedOperation);
            SubscribeLocalEvent<SurgeonComponent, SurgeonStoppedOperation>(OnSurgeonStoppedOperation);

            SubscribeLocalEvent<SurgeryTargetComponent, OperationEnded>(HandleOperationEnded);
        }

        private void OnTargetComponentRemoved(EntityUid uid, SurgeryTargetComponent target, ComponentRemove args)
        {
            if (target.Surgeon == null || target.Operation == null)
            {
                return;
            }

            StopSurgery(target.Surgeon, target);
        }

        public IEntity? GetPopupReceiver(IEntity target)
        {
            if (target.TryGetComponent(out SharedBodyPartComponent? part) &&
                part.Body?.Owner != null)
            {
                return part.Body.Owner;
            }

            return null;
        }

        private void OnSurgeonStartedOperation(EntityUid uid, SurgeonComponent surgeon, SurgeonStartedOperation args)
        {
            args.Target.Surgeon = EntityManager.GetEntity(uid).GetComponent<SurgeonComponent>();
            args.Target.Operation = args.Operation;
        }

        private void OnSurgeonStoppedOperation(EntityUid uid, SurgeonComponent surgeon, SurgeonStoppedOperation args)
        {
            surgeon.SurgeryCancellation?.Cancel();
            surgeon.SurgeryCancellation = null;
            surgeon.Target = null;

            args.OldTarget.Surgeon = null;
            args.OldTarget.Operation = null;
            args.OldTarget.SurgeryTags.Clear();
        }

        private void HandleOperationEnded(EntityUid uid, SurgeryTargetComponent target, OperationEnded args)
        {
            target.Surgeon = null;
            target.Operation = null;
        }

        private void ValidateOperations()
        {
            foreach (var operation in _prototypeManager.EnumeratePrototypes<SurgeryOperationPrototype>())
            {
                foreach (var step in operation.Steps)
                {
                    if (!_prototypeManager.HasIndex<SurgeryStepPrototype>(step.Id))
                    {
                        throw new PrototypeLoadException(
                            $"Invalid {nameof(SurgeryStepPrototype)} found in surgery operation with id {operation.ID}: No step found with id {step}");
                    }
                }
            }
        }

        private CancellationTokenSource StartSurgery(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation)
        {
            StopSurgery(surgeon);

            surgeon.Target = target;

            var cancellation = new CancellationTokenSource();
            surgeon.SurgeryCancellation = cancellation;

            var message = new SurgeonStartedOperation(target, operation);
            RaiseLocalEvent(surgeon.Owner.Uid, message);

            return cancellation;
        }

        private bool TryStartSurgery(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation,
            [NotNullWhen(true)] out CancellationTokenSource? token)
        {
            if (surgeon.Target != null)
            {
                token = null;
                return false;
            }

            token = StartSurgery(surgeon, target, operation);
            return true;
        }

        public bool TryStartSurgery(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation)
        {
            return TryStartSurgery(surgeon, target, operation, out _);
        }

        public bool IsPerformingSurgery(SurgeonComponent surgeon)
        {
            return surgeon.Target != null;
        }

        public bool IsPerformingSurgeryOn(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (surgeon.Target == target)
            {
                return true;
            }

            if (target.Owner.TryGetComponent(out SharedBodyPartComponent? part) &&
                part.Body?.Owner == target.Owner)
            {
                return true;
            }

            return false;
        }

        protected bool IsPerformingSurgeryOnSelf(SurgeonComponent surgeon)
        {
            return surgeon.Target != null && IsPerformingSurgeryOn(surgeon, surgeon.Target);
        }

        protected bool IsReceivingSurgeryOnPart(IEntity target)
        {
            return IsReceivingSurgeryOnPart(target, out _, out _);
        }

        protected bool IsReceivingSurgeryOnPart(
            IEntity target,
            [NotNullWhen(true)] out SharedBodyPartComponent? part,
            [NotNullWhen(true)] out SharedBodyComponent? body)
        {
            body = null;
            return target.TryGetComponent(out part) && (body = part.Body) != null;
        }

        /// <summary>
        ///     Tries to stop the surgery that the surgeon is performing.
        /// </summary>
        /// <returns>True if stopped, false otherwise even if no surgery was underway.</returns>
        public bool StopSurgery(SurgeonComponent surgeon)
        {
            if (surgeon.Target == null)
            {
                return false;
            }

            var oldTarget = surgeon.Target;
            surgeon.Target = null;

            if (!surgeon.Owner.Deleted)
            {
                var message = new SurgeonStoppedOperation(oldTarget);
                RaiseLocalEvent(surgeon.Owner.Uid, message);
            }

            return true;
        }

        public bool StopSurgery(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (surgeon.Target != target)
            {
                return false;
            }

            return StopSurgery(surgeon);
        }

        public bool CanAddSurgeryTag(SurgeryTargetComponent target, SurgeryTag tag)
        {
            if (target.Operation == null ||
                target.Operation.Steps.Count <= target.SurgeryTags.Count)
            {
                return false;
            }

            var nextStep = target.Operation.Steps[target.SurgeryTags.Count];
            if (!nextStep.Necessary(target) || nextStep.Id != tag.Id)
            {
                return false;
            }

            return true;
        }

        public bool TryAddSurgeryTag(SurgeryTargetComponent target, SurgeryTag tag)
        {
            target.SurgeryTags.Add(tag);
            CheckCompletion(target);
            return true;
        }

        public bool TryRemoveSurgeryTag(SurgeryTargetComponent target, SurgeryTag tag)
        {
            if (target.SurgeryTags.Count == 0 ||
                target.SurgeryTags[^1] != tag)
            {
                return false;
            }

            target.SurgeryTags.RemoveAt(target.SurgeryTags.Count - 1);
            return true;
        }

        private void CheckCompletion(SurgeryTargetComponent target)
        {
            if (target.Surgeon == null ||
                target.Operation == null ||
                target.Operation.Steps.Count > target.SurgeryTags.Count)
            {
                return;
            }

            var offset = 0;

            for (var i = 0; i < target.SurgeryTags.Count; i++)
            {
                var step = target.Operation.Steps[i + offset];

                if (!step.Necessary(target))
                {
                    offset++;
                    step = target.Operation.Steps[i + offset];
                }

                var tag = target.SurgeryTags[i];

                if (tag != step.Id)
                {
                    return;
                }
            }

            target.Operation.Effect?.Execute(target.Surgeon, target);
        }

        public virtual void DoBeginPopups(SurgeonComponent surgeon, IEntity target, string id)
        {
            var targetReceiver = GetPopupReceiver(target);
            DoSurgeonBeginPopup(surgeon.Owner, targetReceiver, target, id);

            if (!IsPerformingSurgeryOnSelf(surgeon))
            {
                DoTargetBeginPopup(surgeon.Owner, targetReceiver, target, id);
            }
        }

        public void DoSurgeonBeginPopup(IEntity surgeon, IEntity? target, IEntity part, string id)
        {
            string msg;

            if (target == null)
            {
                var locId = $"surgery-step-{id}-begin-no-zone-surgeon-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("part", part));
            }
            else if (surgeon == target)
            {
                var locId = $"surgery-step-{id}-begin-self-surgeon-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-begin-surgeon-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }

            surgeon.PopupMessage(msg);
        }

        public void DoTargetBeginPopup(IEntity surgeon, IEntity? target, IEntity part, string id)
        {
            var locId = $"surgery-step-{id}-begin-target-popup";
            var msg = _loc.GetString(locId, ("user", surgeon), ("part", part));

            (target ?? part).PopupMessage(msg);
        }

        public virtual void DoSuccessPopups(SurgeonComponent surgeon, IEntity target, string id)
        {
            var surgeonOwner = surgeon.Owner;
            var bodyOwner = target.GetComponentOrNull<SharedBodyPartComponent>()?.Body?.Owner;

            DoSurgeonSuccessPopup(surgeonOwner, bodyOwner, target, id);

            if (bodyOwner != surgeonOwner)
            {
                DoTargetSuccessPopup(surgeonOwner, bodyOwner, target, id);
            }
        }

        public void DoSurgeonSuccessPopup(IEntity surgeon, IEntity? target, IEntity part, string id)
        {
            string msg;

            if (target == null)
            {
                var locId = $"surgery-step-{id}-success-no-zone-surgeon-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("part", part));
            }
            else if (surgeon == target)
            {
                var locId = $"surgery-step-{id}-success-self-surgeon-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-success-surgeon-popup";
                msg = _loc.GetString(locId, ("user", surgeon), ("target", target), ("part", part));
            }

            surgeon.PopupMessage(msg);
        }

        public void DoTargetSuccessPopup(IEntity surgeon, IEntity? target, IEntity part, string id)
        {
            var locId = $"surgery-step-{id}-success-target-popup";
            var msg = _loc.GetString(locId, ("user", surgeon), ("part", part));

            (target ?? part).PopupMessage(msg);
        }
    }
}
