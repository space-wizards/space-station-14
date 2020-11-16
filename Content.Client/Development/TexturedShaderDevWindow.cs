#if DEBUG
#nullable enable
using System;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Development
{

    public sealed class TexturedShaderDevWindow : ShaderDevWindowBase
    {

        public TextureRect BgTextureRect { get; }

        public TextureRect FgTextureRect { get; }

        protected override void OnShaderChanged()
        {
            FgTextureRect.Shader = ShaderInstance;
        }

        protected override Vector2? CustomSize => new Vector2(384, 640);

        public TexturedShaderDevWindow() : base()
        {
            FgTextureRect = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                Stretch = TextureRect.StretchMode.KeepAspect
            };

            BgTextureRect = new TextureRect
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                Stretch = TextureRect.StretchMode.KeepAspect,
                Children = {FgTextureRect}
            };

            BgColorRect.Children.Add(BgTextureRect);

            var bgTextureSpec = new LineEdit {SizeFlagsHorizontal = SizeFlags.FillExpand};
            var bgTextureLoadBtn = new Button {Text = "Load"};
            bgTextureLoadBtn.OnPressed += args =>
            {
                OnTextureLoadPressed(bgTextureSpec, bgTextureLoadBtn, BgTextureRect);
            };
            bgTextureSpec.Text = "/Textures/default_shader_dev_background.png";
            OnTextureLoadPressed(bgTextureSpec, bgTextureLoadBtn, BgTextureRect);

            var fgTextureSpec = new LineEdit {SizeFlagsHorizontal = SizeFlags.FillExpand};
            var fgTextureLoadBtn = new Button {Text = "Load"};
            fgTextureLoadBtn.OnPressed += args =>
            {
                OnTextureLoadPressed(fgTextureSpec, fgTextureLoadBtn, FgTextureRect);
            };
            fgTextureSpec.Text = "/Textures/default_shader_dev_texture.png";
            OnTextureLoadPressed(fgTextureSpec, fgTextureLoadBtn, FgTextureRect);

            ControlsContainer.AddChild(new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            new Label {Text = "Bg Tex: ", ToolTip = "Background Texture"},
                            bgTextureSpec,
                            bgTextureLoadBtn,
                        }
                    },
                    new HBoxContainer
                    {
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            new Label {Text = "Fg Tex: ", ToolTip = "Foreground Texture"},
                            fgTextureSpec,
                            fgTextureLoadBtn,
                        }
                    },
                }
            });
        }

        private void OnTextureLoadPressed(LineEdit pathEditor, Button loadBtn, TextureRect texRect)
        {
            var path = pathEditor.Text;
            texRect.ToolTip = null;
            pathEditor.ToolTip = null;
            loadBtn.ToolTip = null;

            if (string.IsNullOrEmpty(path))
            {
                texRect.Texture = Texture.Transparent;
                return;
            }

            try
            {
                texRect.Texture = Resources.GetResource<TextureResource>(path, false).Texture;
            }
            catch (Exception ex)
            {
                texRect.Texture = Resources.GetFallback<TextureResource>();

                texRect.ToolTip = ex.Message;
                pathEditor.ToolTip = ex.Message;
                loadBtn.ToolTip = ex.Message;
            }
        }

        public TexturedShaderDevWindow(string shaderName)
            : this()
        {
            ShaderName = shaderName;
            Title = "Shader Dev: " + (ShaderName != null ? ("'" + ShaderName + "'") : "NULL");
        }

    }

}
#endif
