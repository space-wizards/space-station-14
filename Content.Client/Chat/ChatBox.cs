# nullable enable

using System;
using System.Collections.Generic;
using Content.Client.Administration;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Chat;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    public class ChatBox : MarginContainer
    {
        public delegate void TextSubmitHandler(ChatBox chatBox, string text);

        public delegate void FilterToggledHandler(ChatChannel toggled, bool enabled);

        public event TextSubmitHandler? TextSubmitted;

        public event FilterToggledHandler? FilterToggled;

        public HistoryLineEdit Input { get; private set; }
        public OutputPanel Contents { get; }

        // order in which the available channel filters show up when available
        private static readonly IReadOnlyList<ChatChannel> ChannelFilterOrder = new List<ChatChannel>
        {
            ChatChannel.Local, ChatChannel.Emotes, ChatChannel.Radio, ChatChannel.OOC, ChatChannel.Dead, ChatChannel.AdminChat,
            ChatChannel.Server
        };

        // order in which the channels show up in the channel selector
        private static readonly IReadOnlyList<ChatChannel> ChannelSelectorOrder = new List<ChatChannel>
        {
            ChatChannel.Local, ChatChannel.Emotes, ChatChannel.Radio, ChatChannel.OOC, ChatChannel.Dead, ChatChannel.AdminChat
        };

        private const float FilterPopupWidth = 110;
        private const int DragMarginSize = 7;

        /// <summary>
        /// Will be Unspecified if set to Console
        /// </summary>
        private ChatChannel SelectedChannel => (ChatChannel) _channelSelector.SelectedId;

        private readonly OptionButton _channelSelector;

        /// <summary>
        ///     Default formatting string for the ClientChatConsole.
        /// </summary>
        public string? DefaultChatFormat { get; set; }

        public bool ReleaseFocusOnEnter { get; set; } = true;

        public bool ClearOnEnter { get; set; } = true;

        // when channel is changed temporarily due to typing an alias
        // prefix, we save the current channel selection here to restore it when
        // the message is sent
        private ChatChannel? _savedSelectedChannel;
        private readonly FilterButton _filterButton;
        private readonly Popup _filterPopup;
        private readonly PanelContainer _filterPopupPanel;
        private readonly VBoxContainer _filterVBox;
        private DragMode _currentDrag = DragMode.None;
        private Vector2 _dragOffsetTopLeft;
        private Vector2 _dragOffsetBottomRight;

        public ChatBox()
        {
            MouseFilter = MouseFilterMode.Stop;

            AddChild(new VBoxContainer
            {
                Children =
                {
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#25252aaa")},
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            new VBoxContainer
                            {
                                Children =
                                {
                                    new MarginContainer
                                    {
                                        MarginLeftOverride = 4, MarginRightOverride = 4,
                                        SizeFlagsVertical = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            (Contents = new OutputPanel())
                                        }
                                    },
                                    new PanelContainer
                                    {
                                        StyleClasses = { StyleNano.StyleClassChatSubPanel },
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new HBoxContainer
                                            {
                                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                SeparationOverride = 4,
                                                Children =
                                                {
                                                    (_channelSelector = new OptionButton
                                                    {
                                                        HideTriangle = true,
                                                        StyleClasses = { StyleNano.StyleClassChatChannelSelectorOptionButton },
                                                        OptionStyleClasses = { StyleNano.StyleClassChatChannelSelectorOptionButton },
                                                        CustomMinimumSize = (75, 0)
                                                    }),
                                                    (Input = new HistoryLineEdit
                                                    {
                                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                        StyleClasses = { StyleNano.StyleClassChatLineEdit }
                                                    }),
                                                    (_filterButton = new FilterButton
                                                    {
                                                        StyleClasses = { StyleNano.StyleClassChatFilterOptionButton }
                                                    })
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                }
            });

            _filterPopup = new Popup
            {
                Children =
                {
                    (_filterPopupPanel = new PanelContainer
                    {
                        StyleClasses = {StyleNano.StyleClassBorderedWindowPanel},
                        Children =
                        {
                            new HBoxContainer
                            {
                                Children =
                                {
                                    new Control{CustomMinimumSize = (10,0)},
                                    (_filterVBox = new VBoxContainer
                                    {
                                        SeparationOverride = 10
                                    })
                                }
                            }
                        }
                    })
                }
            };
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _channelSelector.OnItemSelected += OnChannelItemSelected;
            Input.OnKeyBindDown += InputKeyBindDown;
            Input.OnTextEntered += Input_OnTextEntered;
            Input.OnTextChanged += InputOnTextChanged;
            Input.OnFocusExit += InputOnFocusExit;
            _filterButton.OnToggled += FilterButtonToggled;
            _filterPopup.OnPopupHide += OnPopupHide;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _channelSelector.OnItemSelected -= OnChannelItemSelected;
            Input.OnKeyBindDown -= InputKeyBindDown;
            Input.OnTextEntered -= Input_OnTextEntered;
            Input.OnTextChanged -= InputOnTextChanged;
            Input.OnFocusExit -= InputOnFocusExit;
            _filterButton.OnToggled -= FilterButtonToggled;
            _filterPopup.OnPopupHide -= OnPopupHide;
            foreach (var child in _filterVBox.Children)
            {
                if (child is not ChannelFilterCheckbox checkbox) continue;
                checkbox.OnToggled -= OnFilterCheckboxToggled;
            }

        }


        /// <summary>
        /// Update the available filters / selectable channels and the current filter settings using the provided
        /// data.
        /// </summary>
        /// <param name="selectableChannels">channels currently selectable to send on</param>
        /// <param name="filterableChannels">channels currently able ot filter on</param>
        /// <param name="channelFilters">current settings for the channel filters, this SHOULD always have an entry if
        /// there is a corresponding entry in filterableChannels, but it may also have additional
        /// entries (which should not be presented to the user)</param>
        public void SetChannelPermissions(IReadOnlySet<ChatChannel> selectableChannels, IReadOnlySet<ChatChannel> filterableChannels,
            IReadOnlyDictionary<ChatChannel, bool> channelFilters)
        {
            // update the channel selector
            var selected = (ChatChannel) _channelSelector.SelectedId;
            _channelSelector.Clear();
            foreach (var selectableChannel in ChannelSelectorOrder)
            {
                if (!selectableChannels.Contains(selectableChannel)) continue;
                _channelSelector.AddItem(ChannelDisplayName(selectableChannel) , (int) selectableChannel);
            }
            // console channel is always selectable and represented via Unspecified
            _channelSelector.AddItem("Console", (int) ChatChannel.Unspecified);


            if (_savedSelectedChannel.HasValue && _savedSelectedChannel.Value != ChatChannel.Unspecified &&
                !selectableChannels.Contains(_savedSelectedChannel.Value))
            {
                // we just lost our saved selected channel, the current one will become permanent
                _savedSelectedChannel = null;
            }

            if (!selectableChannels.Contains(selected) && selected != ChatChannel.Unspecified)
            {
                // our previously selected channel no longer exists, default back to OOC, which should always be available
                SafelySelectChannel(ChatChannel.OOC);
            }
            else
            {
                SafelySelectChannel(selected);
            }

            // update the channel filters
            _filterVBox.Children.Clear();
            _filterVBox.AddChild(new Control {CustomMinimumSize = (10, 0)});
            foreach (var channelFilter in ChannelFilterOrder)
            {
                if (!filterableChannels.Contains(channelFilter)) continue;
                var newCheckBox = new ChannelFilterCheckbox(channelFilter)
                {
                    // shouldn't happen, but if there's no explicit enable setting provided, default to enabled
                    Pressed = !channelFilters.TryGetValue(channelFilter, out var enabled) || enabled
                };
                newCheckBox.OnToggled += OnFilterCheckboxToggled;
                _filterVBox.AddChild(newCheckBox);
            }
            _filterVBox.AddChild(new Control {CustomMinimumSize = (10, 0)});


        }

        private string ChannelDisplayName(ChatChannel channel)
        {
            return channel switch
            {
                ChatChannel.AdminChat => "Admin",
                ChatChannel.Unspecified => throw new InvalidOperationException(
                    "cannot create chat filter for Unspecified"),
                _ => channel.ToString()
            };
        }

        private void OnFilterCheckboxToggled(BaseButton.ButtonToggledEventArgs obj)
        {
            if (obj.Button is not ChannelFilterCheckbox checkbox) return;
            FilterToggled?.Invoke(checkbox.Channel, checkbox.Pressed);
        }

        private void FilterButtonToggled(BaseButton.ButtonToggledEventArgs obj)
        {
            if (obj.Pressed)
            {
                var globalPos = _filterButton.GlobalPosition;
                var (minX, minY) = _filterPopupPanel.CombinedMinimumSize;
                var box = UIBox2.FromDimensions(globalPos - (FilterPopupWidth, 0), (Math.Max(minX, FilterPopupWidth), minY));
                UserInterfaceManager.ModalRoot.AddChild(_filterPopup);
                _filterPopup.Open(box);
            }
            else
            {
                _filterPopup.Close();
            }
        }

        private void OnPopupHide()
        {
            UserInterfaceManager.ModalRoot.RemoveChild(_filterPopup);
            // this weird check here is because the hiding of the popup happens prior to the filter button
            // receiving the keydown, which would cause it to then become unpressed
            // and reopen immediately. To avoid this, if the popup was hidden due to clicking on the filter button,
            // we will not auto-unpress the button, instead leaving it up to the button toggle logic
            // (and this requires the button to be set to EnableAllKeybinds = true)
            if (UserInterfaceManager.CurrentlyHovered != _filterButton)
            {
                _filterButton.Pressed = false;
            }
        }

        /// <summary>
        /// Selects the indicated channel, clearing out any temporarily-selected channel
        /// (any currently entered text is preserved). If the specified channel is not selectable,
        /// will just maintain current selection.
        /// </summary>
        public void SelectChannel(ChatChannel toSelect)
        {
            _savedSelectedChannel = null;
            SafelySelectChannel(toSelect);
        }

        private bool SafelySelectChannel(ChatChannel toSelect)
        {
            return _channelSelector.TrySelectId((int) toSelect);
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _currentDrag = GetDragModeFor(args.RelativePosition);

                if (_currentDrag != DragMode.None)
                {
                    _dragOffsetTopLeft = args.PointerLocation.Position / UIScale - Position;
                    _dragOffsetBottomRight = Position + Size - args.PointerLocation.Position / UIScale;
                }
            }

            if (args.CanFocus)
            {
                Input.GrabKeyboardFocus();
            }
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            _dragOffsetTopLeft = _dragOffsetBottomRight = Vector2.Zero;
            _currentDrag = DragMode.None;

            // If this is done in MouseDown, Godot won't fire MouseUp as you need focus to receive MouseUps.
            UserInterfaceManager.KeyboardFocused?.ReleaseKeyboardFocus();
        }

        private void InputKeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.TextReleaseFocus)
            {
                Input.ReleaseKeyboardFocus();
                args.Handle();
                return;
            }

            // if we temporarily selected another channel via a prefx, undo that when we backspace on an empty input
            if (Input.Text.Length == 0 && _savedSelectedChannel.HasValue &&
                args.Function == EngineKeyFunctions.TextBackspace)
            {
                SafelySelectChannel(_savedSelectedChannel.Value);
                _savedSelectedChannel = null;
            }
        }

        // TODO: this drag and drop stuff is somewhat duplicated from Robust but also modified
        [Flags]
        private enum DragMode : byte
        {
            None = 0,
            Bottom = 1 << 1,
            Left = 1 << 2
        }

        private DragMode GetDragModeFor(Vector2 relativeMousePos)
        {
            var mode = DragMode.None;

            if (relativeMousePos.Y > Size.Y - DragMarginSize)
            {
                mode = DragMode.Bottom;
            }

            if (relativeMousePos.X < DragMarginSize)
            {
                mode |= DragMode.Left;
            }

            return mode;
        }

        protected override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            if (Parent == null)
            {
                return;
            }

            if (_currentDrag == DragMode.None)
            {
                var cursor = CursorShape.Arrow;
                var previewDragMode = GetDragModeFor(args.RelativePosition);
                switch (previewDragMode)
                {
                    case DragMode.Bottom:
                        cursor = CursorShape.VResize;
                        break;

                    case DragMode.Left:
                        cursor = CursorShape.HResize;
                        break;

                    case DragMode.Bottom | DragMode.Left:
                        cursor = CursorShape.Crosshair;
                        break;
                }

                DefaultCursorShape = cursor;
            }
            else
            {
                var top = Rect.Top;
                var bottom = Rect.Bottom;
                var left = Rect.Left;
                var right = Rect.Right;
                var (minSizeX, minSizeY) = CombinedMinimumSize;
                if ((_currentDrag & DragMode.Bottom) == DragMode.Bottom)
                {
                    bottom = Math.Max(args.GlobalPosition.Y + _dragOffsetBottomRight.Y, top + minSizeY);
                }

                if ((_currentDrag & DragMode.Left) == DragMode.Left)
                {
                    var maxX = right - minSizeX;
                    left = Math.Min(args.GlobalPosition.X - _dragOffsetTopLeft.X, maxX);
                }

                var rect = new UIBox2(left, top, right, bottom);
                LayoutContainer.SetPosition(this, rect.TopLeft);
                LayoutContainer.SetSize(this, rect.Size);
            }
        }

        protected override void MouseExited()
        {
            if (_currentDrag == DragMode.None)
            {
                DefaultCursorShape = CursorShape.Arrow;
            }
        }


        private void InputOnTextChanged(LineEdit.LineEditEventArgs obj)
        {
            // switch temporarily to a different channel if an alias prefix has been entered.

            // are we already temporarily switching to a channel?
            if (_savedSelectedChannel.HasValue) return;

            var trimmed = obj.Text.Trim();
            if (trimmed.Length == 0 || trimmed.Length > 1) return;

            var channel = GetChannelFromPrefix(trimmed[0]);
            var prevChannel = SelectedChannel;
            if (channel == null || !SafelySelectChannel(channel.Value)) return;
            // we ate the prefix and auto-switched (temporarily) to the channel with that prefix
            _savedSelectedChannel = prevChannel;
            Input.Text = "";
        }

        private static ChatChannel? GetChannelFromPrefix(char prefix)
        {
            return prefix switch
            {
                ChatManager.MeAlias => ChatChannel.Emotes,
                ChatManager.RadioAlias => ChatChannel.Radio,
                ChatManager.AdminChatAlias => ChatChannel.AdminChat,
                ChatManager.OOCAlias => ChatChannel.OOC,
                ChatManager.ConCmdSlash => ChatChannel.Unspecified,
                _ => null
            };
        }

        private static string GetPrefixFromChannel(ChatChannel channel)
        {
            char? prefixChar = channel switch
            {
                ChatChannel.Emotes => ChatManager.MeAlias,
                ChatChannel.Radio => ChatManager.RadioAlias,
                ChatChannel.AdminChat => ChatManager.AdminChatAlias,
                ChatChannel.OOC => ChatManager.OOCAlias,
                ChatChannel.Unspecified => ChatManager.ConCmdSlash,
                _ => null
            };

            return prefixChar.ToString() ?? string.Empty;
        }

        private void OnChannelItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            SafelySelectChannel((ChatChannel) args.Id);
            // we manually selected something so undo the temporary selection
            _savedSelectedChannel = null;
        }

        public void AddLine(string message, ChatChannel channel, Color color)
        {
            if (Disposed)
            {
                return;
            }

            var formatted = new FormattedMessage(3);
            formatted.PushColor(color);
            formatted.AddText(message);
            formatted.Pop();
            Contents.AddMessage(formatted);
        }

        private void InputOnFocusExit(LineEdit.LineEditEventArgs obj)
        {
            // undo the temporary selection, otherwise it will be odd if user
            // comes back to it later only to have their selection cleared upon sending
            if (!_savedSelectedChannel.HasValue) return;
            SafelySelectChannel(_savedSelectedChannel.Value);
            _savedSelectedChannel = null;
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            // We set it there to true so it's set to false by TextSubmitted.Invoke if necessary
            ClearOnEnter = true;

            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                TextSubmitted?.Invoke(this, GetPrefixFromChannel((ChatChannel)_channelSelector.SelectedId)
                                            + args.Text);
            }

            if (ClearOnEnter)
            {
                Input.Clear();
                if (_savedSelectedChannel.HasValue)
                {
                    SafelySelectChannel(_savedSelectedChannel.Value);
                    _savedSelectedChannel = null;
                }
            }

            if (ReleaseFocusOnEnter)
            {
                Input.ReleaseKeyboardFocus();
            }
        }
    }

    public sealed class FilterButton : ContainerButton
    {
        private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
        private static readonly Color ColorHovered = Color.FromHex("#9699bb");
        private static readonly Color ColorPressed = Color.FromHex("#789B8C");

        private readonly TextureRect _textureRect;

        public FilterButton()
        {
            var filterTexture = IoCManager.Resolve<IResourceCache>()
                .GetTexture("/Textures/Interface/Nano/filter.svg.96dpi.png");

            Mode = ActionMode.Press;
            // needed so the popup is untoggled regardless of which key is pressed when hovering this button
            EnableAllKeybinds = true;

            AddChild(
                (_textureRect = new TextureRect
                {
                    Texture = filterTexture,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                })
            );
            ToggleMode = true;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
            if (args.Function == EngineKeyFunctions.Use) return;
            base.KeyBindDown(args);
        }

        private void UpdateChildColors()
        {
            if (_textureRect == null) return;
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    _textureRect.ModulateSelfOverride = ColorNormal;
                    break;

                case DrawModeEnum.Pressed:
                    _textureRect.ModulateSelfOverride = ColorPressed;
                    break;

                case DrawModeEnum.Hover:
                    _textureRect.ModulateSelfOverride = ColorHovered;
                    break;

                case DrawModeEnum.Disabled:
                    break;
            }
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();
            UpdateChildColors();
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();
            UpdateChildColors();
        }

    }

    public sealed class ChannelFilterCheckbox : CheckBox
    {
        public ChatChannel Channel { get; }

        public ChannelFilterCheckbox(ChatChannel channel)
        {
            Channel = channel;

            var name = channel switch
            {
                ChatChannel.AdminChat => "Admin",
                ChatChannel.Unspecified => throw new InvalidOperationException(
                    "cannot create chat filter for Unspecified"),
                _ => channel.ToString()
            };

            Text = name;
        }
    }
}
