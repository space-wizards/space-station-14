using System.Numerics;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Event raised when determining the direction a user wishes to move in, allowing other systems to modify it.
/// </summary>
/// <param name="WishDir">The targetted direction.</param>
/// <remarks>Applied before friction/acceleration, making it ideal for "modifying user input".</remarks>
[ByRefEvent]
public record struct ModifyMovementTargetDirectionEvent(Vector2 WishDir);
