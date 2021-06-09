#nullable enable
using Content.Shared.Actions;

namespace Content.Server.GameObjects.Components.Observer
{
    /// <summary>
    ///     Allow ghost to interact with object by boo action
    /// </summary>
    public interface IGhostBooAffected
    {
        /// <summary>
        ///     Invokes when ghost used boo action near entity.
        ///     Use it to blink lights or make something spooky.
        /// </summary>
        /// <param name="args">Boo action details</param>
        /// <returns>Returns true if object was affected</returns>
        bool AffectedByGhostBoo(InstantActionEventArgs args);
    }
}
