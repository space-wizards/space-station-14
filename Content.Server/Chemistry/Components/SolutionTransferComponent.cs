using System;
using System.Threading.Tasks;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    ///     Gives click behavior for transferring to/from other reagent containers.
    /// </summary>
    [RegisterComponent]
    public sealed class SolutionTransferComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        // Behavior is as such:
        // If it's a reagent tank, TAKE reagent.
        // If it's anything else, GIVE reagent.
        // Of course, only if possible.

        public override string Name => "SolutionTransfer";

        /// <summary>
        ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
        /// </summary>
        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(5);

        /// <summary>
        ///     The minimum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("minTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MinimumTransferAmount { get; set; } = FixedPoint2.New(5);

        /// <summary>
        ///     The maximum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("maxTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MaximumTransferAmount { get; set; } = FixedPoint2.New(50);

        /// <summary>
        ///     Can this entity take reagent from reagent tanks?
        /// </summary>
        [DataField("canReceive")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReceive { get; set; } = true;

        /// <summary>
        ///     Can this entity give reagent to other reagent containers?
        /// </summary>
        [DataField("canSend")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanSend { get; set; } = true;

        /// <summary>
        /// Whether you're allowed to change the transfer amount.
        /// </summary>
        [DataField("canChangeTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanChangeTransferAmount { get; set; } = false;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(TransferAmountUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        public void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (serverMsg.Session.AttachedEntity == null)
                return;

            switch (serverMsg.Message)
            {
                case TransferAmountSetValueMessage svm:
                    var sval = svm.Value.Float();
                    var amount = Math.Clamp(sval, MinimumTransferAmount.Float(),
                        MaximumTransferAmount.Float());

                    serverMsg.Session.AttachedEntity.Value.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount",
                        ("amount", amount)));
                    SetTransferAmount(FixedPoint2.New(amount));
                    break;
            }
        }

        public void SetTransferAmount(FixedPoint2 amount)
        {
            amount = FixedPoint2.New(Math.Clamp(amount.Int(), MinimumTransferAmount.Int(),
                MaximumTransferAmount.Int()));
            TransferAmount = amount;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();

            if (!eventArgs.InRangeUnobstructed() || eventArgs.Target == null)
                return false;

            if (!_entities.HasComponent<SolutionContainerManagerComponent>(Owner))
                return false;

            var target = eventArgs.Target!.Value;
            if (!_entities.HasComponent<SolutionContainerManagerComponent>(target))
            {
                return false;
            }


            if (CanReceive && _entities.TryGetComponent(target, out ReagentTankComponent? tank)
                           && solutionsSys.TryGetRefillableSolution(Owner, out var ownerRefill)
                           && solutionsSys.TryGetDrainableSolution(target, out var targetDrain))
            {
                var tankTransferAmount = tank.TransferAmount;

                if (_entities.TryGetComponent(Owner, out RefillableSolutionComponent? refill) && refill.MaxRefill != null)
                {
                    tankTransferAmount = FixedPoint2.Min(tankTransferAmount, (FixedPoint2) refill.MaxRefill);
                }

                var transferred = DoTransfer(eventArgs.User, target, targetDrain, Owner, ownerRefill, tankTransferAmount);
                if (transferred > 0)
                {
                    var toTheBrim = ownerRefill.AvailableVolume == 0;
                    var msg = toTheBrim
                        ? "comp-solution-transfer-fill-fully"
                        : "comp-solution-transfer-fill-normal";

                    target.PopupMessage(eventArgs.User,
                        Loc.GetString(msg, ("owner", eventArgs.Target), ("amount", transferred), ("target", Owner)));
                    return true;
                }
            }

            if (CanSend && solutionsSys.TryGetRefillableSolution(target, out var targetRefill)
                        && solutionsSys.TryGetDrainableSolution(Owner, out var ownerDrain))
            {
                var transferAmount = TransferAmount;

                if (_entities.TryGetComponent(target, out RefillableSolutionComponent? refill) && refill.MaxRefill != null)
                {
                    transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill);
                }

                var transferred = DoTransfer(eventArgs.User, Owner, ownerDrain, target, targetRefill, transferAmount);

                if (transferred > 0)
                {
                    Owner.PopupMessage(eventArgs.User,
                        Loc.GetString("comp-solution-transfer-transfer-solution",
                            ("amount", transferred),
                            ("target", target)));

                    return true;
                }
            }

            return true;
        }

        /// <returns>The actual amount transferred.</returns>
        private static FixedPoint2 DoTransfer(EntityUid user,
            EntityUid sourceEntity,
            Solution source,
            EntityUid targetEntity,
            Solution target,
            FixedPoint2 amount)
        {

            if (source.DrainAvailable == 0)
            {
                sourceEntity.PopupMessage(user,
                    Loc.GetString("comp-solution-transfer-is-empty", ("target", sourceEntity)));
                return FixedPoint2.Zero;
            }

            if (target.AvailableVolume == 0)
            {
                targetEntity.PopupMessage(user,
                    Loc.GetString("comp-solution-transfer-is-full", ("target", targetEntity)));
                return FixedPoint2.Zero;
            }

            var actualAmount = FixedPoint2.Min(amount, FixedPoint2.Min(source.DrainAvailable, target.AvailableVolume));

            var solution = EntitySystem.Get<SolutionContainerSystem>().Drain(sourceEntity, source, actualAmount);
            EntitySystem.Get<SolutionContainerSystem>().Refill(targetEntity, target, solution);

            return actualAmount;
        }
    }
}
