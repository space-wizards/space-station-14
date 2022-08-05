using System.Threading.Tasks;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    ///     Gives click behavior for transferring to/from other reagent containers.
    /// </summary>
    [RegisterComponent]
    public sealed class SolutionTransferComponent : Component
    {
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
    }
}
