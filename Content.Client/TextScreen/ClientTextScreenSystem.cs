using Content.Shared.TextScreen.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.TextScreen.Systems;

/// <inheritdoc/>
public sealed partial class ClientTextScreenSystem : TextScreenSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<TextScreenTimerComponent> _screenTimerQuery = default!;

    /// <summary>
    /// Contains char/state Key/Value pairs. <br/>
    /// The states in Textures/Effects/text.rsi that special character should be replaced with.
    /// </summary>
    private static readonly Dictionary<char, string> CharStatePairs = new() {
        { '<', "angle-l" },
        { '>', "angle-r" },
        {'\'', "apostrophe" },
        {'\\', "backslash" },
        { ' ', "blank" },
        { '[', "bracket-l" },
        { ']', "bracket-r" },
        { '^', "caret" },
        { ':', "colon" },
        { ',', "comma" },
        { '-', "dash" },
        { '=', "equals" },
        { '!', "exclamation" },
        { '#', "hash" },
        { '(', "paren-l" },
        { ')', "paren-r" },
        { '%', "percent" },
        { '.', "period" },
        { '+', "plus" },
        { '?', "question" },
        { '"', "quotation" },
        { ';', "semicolon" },
        { '/', "slash" },
        { '$', "speso" },
        { '*', "star" },
        { '_', "underscore" },
    };

    private const string DefaultState = "blank";

    /// <summary>
    /// A string prefix for all text layers.
    /// </summary>
    private const string TextMapKey = "textMapKey";

    /// <summary>
    /// The path to the RSI containing the text sprites.
    /// </summary>
    private const string TextPath = "Effects/text.rsi";

    /// <summary>
    /// The width of an individual character, in pixels.
    /// </summary>
    private const int CharWidth = 4;

    /// <summary>
    /// The maximum number of characters to display per row.
    /// </summary>
    private const int MaxScrollingCharacters = 32;

    /// <summary>
    /// The longest that a message should take to cross the screen before wrapping around.
    /// </summary>
    private static readonly TimeSpan MaxMessageScrollTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The longest that it should take to scroll one pixel on a screen.
    /// </summary>
    private static readonly TimeSpan MaxPixelScrollTime = TimeSpan.FromMilliseconds(100);

    #region Public API
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    /// <summary>
    /// Update handler - keep timers and scrolling text up to date.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TextScreenTimerComponent>();
        while (query.MoveNext(out var uid, out var timer))
        {
            if (timer.DisplayTime == null)
            {
                if (timer.FinishDisplayed)
                    continue;

                SetTextToDisplay(uid, timer.FinishedText);
                timer.FinishDisplayed = true;
            }
            else
            {
                int screenValue = ConvertTimeToScreenValue(timer.DisplayTime.Value, _timing.CurTime);
                if (screenValue == 0)
                {
                    SetTextToDisplay(uid, timer.FinishedText);
                    timer.FinishDisplayed = true;
                    timer.DisplayTime = null;
                    timer.ScreenValue = 0;
                }
                else if (screenValue != timer.ScreenValue)
                {
                    var timerText = ConstructTimerText(timer);
                    SetTextToDisplay(uid, timerText);
                    timer.ScreenValue = screenValue;
                }
            }
        }

        var query = EntityQueryEnumerator<TextScreenComponent>();
        while (query.MoveNext(out var uid, out var screen))
        {
            if (_screenTimerQuery.TryComp(uid, out var timer))
            {
                if (_gameTiming.CurTime < timer.Target)
                {
                    BuildTimerLayers((uid, timer, screen));
                    DrawStaticLayers(uid, timer.LayerStatesToDraw);
                }
                else
                {
                    TeardownTimer((uid, screen));
                }
            }
            else if (screen.ScrollEnabled && screen.NextScrollTime.Any(x => x < _gameTiming.CurTime))
            {
                DrawScrolledLayers((uid, screen));
            }
        }
    }

    /// <summary>
    /// Converts the difference between two timespans into a value between 0 and 9999.
    /// </summary>
    public int ConvertTimeToScreenValue(TimeSpan targetTime, TimeSpan curTime)
    {
        if (targetTime <= curTime)
            return 0;
        var milliseconds = (targetTime - curTime).TotalMilliseconds;
        if (milliseconds < 10_000) // 9999, 99:99, the largest value that could fit in two fields.
            return (int)milliseconds;
        else if (milliseconds <= TimeSpan.MillisecondsPerHour)
            return targetTime.Minutes * 100 + targetTime.Seconds;
        else
            return targetTime.Hours * 100 + targetTime.Minutes;
    }

    /// <summary>
    /// Converts the difference between two timespans into a value between 0 and 9999.
    /// </summary>
    public void SetTextToDisplay(Entity<TextScreenComponent?> ent, string? text)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.TextToDisplay == text)
            return;

        ent.Comp.TextToDisplay = text;
        ent.Comp.NewTextToDisplay = true;
    }
    #endregion Public API

    #region Event Handlers
    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStartup(Entity<TextScreenComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        for (var rowIdx = 0; rowIdx < ent.Comp.Rows; rowIdx++)
        {
            var maxIndex = ent.Comp.ScrollEnabled ? ent.Comp.RowLength + 1 : ent.Comp.RowLength;
            var textScreenRow = ent.Comp.RowData[rowIdx];

            for (var chr = 0; chr <= maxIndex; chr++)
            {
                var newKey = TextMapKey + rowIdx + chr;
                _sprite.LayerMapReserve((ent, sprite), newKey);
                textScreenRow.Layers.Add((newKey, null));
            }
        }

        if (ent.Comp.FrameState != null)
            _sprite.AddLayer((ent, sprite), ent.Comp.FrameState, null);
    }

    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAutoHandleState(Entity<TextScreenComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_screenTimerQuery.HasComp(ent))
            return;


    }

    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAutoHandleState(Entity<TextScreenTimerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.TargetTime != ent.Comp.DisplayTime)
    }

    /// <summary>
    /// Handles non-trivial pause timing for scrolling.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnpaused(Entity<TextScreenComponent> ent, ref EntityUnpausedEvent args)
    {
        for (int i = 0; i < ent.Comp.RowData.Length; i++)
            AddTime(ref ent.Comp.RowData[i], args.PausedTime);
    }

    /// <summary>
    /// Adds <paramref name="time"/> to the row's scrolling timer.
    /// </summary>
    private void AddTime(ref TextScreenRow row, TimeSpan time)
    {
        row.NextScroll += time;
    }
    #endregion Event Handlers
}
