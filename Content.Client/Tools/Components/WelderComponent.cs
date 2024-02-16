using Content.Client.Tools.UI;
using Content.Shared.Tools.Components;

namespace Content.Client.Tools.Components
{
    [RegisterComponent, Access(typeof(ToolSystem), typeof(WelderStatusControl))]
    public sealed partial class WelderComponent : SharedWelderComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded { get; set; }

        [ViewVariables]
        public float FuelCapacity { get; set; }

        [ViewVariables]
        public float Fuel { get; set; }
    }
}
