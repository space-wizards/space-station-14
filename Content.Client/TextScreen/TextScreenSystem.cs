using System.Linq;
using System.Numerics;
using Content.Shared.TextScreen;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.TextScreen;

/// overview:
/// Data is passed from server to client through <see cref="SharedAppearanceSystem.SetData"/>,
/// calling <see cref="OnAppearanceChange"/>, which calls almost everything else.

/// Data for the (at most one) timer is stored in <see cref="TextScreenTimerComponent"/>.

/// All screens have <see cref="TextScreenVisualsComponent"/>, but:
/// the update method only updates the timers, so the timercomp is added/removed by appearance changes/timing out.

/// Because the sprite component stores layers in a dict with no nesting, individual layers
/// have to be mapped to unique ids e.g. {"textMapKey01" : <first row, second char layerstate>}
/// in either the visuals or timer component.


/// <summary>
///     The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
/// </summary>
public sealed class TextScreenSystem : VisualizerSystem<TextScreenVisualsComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    ///     Contains char/state Key/Value pairs. <br/>
    ///     The states in Textures/Effects/text.rsi that special character should be replaced with.
    /// </summary>
    private static readonly Dictionary<char, string> CharStatePairs = new()
        {
            { ':', "colon" },
            { '!', "exclamation" },
            { '?', "question" },
            { '*', "star" },
            { '+', "plus" },
            { '-', "dash" },
            { ' ', "blank" }
        };

    private const string DefaultState = "blank";

    /// <summary>
    ///     A string prefix for all text layers.
    /// </summary>
    private const string TextMapKey = "textMapKey";
    /// <summary>
    ///     A string prefix for all timer layers.
    /// </summary>
    private const string TimerMapKey = "timerMapKey";
    private const string TextPath = "Effects/text.rsi";
    private const int CharWidth = 4;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TextScreenVisualsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TextScreenTimerComponent, ComponentInit>(OnTimerInit);

        UpdatesOutsidePrediction = true;
    }

    private void OnInit(EntityUid uid, TextScreenVisualsComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        // awkward to specify a textoffset of e.g. 0.1875 in the prototype
        component.TextOffset = Vector2.Multiply(TextScreenVisualsComponent.PixelSize, component.TextOffset);
        component.TimerOffset = Vector2.Multiply(TextScreenVisualsComponent.PixelSize, component.TimerOffset);

        ResetText(uid, component, sprite);
        BuildTextLayers(uid, component, sprite);
    }

    /// <summary>
    ///     Instantiates <see cref="SpriteComponent.Layers"/> with {<see cref="TimerMapKey"/> + int : <see cref="DefaultState"/>} pairs.
    /// </summary>
    private void OnTimerInit(EntityUid uid, TextScreenTimerComponent timer, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<TextScreenVisualsComponent>(uid, out var screen))
            return;

        for (var i = 0; i < screen.RowLength; i++)
        {
            SpriteSystem.LayerMapReserve((uid, sprite), TimerMapKey + i);
            timer.LayerStatesToDraw.Add(TimerMapKey + i, null);
            SpriteSystem.LayerSetRsi((uid, sprite), TimerMapKey + i, new ResPath(TextPath));
            SpriteSystem.LayerSetColor((uid, sprite), TimerMapKey + i, screen.Color);
            SpriteSystem.LayerSetRsiState((uid, sprite), TimerMapKey + i, DefaultState);
        }
    }

    /// <summary>
    ///     Called by <see cref="SharedAppearanceSystem.SetData"/> to handle text updates,
    ///     and spawn a <see cref="TextScreenTimerComponent"/> if necessary
    /// </summary>
    /// <remarks>
    ///     The appearance updates are batched; order matters for both sender and receiver.
    /// </remarks>
    protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref args.Sprite))
            return;

        if (args.AppearanceData.TryGetValue(TextScreenVisuals.Color, out var color) && color is Color)
            component.Color = (Color)color;

        // DefaultText: fallback text e.g. broadcast updates from comms consoles
        if (args.AppearanceData.TryGetValue(TextScreenVisuals.DefaultText, out var newDefault) && newDefault is string)
            component.Text = SegmentText((string)newDefault, component);

        // ScreenText: currently rendered text e.g. the "ETA" accompanying shuttle timers
        if (args.AppearanceData.TryGetValue(TextScreenVisuals.ScreenText, out var text) && text is string)
        {
            component.TextToDraw = SegmentText((string)text, component);
            ResetText(uid, component);
            BuildTextLayers(uid, component, args.Sprite);
            DrawLayers(uid, component.LayerStatesToDraw);
        }

        if (args.AppearanceData.TryGetValue(TextScreenVisuals.TargetTime, out var time) && time is TimeSpan target)
        {
            if (target > _gameTiming.CurTime)
            {
                var timer = EnsureComp<TextScreenTimerComponent>(uid);
                timer.Target = target;
                BuildTimerLayers(uid, timer, component);
                DrawLayers(uid, timer.LayerStatesToDraw);
            }
            else
            {
                OnTimerFinish(uid, component);
            }
        }
    }

    /// <summary>
    ///     Removes the timer component, clears the sprite layer dict,
    ///     and draws <see cref="TextScreenVisualsComponent.Text"/>
    /// </summary>
    private void OnTimerFinish(EntityUid uid, TextScreenVisualsComponent screen)
    {
        screen.TextToDraw = screen.Text;

        if (!TryComp<TextScreenTimerComponent>(uid, out var timer) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var key in timer.LayerStatesToDraw.Keys)
            SpriteSystem.RemoveLayer((uid, sprite), key);

        RemComp<TextScreenTimerComponent>(uid);

        ResetText(uid, screen);
        BuildTextLayers(uid, screen, sprite);
        DrawLayers(uid, screen.LayerStatesToDraw);
    }

    /// <summary>
    ///     Converts string to string?[] based on
    ///     <see cref="TextScreenVisualsComponent.RowLength"/> and <see cref="TextScreenVisualsComponent.Rows"/>.
    /// </summary>
    private string?[] SegmentText(string text, TextScreenVisualsComponent component)
    {
        int segment = component.RowLength;
        var segmented = new string?[Math.Min(component.Rows, (text.Length - 1) / segment + 1)];

        // populate segmented with a string sliding window using Substring.
        // (Substring(5, 5) will return the 5 characters starting from 5th index)
        // the Mins are for the very short string case, the very long string case, and to not OOB the end of the string.
        for (int i = 0; i < Math.Min(text.Length, segment * component.Rows); i += segment)
            segmented[i / segment] = text.Substring(i, Math.Min(text.Length - i, segment)).Trim();

        return segmented;
    }

    /// <summary>
    ///     Clears <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/>, and instantiates new blank defaults.
    /// </summary>
    private void ResetText(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        foreach (var key in component.LayerStatesToDraw.Keys)
            SpriteSystem.RemoveLayer((uid, sprite), key);

        component.LayerStatesToDraw.Clear();

        for (var row = 0; row < component.Rows; row++)
            for (var i = 0; i < component.RowLength; i++)
            {
                var key = TextMapKey + row + i;
                SpriteSystem.LayerMapReserve((uid, sprite), key);
                component.LayerStatesToDraw.Add(key, null);
                SpriteSystem.LayerSetRsi((uid, sprite), key, new ResPath(TextPath));
                SpriteSystem.LayerSetColor((uid, sprite), key, component.Color);
                SpriteSystem.LayerSetRsiState((uid, sprite), key, DefaultState);
            }
    }

    /// <summary>
    ///     Sets the states in the <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/> to match the component
    ///     <see cref="TextScreenVisualsComponent.TextToDraw"/> string?[].
    /// </summary>
    /// <remarks>
    ///     Remember to set <see cref="TextScreenVisualsComponent.TextToDraw"/> to a string?[] first.
    /// </remarks>
    private void BuildTextLayers(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        for (var rowIdx = 0; rowIdx < Math.Min(component.TextToDraw.Length, component.Rows); rowIdx++)
        {
            var row = component.TextToDraw[rowIdx];
            if (row == null)
                continue;
            var min = Math.Min(row.Length, component.RowLength);

            for (var chr = 0; chr < min; chr++)
            {
                component.LayerStatesToDraw[TextMapKey + rowIdx + chr] = GetStateFromChar(row[chr]);
                SpriteSystem.LayerSetOffset(
                    (uid, sprite),
                    TextMapKey + rowIdx + chr,
                    Vector2.Multiply(
                        new Vector2((chr - min / 2f + 0.5f) * CharWidth, -rowIdx * component.RowOffset),
                        TextScreenVisualsComponent.PixelSize
                        ) + component.TextOffset
                );
            }
        }
    }

    /// <summary>
    ///     Populates timer.LayerStatesToDraw & the sprite component's layer dict with calculated offsets.
    /// </summary>
    private void BuildTimerLayers(EntityUid uid, TextScreenTimerComponent timer, TextScreenVisualsComponent screen)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var time = TimeToString(
            (_gameTiming.CurTime - timer.Target).Duration(),
            false,
            screen.HourFormat, screen.MinuteFormat, screen.SecondFormat
            );

        var min = Math.Min(time.Length, screen.RowLength);

        for (var i = 0; i < min; i++)
        {
            timer.LayerStatesToDraw[TimerMapKey + i] = GetStateFromChar(time[i]);
            SpriteSystem.LayerSetOffset(
                (uid, sprite),
                TimerMapKey + i,
                Vector2.Multiply(
                    new Vector2((i - min / 2f + 0.5f) * CharWidth, 0f),
                    TextScreenVisualsComponent.PixelSize
                    ) + screen.TimerOffset
            );
        }
    }

    /// <summary>
    ///     Draws a LayerStates dict by setting the sprite states individually.
    /// </summary>
    private void DrawLayers(EntityUid uid, Dictionary<string, string?> layerStates, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        foreach (var (key, state) in layerStates.Where(pairs => pairs.Value != null))
            SpriteSystem.LayerSetRsiState((uid, sprite), key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TextScreenTimerComponent, TextScreenVisualsComponent>();
        while (query.MoveNext(out var uid, out var timer, out var screen))
        {
            if (timer.Target < _gameTiming.CurTime)
            {
                OnTimerFinish(uid, screen);
                continue;
            }

            BuildTimerLayers(uid, timer, screen);
            DrawLayers(uid, timer.LayerStatesToDraw);
        }
    }

    /// <summary>
    ///     Returns the <paramref name="timeSpan"/> converted to a string in either HH:MM, MM:SS or potentially SS:mm format.
    /// </summary>
    /// <param name="timeSpan">TimeSpan to convert into string.</param>
    /// <param name="getMilliseconds">Should the string be ss:ms if minutes are less than 1?</param>
    /// <remarks>
    ///     hours, minutes, seconds, and centiseconds are each set to 2 decimal places by default.
    /// </remarks>
    public static string TimeToString(TimeSpan timeSpan, bool getMilliseconds = true, string hours = "D2", string minutes = "D2", string seconds = "D2", string cs = "D2")
    {
        string firstString;
        string lastString;

        if (timeSpan.TotalHours >= 1)
        {
            firstString = timeSpan.Hours.ToString(hours);
            lastString = timeSpan.Minutes.ToString(minutes);
        }
        else if (timeSpan.TotalMinutes >= 1 || !getMilliseconds)
        {
            firstString = timeSpan.Minutes.ToString(minutes);
            lastString = timeSpan.Seconds.ToString(seconds);
        }
        else
        {
            firstString = timeSpan.Seconds.ToString(seconds);
            var centiseconds = timeSpan.Milliseconds / 10;
            lastString = centiseconds.ToString(cs);
        }

        return firstString + ':' + lastString;
    }

    /// <summary>
    ///     Returns the Effects/text.rsi state string based on <paramref name="character"/>, or null if none available.
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
}
