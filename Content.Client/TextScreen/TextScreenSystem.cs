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
            sprite.LayerMapReserveBlank(TimerMapKey + i);
            timer.LayerStatesToDraw.Add(TimerMapKey + i, null);
            sprite.LayerSetRSI(TimerMapKey + i, new ResPath(TextPath));
            sprite.LayerSetColor(TimerMapKey + i, screen.Color);
            sprite.LayerSetState(TimerMapKey + i, DefaultState);
        }
    }

    /// <summary>
    ///     Called by <see cref="SharedAppearanceSystem.SetData"/> to handle text updates,
    ///     and spawn a <see cref="TextScreenTimerComponent"/> if necessary
    /// </summary>
    protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!Resolve(uid, ref args.Sprite))
            return;

        var appearance = args.Component;

        if (AppearanceSystem.TryGetData(uid, TextScreenVisuals.TargetTime, out TimeSpan time, appearance))
        {
            if (time > _gameTiming.CurTime)
            {
                var timer = EnsureComp<TextScreenTimerComponent>(uid);
                timer.Target = time;
                BuildTimerLayers(uid, timer, component);
                DrawLayers(uid, timer.LayerStatesToDraw);
            }
            else
            {
                OnTimerFinish(uid, component);
            }
        }

        if (AppearanceSystem.TryGetData(uid, TextScreenVisuals.ScreenText, out string?[] text, appearance))
        {
            component.TextToDraw = text;
            ResetText(uid, component);
            BuildTextLayers(uid, component, args.Sprite);
            DrawLayers(uid, component.LayerStatesToDraw);
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
            sprite.RemoveLayer(key);

        RemComp<TextScreenTimerComponent>(uid);

        ResetText(uid, screen);
        BuildTextLayers(uid, screen, sprite);
        DrawLayers(uid, screen.LayerStatesToDraw);
    }

    /// <summary>
    ///     Clears <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/>, and instantiates new blank defaults.
    /// </summary>
    public void ResetText(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        foreach (var key in component.LayerStatesToDraw.Keys)
            sprite.RemoveLayer(key);

        component.LayerStatesToDraw.Clear();

        for (var row = 0; row < component.Rows; row++)
            for (var i = 0; i < component.RowLength; i++)
            {
                sprite.LayerMapReserveBlank(TextMapKey + row + i);
                component.LayerStatesToDraw.Add(TextMapKey + row + i, null);
                sprite.LayerSetRSI(TextMapKey + row + i, new ResPath(TextPath));
                sprite.LayerSetColor(TextMapKey + row + i, component.Color);
                sprite.LayerSetState(TextMapKey + row + i, DefaultState);
            }
    }

    /// <summary>
    ///     Sets the states in the <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/> to match the component
    ///     <see cref="TextScreenVisualsComponent.TextToDraw"/> string?[].
    /// </summary>
    /// <remarks>
    ///     Remember to set <see cref="TextScreenVisualsComponent.TextToDraw"/> to a string?[] first.
    /// </remarks>
    public void BuildTextLayers(EntityUid uid, TextScreenVisualsComponent component, SpriteComponent? sprite = null)
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
                sprite.LayerSetOffset(
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
    public void BuildTimerLayers(EntityUid uid, TextScreenTimerComponent timer, TextScreenVisualsComponent screen)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        string time = TimeToString(
            (_gameTiming.CurTime - timer.Target).Duration(),
            false,
            screen.HourFormat, screen.MinuteFormat, screen.SecondFormat
            );

        int min = Math.Min(time.Length, screen.RowLength);

        for (int i = 0; i < min; i++)
        {
            timer.LayerStatesToDraw[TimerMapKey + i] = GetStateFromChar(time[i]);
            sprite.LayerSetOffset(
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
            sprite.LayerSetState(key, state);
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
        if (CharStatePairs.ContainsKey(character.Value))
            return CharStatePairs[character.Value];

        // Or else it checks if its a normal letter or digit
        if (char.IsLetterOrDigit(character.Value))
            return character.Value.ToString().ToLower();

        return null;
    }
}
