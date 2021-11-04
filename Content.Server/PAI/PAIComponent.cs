using Robust.Shared.GameObjects;

namespace Content.Server.PAI
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
    [RegisterComponent]
    public class PAIComponent : Component
    {
        public override string Name => "PAI";
    }
}

