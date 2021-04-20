using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Chat;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    public class ChatBox : Control
    {
        public const float InitialChatBottom = 235;

        public delegate void TextSubmitHandler(ChatBox chatBox, string text);

        public delegate void FilterToggledHandler(ChatChannel toggled, bool enabled);

        public event TextSubmitHandler? TextSubmitted;

        public event FilterToggledHandler? FilterToggled;

        public HistoryLineEdit Input { get; private set; }
        public OutputPanel Contents { get; }

        public event Action<ChatResizedEventArgs>? OnResized;

        // order in which the available channel filters show up when available
        public static readonly IReadOnlyList<ChatChannel> ChannelFilterOrder = new List<ChatChannel>
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
        private const int MinDistanceFromBottom = 255;
        private const int MinLeft = 500;

        /// <summary>
        /// Will be Unspecified if set to Console
        /// </summary>
        public ChatChannel SelectedChannel;

        /// <summary>
        ///     Default formatting string for the ClientChatConsole.
        /// </summary>
        public string DefaultChatFormat { get; set; } = string.Empty;

        public bool ReleaseFocusOnEnter { get; set; } = true;

        public bool ClearOnEnter { get; set; } = true;

        // when channel is changed temporarily due to typing an alias
        // prefix, we save the current channel selection here to restore it when
        // the message is sent
        private ChatChannel? _savedSelectedChannel;

        private readonly Popup _channelSelectorPopup;
        private readonly Button _channelSelector;
        private readonly HBoxContainer _channelSelectorHBox;
        private readonly FilterButton _filterButton;
        private readonly Popup _filterPopup;
        private readonly PanelContainer _filterPopupPanel;
        private readonly VBoxContainer _filterVBox;
        private DragMode _currentDrag = DragMode.None;
        private Vector2 _dragOffsetTopLeft;
        private Vector2 _dragOffsetBottomRight;
        private readonly IClyde _clyde;
        private readonly bool _lobbyMode;
        private byte _clampIn;
        // currently known selectable channels as provided by ChatManager,
        // never contains Unspecified (which corresponds to Console which is always available)
        public List<ChatChannel> SelectableChannels = new();

        /// <summary>
        /// When lobbyMode is false, will position / add to correct location in StateRoot and
        /// be resizable.
        /// wWen true, will leave layout up to parent and not be resizable.
        /// </summary>
        public ChatBox()
        {
            //TODO Paul needs to fix xaml ctor args so we can pass this instead of resolving it.
            var stateManager = IoCManager.Resolve<IStateManager>();
            _lobbyMode = stateManager.CurrentState is LobbyState;

            // TODO: Revisit the resizing stuff after https://github.com/space-wizards/RobustToolbox/issues/1392 is done,
            // Probably not "supposed" to inject IClyde, but I give up.
            // I can't find any other way to allow this control to properly resize when the
            // window is resized. Resized() isn't reliably called when resizing the window,
            // and layoutcontainer anchor / margin don't seem to adjust how we need
            // them to when the window is resized. We need it to be able to resize
            // within some bounds so that it doesn't overlap other UI elements, while still
            // being freely resizable within those bounds.
            _clyde = IoCManager.Resolve<IClyde>();
            MouseFilter = MouseFilterMode.Stop;
            LayoutContainer.SetMarginLeft(this, 4);
            LayoutContainer.SetMarginRight(this, 4);
            MinHeight = 128;
            MinWidth = 200;

            AddChild(new PanelContainer
            {
                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#25252aaa")},
                VerticalExpand = true,
                HorizontalExpand = true,
                Children =
                {
                    new VBoxContainer
                    {
                        Children =
                        {
                            (Contents = new OutputPanel
                            {
                                VerticalExpand = true,
                            }),
                            new PanelContainer
                            {
                                StyleClasses = { StyleNano.StyleClassChatSubPanel },
                                HorizontalExpand = true,
                                Children =
                                {
                                    new HBoxContainer
                                    {
                                        HorizontalExpand = true,
                                        SeparationOverride = 4,
                                        Children =
                                        {
                                            (_channelSelector = new ChannelSelectorButton
                                            {
                                                StyleClasses = { StyleNano.StyleClassChatChannelSelectorButton },
                                                MinWidth = 75,
                                                Text = Loc.GetString("hud-chatbox-ooc"),
                                                ToggleMode = true
                                            }),
                                            (Input = new HistoryLineEdit
                                            {
                                                PlaceHolder = Loc.GetString("hud-chatbox-info"),
                                                HorizontalExpand = true,
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
                                    new Control{MinSize = (10,0)},
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

            _channelSelectorPopup = new Popup
            {
                Children =
                {
                    (_channelSelectorHBox = new HBoxContainer
                    {
                        SeparationOverride = 4
                    })
                }
            };

            if (!_lobbyMode)
            {
                UserInterfaceManager.StateRoot.AddChild(this);
                LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopRight, margin: 10);
                LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopRight, margin: 10);
                LayoutContainer.SetMarginLeft(this, -475);
                LayoutContainer.SetMarginBottom(this, InitialChatBottom);
                OnResized?.Invoke(new ChatResizedEventArgs(InitialChatBottom));
            }
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _channelSelector.OnToggled += OnChannelSelectorToggled;
            _filterButton.OnToggled += OnFilterButtonToggled;
            Input.OnKeyBindDown += InputKeyBindDown;
            Input.OnTextEntered += Input_OnTextEntered;
            Input.OnTextChanged += InputOnTextChanged;
            Input.OnFocusExit += InputOnFocusExit;
            _channelSelectorPopup.OnPopupHide += OnChannelSelectorPopupHide;
            _filterPopup.OnPopupHide += OnFilterPopupHide;
            _clyde.OnWindowResized += ClydeOnOnWindowResized;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _channelSelector.OnToggled -= OnChannelSelectorToggled;
            _filterButton.OnToggled -= OnFilterButtonToggled;
            Input.OnKeyBindDown -= InputKeyBindDown;
            Input.OnTextEntered -= Input_OnTextEntered;
            Input.OnTextChanged -= InputOnTextChanged;
            Input.OnFocusExit -= InputOnFocusExit;
            _channelSelectorPopup.OnPopupHide -= OnChannelSelectorPopupHide;
            _filterPopup.OnPopupHide -= OnFilterPopupHide;
            _clyde.OnWindowResized -= ClydeOnOnWindowResized;
            UnsubFilterItems();
            UnsubChannelItems();

        }

        private void UnsubFilterItems()
        {
            foreach (var child in _filterVBox.Children)
            {
                if (child is not ChannelFilterCheckbox checkbox) continue;
                checkbox.OnToggled -= OnFilterCheckboxToggled;
            }
        }

        private void UnsubChannelItems()
        {
            foreach (var child in _channelSelectorHBox.Children)
            {
                if (child is not ChannelItemButton button) continue;
                button.OnPressed -= OnChannelSelectorItemPressed;
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
        /// <param name="unreadMessages">unread message counts for each disabled channel, values 10 or higher will show as 9+</param>
        public void SetChannelPermissions(List<ChatChannel> selectableChannels, IReadOnlySet<ChatChannel> filterableChannels,
            IReadOnlyDictionary<ChatChannel, bool> channelFilters, IReadOnlyDictionary<ChatChannel, byte> unreadMessages)
        {
            SelectableChannels = selectableChannels;
            // update the channel selector
            UnsubChannelItems();
            _channelSelectorHBox.RemoveAllChildren();
            foreach (var selectableChannel in ChannelSelectorOrder)
            {
                if (!selectableChannels.Contains(selectableChannel)) continue;
                var newButton = new ChannelItemButton(selectableChannel);
                newButton.OnPressed += OnChannelSelectorItemPressed;
                _channelSelectorHBox.AddChild(newButton);
            }
            // console channel is always selectable and represented via Unspecified
            var consoleButton = new ChannelItemButton(ChatChannel.Unspecified);
            consoleButton.OnPressed += OnChannelSelectorItemPressed;
            _channelSelectorHBox.AddChild(consoleButton);


            if (_savedSelectedChannel.HasValue && _savedSelectedChannel.Value != ChatChannel.Unspecified &&
                !selectableChannels.Contains(_savedSelectedChannel.Value))
            {
                // we just lost our saved selected channel, the current one will become permanent
                _savedSelectedChannel = null;
            }

            if (!selectableChannels.Contains(SelectedChannel) && SelectedChannel != ChatChannel.Unspecified)
            {
                // our previously selected channel no longer exists, default back to OOC, which should always be available
                if (selectableChannels.Contains(ChatChannel.OOC))
                {
                    SafelySelectChannel(ChatChannel.OOC);
                }
                else //This shouldn't happen but better to be safe than sorry
                {
                    SafelySelectChannel(selectableChannels.First());
                }
            }
            else
            {
                SafelySelectChannel(SelectedChannel);
            }

            // update the channel filters
            UnsubFilterItems();
            _filterVBox.Children.Clear();
            _filterVBox.AddChild(new Control {CustomMinimumSize = (10, 0)});
            foreach (var channelFilter in ChannelFilterOrder)
            {
                if (!filterableChannels.Contains(channelFilter)) continue;
                byte? unreadCount = null;
                if (unreadMessages.TryGetValue(channelFilter, out var unread))
                {
                    unreadCount = unread;
                }
                var newCheckBox = new ChannelFilterCheckbox(channelFilter, unreadCount)
                {
                    // shouldn't happen, but if there's no explicit enable setting provided, default to enabled
                    Pressed = !channelFilters.TryGetValue(channelFilter, out var enabled) || enabled
                };
                newCheckBox.OnToggled += OnFilterCheckboxToggled;
                _filterVBox.AddChild(newCheckBox);
            }
            _filterVBox.AddChild(new Control {CustomMinimumSize = (10, 0)});
        }

        /// <summary>
        /// Update the unread message counts in the filters based on the provided data.
        /// </summary>
        /// <param name="unreadMessages">counts for each channel, any values above 9 will show as 9+</param>
        public void UpdateUnreadMessageCounts(IReadOnlyDictionary<ChatChannel, byte> unreadMessages)
        {
            foreach (var channelFilter in _filterVBox.Children)
            {
                if (channelFilter is not ChannelFilterCheckbox filterCheckbox) continue;
                if (unreadMessages.TryGetValue(filterCheckbox.Channel, out var unread))
                {
                    filterCheckbox.UpdateUnreadCount(unread);
                }
                else
                {
                    filterCheckbox.UpdateUnreadCount(null);
                }
            }
        }

        private void OnFilterCheckboxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Button is not ChannelFilterCheckbox checkbox) return;
            FilterToggled?.Invoke(checkbox.Channel, checkbox.Pressed);
        }


        private void OnFilterButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Pressed)
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

        private void OnChannelSelectorToggled(BaseButton.ButtonToggledEventArgs args)
        {
            if (args.Pressed)
            {
                var globalLeft = GlobalPosition.X;
                var globalBot = GlobalPosition.Y + Height;
                var box = UIBox2.FromDimensions((globalLeft, globalBot), (SizeBox.Width, AlertsUI.ChatSeparation));
                UserInterfaceManager.ModalRoot.AddChild(_channelSelectorPopup);
                _channelSelectorPopup.Open(box);
            }
            else
            {
                _channelSelectorPopup.Close();
            }
        }

        private void OnFilterPopupHide()
        {
            OnPopupHide(_filterPopup, _filterButton);
        }

        private void OnChannelSelectorPopupHide()
        {
            OnPopupHide(_channelSelectorPopup, _channelSelector);
        }

        private void OnPopupHide(Control popup, BaseButton button)
        {
            UserInterfaceManager.ModalRoot.RemoveChild(popup);
            // this weird check here is because the hiding of the popup happens prior to the button
            // receiving the keydown, which would cause it to then become unpressed
            // and reopen immediately. To avoid this, if the popup was hidden due to clicking on the button,
            // we will not auto-unpress the button, instead leaving it up to the button toggle logic
            // (and this requires the button to be set to EnableAllKeybinds = true)
            if (UserInterfaceManager.CurrentlyHovered != button)
            {
                button.Pressed = false;
            }
        }

        private void OnChannelSelectorItemPressed(BaseButton.ButtonEventArgs obj)
        {
            if (obj.Button is not ChannelItemButton button) return;
            SafelySelectChannel(button.Channel);
            _channelSelectorPopup.Close();
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
            if (toSelect == ChatChannel.Unspecified ||
                SelectableChannels.Contains(toSelect))
            {
                SelectedChannel = toSelect;
                _channelSelector.Text = ChannelSelectorName(toSelect);
                return true;
            }
            // keep current setting
            return false;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function == EngineKeyFunctions.UIClick && !_lobbyMode)
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

            if (args.Function != EngineKeyFunctions.UIClick || _lobbyMode)
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

        // TODO: this drag and drop stuff is somewhat duplicated from Robust BaseWindow but also modified
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

            if (Parent == null || _lobbyMode)
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

                ClampSize(left, bottom);
            }
        }

        protected override void UIScaleChanged()
        {
            base.UIScaleChanged();
            ClampAfterDelay();
        }

        private void ClydeOnOnWindowResized(WindowResizedEventArgs obj)
        {
            ClampAfterDelay();
        }

        private void ClampAfterDelay()
        {
            if (!_lobbyMode)
                _clampIn = 2;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (_lobbyMode) return;
            // we do the clamping after a delay (after UI scale / window resize)
            // because we need to wait for our parent container to properly resize
            // first, so we can calculate where we should go. If we do it right away,
            // we won't have the correct values from the parent to know how to adjust our margins.
            if (_clampIn <= 0) return;
            _clampIn -= 1;
            if (_clampIn == 0) ClampSize();
        }

        private void ClampSize(float? desiredLeft = null, float? desiredBottom = null)
        {
            if (Parent == null || _lobbyMode) return;
            var top = Rect.Top;
            var right = Rect.Right;
            var left = desiredLeft ?? Rect.Left;
            var bottom = desiredBottom ?? Rect.Bottom;

            // clamp so it doesn't go too high or low (leave space for alerts UI)
            var maxBottom = Parent.Size.Y - MinDistanceFromBottom;
            if (maxBottom <= MinHeight)
            {
                // we can't fit in our given space (window made awkwardly small), so give up
                // and overlap at our min height
                bottom = MinHeight;
            }
            else
            {
                bottom = Math.Clamp(bottom, MinHeight, maxBottom);
            }

            var maxLeft = Parent.Size.X - MinWidth;
            if (maxLeft <= MinLeft)
            {
                // window too narrow, give up and overlap at our max left
                left = maxLeft;
            }
            else
            {
                left = Math.Clamp(left, MinLeft, maxLeft);
            }

            LayoutContainer.SetMarginLeft(this, -((right + 10) - left));
            LayoutContainer.SetMarginBottom(this, bottom);
            OnResized?.Invoke(new ChatResizedEventArgs(bottom));
        }

        protected override void MouseExited()
        {
            if (_currentDrag == DragMode.None && !_lobbyMode)
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

        public static string ChannelSelectorName(ChatChannel channel)
        {
            return channel switch
            {
                ChatChannel.AdminChat => Loc.GetString("hud-chatbox-admin"),
                ChatChannel.Unspecified => Loc.GetString("hud-chatbox-console"),
                _ => Loc.GetString(channel.ToString())
            };
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
                TextSubmitted?.Invoke(this, GetPrefixFromChannel(SelectedChannel)
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

    /// <summary>
    /// Only needed to avoid the issue where right click on the button closes the popup
    /// but leaves the button highlighted.
    /// </summary>
    public sealed class ChannelSelectorButton : Button
    {
        public ChannelSelectorButton()
        {
            // needed so the popup is untoggled regardless of which key is pressed when hovering this button.
            // If we don't have this, then right clicking the button while it's toggled on will hide
            // the popup but keep the button toggled on
            Mode = ActionMode.Press;
            EnableAllKeybinds = true;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
            if (args.Function == EngineKeyFunctions.Use) return;
            base.KeyBindDown(args);
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

            // needed for same reason as ChannelSelectorButton
            Mode = ActionMode.Press;
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

    public sealed class ChannelItemButton : Button
    {
        public readonly ChatChannel Channel;

        public ChannelItemButton(ChatChannel channel)
        {
            Channel = channel;
            AddStyleClass(StyleNano.StyleClassChatChannelSelectorButton);
            Text = ChatBox.ChannelSelectorName(channel);
        }
    }

    public sealed class ChannelFilterCheckbox : CheckBox
    {
        public readonly ChatChannel Channel;

        public ChannelFilterCheckbox(ChatChannel channel, byte? unreadCount)
        {
            Channel = channel;

            UpdateText(unreadCount);
        }

        private void UpdateText(byte? unread)
        {
            var name = Channel switch
            {
                ChatChannel.AdminChat => Loc.GetString("hud-chatbox-admin"),
                ChatChannel.Unspecified => throw new InvalidOperationException(
                    "cannot create chat filter for Unspecified"),
                _ => Loc.GetString(Channel.ToString())
            };

            if (unread > 0)
            {
                Text = name + " (" + (unread > 9 ? "9+" : unread) + ")";
            }
            else
            {
                Text = name;
            }
        }

        public void UpdateUnreadCount(byte? unread)
        {
            UpdateText(unread);
        }
    }

    public readonly struct ChatResizedEventArgs
    {
        /// new bottom that the chat rect is going to have in virtual pixels
        /// after the imminent relayout
        public readonly float NewBottom;

        public ChatResizedEventArgs(float newBottom)
        {
            NewBottom = newBottom;
        }
    }
}
