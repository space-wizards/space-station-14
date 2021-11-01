using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Internals
{
    public class ToggleInternalsEvent : HandledEntityEventArgs
    {
        // Disconnect is done via forced means, already ensured to be valid
        public readonly bool BypassCheck = false;
        // Null for toggle, otherwise set the state to requested value
        public readonly bool? ForcedState;
        // Pass the actions down the chain to keep them updated
        public readonly ItemActionsComponent? Actions;

        public ToggleInternalsEvent(bool? force = null)
        {
            ForcedState = force;
        }

        public ToggleInternalsEvent(bool? force, bool bypassCheck, ItemActionsComponent? actions = null)
        {
            BypassCheck = bypassCheck;
            ForcedState = force;
            Actions = actions;
        }
    }
}
