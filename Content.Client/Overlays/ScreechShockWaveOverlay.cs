using System.Numerics;
using Content.Shared.Screech;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Overlays;

public sealed partial class ScreechShockWaveOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private SharedTransformSystem? _xformSystem;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> ScreechPrototype = "ScreechShockWave";

    /// <summary>
    /// Contains a cached list of all screech shock wave entities in PVS
    /// </summary>
    private readonly List<(EntityUid, InnerShaderInstance)> _cached;

    // The hell of shader variables
    /// <summary>
    /// Keeps track of the current amount of registered shockwaves
    /// </summary>
    private int _currentCount;

    /// <summary>
    /// This constant governs the maximum amount of instances. This is mirrored in the shader itself.
    /// </summary>
    private const int MaximumInstances = 10;

    private readonly Vector2[] _positions;
    private readonly float[] _waveStrengths;
    private readonly float[] _waveSpeeds;
    private readonly float[] _downScales;
    private readonly float[] _fades;
    private readonly float[] _times;

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
        _cached = new List<(EntityUid, InnerShaderInstance)>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null || _xformSystem is null && !_entMan.TrySystem(out _xformSystem))
            return false;

        _currentCount = 0;

        // check for removal of instances whose times are elapsed
        _cached.RemoveAll((k) => (float)(_timing.CurTime - k.Item2.InitTime).TotalSeconds > k.Item2.FadeTime);

        foreach (var (entityUid, distortion) in _cached)
        {
            // check if its alive (we don't remove it now, it'll be removed later anyway)
            if (!_entMan.EntityExists(entityUid))
                continue;

            // if it's not on the same map, we don't care
            var xform = _entMan.GetComponent<TransformComponent>(entityUid);
            if (xform.MapID != args.MapId)
                continue;

            // shorthand
            var mapPos = _xformSystem.GetWorldPosition(entityUid);
            var tempCoords = args.Viewport.WorldToLocal(mapPos);

            // normalized coords, 0 - 1 plane. This is pure hell, we subtract 1 because fragment calculates from the bottom and local goes from the top of the viewport
            tempCoords.Y = 1 - tempCoords.Y / args.Viewport.Size.Y;
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
        _shader.SetParameter("positions", _positions);
        _shader.SetParameter("waveSpeeds", _waveSpeeds);
        _shader.SetParameter("downScales", _downScales);
        _shader.SetParameter("waveStrengths", _waveStrengths);
        _shader.SetParameter("fades", _fades);
        _shader.SetParameter("times", _times);
        _shader.SetParameter("count", _currentCount);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        // finally do the rendering
        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);

        // i wonder what would happen if this line wasn't there :godo:
        worldHandle.UseShader(null);
    }

    /// <summary>
    /// Adds this entity to the cache
    /// </summary>
    public void Register(Entity<ScreechShockWaveComponent> ent)
    {
        _cached.Add((ent.Owner, new InnerShaderInstance
        {
            WaveSpeed = ent.Comp.WaveSpeed,
            WaveStrength = ent.Comp.WaveStrength,
            DownScale = ent.Comp.DownScale,
            FadeTime = ent.Comp.FadeTime,
            InitTime = ent.Comp.InitTime
        }));
    }

    /// <summary>
    /// This struct represents one distorting screech instance
    /// </summary>
    private struct InnerShaderInstance
    {
        // these fields are a copy of <see cref="ScreechShockWaveComponent"/>'s
        public float WaveSpeed;
        public float WaveStrength;
        public float DownScale;
        public float FadeTime;
        public TimeSpan InitTime;
    }
}
