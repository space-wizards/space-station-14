using Content.Shared.StatusIcon;

namespace Content.Shared.TypingIndicator;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedTypingIndicatorSystem : EntitySystem
{
    /// <summary>
    ///     Default ID of typing indicator icon <see cref="StatusIconPrototype"/>
    /// </summary>
    [ValidatePrototypeId<TypingIndicatorPrototype>]
    public const string InitialIndicatorId = "Default";

    public override void Initialize()
    {

    }
}
