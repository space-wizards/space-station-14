using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Stack
{
    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent]
    [ComponentReference(typeof(SharedStackComponent))]
    public class StackComponent : SharedStackComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually { get; set; } = false;
    }
}
