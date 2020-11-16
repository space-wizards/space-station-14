#if DEBUG
#nullable enable
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Development
{

    public sealed class SimpleShaderDevWindow : ShaderDevWindowBase
    {

        public ColorRect FgColorRect { get; }

        public readonly ColorSlider FgColorRedSlider;

        public readonly ColorSlider FgColorGreenSlider;

        public readonly ColorSlider FgColorBlueSlider;

        protected override void OnShaderChanged()
        {
            FgColorRect.Shader = ShaderInstance;
        }

        protected override Vector2? CustomSize => new Vector2(384, 640);

        public SimpleShaderDevWindow()
        {
            FgColorRect = new ColorRect
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
            };

            BgColorRect.AddChild(FgColorRect);

            FgColorRect.Color = new Color(0, 0, 0);

            FgColorRedSlider = new ColorSlider(StyleNano.StyleClassSliderRed);
            FgColorGreenSlider = new ColorSlider(StyleNano.StyleClassSliderGreen);
            FgColorBlueSlider = new ColorSlider(StyleNano.StyleClassSliderBlue);

            FgColorRedSlider.OnValueChanged += OnFgColorSliderChanged;
            FgColorGreenSlider.OnValueChanged += OnFgColorSliderChanged;
            FgColorBlueSlider.OnValueChanged += OnFgColorSliderChanged;

            ControlsContainer.AddChild(new VBoxContainer
            {
                Children =
                {
                    new Label {Text = "Foreground Color: "},
                    new VBoxContainer
                    {
                        Children =
                        {
                            FgColorRedSlider,
                            FgColorGreenSlider,
                            FgColorBlueSlider,
                        },
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                    }
                },
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
            });
        }

        private void OnFgColorSliderChanged()
        {
            OnColorSliderChanged(FgColorRect, FgColorRedSlider, FgColorGreenSlider, FgColorBlueSlider);
        }

        public SimpleShaderDevWindow(string shaderName)
            : this()
        {
            ShaderName = shaderName;
            Title = "Shader Dev: " + (ShaderName != null ? ("'" + ShaderName + "'") : "NULL");
        }

    }

}
#endif
