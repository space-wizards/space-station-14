using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Client.Chemistry.Components
{
    /// <summary>
    /// Client behavior for injectors & syringes. Used for item status on injectors
    /// </summary>
    [RegisterComponent]
    public sealed partial class InjectorComponent : SharedInjectorComponent
    {
        [ViewVariables]
        public FixedPoint2 CurrentVolume;
        [ViewVariables]
        public FixedPoint2 TotalVolume;
        [ViewVariables]
        public InjectorToggleMode CurrentMode;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded;
    }
}
