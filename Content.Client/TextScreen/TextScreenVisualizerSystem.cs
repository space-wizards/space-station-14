using System.Numerics;
using Content.Client.TextScreen.Components;
using Content.Shared.TextScreen.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.TextScreen.Systems;

// TODO!
// GO BACK TO APPEARANCE DATA!
// should be able to replace AfterAutoHandleState handlers

/// <summary>
/// The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// It optionally supports scrolling text.
/// </summary>
/// <remarks>
/// Not using a VisualizerSystem since this cares about two different components.
/// </remarks>
/// <seealso cref="TextScreenVisualsComponent"/>
/// <seealso cref="TextScreenTimerComponent"/>
public sealed partial class TextScreenVisualizerSystem : VisualizerSystem<TextScreenVisualsComponent>
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<TextScreenTimerVisualsComponent> _screenTimerQuery;

    /// <summary>
    /// Contains char/state Key/Value pairs. <br/>
    /// The states in Textures/Effects/text.rsi that special character should be replaced with.
    /// </summary>
    private static readonly Dictionary<char, string> CharStatePairs = new() {
        { '<', "angle-l" },
        { '>', "angle-r" },
        {'\'', "apostrophe" },
        {'\\', "backslash" },
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

        var timerQuery = EntityQueryEnumerator<TextScreenTimerVisualsComponent>();
        while (timerQuery.MoveNext(out var uid, out var timer))
        {
            if (timer.TargetTime == null || timer.TargetTime <= _timing.CurTime)
            {
                if (timer.ScreenValue == 0)
                    continue;

                SetTextToDisplay(uid, timer.FinishedText);
                timer.ScreenValue = 0;
            }
            else
            {
                int screenValue = ConvertTimeToScreenValue(timer.TargetTime.Value, _timing.CurTime);
                if (screenValue == 0)
                {
                    SetTextToDisplay(uid, timer.FinishedText);
                    timer.ScreenValue = 0;
                }
                else if (screenValue != timer.ScreenValue)
                {
                    var timerText = GetTimerString((uid, timer), screenValue);
                    SetTextToDisplay(uid, timerText);
                    timer.ScreenValue = screenValue;
                }
            }
        }

        var screenQuery = EntityQueryEnumerator<TextScreenVisualsComponent, SpriteComponent>();
        while (screenQuery.MoveNext(out var uid, out var screen, out var sprite))
        {
            if (screen.NewTextToDisplay)
            {
                // Update text
                DrawNewText((uid, screen));
                screen.NewTextToDisplay = false;
            }

            // Handle scrolling.
            if (screen.ScrollEnabled)
            {
                for (int i = 0; i < screen.RowData.Length; i++)
                {
                    var rowData = screen.RowData[i];
                    if (rowData.NextScroll <= _timing.CurTime)
                    {
                        ScrollRow(ref rowData);
                        DrawLayers((uid, screen, sprite), rowData, i);
                    }
                }
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
    public void SetTextToDisplay(Entity<TextScreenVisualsComponent?> ent, string? text)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.TextToDisplay == text)
            return;

        ent.Comp.TextToDisplay = text;
        ent.Comp.NewTextToDisplay = true;
    }


    /// <summary>
    /// Returns the Effects/text.rsi state string based on <paramref name="character"/>, or null if none available.
    /// </summary>
    public static string? GetStateFromChar(char? character)
    {
        if (character == null)
            return null;

        // First checks if its one of our special characters
        if (CharStatePairs.TryGetValue(character.Value, out var value))
            return value;

        // Or else it checks if its a normal letter or digit
        if (char.IsLetterOrDigit(character.Value))
            return character.Value.ToString().ToLower();

        return null;
    }
    #endregion Public API

    #region Event Handlers
    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStartup(Entity<TextScreenVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        for (var rowIdx = 0; rowIdx < ent.Comp.RowData.Length; rowIdx++)
        {
            var maxIndex = ent.Comp.ScrollEnabled ? ent.Comp.RowLength + 1 : ent.Comp.RowLength;
            var textScreenRow = ent.Comp.RowData[rowIdx];

            for (var chr = 0; chr <= maxIndex; chr++)
            {
                var newKey = TextMapKey + rowIdx + chr;
                SpriteSystem.LayerMapReserve((ent, sprite), newKey);
                textScreenRow.Layers.Add((newKey, null));
            }
        }

        if (ent.Comp.FrameState != null)
            SpriteSystem.AddLayer((ent, sprite), ent.Comp.FrameState, null);
    }

    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        bool anyChange = false;
        if (!args.TryGetData<Color>(TextScreenVisuals.Color, out var color)
            && color != comp.Color)
        {
            comp.Color = color;
            anyChange = true;
        }

        //
        if (!args.TryGetData(TextScreenVisuals.ScreenText, out string? text))
        {
            args.TryGetData(TextScreenVisuals.DefaultText, out text);
        }

        if (!args.TryGetData(TextScreenVisuals.ScreenTextTime, out TimeSpan? scrollTime))
            scrollTime = _timing.CurTime;

        if (_screenTimerQuery.HasComp(uid)
            && args.TryGetData(TextScreenVisuals.TargetTime, out TimeSpan? textTime))
        {
            // If we have a valid timer, draw the timer.
        }
        else if (text is { } newText)
        {
            // Otherwise, if we have text, draw our text.
        }
    }

    /// <summary>
    /// Handles updates from the server.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAutoHandleState(Entity<TextScreenTimerVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        var newScreenValue = 0;

        if (ent.Comp.TargetTime != null)
            newScreenValue = ConvertTimeToScreenValue(ent.Comp.TargetTime.Value, _timing.CurTime);

        if (newScreenValue == 0)
        {
            SetTextToDisplay(ent.Owner, ent.Comp.FinishedText);
            ent.Comp.ScreenValue = 0;
        }
        else
        {
            var newScreenText = GetTimerString(ent, newScreenValue);
            SetTextToDisplay(ent.Owner, newScreenText);
            ent.Comp.ScreenValue = newScreenValue;
        }
    }

    private string GetTimerString(Entity<TextScreenTimerVisualsComponent> ent, int newScreenValue)
    {
        var strings = ent.Comp.RunningText.Split("\n");
        if (ent.Comp.TimerRow >= 0 && ent.Comp.TimerRow < strings.Length)
        {
            strings[ent.Comp.TimerRow] = $"{newScreenValue / 100:D2}:{newScreenValue % 100:D2}";
        }
        return string.Join('\n', strings);
    }

    private void DrawNewText(Entity<TextScreenVisualsComponent> ent)
    {
        // No sprite, put in a default state.
        if (!_spriteQuery.TryComp(ent, out var sprite))
        {
            for (var i = 0; i < ent.Comp.RowData.Length; i++)
            {
                var rowData = ent.Comp.RowData[i];
                rowData.NextScroll = TimeSpan.MaxValue;
                rowData.ScrollDelay = TimeSpan.MaxValue;
                rowData.ScrollPosition = 0;
                rowData.Text = "";

                ent.Comp.RowData[i] = rowData;
            }
            return;
        }

        var texts = ent.Comp.Text?.Split("\n") ?? [];

        for (var i = 0; i < ent.Comp.RowData.Length; i++)
        {
            var rowData = ent.Comp.RowData[i];

            if (i >= texts.Length || texts[i].Length == 0)
            {
                for (var j = 0; j < rowData.Layers.Count; j++)
                {
                    var layerTuple = rowData.Layers[j];

                    if (SpriteSystem.LayerMapTryGet((ent, sprite), layerTuple.Key, out var layerIndex, false))
                        SpriteSystem.LayerSetRsiState((ent, sprite), layerIndex, null);

                    rowData.Layers[j] = new(layerTuple.Key, null);
                }
                rowData.ScrollDelay = TimeSpan.MaxValue;
                rowData.NextScroll = TimeSpan.MaxValue;
                rowData.ScrollPosition = 0;
                rowData.Text = "";
            }
            else
            {
                if (texts[i].Length <= ent.Comp.RowLength)
                {
                    rowData.ScrollDelay = TimeSpan.MaxValue;
                    rowData.NextScroll = TimeSpan.MaxValue;
                    rowData.ScrollPosition = 0;
                    rowData.Text = texts[i];
                }
                else
                {
                    var newMaxScrollTime = MaxMessageScrollTime / texts[i].Length / CharWidth;
                    rowData.ScrollDelay = newMaxScrollTime < MaxPixelScrollTime ? newMaxScrollTime : MaxPixelScrollTime;
                    rowData.NextScroll = ent.Comp.TextTime;
                    rowData.ScrollPosition = 0;
                    var rowText = texts[i].Substring(0, int.Min(texts[i].Length, MaxScrollingCharacters));
                    rowData.Text = rowText.PadRight(rowText.Length + ent.Comp.RowLength - 1);
                    ScrollRow(ref rowData);
                }

                DrawLayers((ent.Owner, ent.Comp, sprite), rowData, i);
            }

            ent.Comp.RowData[i] = rowData;
        }
    }

    private void ScrollRow(ref TextScreenRow rowData)
    {
        var difference = (_timing.CurTime - rowData.NextScroll).TotalSeconds;
        var increments = (int)Math.Truncate(difference / rowData.ScrollDelay.TotalSeconds) + 1;
        rowData.ScrollPosition += increments;
        rowData.NextScroll += increments * rowData.ScrollDelay;
    }

    private void DrawLayers(Entity<TextScreenVisualsComponent, SpriteComponent> ent, TextScreenRow rowData, int rowIndex)
    {
        Entity<SpriteComponent?> sprite = (ent.Owner, ent.Comp2);
        var screen = ent.Comp1;

        // TODO: offset layers, draw states onto their respective layers based on the input string.
        var maxCharIndex = int.Min(rowData.Layers.Count, rowData.Text.Length);
        for (var j = 0; j < maxCharIndex; j++)
        {
            var layerTuple = rowData.Layers[j];
            var charIndex = (j + rowData.ScrollPosition / CharWidth) % rowData.Text.Length;

            var newState = GetStateFromChar(rowData.Text[charIndex]);

            if (SpriteSystem.LayerMapTryGet(sprite, layerTuple.Key, out var layerIndex, false))
            {
                SpriteSystem.LayerSetRsiState(sprite, layerIndex, newState);
                SpriteSystem.LayerSetOffset(sprite, layerIndex, screen.TextOffset + Vector2.Multiply(
                        new Vector2((j - maxCharIndex / 2f + 0.5f) * CharWidth, -rowIndex * screen.RowOffset),
                        TextScreenVisualsComponent.PixelSize));
            }

            rowData.Layers[j] = new(layerTuple.Key, newState);
        }
        for (var j = maxCharIndex; j < rowData.Layers.Count; j++)
        {
            var layerTuple = rowData.Layers[j];

            if (SpriteSystem.LayerMapTryGet((ent, sprite), layerTuple.Key, out var layerIndex, false))
                SpriteSystem.LayerSetRsiState((ent, sprite), layerIndex, null);

            rowData.Layers[j] = new(layerTuple.Key, null);
        }
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
