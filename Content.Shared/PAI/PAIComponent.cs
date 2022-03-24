using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;

namespace Content.Shared.PAI
{
    /// <summary>
    /// pAIs, or Personal AIs, are essentially portable ghost role generators.
    /// In their current implementation in SS14, they create a ghost role anyone can access,
    /// and that a player can also "wipe" (reset/kick out player).
    /// Theoretically speaking pAIs are supposed to use a dedicated "offer and select" system,
    ///  with the player holding the pAI being able to choose one of the ghosts in the round.
    /// This seems too complicated for an initial implementation, though,
    ///  and there's not always enough players and ghost roles to justify it.
    /// All logic in PAISystem.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class PAIComponent : Component
    {
        [DataField("midiAction", required: true, serverOnly: true)] // server only, as it uses a server-BUI event !type
        public InstantAction? MidiAction;
    }
}

