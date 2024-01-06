namespace Content.Shared.Turnstile;

using Content.Shared.Turnstile.Components;

public sealed class TurnstileEvents
{
    /// <summary>
    /// Raised when the door's State variable is changed to a new variable that it was not equal to before.
    /// </summary>
    public sealed class TurnstileStateChangedEvent : EntityEventArgs
    {
        public readonly TurnstileState State;

        public TurnstileStateChangedEvent(TurnstileState state)
        {
            State = state;
        }
    }
}
