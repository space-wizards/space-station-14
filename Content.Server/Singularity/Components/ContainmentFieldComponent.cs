using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedContainmentFieldComponent))]
    public class ContainmentFieldComponent : SharedContainmentFieldComponent
    {
        public ContainmentFieldConnection? Parent;
    }
}
