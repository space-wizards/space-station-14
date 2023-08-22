using Content.Shared.Drunk;
using Content.Shared.StatusEffect;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Drunk;

public sealed class DrunkOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    ISawmill s = default!;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public float CurrentBoozePower = 0f;
    private readonly ShaderInstance _drunkShader;


    private const float VisualThreshold = 10.0f;
    private const float PowerDivisor = 250.0f;

    private float _visualScale = 0;

    public DrunkOverlay()
    {
        IoCManager.InjectDependencies(this);
        _drunkShader = _prototypeManager.Index<ShaderPrototype>("Drunk").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        s = Logger.GetSawmill("drink");
        var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

        if (playerEntity == null)
            return;
        s.Debug("11");
        if (!_entityManager.TryGetComponent<DrunkComponent>(playerEntity, out var drunkComp)
            || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
            return;
        s.Debug("22");

        
        
        s.Debug(CurrentBoozePower.ToString());
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalPlayer?.ControlledEntity, out EyeComponent? eyeComp))
            return false;

        if (!_entityManager.TryGetComponent<DrunkComponent>(_playerManager.LocalPlayer?.ControlledEntity, out var drunkComp))
            return false;
        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        _visualScale = BoozePowerToVisual(CurrentBoozePower);
        return _visualScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _drunkShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _drunkShader.SetParameter("boozePower", _visualScale);
        handle.UseShader(_drunkShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    /// <summary>
    ///     Converts the # of seconds the drunk effect lasts for (booze power) to a percentage
    ///     used by the actual shader.
    /// </summary>
    /// <param name="boozePower"></param>
    private float BoozePowerToVisual(float boozePower)
    {
        return Math.Clamp((boozePower - VisualThreshold) / PowerDivisor, 0.0f, 1.0f);
    }

    public sealed class OnOverlayUpdateEvent : EventArgs
    {
        public float CurrentBoozePower;
        public OnOverlayUpdateEvent(float currentBoozePower)
        {
            CurrentBoozePower = currentBoozePower;
        }
    }
}
