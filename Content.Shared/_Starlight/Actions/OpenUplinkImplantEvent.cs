using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared.Actions
{
    public sealed partial class OpenUplinkImplantEvent : InstantActionEvent
    {
        [ViewVariables]
        public EntityUid User { get; set; }
    }
}
