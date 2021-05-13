using System;
using System.Collections.Generic;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public sealed partial class OptionsMenu
    {
        private sealed class KeyRebindControl : Control
        {
            // List of key functions that must be registered as toggle instead.
            private static readonly HashSet<BoundKeyFunction> ToggleFunctions = new()
            {
                EngineKeyFunctions.ShowDebugMonitors,
                EngineKeyFunctions.HideUI,
            };

            [Dependency] private readonly IInputManager _inputManager = default!;

            private BindButton? _currentlyRebinding;

            private readonly Dictionary<BoundKeyFunction, KeyControl> _keyControls =
                new();

            private readonly List<Action> _deferCommands = new();

            public KeyRebindControl()
            {
                IoCManager.InjectDependencies(this);

                Button resetAllButton;
                var vBox = new VBoxContainer {Margin = new Thickness(2, 0, 0, 0)};
                AddChild(new VBoxContainer
                {
                    Children =
                    {
                        new ScrollContainer
                        {
                            VerticalExpand = true,
                            Children = {vBox}
                        },

                        new StripeBack
                        {
                            HasBottomEdge = false,
                            HasMargins = false,
                            Children =
                            {
                                new HBoxContainer
                                {
                                    Children =
                                    {
                                        new Control {MinSize = (2, 0)},
                                        new Label
                                        {
                                            StyleClasses = {StyleBase.StyleClassLabelSubText},
                                            Text = Loc.GetString("ui-options-binds-explanation")
                                        },
                                        (resetAllButton = new Button
                                        {
                                            Text = Loc.GetString("ui-options-binds-reset-all"),
                                            StyleClasses = {StyleBase.ButtonCaution},
                                            HorizontalExpand = true,
                                            HorizontalAlignment = HAlignment.Right
                                        })
                                    }
                                }
                            }
                        }
                    }
                });

                resetAllButton.OnPressed += _ =>
                {
                    _deferCommands.Add(() =>
                    {
                        _inputManager.ResetAllBindings();
                        _inputManager.SaveToUserData();
                    });
                };

                var first = true;

                void AddHeader(string headerContents)
                {
                    if (!first)
                    {
                        vBox.AddChild(new Control {MinSize = (0, 8)});
                    }

                    first = false;
                    vBox.AddChild(new Label
                    {
                        Text = Loc.GetString(headerContents),
                        FontColorOverride = StyleNano.NanoGold,
                        StyleClasses = {StyleNano.StyleClassLabelKeyText}
                    });
                }

                void AddButton(BoundKeyFunction function)
                {
                    var control = new KeyControl(this, function);
                    vBox.AddChild(control);
                    _keyControls.Add(function, control);
                }

                AddHeader("ui-options-header-movement");
                AddButton(EngineKeyFunctions.MoveUp);
                AddButton(EngineKeyFunctions.MoveLeft);
                AddButton(EngineKeyFunctions.MoveDown);
                AddButton(EngineKeyFunctions.MoveRight);
                AddButton(EngineKeyFunctions.Walk);

                AddHeader("ui-options-header-interaction-basic");
                AddButton(EngineKeyFunctions.Use);
                AddButton(ContentKeyFunctions.WideAttack);
                AddButton(ContentKeyFunctions.ActivateItemInHand);
                AddButton(ContentKeyFunctions.ActivateItemInWorld);
                AddButton(ContentKeyFunctions.Drop);
                AddButton(ContentKeyFunctions.ExamineEntity);
                AddButton(ContentKeyFunctions.SwapHands);

                AddHeader("ui-options-header-interaction-adv");
                AddButton(ContentKeyFunctions.SmartEquipBackpack);
                AddButton(ContentKeyFunctions.SmartEquipBelt);
                AddButton(ContentKeyFunctions.ThrowItemInHand);
                AddButton(ContentKeyFunctions.TryPullObject);
                AddButton(ContentKeyFunctions.MovePulledObject);
                AddButton(ContentKeyFunctions.ReleasePulledObject);
                AddButton(ContentKeyFunctions.Point);

                AddHeader("ui-options-header-ui");
                AddButton(ContentKeyFunctions.FocusChat);
                AddButton(ContentKeyFunctions.FocusLocalChat);
                AddButton(ContentKeyFunctions.FocusRadio);
                AddButton(ContentKeyFunctions.FocusOOC);
                AddButton(ContentKeyFunctions.FocusAdminChat);
                AddButton(ContentKeyFunctions.CycleChatChannelForward);
                AddButton(ContentKeyFunctions.CycleChatChannelBackward);
                AddButton(ContentKeyFunctions.OpenCharacterMenu);
                AddButton(ContentKeyFunctions.OpenContextMenu);
                AddButton(ContentKeyFunctions.OpenCraftingMenu);
                AddButton(ContentKeyFunctions.OpenInventoryMenu);
                AddButton(ContentKeyFunctions.OpenInfo);
                AddButton(ContentKeyFunctions.OpenActionsMenu);
                AddButton(ContentKeyFunctions.OpenEntitySpawnWindow);
                AddButton(ContentKeyFunctions.OpenSandboxWindow);
                AddButton(ContentKeyFunctions.OpenTileSpawnWindow);
                AddButton(ContentKeyFunctions.OpenAdminMenu);

                AddHeader("ui-options-header-misc");
                AddButton(ContentKeyFunctions.TakeScreenshot);
                AddButton(ContentKeyFunctions.TakeScreenshotNoUI);

                AddHeader("ui-options-header-hotbar");
                AddButton(ContentKeyFunctions.Hotbar1);
                AddButton(ContentKeyFunctions.Hotbar2);
                AddButton(ContentKeyFunctions.Hotbar3);
                AddButton(ContentKeyFunctions.Hotbar4);
                AddButton(ContentKeyFunctions.Hotbar5);
                AddButton(ContentKeyFunctions.Hotbar6);
                AddButton(ContentKeyFunctions.Hotbar7);
                AddButton(ContentKeyFunctions.Hotbar8);
                AddButton(ContentKeyFunctions.Hotbar9);
                AddButton(ContentKeyFunctions.Hotbar0);
                AddButton(ContentKeyFunctions.Loadout1);
                AddButton(ContentKeyFunctions.Loadout2);
                AddButton(ContentKeyFunctions.Loadout3);
                AddButton(ContentKeyFunctions.Loadout4);
                AddButton(ContentKeyFunctions.Loadout5);
                AddButton(ContentKeyFunctions.Loadout6);
                AddButton(ContentKeyFunctions.Loadout7);
                AddButton(ContentKeyFunctions.Loadout8);
                AddButton(ContentKeyFunctions.Loadout9);

                AddHeader("ui-options-header-map-editor");
                AddButton(EngineKeyFunctions.EditorPlaceObject);
                AddButton(EngineKeyFunctions.EditorCancelPlace);
                AddButton(EngineKeyFunctions.EditorGridPlace);
                AddButton(EngineKeyFunctions.EditorLinePlace);
                AddButton(EngineKeyFunctions.EditorRotateObject);

                AddHeader("ui-options-header-dev");
                AddButton(EngineKeyFunctions.ShowDebugConsole);
                AddButton(EngineKeyFunctions.ShowDebugMonitors);
                AddButton(EngineKeyFunctions.HideUI);

                foreach (var control in _keyControls.Values)
                {
                    UpdateKeyControl(control);
                }
            }

            private void UpdateKeyControl(KeyControl control)
            {
                var activeBinds = _inputManager.GetKeyBindings(control.Function);

                IKeyBinding? bind1 = null;
                IKeyBinding? bind2 = null;

                if (activeBinds.Count > 0)
                {
                    bind1 = activeBinds[0];

                    if (activeBinds.Count > 1)
                    {
                        bind2 = activeBinds[1];
                    }
                }

                control.BindButton1.Binding = bind1;
                control.BindButton1.UpdateText();

                control.BindButton2.Binding = bind2;
                control.BindButton2.UpdateText();

                control.BindButton2.Button.Disabled = activeBinds.Count == 0;
                control.ResetButton.Disabled = !_inputManager.IsKeyFunctionModified(control.Function);
            }

            protected override void EnteredTree()
            {
                base.EnteredTree();

                _inputManager.FirstChanceOnKeyEvent += InputManagerOnFirstChanceOnKeyEvent;
                _inputManager.OnKeyBindingAdded += OnKeyBindAdded;
                _inputManager.OnKeyBindingRemoved += OnKeyBindRemoved;
            }

            protected override void ExitedTree()
            {
                base.ExitedTree();

                _inputManager.FirstChanceOnKeyEvent -= InputManagerOnFirstChanceOnKeyEvent;
                _inputManager.OnKeyBindingAdded -= OnKeyBindAdded;
                _inputManager.OnKeyBindingRemoved -= OnKeyBindRemoved;
            }

            private void OnKeyBindRemoved(IKeyBinding obj)
            {
                OnKeyBindModified(obj, true);
            }

            private void OnKeyBindAdded(IKeyBinding obj)
            {
                OnKeyBindModified(obj, false);
            }

            private void OnKeyBindModified(IKeyBinding bind, bool removal)
            {
                if (!_keyControls.TryGetValue(bind.Function, out var keyControl))
                {
                    return;
                }

                if (removal && _currentlyRebinding?.KeyControl == keyControl)
                {
                    // Don't do update if the removal was from initiating a rebind.
                    return;
                }

                UpdateKeyControl(keyControl);

                if (_currentlyRebinding == keyControl.BindButton1 || _currentlyRebinding == keyControl.BindButton2)
                {
                    _currentlyRebinding = null;
                }
            }

            private void InputManagerOnFirstChanceOnKeyEvent(KeyEventArgs keyEvent, KeyEventType type)
            {
                DebugTools.Assert(IsInsideTree);

                if (_currentlyRebinding == null)
                {
                    return;
                }

                keyEvent.Handle();

                if (type != KeyEventType.Up)
                {
                    return;
                }

                var key = keyEvent.Key;

                // Figure out modifiers based on key event.
                // TODO: this won't allow for combinations with keys other than the standard modifier keys,
                // even though the input system totally supports it.
                var mods = new Keyboard.Key[3];
                var i = 0;
                if (keyEvent.Control && key != Keyboard.Key.Control)
                {
                    mods[i] = Keyboard.Key.Control;
                    i += 1;
                }

                if (keyEvent.Shift && key != Keyboard.Key.Shift)
                {
                    mods[i] = Keyboard.Key.Shift;
                    i += 1;
                }

                if (keyEvent.Alt && key != Keyboard.Key.Alt)
                {
                    mods[i] = Keyboard.Key.Alt;
                    i += 1;
                }

                // The input system can only handle 3 modifier keys so if you hold all 4 of the modifier keys
                // then system gets the shaft, I guess.
                if (keyEvent.System && i != 3 && key != Keyboard.Key.LSystem && key != Keyboard.Key.RSystem)
                {
                    mods[i] = Keyboard.Key.LSystem;
                }

                var function = _currentlyRebinding.KeyControl.Function;
                var bindType = KeyBindingType.State;
                if (ToggleFunctions.Contains(function))
                {
                    bindType = KeyBindingType.Toggle;
                }

                var registration = new KeyBindingRegistration
                {
                    Function = function,
                    BaseKey = key,
                    Mod1 = mods[0],
                    Mod2 = mods[1],
                    Mod3 = mods[2],
                    Priority = 0,
                    Type = bindType,
                    CanFocus = key == Keyboard.Key.MouseLeft
                               || key == Keyboard.Key.MouseRight
                               || key == Keyboard.Key.MouseMiddle,
                    CanRepeat = false
                };

                _inputManager.RegisterBinding(registration);
                // OnKeyBindModified will cause _currentlyRebinding to be reset and the UI to update.
                _inputManager.SaveToUserData();
            }

            private void RebindButtonPressed(BindButton button)
            {
                if (_currentlyRebinding != null)
                {
                    return;
                }

                _currentlyRebinding = button;
                _currentlyRebinding.Button.Text = Loc.GetString("ui-options-key-prompt");

                if (button.Binding != null)
                {
                    _deferCommands.Add(() =>
                    {
                        // Have to do defer this or else there will be an exception in InputManager.
                        // Because this IS fired from an input event.
                        _inputManager.RemoveBinding(button.Binding);
                    });
                }
            }

            protected override void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                if (_deferCommands.Count == 0)
                {
                    return;
                }

                foreach (var command in _deferCommands)
                {
                    command();
                }

                _deferCommands.Clear();
            }

            private sealed class KeyControl : Control
            {
                public readonly BoundKeyFunction Function;
                public readonly BindButton BindButton1;
                public readonly BindButton BindButton2;
                public readonly Button ResetButton;

                public KeyControl(KeyRebindControl parent, BoundKeyFunction function)
                {
                    Function = function;
                    var name = new Label
                    {
                        Text = Loc.GetString(
                            $"ui-options-function-{CaseConversion.PascalToKebab(function.FunctionName)}"),
                        HorizontalExpand = true,
                        HorizontalAlignment = HAlignment.Left
                    };

                    BindButton1 = new BindButton(parent, this, StyleBase.ButtonOpenRight);
                    BindButton2 = new BindButton(parent, this, StyleBase.ButtonOpenLeft);
                    ResetButton = new Button {Text = Loc.GetString("ui-options-bind-reset"), StyleClasses = {StyleBase.ButtonCaution}};

                    var hBox = new HBoxContainer
                    {
                        Children =
                        {
                            new Control {MinSize = (5, 0)},
                            name,
                            BindButton1,
                            BindButton2,
                            new Control {MinSize = (10, 0)},
                            ResetButton
                        }
                    };

                    ResetButton.OnPressed += args =>
                    {
                        parent._deferCommands.Add(() =>
                        {
                            parent._inputManager.ResetBindingsFor(function);
                            parent._inputManager.SaveToUserData();
                        });
                    };

                    AddChild(hBox);
                }
            }

            private sealed class BindButton : Control
            {
                private readonly KeyRebindControl _control;
                public readonly KeyControl KeyControl;
                public readonly Button Button;
                public IKeyBinding? Binding;

                public BindButton(KeyRebindControl control, KeyControl keyControl, string styleClass)
                {
                    _control = control;
                    KeyControl = keyControl;
                    Button = new Button {StyleClasses = {styleClass}};
                    UpdateText();
                    AddChild(Button);

                    Button.OnPressed += args =>
                    {
                        control.RebindButtonPressed(this);
                    };

                    Button.OnKeyBindDown += ButtonOnOnKeyBindDown;

                    MinSize = (200, 0);
                }

                private void ButtonOnOnKeyBindDown(GUIBoundKeyEventArgs args)
                {
                    if (args.Function == EngineKeyFunctions.UIRightClick)
                    {
                        if (Binding != null)
                        {
                            _control._deferCommands.Add(() =>
                            {
                                _control._inputManager.RemoveBinding(Binding);
                                _control._inputManager.SaveToUserData();
                            });
                        }

                        args.Handle();
                    }
                }

                public void UpdateText()
                {
                    Button.Text = Binding?.GetKeyString() ?? Loc.GetString("ui-options-unbound");
                }
            }
        }
    }
}
