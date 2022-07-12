using Content.Shared.Stacks;

namespace Content.Server.Stack
{
    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent, Access(typeof(StackSystem))]
    [ComponentReference(typeof(SharedStackComponent))]
    public sealed class StackComponent : SharedStackComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually { get; set; } = false;
    }
}
