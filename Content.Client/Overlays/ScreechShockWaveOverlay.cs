using System.Numerics;
using Content.Shared.Screech;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Overlays;

public sealed partial class ScreechShockWaveOverlay : Overlay
{
    // Dependencies
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    // Fields
    private SharedTransformSystem? _xformSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> ScreechPrototype = "ScreechShockWave";

    // The hell of shader variables
    private int _currentCount = 0;
    /// <summary>
    /// This constant governs the maximum amount of instances. This is mirrored in the shader itself.
    /// </summary>
    private static readonly int MaximumInstances = 10;
    private Vector2[] _positions;
    private float[] _waveStrengths;
    private float[] _waveSpeeds;
    private float[] _downScales;
    private float[] _fades;
    private float[] _times;

    public ScreechShockWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ScreechPrototype).Instance().Duplicate();
        _positions = new Vector2[MaximumInstances];
        _waveStrengths = new float[MaximumInstances];
        _waveSpeeds = new float[MaximumInstances];
        _downScales = new float[MaximumInstances];
        _fades = new float[MaximumInstances];
        _times = new float[MaximumInstances];
    }
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null || _xformSystem is null && !_entMan.TrySystem(out _xformSystem))
            return false;
        var query = _entMan.EntityQueryEnumerator<ScreechShockWaveComponent, TransformComponent>();
        _currentCount = 0;
        while (query.MoveNext(out var uid, out var distortion, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var mapPos = _xformSystem.GetWorldPosition(uid);
            var tempCoords = args.Viewport.WorldToLocal(mapPos);

            // normalized coords, 0 - 1 plane. This is pure hell, we subtract 1 because fragment calculates from the bottom and local goes from the top of the viewport
            tempCoords.Y = 1 - (tempCoords.Y / args.Viewport.Size.Y);
            tempCoords.X /= args.Viewport.Size.X;

            var position = tempCoords;
            var waveStrength = distortion.WaveStrength;
            var waveSpeed = distortion.WaveSpeed;
            var downScale = distortion.DownScale;

            var time = (float)(_timing.CurTime - distortion.InitTime).TotalSeconds;
            var fade = 1f - time / distortion.FadeTime;

            // shorthand
            var i = _currentCount;
            _positions[i] = position;
            _waveStrengths[i] = waveStrength;
            _waveSpeeds[i] = waveSpeed;
            _downScales[i] = downScale;
            _fades[i] = fade;
            _times[i] = time;

            _currentCount += 1;
            if (_currentCount == MaximumInstances)
            {
                break;
            }
        }

        return _currentCount != 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;


        // set the parameters
        _shader?.SetParameter("positions", _positions);
        _shader?.SetParameter("waveSpeeds", _waveSpeeds);
        _shader?.SetParameter("downScales", _downScales);
        _shader?.SetParameter("waveStrengths", _waveStrengths);
        _shader?.SetParameter("fades", _fades);
        _shader?.SetParameter("times", _times);
        _shader?.SetParameter("count", _currentCount);
        _shader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        // finally do the rendering
        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);

        // i wonder what would happen if this line wasn't there :godo:
        worldHandle.UseShader(null);
    }

    /// <summary>
    /// This struct represents one distorting screech instance
    /// </summary>
    private struct InnerShaderInstance
    {
        public Vector2 Position;
        public float WaveStrength;
        public float WaveSpeed;
        public float DownScale;
        public float Fade;
    }
}
