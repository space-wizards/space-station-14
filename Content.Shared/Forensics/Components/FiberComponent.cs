using Robust.Shared.GameStates;

namespace Content.Server.Forensics
{
    /// <summary>
    /// This controls fibers left by gloves on items,
    /// which the forensics system uses.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class FiberComponent : Component
    {
        [DataField, AutoNetworkedField]
        public LocId FiberMaterial = "fibers-synthetic";

        [DataField, AutoNetworkedField]
        public string? FiberColor;
    }
}
