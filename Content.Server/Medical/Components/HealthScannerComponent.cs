using Content.Server.UserInterface;
using Content.Shared.HealthScanner;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHealthScannerComponent))]
    public class HealthScannerComponent : SharedHealthScannerComponent
    {

        /// <summary>
        /// How long it takes to scan someone.
        /// </summary>
        [DataField("delay")]
        [ViewVariables]
        public float ScanDelay = 0.8f;
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthScannerUiKey.Key);
        public string TargetName = "Unknown";
        public bool TargetIsAlive = false;
        public string TotalDamage = "";
        public List<MobDamageGroup> DamageGroups = new();
    }
}
