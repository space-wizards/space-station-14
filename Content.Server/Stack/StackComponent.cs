using Content.Shared.Stacks;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Stack
{
    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent, Friend(typeof(StackSystem))]
    [ComponentReference(typeof(SharedStackComponent))]
    public sealed class StackComponent : SharedStackComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually { get; set; } = false;
    }
}
