using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Chemistry.Components
{
    /// <summary>
    /// Client behavior for injectors & syringes. Used for item status on injectors
    /// </summary>
    [RegisterComponent]
    public sealed class InjectorComponent : SharedInjectorComponent
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
