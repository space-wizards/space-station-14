using Robust.Shared.GameObjects;

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Event raised when the RNG device should roll
/// </summary>
public readonly struct RollEvent
{
    public int Outputs { get; }
    public EntityUid? User { get; }
    public bool Handled { get; }

    public RollEvent(int outputs, EntityUid? user = null, bool handled = false)
    {
        Outputs = outputs;
        User = user;
        Handled = handled;
    }

    public RollEvent WithHandled(bool handled) => new(Outputs, User, handled);
}
