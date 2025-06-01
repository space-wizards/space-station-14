using Content.Shared.Actions;

namespace Content.Shared.RepulseAttract.Events;

// Action event to repulse/attract
// TODO: Give speech support later for wizard
// TODO: When actions are refactored, give action targeting data (to change between single target, all around, etc)
public sealed partial class RepulseAttractActionEvent : InstantActionEvent;
