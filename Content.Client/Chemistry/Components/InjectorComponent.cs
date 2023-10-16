using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Client.Chemistry.UI;

namespace Content.Client.Chemistry.Components
{
    /// <summary>
    /// Client behavior for injectors & syringes. Used for item status on injectors
    /// </summary>
    [RegisterComponent]
    public sealed partial class InjectorComponent : SharedInjectorComponent, ITransferControlValues
    {
        [ViewVariables]
        public FixedPoint2 CurrentVolume { get; set; }
        [ViewVariables]
        public FixedPoint2 TotalVolume { get; set; }
        [ViewVariables]
        public SharedTransferToggleMode CurrentMode { get; set; }
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded { get; set; }
    }
}
