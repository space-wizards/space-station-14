using Content.Shared.TextScreen;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.TextScreen
{
    /// <summary>
    ///     The TextScreenSystem draws text in the game world using 3x5 sprite states for each character.
    /// </summary>
    public sealed partial class TextScreenSystem : VisualizerSystem<TextScreenVisualsComponent>
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
        private const string TextScreenLayerMapKey = "textScreenLayerMapKey";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TextScreenVisualsComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, TextScreenVisualsComponent component, ComponentInit args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            ResetTextLength(component, sprite);
            PrepareLayerStatesToDraw(component, sprite);
            UpdateLayersToDraw(component, sprite);
        }

        /// <summary>
        ///     Resets all TextScreenComponent sprite layers, through removing them and then creating new ones.
        /// </summary>
        public void ResetTextLength(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            foreach (var (key, _) in component.LayerStatesToDraw)
                sprite.RemoveLayer(key);

            component.LayerStatesToDraw.Clear();

            var length = component.TextLength;
            component.TextLength = 0;
            SetTextLength(component, length, sprite);
        }

        /// <summary>
        ///     Sets <see cref="TextScreenVisualsComponent.TextLength"/>, adding or removing sprite layers if necessary.
        /// </summary>
        public void SetTextLength(TextScreenVisualsComponent component, int newLength, SpriteComponent? sprite = null)
        {
            if (newLength == component.TextLength)
                return;

            if (!Resolve(component.Owner, ref sprite))
                return;

            if (newLength > component.TextLength)
            {
                for (var i = component.TextLength; i < newLength; i++)
                {
                    sprite.LayerMapReserveBlank(TextScreenLayerMapKey + i);
                    component.LayerStatesToDraw.Add(TextScreenLayerMapKey + i, null);
                    sprite.LayerSetRSI(TextScreenLayerMapKey + i, new ResourcePath("Effects/text.rsi"));
                    sprite.LayerSetColor(TextScreenLayerMapKey + i, component.Color);
                    sprite.LayerSetState(TextScreenLayerMapKey + i, DefaultState);
                }
            }
            else
            {
                for (var i = component.TextLength; i > newLength; i--)
                {
                    sprite.LayerMapGet(TextScreenLayerMapKey + (i - 1));
                    component.LayerStatesToDraw.Remove(TextScreenLayerMapKey + (i - 1));
                    sprite.RemoveLayer(TextScreenLayerMapKey + (i - 1));
                }
            }

            UpdateOffsets(component, sprite);

            component.TextLength = newLength;
        }

        /// <summary>
        ///     Updates the layers offsets based on the text length, so it is drawn correctly.
        /// </summary>
        public void UpdateOffsets(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            for (var i = 0; i < component.LayerStatesToDraw.Count; i++)
            {
                var offset = i - (component.LayerStatesToDraw.Count - 1) / 2.0f;
                sprite.LayerSetOffset(TextScreenLayerMapKey + i, new Vector2(offset * TextScreenVisualsComponent.PixelSize * 4.0f, 0.0f) + component.TextOffset);
            }
        }

        protected override void OnAppearanceChange(EntityUid uid, TextScreenVisualsComponent component, ref AppearanceChangeEvent args)
        {
            UpdateAppearance(component, args.Component, args.Sprite);
        }

        public void UpdateAppearance(TextScreenVisualsComponent component, AppearanceComponent? appearance = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref appearance, ref sprite))
                return;

            if (appearance.TryGetData(TextScreenVisuals.On, out bool on))
            {
                component.Activated = on;
                UpdateVisibility(component, sprite);
            }

            if (appearance.TryGetData(TextScreenVisuals.Mode, out TextScreenMode mode))
            {
                component.CurrentMode = mode;
                UpdateText(component);
            }

            if (appearance.TryGetData(TextScreenVisuals.TargetTime, out TimeSpan time))
            {
                component.TargetTime = time;
            }

            if (appearance.TryGetData(TextScreenVisuals.ScreenText, out string text))
            {
                component.Text = text;
                UpdateText(component);
            }

            PrepareLayerStatesToDraw(component, sprite);
            UpdateLayersToDraw(component, sprite);
        }

        /// <summary>
        ///     If currently in <see cref="TextScreenMode.Text"/> mode: <br/>
        ///     Sets <see cref="TextScreenVisualsComponent.TextToDraw"/> to the value of <see cref="TextScreenVisualsComponent.Text"/>
        /// </summary>
        public static void UpdateText(TextScreenVisualsComponent component)
        {
            if (component.CurrentMode == TextScreenMode.Text)
                component.TextToDraw = component.Text;
        }

        /// <summary>
        ///     Sets visibility of text to <see cref="TextScreenVisualsComponent.Activated"/>.
        /// </summary>
        public void UpdateVisibility(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            foreach (var (key, _) in component.LayerStatesToDraw)
            {
                sprite.LayerSetVisible(key, component.Activated);
            }
        }

        /// <summary>
        ///     Sets the states in the <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/> to match the component <see cref="TextScreenVisualsComponent.TextToDraw"/> string.
        /// </summary>
        /// <remarks>
        ///     Remember to set <see cref="TextScreenVisualsComponent.TextToDraw"/> to a string first.
        /// </remarks>
        public void PrepareLayerStatesToDraw(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            for (var i = 0; (i < component.TextLength); i++)
            {
                if (i >= component.TextToDraw.Length)
                {
                    component.LayerStatesToDraw[TextScreenLayerMapKey + i] = DefaultState;
                    continue;
                }
                component.LayerStatesToDraw[TextScreenLayerMapKey + i] = GetStateFromChar(component.TextToDraw[i]);
            }
        }

        /// <summary>
        ///     Iterates through <see cref="TextScreenVisualsComponent.LayerStatesToDraw"/>, setting sprite states to the appropriate layers.
        /// </summary>
        public void UpdateLayersToDraw(TextScreenVisualsComponent component, SpriteComponent? sprite = null)
        {
            if (!Resolve(component.Owner, ref sprite))
                return;

            foreach (var (key, state) in component.LayerStatesToDraw)
            {
                if (state == null)
                    continue;
                sprite.LayerSetState(key, state);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in EntityQuery<TextScreenVisualsComponent>())
            {
                // If this is in timing mode, update it regularly.
                if (comp.CurrentMode == TextScreenMode.Timer)
                {
                    // Basically Abs(TimeSpan, TimeSpan) -> Gives the difference between the current time and the target time.
                    var timeToShow = _gameTiming.CurTime > comp.TargetTime ? _gameTiming.CurTime - comp.TargetTime : comp.TargetTime - _gameTiming.CurTime;
                    comp.TextToDraw = TimeToString(timeToShow, false);
                    PrepareLayerStatesToDraw(comp);
                    UpdateLayersToDraw(comp);
                }
            }
        }

        /// <summary>
        ///     Returns the <paramref name="timeSpan"/> converted to a string in either HH:MM, MM:SS or potentially SS:mm format.
        /// </summary>
        /// <param name="timeSpan">TimeSpan to convert into string.</param>
        /// <param name="getMilliseconds">Should the string be ss:ms if minutes are less than 1?</param>
        public static string TimeToString(TimeSpan timeSpan, bool getMilliseconds = true)
        {
            string firstString;
            string lastString;

            if (timeSpan.TotalHours >= 1)
            {
                firstString = timeSpan.Hours.ToString("D2");
                lastString = timeSpan.Minutes.ToString("D2");
            }
            else if (timeSpan.TotalMinutes >= 1 || !getMilliseconds)
            {
                firstString = timeSpan.Minutes.ToString("D2");
                // It's nicer to see a timer set at 5 seconds actually start at 00:05 instead of 00:04.
                var seconds = timeSpan.Seconds + (timeSpan.Milliseconds > 500 ? 1 : 0);
                lastString = seconds.ToString("D2");
            }
            else
            {
                firstString = timeSpan.Seconds.ToString("D2");
                var centiseconds = timeSpan.Milliseconds / 10;
                lastString = centiseconds.ToString("D2");
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
}
