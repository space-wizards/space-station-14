using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that a switch icon and a secondary text label both showing the current state
    /// </summary>
    [Virtual]
    public class SwitchButton : ContainerButton
    {
        public const string StyleClassTrackFill = "trackFill";
        public const string StyleClassTrackOutline = "trackOutline";
        public const string StyleClassThumbFill = "thumbFill";
        public const string StyleClassThumbOutline = "thumbOutline";
        public const string StyleClassSymbol = "symbol";

        public const string StylePropertySeparation = "separation";

        private const int DefaultSeparation = 0;

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
        public Label Label { get; }
        public Label OffStateLabel { get; }
        public Label OnStateLabel { get; }

        // I tried to find a way not to have five textures here, but the other
        // options were worse.
        public TextureRect TrackFill { get; }
        public TextureRect TrackOutline { get; }
        public TextureRect ThumbFill { get; }
        public TextureRect ThumbOutline { get; }
        public TextureRect Symbol { get; }

        public SwitchButton()
        {
            ToggleMode = true;

            TrackFill = new TextureRect
            {
                StyleClasses = { StyleClassTrackFill },
                VerticalAlignment = VAlignment.Center,
            };

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
            AddChild(TrackOutline);
            AddChild(ThumbFill);
            AddChild(ThumbOutline);
            AddChild(Symbol);
            AddChild(OffStateLabel);
            AddChild(OnStateLabel);
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
                Label.RemoveStyleClass("dummy");
                TrackFill.RemoveStyleClass("dummy");
                TrackOutline.RemoveStyleClass("dummy");
                ThumbFill.RemoveStyleClass("dummy");
                ThumbOutline.RemoveStyleClass("dummy");
                Symbol.RemoveStyleClass("dummy");
                OffStateLabel.RemoveStyleClass("dummy");
                OnStateLabel.RemoveStyleClass("dummy");
            }

            // no base.DrawModeChanged() call - ContainerButton's pseudoclass handling
            // doesn't support a button being both pressed and disabled

            UpdateAppearance();
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

            var actualMainLabelWidth = finalSize.X - separation - TrackOutline.DesiredSize.X;
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

            ThumbOutline.Measure(TrackOutline.DesiredSize); // didn't measure in MeasureOverride, don't need its size there
            var thumbLeft = iconTargetBox.Left;
            if (Pressed)
                thumbLeft = iconTargetBox.Right - ThumbOutline.DesiredSize.X;
            var thumbTargetBox = new UIBox2(thumbLeft, 0, thumbLeft + ThumbOutline.DesiredSize.X, finalSize.Y);
            ThumbFill.Arrange(thumbTargetBox);
            ThumbOutline.Arrange(thumbTargetBox);

            var stateLabelsTargetBox = new UIBox2(stateLabelPosition, 0, finalSize.X, finalSize.Y);
            OffStateLabel?.Arrange(stateLabelsTargetBox);
            OnStateLabel?.Arrange(stateLabelsTargetBox);

            return finalSize;
        }
    }
}
