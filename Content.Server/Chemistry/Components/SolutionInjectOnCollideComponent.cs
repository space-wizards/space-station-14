using Content.Shared.FixedPoint;
using Content.Shared.Inventory;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// On colliding with an entity that has a bloodstream will dump its solution onto them.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionInjectOnCollideComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("transferAmount")]
    public FixedPoint2 TransferAmount = FixedPoint2.New(1);

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferEfficiency { get => _transferEfficiency; set => _transferEfficiency = Math.Clamp(value, 0, 1); }

    [DataField("transferEfficiency")]
    private float _transferEfficiency = 1f;

    /// <summary>
    /// If anything covers any of these slots then the injection fails.
    /// </summary>
    [DataField("blockSlots"), ViewVariables(VVAccess.ReadWrite)]
    public SlotFlags BlockSlots = SlotFlags.MASK;
}
