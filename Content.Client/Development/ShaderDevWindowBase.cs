#if DEBUG
#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq.Expressions;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using StringToExpression.LanguageDefinitions;
using YamlDotNet.RepresentationModel;

namespace Content.Client.Development
{

    public abstract class ShaderDevWindowBase
        : SS14Window
    {

        [Dependency]
        protected readonly IResourceCache Resources = default!;

        [Dependency]
        protected readonly IPrototypeManager PrototypeManager = default!;

        protected ShaderInstance? ShaderInstance;

        private string? _shaderName;

        protected ShaderPrototype? ShaderPrototype;

        public ConcurrentDictionary<string, object> Parameters
            = new ConcurrentDictionary<string, object>();

        protected ColorSlider BgColorRedSlider;

        protected ColorSlider BgColorGreenSlider;

        protected ColorSlider BgColorBlueSlider;

        protected VBoxContainer ParamsContainer { get; }

        protected VBoxContainer ControlsContainer { get; set; }

        public ColorRect BgColorRect { get; protected set; }

        public string? ShaderName
        {
            get => _shaderName;
            set
            {
                if (value == _shaderName) return;

                _shaderName = value;
                ShaderInstance?.Dispose();
                if (ShaderName != null)
                {
                    if (PrototypeManager.TryIndex<ShaderPrototype>(ShaderName, out ShaderPrototype))
                    {
                        var parameters = ShaderPrototype.Parameters;
                        if (parameters != null)
                        {
                            foreach (var (k, v) in parameters)
                            {
                                Parameters[k] = v;
                            }
                        }

                        ShaderInstance = ShaderPrototype.InstanceUnique();
                        ShaderPrototype.ExportUniforms(ShaderPrototype, Parameters);
                        OnShaderChanged();
                        RebuildParameterControls();
                        SetupShaderParameters();
                        return;
                    }
                }

                ShaderInstance = null;
                Parameters.Clear();
                ParamsContainer.RemoveAllChildren();
                OnShaderChanged();
            }
        }

        protected abstract void OnShaderChanged();

        public void ParseParameters(string parameters)
        {
            if (ShaderPrototype == null)
            {
                throw new InvalidOperationException("No shader prototype loaded.");
            }

            var yaml = new YamlStream();

            yaml.Load(new StringReader(parameters));

            var doc = yaml.Documents[0];

            var root = doc.RootNode;

            if (!(root is YamlMappingNode paramMapping))
            {
                throw new InvalidOperationException("Invalid yaml expression.");
            }

            ShaderPrototype.ParseMapping(ShaderPrototype, paramMapping, Parameters);

            RebuildParameterControls();

            SetupShaderParameters();
        }

        protected void RebuildParameterControls()
        {
            ParamsContainer.RemoveAllChildren();

            foreach (var (name, value) in Parameters)
            {
                var editor = new LineEdit
                {
                    Text = value?.ToString() ?? "",
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                };

                editor.OnTextChanged += args =>
                {
                    Parameters[name] = args.Text;
                    try
                    {
                        SetupShaderParameters();
                        editor.ToolTip = null;
                    }
                    catch (Exception ex)
                    {
                        editor.ToolTip = ex.Message;
                    }
                };

                ParamsContainer.AddChild(
                    new HBoxContainer
                    {
                        Children =
                        {
                            new Label {Text = name},
                            new Label {Text = ": "},
                            editor
                        },
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                    }
                );
            }
        }

        protected void OnColorSliderChanged(ColorRect rect, ColorSlider r, ColorSlider g, ColorSlider b)
        {
            rect.Color = new Color(
                r.ColorValue,
                g.ColorValue,
                b.ColorValue
            );
        }

        protected struct MathExprCtx<T>
        {

            public T prev, init;

        }

        protected Func<T, float> ParseMathExpression<T>(string expression, T initial)
        {
            var language = new FloatMathDsl();
            Expression<Func<MathExprCtx<T>, float>> expressionFunction = language.Parse<MathExprCtx<T>>(expression);
            var fn = expressionFunction.Compile();
            return prev => fn(new MathExprCtx<T> {prev = prev, init = initial});
        }

        protected bool SetupShaderParameters(bool onlyReEval = false)
        {
            if (ShaderInstance == null)
            {
                return true;
            }

            foreach (var (key, value) in Parameters)
            {
                switch (value)
                {
                    case string s:
                        if (onlyReEval) continue;

                        if (ShaderPrototype?.Parameters == null)
                        {
                            throw new ArgumentException("Unknown parameter type.", key);
                        }

                        if (!ShaderPrototype.Parameters.TryGetValue(key, out var protoVal))
                        {
                            throw new NotImplementedException();
                        }

                        switch (protoVal)
                        {
                            case float initial:
                            {
                                if (!float.TryParse(s, out var newVal))
                                {
                                    var expr = ParseMathExpression(s, initial);
                                    newVal = expr(0);
                                    Parameters[key] = (newVal, expr);
                                    ShaderInstance.SetParameter(key, newVal);
                                    break;
                                }

                                Parameters[key] = newVal;
                                ShaderInstance.SetParameter(key, newVal);
                                break;
                            }
                            case int initial:
                            {
                                if (!int.TryParse(s, out var newVal))
                                {
                                    var expr = ParseMathExpression(s, (float) initial);
                                    newVal = (int) MathF.Round(expr(0));
                                    Parameters[key] = (newVal, expr);
                                    ShaderInstance.SetParameter(key, newVal);
                                    break;
                                }

                                Parameters[key] = newVal;
                                ShaderInstance.SetParameter(key, newVal);
                                break;
                            }
                            case bool initial:
                            {
                                if (!bool.TryParse(s, out var newVal))
                                {
                                    var expr = ParseMathExpression(s, initial ? 1f : 0f);
                                    newVal = expr(0f) != 0f;
                                    Parameters[key] = (newVal, expr);
                                    ShaderInstance.SetParameter(key, newVal);
                                    break;
                                }

                                Parameters[key] = newVal;
                                ShaderInstance.SetParameter(key, newVal);
                                break;
                            }
                            case Texture _:
                            {
                                var newVal = Resources.GetTexture(s);
                                Parameters[key] = newVal;
                                ShaderInstance.SetParameter(key, newVal);
                                break;
                            }
                            case Vector2 _:
                            case Vector2i _:
                            case Vector3 _:
                            case Vector4 _:
                            case Matrix3 _:
                            case Matrix4 _:
                                throw new NotImplementedException();
                        }

                        break;
                    case int i:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, i);
                        break;
                    case float f:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, f);
                        break;
                    case bool b:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, b);
                        break;
                    case Texture t:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, t);
                        break;
                    case Vector2 v2:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, v2);
                        break;
                    case Vector2i v2:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, v2);
                        break;
                    case Vector3 v3:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, v3);
                        break;
                    case Vector4 v4:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, v4);
                        break;
                    case Matrix3 m3:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, m3);
                        break;
                    case Matrix4 m4:
                        if (onlyReEval) continue;
                        ShaderInstance.SetParameter(key, m4);
                        break;
                    case ValueTuple<float, Func<float, float>> t:
                        ShaderInstance.SetParameter(key, t.Item2(t.Item1));
                        break;
                    case ValueTuple<int, Func<float, float>> t:
                        ShaderInstance.SetParameter(key, (int) t.Item2(t.Item1));
                        break;
                    case ValueTuple<bool, Func<float, float>> t:
                        ShaderInstance.SetParameter(key, t.Item2(t.Item1 ? 1f : 0f) != 0f);
                        break;
                }
            }

            return true;
        }

        protected ShaderDevWindowBase()
        {
            IoCManager.InjectDependencies(this);

            Title = "Shader Development Window ";

            BgColorRect = new ColorRect
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
                Color = new Color(0, 0, 0),
            };

            BgColorRedSlider = new ColorSlider(StyleNano.StyleClassSliderRed);
            BgColorGreenSlider = new ColorSlider(StyleNano.StyleClassSliderGreen);
            BgColorBlueSlider = new ColorSlider(StyleNano.StyleClassSliderBlue);

            BgColorRedSlider.OnValueChanged += OnBgColorSliderChanged;
            BgColorGreenSlider.OnValueChanged += OnBgColorSliderChanged;
            BgColorBlueSlider.OnValueChanged += OnBgColorSliderChanged;

            ParamsContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            ControlsContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                Children =
                {
                    new Label {Text = "Background Color: "},
                    new VBoxContainer
                    {
                        Children =
                        {
                            BgColorRedSlider,
                            BgColorGreenSlider,
                            BgColorBlueSlider,
                        },
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                    },
                }
            };

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    BgColorRect,
                    ControlsContainer,
                    new Label {Text = "Parameters:"},
                    ParamsContainer
                },
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SizeFlagsVertical = SizeFlags.FillExpand,
            });
        }

        private void OnBgColorSliderChanged()
        {
            OnColorSliderChanged(BgColorRect, BgColorRedSlider, BgColorGreenSlider, BgColorBlueSlider);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            SetupShaderParameters(true);
        }

    }

}
#endif
