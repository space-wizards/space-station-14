using System.Numerics;
using Content.Shared.CF.Combat;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Client.CF.Combat;

/// <summary>
/// Floats Hit / Miss / Crit Hit / Crit Miss labels (then damage) above the target.
/// </summary>
public sealed partial class CFDamageNumberSystem : EntitySystem
{
    private const float RisePixels = 58f;
    private const float HeadClearance = 26f;

    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly List<FloatingNumber> _numbers = new();
    private Font _font = default!;

    public override void Initialize()
    {
        base.Initialize();
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 16);
        _overlays.AddOverlay(new CFDamageNumberOverlay(this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay<CFDamageNumberOverlay>();
    }

    public override void FrameUpdate(float frameTime)
    {
        for (var i = _numbers.Count - 1; i >= 0; i--)
        {
            var number = _numbers[i];
            number.Age += frameTime;
            if (number.Age >= number.Lifetime)
                _numbers.RemoveAt(i);
        }
    }

    [SubscribeNetworkEvent]
    private void OnNumber(CFCombatNumberEvent ev)
    {
        var target = GetEntity(ev.Target);
        if (!TryComp(target, out TransformComponent? transform))
            return;

        var origin = _transform.GetMapCoordinates(target, transform);
        var critical = ev.Degree is CFStrikeDegree.CriticalSuccess or CFStrikeDegree.CriticalFailure;
        var horizontal = _random.Next(-16, 17);

        _numbers.Add(new FloatingNumber
        {
            Map = origin,
            Text = DegreeLabel(ev.Degree),
            Color = DegreeColor(ev.Degree),
            Critical = critical,
            Scale = critical ? 1.35f : 1.05f,
            Lifetime = 0.85f + _random.Next(0, 41) / 100f,
            Horizontal = horizontal,
            Stack = 0,
        });

        if (ev.Degree is CFStrikeDegree.Success or CFStrikeDegree.CriticalSuccess)
        {
            var blocked = ev.Amount <= 0;
            var criticalHit = ev.Degree == CFStrikeDegree.CriticalSuccess;
            _numbers.Add(new FloatingNumber
            {
                Map = origin,
                Text = blocked ? "0" : ev.Amount.ToString(),
                Color = blocked ? new Color(255, 40, 40) : ColorForDamage(ev.DamageType),
                Critical = criticalHit,
                Scale = FontScale(blocked ? 1 : ev.Amount, criticalHit),
                Lifetime = 0.9f + _random.Next(0, 41) / 100f,
                Horizontal = horizontal + _random.Next(-4, 5),
                Stack = 1,
            });
        }
    }

    public void Draw(DrawingHandleScreen handle, OverlayDrawArgs args, float uiScale)
    {
        if (_numbers.Count == 0 || args.ViewportControl == null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        foreach (var number in _numbers)
        {
            if (number.Map.MapId != args.MapId || !args.WorldBounds.Contains(number.Map.Position))
                continue;

            var t = Math.Clamp(number.Age / number.Lifetime, 0f, 1f);
            var alpha = 1f - t * t;
            var rise = (1f - (1f - t) * (1f - t)) * RisePixels * uiScale;
            var screen = Vector2.Transform(number.Map.Position, matrix);
            screen.X += number.Horizontal * uiScale;
            screen.Y -= (HeadClearance + number.Stack * 20f) * uiScale + rise;

            var scale = number.Scale * uiScale;
            var size = handle.GetDimensions(_font, number.Text, scale);
            var position = screen - size / 2f;
            var fill = number.Color.WithAlpha(alpha);
            var outline = number.Critical
                ? new TextOutline(2f, new Color(255, 90, 20).WithAlpha(alpha))
                : new TextOutline(1f, Color.Black.WithAlpha(alpha * 0.82f));
            handle.DrawString(_font, position, number.Text, scale, fill, outline);
        }
    }

    public float UiScale()
    {
        var scale = _config.GetCVar(CVars.DisplayUIScale);
        return scale == 0f ? _ui.DefaultUIScale : scale;
    }

    private string DegreeLabel(CFStrikeDegree degree)
    {
        return degree switch
        {
            CFStrikeDegree.CriticalSuccess => Loc.GetString("cf-combat-number-crit-hit"),
            CFStrikeDegree.Success => Loc.GetString("cf-combat-number-hit"),
            CFStrikeDegree.Failure => Loc.GetString("cf-combat-number-miss"),
            CFStrikeDegree.CriticalFailure => Loc.GetString("cf-combat-number-crit-miss"),
            _ => Loc.GetString("cf-combat-number-miss"),
        };
    }

    private static Color DegreeColor(CFStrikeDegree degree)
    {
        return degree switch
        {
            CFStrikeDegree.CriticalSuccess => new Color(255, 196, 64),
            CFStrikeDegree.Success => new Color(255, 236, 160),
            CFStrikeDegree.Failure => new Color(180, 190, 200),
            CFStrikeDegree.CriticalFailure => new Color(140, 150, 165),
            _ => Color.White,
        };
    }

    private static float FontScale(int amount, bool critical)
    {
        var shown = Math.Max(amount, 1);
        var scale = 0.9f + shown * 0.04f;
        if (critical)
            scale += 0.45f;

        return Math.Clamp(scale, 0.9f, 2.6f);
    }

    private static Color ColorForDamage(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "bludgeoning" or "blunt" => new Color(160, 170, 178),
            "piercing" => new Color(200, 42, 42),
            "slashing" or "slash" => new Color(255, 51, 51),
            "acid" or "caustic" => new Color(50, 205, 50),
            "cold" => new Color(0, 255, 255),
            "fire" or "heat" => new Color(255, 127, 0),
            "force" => new Color(218, 112, 214),
            "lightning" or "shock" or "electricity" => new Color(255, 230, 0),
            "void" => new Color(90, 40, 140),
            "vitality" or "healing" => new Color(255, 220, 80),
            "mental" => new Color(255, 105, 180),
            "sonic" => new Color(80, 160, 255),
            "necrotic" => new Color(75, 83, 32),
            "poison" => new Color(160, 32, 240),
            "psychic" => new Color(255, 20, 147),
            "radiant" or "holy" => new Color(255, 215, 0),
            "thunder" => new Color(30, 144, 255),
            _ => new Color(255, 236, 160),
        };
    }

    private sealed class FloatingNumber
    {
        public MapCoordinates Map;
        public string Text = string.Empty;
        public Color Color;
        public bool Critical;
        public float Scale = 1f;
        public float Lifetime = 1f;
        public float Age;
        public float Horizontal;
        public int Stack;
    }
}
