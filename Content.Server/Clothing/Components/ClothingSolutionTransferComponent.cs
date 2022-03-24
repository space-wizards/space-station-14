using Content.Shared.FixedPoint;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Toggleable;

namespace Content.Server.Clothing.Components
{
    [RegisterComponent]
	public sealed class ClothingSolutionTransferComponent : Component
    {
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
        /// Whether you're allowed to change the transfer amount.
        /// </summary>
        [DataField("canChangeTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanChangeTransferAmount { get; set; } = false;

        // TODO: add sidebar action
    }
}

