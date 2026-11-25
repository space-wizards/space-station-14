using Content.Shared.FixedPoint;
using Content.Shared.Inventory;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Base class for components that inject a solution into a target's bloodstream in response to an event.
/// </summary>
public abstract partial class BaseSolutionInjectOnEventComponent : Component
{
    /// <summary>
    /// How much solution to remove from this entity per target when transferring.
    /// </summary>
    /// <remarks>
    /// Note that this amount is per target, so the total amount removed will be
    /// multiplied by the number of targets hit.
    /// </remarks>
    [DataField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(1);

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }

    /// <summary>
    /// Proportion of the <see cref="TransferAmount"/> that will actually be injected
    /// into the target's bloodstream. The rest is lost.
    /// 0 means none of the transferred solution will enter the bloodstream.
    /// 1 means the entire amount will enter the bloodstream.
    /// </summary>
    [DataField("transferEfficiency")]
    private float _transferEfficiency = 1f;

    /// <summary>
    /// Solution to inject from.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// Whether this will inject through hardsuits or not.
    /// </summary>
    [DataField]
    public bool PierceArmor = true;

    /// <summary>
    /// Contents of popup message to display to the attacker when injection
    /// fails due to the target wearing a hardsuit.
    /// </summary>
    /// <remarks>
    /// Passed values: $weapon and $target
    /// </remarks>
    [DataField]
    public LocId BlockedByHardsuitPopupMessage = "melee-inject-failed-hardsuit";

    /// <summary>
    /// If anything covers any of these slots then the injection fails.
    /// </summary>
    [DataField]
    public SlotFlags BlockSlots = SlotFlags.NONE;
}
