namespace Content.Client.Radar;

public sealed class ShuttleConsoleSystem : EntitySystem
{
    private ShuttleConsoleWindow? _window;

    private Vector2 _dummy;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_window == null) return;

        _window.SetLinearVelocity(_dummy + Vector2.One * frameTime);
    }
}
