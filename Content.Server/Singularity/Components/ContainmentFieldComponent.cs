using Robust.Shared.GameObjects;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    public class ContainmentFieldComponent : Component
    {
        public override string Name => "ContainmentField";
        public ContainmentFieldConnection? Parent;
    }
}
