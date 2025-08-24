using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components
{
    /// <summary>
    /// Used to take a sample of someone's fingerprints.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ForensicPadComponent : Component
    {
        [DataField("scanDelay"), AutoNetworkedField]
        public float ScanDelay = 3.0f;

        public bool Used = false;
        public String Sample = string.Empty;
    }
}
