using Robust.Shared.Player;

namespace Content.Server.Ghost.Roles.Raffles;

/// <summary>
/// Chooses a winner of a ghost role raffle.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface IGhostRoleRaffleDecider
{
    /// <summary>
    /// Chooses a winner of a ghost role raffle draw from the given pool of candidates.
    /// </summary>
    /// <param name="candidates">The players in the session at the time of drawing.</param>
    /// <param name="tryTakeover">
    /// Call this with the chosen winner as argument.
    /// <ul><li>If <c>true</c> is returned, your winner was able to take over the ghost role, and the drawing is complete.
    /// <b>Do not call <see cref="tryTakeover"/> again after true is returned.</b></li>
    /// <li>If <c>false</c> is returned, your winner was not able to take over the ghost role,
    /// and you must choose another winner, and call <see cref="tryTakeover"/> with the new winner as argument.</li>
    /// </ul>
    ///
    /// If <see cref="tryTakeover"/> is not called, or only returns false, the raffle will end without a winner.
    /// Do not call <see cref="tryTakeover"/> with the same player several times.
    /// </param>
    void PickWinner(IEnumerable<ICommonSession> candidates, Func<ICommonSession, bool> tryTakeover);
}

