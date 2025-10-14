using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.GameTicking;

public sealed class AutoRoundEndingHudUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private AutoRoundEndingHudControl? _control;

    private TimeSpan _startTime;
    private float _delaySeconds;
    private string? _label;
    private Texture? _icon;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AutoRoundEndingHudEvent>(OnHudEvent);
        SubscribeNetworkEvent<AutoRoundEndingHudClearEvent>(OnHudClear);
    }

    public void OnStateEntered(GameplayState state)
    {
        _control = new AutoRoundEndingHudControl();
        UIManager.RootControl.AddChild(_control);
        UpdateControl();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_control == null)
            return;
        UIManager.RootControl.RemoveChild(_control);
        _control.Dispose();
        _control = null;
    }

    private void OnHudEvent(AutoRoundEndingHudEvent ev, EntitySessionEventArgs args)
    {
        _startTime = ev.StartTime;
        _delaySeconds = ev.DelaySeconds;
        _label = string.IsNullOrWhiteSpace(ev.Label) ? null : ev.Label;

        _icon = null;
        if (!string.IsNullOrWhiteSpace(ev.HudIconRsi) && !string.IsNullOrWhiteSpace(ev.HudIconState))
        {
            var cache = IoCManager.Resolve<IResourceCache>();
            var path = new ResPath("/Textures") / ev.HudIconRsi!;
            if (cache.TryGetResource(path, out RSIResource? rsiRes))
            {
                var rsi = rsiRes.RSI;
                var stateName = ev.HudIconState;
                if (stateName != null && rsi.TryGetState(stateName, out var state))
                {
                    _icon = state.Frame0;
                }
            }
        }

        UpdateControl();
    }

    private void OnHudClear(AutoRoundEndingHudClearEvent ev, EntitySessionEventArgs args)
    {
        _icon = null;
        _label = null;
        _startTime = TimeSpan.Zero;
        _delaySeconds = 0f;
        UpdateControl();
    }

    private void UpdateControl()
    {
        if (_control == null)
            return;
        _control.SetData(_startTime, _delaySeconds, _label, _icon, _timing);
    }

    private sealed class AutoRoundEndingHudControl : Control
    {
        private Font _fontMedium = default!;
        private Font _fontLarge = default!;

        private TimeSpan _start;
        private float _delay;
        private string? _label;
        private Texture? _icon;
        private IGameTiming? _timing;

        public AutoRoundEndingHudControl()
        {
            MouseFilter = MouseFilterMode.Ignore;
            var cache = IoCManager.Resolve<IResourceCache>();
            _fontMedium = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 16);
            _fontLarge = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 20);
        }

        public void SetData(TimeSpan start, float delay, string? label, Texture? icon, IGameTiming timing)
        {
            _start = start;
            _delay = delay;
            _label = label;
            _icon = icon;
            _timing = timing;
            Visible = _delay > 0f; // show only if active
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            if (!Visible || _timing == null)
                return;

            var remaining = MathF.Max(0f, _delay - (float)(_timing.CurTime - _start).TotalSeconds);
            if (remaining <= 0f)
                return;

            // Layout at top-center, slightly below the top edge.
            var marginTop = 48f; // a bit higher

            // Icon scale: 3x for 32px -> 96px
            var iconScale = 3f;
            var textLeftPadding = 12f; // space between icon and text

            // Prepare text contents
            var label = _label ?? string.Empty;
            var minutes = (int)(remaining / 60f);
            var seconds = (int)MathF.Round(remaining % 60f, MidpointRounding.AwayFromZero);
            if (seconds == 60)
            {
                minutes += 1;
                seconds = 0;
            }
            var timeText = $"{minutes:00}:{seconds:00}";

            // Measure text to compute total width
            var labelDims = handle.GetDimensions(_fontLarge, label, UIScale);
            var timeDims = handle.GetDimensions(_fontMedium, timeText, UIScale);
            var textWidth = MathF.Max(labelDims.X, timeDims.X);

            // Icon scaled size
            var iconWidth = 0f;
            var iconHeight = 0f;
            if (_icon != null)
            {
                var size = _icon.Size;
                iconWidth = size.X * iconScale;
                iconHeight = size.Y * iconScale;
            }

            var padding = _icon != null ? textLeftPadding : 0f;
            var totalWidth = iconWidth + padding + textWidth;

            // Center horizontally with a slight left shift
            var shiftX = -150f; // nudge further left
            var baseX = (PixelSize.X - totalWidth) / 2f + shiftX;
            var baseY = marginTop;

            // Draw icon (left)
            if (_icon != null)
            {
                var rect = new UIBox2(new Vector2(baseX, baseY), new Vector2(baseX + iconWidth, baseY + iconHeight));
                handle.DrawTextureRect(_icon, rect);
                baseX = rect.Right + padding;
            }

            // Draw text (right of icon): label then time
            var yLabel = baseY; // top aligned with icon top
            handle.DrawString(_fontLarge, new Vector2(baseX, yLabel), label, UIScale, Color.White);

            var yTime = yLabel + labelDims.Y + 2f;
            handle.DrawString(_fontMedium, new Vector2(baseX, yTime), timeText, UIScale, Color.LightGray);
        }
    }
}
