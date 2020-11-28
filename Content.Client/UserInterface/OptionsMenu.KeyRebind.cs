#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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
                var vBox = new VBoxContainer();
                AddChild(new VBoxContainer
                {
                    Children =
                    {
                        new ScrollContainer
                        {
                            SizeFlagsVertical = SizeFlags.FillExpand,
                            Children =
                            {
                                new MarginContainer
                                {
                                    MarginLeftOverride = 2,
                                    Children =
                                    {
                                        vBox
                                    }
                                }
                            }
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
                                        new Control {CustomMinimumSize = (2, 0)},
                                        new Label
                                        {
                                            StyleClasses = {StyleBase.StyleClassLabelSubText},
                                            Text = "Click to change binding, right-click to clear"
                                        },
                                        (resetAllButton = new Button
                                        {
                                            Text = "Reset ALL keybinds",
                                            StyleClasses = {StyleBase.ButtonCaution},
                                            SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand
                                        })
                                    }
                                }
                            }
                        }
                    }
                });

                resetAllButton.OnPressed += args =>
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
                        vBox.AddChild(new Control {CustomMinimumSize = (0, 8)});
                    }

                    first = false;
                    vBox.AddChild(new Label
                    {
                        Text = headerContents,
                        FontColorOverride = StyleNano.NanoGold,
                        StyleClasses = {StyleNano.StyleClassLabelKeyText}
                    });
                }

                void AddButton(BoundKeyFunction function, string name)
                {
                    var control = new KeyControl(this, name, function);
                    vBox.AddChild(control);
                    _keyControls.Add(function, control);
                }

                AddHeader("Movement");
                AddButton(EngineKeyFunctions.MoveUp, "Move up");
                AddButton(EngineKeyFunctions.MoveLeft, "Move left");
                AddButton(EngineKeyFunctions.MoveDown, "Move down");
                AddButton(EngineKeyFunctions.MoveRight, "Move right");
                AddButton(EngineKeyFunctions.Walk, "Walk");

                AddHeader("Basic Interaction");
                AddButton(EngineKeyFunctions.Use, "Use");
                AddButton(ContentKeyFunctions.WideAttack, "Wide attack");
                AddButton(ContentKeyFunctions.ActivateItemInHand, "Activate item in hand");
                AddButton(ContentKeyFunctions.ActivateItemInWorld, "Activate item in world");
                AddButton(ContentKeyFunctions.Drop, "Drop item");
                AddButton(ContentKeyFunctions.ExamineEntity, "Examine");
                AddButton(ContentKeyFunctions.SwapHands, "Swap hands");
                AddButton(ContentKeyFunctions.ToggleCombatMode, "Toggle combat mode");

                AddHeader("Advanced Interaction");
                AddButton(ContentKeyFunctions.SmartEquipBackpack, "Smart-equip to backpack");
                AddButton(ContentKeyFunctions.SmartEquipBelt, "Smart-equip to belt");
                AddButton(ContentKeyFunctions.ThrowItemInHand, "Throw item");
                AddButton(ContentKeyFunctions.TryPullObject, "Pull object");
                AddButton(ContentKeyFunctions.MovePulledObject, "Move pulled object");
                AddButton(ContentKeyFunctions.ReleasePulledObject, "Release pulled object");
                AddButton(ContentKeyFunctions.Point, "Point at location");


                AddHeader("User Interface");
                AddButton(ContentKeyFunctions.FocusChat, "Focus chat");
                AddButton(ContentKeyFunctions.FocusOOC, "Focus chat (OOC)");
                AddButton(ContentKeyFunctions.FocusAdminChat, "Focus chat (admin)");
                AddButton(ContentKeyFunctions.OpenCharacterMenu, "Open character menu");
                AddButton(ContentKeyFunctions.OpenContextMenu, "Open context menu");
                AddButton(ContentKeyFunctions.OpenCraftingMenu, "Open crafting menu");
                AddButton(ContentKeyFunctions.OpenInventoryMenu, "Open inventory");
                AddButton(ContentKeyFunctions.OpenTutorial, "Open tutorial");
                AddButton(ContentKeyFunctions.OpenEntitySpawnWindow, "Open entity spawn menu");
                AddButton(ContentKeyFunctions.OpenSandboxWindow, "Open sandbox menu");
                AddButton(ContentKeyFunctions.OpenTileSpawnWindow, "Open tile spawn menu");
                AddButton(ContentKeyFunctions.OpenAdminMenu, "Open admin menu");

                AddHeader("Miscellaneous");
                AddButton(ContentKeyFunctions.TakeScreenshot, "Take screenshot");
                AddButton(ContentKeyFunctions.TakeScreenshotNoUI, "Take screenshot (without UI)");

                AddHeader("Map Editor");
                AddButton(EngineKeyFunctions.EditorPlaceObject, "Place object");
                AddButton(EngineKeyFunctions.EditorCancelPlace, "Cancel placement");
                AddButton(EngineKeyFunctions.EditorGridPlace, "Place in grid");
                AddButton(EngineKeyFunctions.EditorLinePlace, "Place line");
                AddButton(EngineKeyFunctions.EditorRotateObject, "Rotate");

                AddHeader("Development");
                AddButton(EngineKeyFunctions.ShowDebugConsole, "Open Console");
                AddButton(EngineKeyFunctions.ShowDebugMonitors, "Show Debug Monitors");
                AddButton(EngineKeyFunctions.HideUI, "Hide UI");

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
                _currentlyRebinding.Button.Text = Loc.GetString("Press a key...");

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

                public KeyControl(KeyRebindControl parent, string niceName, BoundKeyFunction function)
                {
                    Function = function;
                    var name = new Label
                    {
                        Text = Loc.GetString(niceName),
                        SizeFlagsHorizontal = SizeFlags.Expand
                    };

                    BindButton1 = new BindButton(parent, this, StyleBase.ButtonOpenRight);
                    BindButton2 = new BindButton(parent, this, StyleBase.ButtonOpenLeft);
                    ResetButton = new Button {Text = "Reset", StyleClasses = {StyleBase.ButtonCaution}};

                    var hBox = new HBoxContainer
                    {
                        Children =
                        {
                            new Control {CustomMinimumSize = (5, 0)},
                            name,
                            BindButton1,
                            BindButton2,
                            new Control {CustomMinimumSize = (10, 0)},
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

                    CustomMinimumSize = (200, 0);
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
                    Button.Text = Binding?.GetKeyString() ?? "Unbound";
                }
            }
        }
    }
}
