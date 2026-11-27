using System.Numerics;
using Content.Shared.CCVar;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that a switch icon and a secondary text label both showing the current state
    /// </summary>
    [Virtual]
    public partial class SwitchButton : ContainerButton
    {
        [Dependency] private IConfigurationManager _cfg = default!;

        public const string StyleClassTrackFill = "trackFill";
        public const string StyleClassTrackFillOn = "trackFillOn";
        public const string StyleClassTrackFillOnClip = "trackFillOnClip";
        public const string StyleClassTrackOutline = "trackOutline";
        public const string StyleClassThumbFill = "thumbFill";
        public const string StyleClassThumbOutline = "thumbOutline";
        public const string StyleClassSymbol = "symbol";

        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 0;
        private const float DefaultThumbAnimationDuration = 0.125f;

        /// <summary>
        ///     Current thumb position, from 0 (off) to 1 (on).
        /// </summary>
        private float _thumbPosition;
        private float _thumbStartPosition;
        private float _thumbTargetPosition;
        private float _thumbAnimationTime;
        private bool _thumbPositionInitialized;
        private bool _skipNextThumbAnimation;

        private int ActualSeparation
        {
            get
            {
                if (TryGetStyleProperty(StylePropertySeparation, out int separation))
                {
                    return separation;
                }

                return SeparationOverride ?? DefaultSeparation;
            }
        }

        public int? SeparationOverride { get; set; }
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThumbAnimationDuration { get; set; } = DefaultThumbAnimationDuration;

        public Label Label { get; }
        public Label OffStateLabel { get; }
        public Label OnStateLabel { get; }

        // I tried to find a way not to have five textures here, but the other
        // options were worse.
        public TextureRect TrackFill { get; }
        public TextureRect TrackFillOn { get; }
        public ClipControl TrackFillOnClip { get; }
        public TextureRect TrackOutline { get; }
        public TextureRect ThumbFill { get; }
        public TextureRect ThumbOutline { get; }
        public TextureRect Symbol { get; }

        public SwitchButton()
        {
            IoCManager.InjectDependencies(this);

            ToggleMode = true;

            TrackFill = new TextureRect
            {
                StyleClasses = { StyleClassTrackFill },
                VerticalAlignment = VAlignment.Center,
            };

            TrackFillOn = new TextureRect
            {
                StyleClasses = { StyleClassTrackFillOn },
            };

            // Reveals the "on" fill from the left as the thumb slides across.
            TrackFillOnClip = new ClipControl
            {
                StyleClasses = { StyleClassTrackFillOnClip },
                ClipHorizontal = false,
                ClipVertical = false,
                RectClipContent = true,
            };
            TrackFillOnClip.AddChild(TrackFillOn);

            TrackOutline = new TextureRect
            {
                StyleClasses = { StyleClassTrackOutline },
                VerticalAlignment = VAlignment.Center,
            };

            ThumbFill = new TextureRect
            {
                StyleClasses = { StyleClassThumbFill },
                VerticalAlignment = VAlignment.Center,
            };

            ThumbOutline = new TextureRect
            {
                StyleClasses = { StyleClassThumbOutline },
                VerticalAlignment = VAlignment.Center,
            };

            Symbol = new TextureRect
            {
                StyleClasses = { StyleClassSymbol },
                VerticalAlignment = VAlignment.Center,
            };

            Label = new Label();
            Label.Visible = false;

            OffStateLabel = new Label();
            OffStateLabel.Text = Loc.GetString("toggle-switch-default-off-state-label");
            OffStateLabel.ReservesSpace = true;

            OnStateLabel = new Label();
            OnStateLabel.Text = Loc.GetString("toggle-switch-default-on-state-label");
            OnStateLabel.ReservesSpace = true;
            OnStateLabel.Visible = false;

            Label.HorizontalExpand = true;

            AddChild(Label);
            AddChild(TrackFill);
            AddChild(TrackFillOnClip);
            AddChild(TrackOutline);
            // Symbol sits under the thumb so the thumb slides over it.
            AddChild(Symbol);
            AddChild(ThumbFill);
            AddChild(ThumbOutline);
            AddChild(OffStateLabel);
            AddChild(OnStateLabel);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (MathHelper.CloseTo(_thumbPosition, _thumbTargetPosition))
                return;

            if (ThumbAnimationDuration <= 0f)
            {
                _thumbPosition = _thumbTargetPosition;
                InvalidateArrange();
                return;
            }

            _thumbAnimationTime += args.DeltaSeconds;

            var progress = MathHelper.Clamp01(_thumbAnimationTime / ThumbAnimationDuration);
            // Ease-out cubic.
            var easedProgress = 1f - MathF.Pow(1f - progress, 3f);

            _thumbPosition = progress >= 1f
                ? _thumbTargetPosition
                : MathHelper.Lerp(_thumbStartPosition, _thumbTargetPosition, easedProgress);

            InvalidateArrange();
        }

        protected override void DrawModeChanged()
        {
            // Workaround for child controls not being updated automatically.
            // Remove once https://github.com/space-wizards/RobustToolbox/pull/6264
            // or similar is merged.
            var relevantChangeMade = false;

            if (Disabled)
            {
                if (!HasStylePseudoClass(StylePseudoClassDisabled))
                {
                    AddStylePseudoClass(StylePseudoClassDisabled);
                    relevantChangeMade = true;
                }
            }
            else
            {
                if (HasStylePseudoClass(StylePseudoClassDisabled))
                {
                    RemoveStylePseudoClass(StylePseudoClassDisabled);
                    relevantChangeMade = true;
                }
            }

            if (Pressed)
            {
                if (!HasStylePseudoClass(StylePseudoClassPressed))
                {
                    AddStylePseudoClass(StylePseudoClassPressed);
                    relevantChangeMade = true;
                }
            }
            else
            {
                if (HasStylePseudoClass(StylePseudoClassPressed))
                {
                    RemoveStylePseudoClass(StylePseudoClassPressed);
                    relevantChangeMade = true;
                }
            }

            if (relevantChangeMade)
            {
                foreach (var c in Children)
                {
                    c.AddStyleClass("dummy");
                    c.RemoveStyleClass("dummy");
                }

                // Not a direct child, so the loop above doesn't reach it.
                TrackFillOn.AddStyleClass("dummy");
                TrackFillOn.RemoveStyleClass("dummy");
            }

            // no base.DrawModeChanged() call - ContainerButton's pseudoclass handling
            // doesn't support a button being both pressed and disabled

            UpdateAppearance();
            UpdateThumbTarget();
        }

        /// <summary>
        ///     If true, the button will allow shrinking and clip text of the main
        ///     label to prevent the text from going outside the bounds of the button.
        ///     If false, the minimum size will always fit the contained text.
        /// </summary>
        [ViewVariables]
        public bool ClipText { get => Label.ClipText; set => Label.ClipText = value; }

        /// <summary>
        ///     The text displayed by the button's main label.
        /// </summary>
        [ViewVariables]
        public string? Text
        {
            get => Label.Text;
            set
            {
                Label.Text = value;
                Label.Visible = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        ///     The text displayed by the button's secondary label in the off state.
        /// </summary>
        [ViewVariables]
        public string? OffStateText
        {
            get => OffStateLabel.Text;
            set => OffStateLabel.Text = value;
        }

        /// <summary>
        ///     The text displayed by the button's secondary label in the on state.
        /// </summary>
        [ViewVariables]
        public string? OnStateText
        {
            get => OnStateLabel.Text;
            set => OnStateLabel.Text = value;
        }

        private void UpdateAppearance()
        {
            if (OffStateLabel is not null)
            {
                OffStateLabel.Visible = !Pressed;
            }

            if (OnStateLabel is not null)
            {
                OnStateLabel.Visible = Pressed;
            }
        }

        private void UpdateThumbTarget()
        {
            _thumbTargetPosition = Pressed ? 1f : 0f;

            if (!_thumbPositionInitialized ||
                _skipNextThumbAnimation ||
                _cfg.GetCVar(CCVars.ReducedMotion) ||
                ThumbAnimationDuration <= 0f)
            {
                _thumbPosition = _thumbTargetPosition;
                _thumbStartPosition = _thumbTargetPosition;
                _thumbAnimationTime = 0f;
                _thumbPositionInitialized = true;
                _skipNextThumbAnimation = false;
                InvalidateArrange();
                return;
            }

            _thumbStartPosition = _thumbPosition;
            _thumbAnimationTime = 0f;
            InvalidateArrange();
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();
            UpdateAppearance();
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var desiredSize = Vector2.Zero;
            var separation = ActualSeparation;

            // Start with the icon, since it always appears
            if (TrackOutline is not null)
            {
                TrackOutline.Measure(availableSize);
                desiredSize = TrackOutline.DesiredSize;
            }

            // Add space for the label if it has text
            if (! string.IsNullOrEmpty(Label?.Text))
            {
                Label.Measure(availableSize);
                desiredSize.X += separation + Label.DesiredSize.X;
                desiredSize.Y = float.Max(desiredSize.Y, Label.DesiredSize.Y);
            }

            // Add space for the state labels if at least one of them has text
            var stateLabelSpace = Vector2.Zero;
            if (! string.IsNullOrEmpty(OffStateLabel?.Text))
            {
                OffStateLabel.Measure(availableSize);
                stateLabelSpace = OffStateLabel.DesiredSize;
            }

            if (! string.IsNullOrEmpty(OnStateLabel?.Text))
            {
                OnStateLabel.Measure(availableSize);
                stateLabelSpace.Y = float.Max(stateLabelSpace.Y, OnStateLabel.DesiredSize.Y);
                stateLabelSpace.X = float.Max(stateLabelSpace.X, OnStateLabel.DesiredSize.X);
            }

            if (stateLabelSpace != Vector2.Zero)
            {
                desiredSize.X += separation + stateLabelSpace.X;
                desiredSize.Y = float.Max(desiredSize.Y, stateLabelSpace.Y);
            }

            return desiredSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var separation = ActualSeparation;

            var actualMainLabelWidth = Math.Max(0f, finalSize.X - separation - TrackOutline.DesiredSize.X);
            float iconPosition = 0;
            float stateLabelPosition = 0;

            if (string.IsNullOrEmpty(Label?.Text))
            {
                stateLabelPosition = TrackOutline.DesiredSize.X + separation;
            }
            else
            {
                if (!string.IsNullOrEmpty(OffStateLabel?.Text) || !string.IsNullOrEmpty(OnStateLabel?.Text))
                {
                    var stateLabelsWidth = float.Max(OffStateLabel!.DesiredSize.X, OnStateLabel.DesiredSize.X);
                    actualMainLabelWidth -= (separation + stateLabelsWidth);
                }
                actualMainLabelWidth = float.Max(actualMainLabelWidth, 0);
                iconPosition = actualMainLabelWidth + separation;
                stateLabelPosition = iconPosition + TrackOutline.DesiredSize.X + separation;
            }

            var mainLabelTargetBox = new UIBox2(0, 0, actualMainLabelWidth, finalSize.Y);
            Label?.Arrange(mainLabelTargetBox);

            var iconTargetBox = new UIBox2(iconPosition, 0, iconPosition + TrackOutline.DesiredSize.X, finalSize.Y);
            TrackFill.Arrange(iconTargetBox);
            TrackOutline.Arrange(iconTargetBox);
            Symbol.Arrange(iconTargetBox);

            // The clip places the fill at its top-left, so centre the clip box vertically
            // to line up with the centred TrackFill.
            TrackFillOnClip.Measure(TrackOutline.DesiredSize);
            var onFillHeight = TrackFillOnClip.DesiredSize.Y;
            var onFillTop = (finalSize.Y - onFillHeight) / 2f;
            var onFillWidth = TrackOutline.DesiredSize.X * _thumbPosition;
            TrackFillOnClip.Arrange(new UIBox2(iconPosition, onFillTop, iconPosition + onFillWidth, onFillTop + onFillHeight));

            ThumbOutline.Measure(TrackOutline.DesiredSize); // didn't measure in MeasureOverride, don't need its size there
            var thumbOffLeft = iconTargetBox.Left;
            var thumbOnLeft = iconTargetBox.Right - ThumbOutline.DesiredSize.X;
            var thumbLeft = MathHelper.Lerp(thumbOffLeft, thumbOnLeft, _thumbPosition);
            var thumbTargetBox = new UIBox2(thumbLeft, 0, thumbLeft + ThumbOutline.DesiredSize.X, finalSize.Y);
            ThumbFill.Arrange(thumbTargetBox);
            ThumbOutline.Arrange(thumbTargetBox);

            var stateLabelsTargetBox = new UIBox2(Math.Min(finalSize.X, stateLabelPosition), 0, finalSize.X, finalSize.Y);
            OffStateLabel?.Arrange(stateLabelsTargetBox);
            OnStateLabel?.Arrange(stateLabelsTargetBox);

            return finalSize;
        }
    }
}
