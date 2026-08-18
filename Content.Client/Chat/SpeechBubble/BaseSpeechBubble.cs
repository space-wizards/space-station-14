using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Chat.SpeechBubble;

//TODO: Move things to this
public abstract partial class BaseSpeechBubble : Control
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    /// <summary>
    ///     The total time a speech bubble stays on screen.
    /// </summary>
    private static readonly TimeSpan TotalTime = TimeSpan.FromSeconds(4f);

    /// <summary>
    ///     The amount of time at the end of the bubble's life at which it starts fading.
    /// </summary>
    private static readonly TimeSpan FadeOutTime = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    ///     The amount of time at the end of the bubble's life at which it starts fading.
    /// </summary>
    private static readonly TimeSpan FadeInTime = TimeSpan.FromSeconds(0.10f);

    /// <summary>
    /// The time this bubble was created
    /// </summary>
    public TimeSpan SpawnTime;

    /// <summary>
    /// The time at which this bubble will die.
    /// </summary>
    public TimeSpan DeathTime { get; private set; }

    private readonly EntityUid _senderEntity;

    public event Action<EntityUid, BaseSpeechBubble>? OnDied;

    public Vector2 ContentSize { get; internal set; }

    protected BaseSpeechBubble()
    {
    }

    protected BaseSpeechBubble(EntityUid senderEntity)
    {
        _senderEntity = senderEntity;
        IoCManager.InjectDependencies(this);

        SpawnTime = _timing.RealTime;
        DeathTime = _timing.RealTime + TotalTime;
        OverlaySetup();

        //for fade in
        Modulate = Color.White.WithAlpha(0);
    }

    /// <summary>
    /// These are typically drawn in the overlay, so regular FrameUpdate doesn't apply.
    /// This is called from the overlay.
    /// </summary>
    public void Update(FrameEventArgs args)
    {
        var timeAlive = (float)(_timing.RealTime - SpawnTime).TotalSeconds;

        var timeLeft = (float)(DeathTime - _timing.RealTime).TotalSeconds;

        if (_entityManager.Deleted(_senderEntity) || timeLeft <= 0)
        {
            // Timer spawn to prevent concurrent modification exception.
            Timer.Spawn(0, Die);
            return;
        }

        //Hide if offscreen
        if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform) || xform.MapID != _eyeManager.CurrentEye.Position.MapId)
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        if (timeAlive < FadeInTime.TotalSeconds)
        {
            Modulate = Color.White.WithAlpha(timeAlive / (float)FadeInTime.TotalSeconds);
        }
        else if (timeLeft <= FadeOutTime.TotalSeconds)
        {
            // Update alpha if we're fading.
            Modulate = Color.White.WithAlpha(timeLeft / (float)FadeOutTime.TotalSeconds);
        }
        else
        {
            // Make opaque otherwise, because it might have been hidden before
            Modulate = Color.White;
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        //If for some reason this isn't drawn in an overlay.
        //Probably shouldn't happen.
        Update(args);
    }

    public void SetDeathTime(TimeSpan time)
    {
        DeathTime = time;
    }

    //TODO: Replace
    private void Die()
    {
        if (Disposed)
        {
            return;
        }

        OnDied?.Invoke(_senderEntity, this);
    }

    /// <summary>
    /// Causes the speech bubble to start fading IMMEDIATELY.
    /// </summary>
    public void FadeNow()
    {
        if (DeathTime > _timing.RealTime)
        {
            DeathTime = _timing.RealTime + FadeOutTime;
        }
    }

    internal void SetContentSize(Vector2 size)
    {
        ContentSize = size;
    }

    /// <summary>
    /// Prepares the control to be used in an overlay.
    /// TODO: Figure out how much of this is actually needed.
    /// </summary>
    internal void OverlaySetup()
    {
        InvalidateStyleSheet();
        ForceRunStyleUpdate();

        InvalidateMeasure();
        InvalidateArrange();

        Measure(Vector2.PositiveInfinity);

        Arrange(UIBox2.FromDimensions(Vector2.Zero, DesiredSize));
    }

}

