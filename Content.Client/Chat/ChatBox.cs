# nullable enable

using System;
using System.Collections.Generic;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

        // TODO: Maybe don't expose so many of our controls if we don't really need to, makes this
        // control a bit harder to understand
        public HistoryLineEdit Input { get; private set; }
        public OutputPanel Contents { get; }

        // order in which the available channel filters show up
        private static readonly IReadOnlyList<ChatChannel> ChannelFilters = new List<ChatChannel>
        {
            ChatChannel.Local, ChatChannel.Radio, ChatChannel.OOC, ChatChannel.Dead, ChatChannel.AdminChat,
            ChatChannel.Server
        };

        private const float FilterPopupWidth = 110;

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
        private readonly IClientConGroupController _groupController;
        private readonly FilterButton _filterButton;
        private readonly Popup _filterPopup;
        private readonly PanelContainer _filterPopupPanel;
        private readonly VBoxContainer _filterVBox;




        public ChatBox()
        {
            _groupController = IoCManager.Resolve<IClientConGroupController>();

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

            RepopulateChannelFilter();

            RepopulateChannelSelector();
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _channelSelector.OnItemSelected += OnChannelItemSelected;
            Input.OnKeyBindDown += InputKeyBindDown;
            Input.OnTextEntered += Input_OnTextEntered;
            Input.OnTextChanged += InputOnTextChanged;
            Input.OnFocusExit += InputOnFocusExit;
            _groupController.ConGroupUpdated += RepopulateChannelSelector;
            _filterButton.OnToggled += FilterButtonOnOnToggled;
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
            _groupController.ConGroupUpdated -= RepopulateChannelSelector;
            _filterButton.OnToggled -= FilterButtonOnOnToggled;
            _filterPopup.OnPopupHide -= OnPopupHide;
        }

        public void SetChannelFilters(IReadOnlySet<ChatChannel> enabledChannels)
        {
            _filterVBox.Children.Clear();
            _filterVBox.AddChild(new Control {CustomMinimumSize = (10, 0)});
            foreach (var channelFilter in ChannelFilters)
            {
                _filterVBox.AddChild(new ChannelFilterCheckbox(channelFilter)
                {
                    Pressed = enabledChannels.Contains(channelFilter)
                });
            }
            _filterVBox.AddChild(new Control {CustomMinimumSize = (10, 0)});
        }

        private void RepopulateChannelFilter()
        {
            var selectedChannels = new HashSet<ChatChannel>();
            foreach (var child in _filterVBox.Children)
            {
                if (child is not ChannelFilterCheckbox checkbox) continue;
                if (checkbox.Pressed) selectedChannels.Add(checkbox.Channel);
            }

            SetChannelFilters(selectedChannels);
        }

        private void RepopulateChannelSelector()
        {
            var selected = (ChatChannel) _channelSelector.SelectedId;
            _channelSelector.Clear();
            // TODO: possibly some of the channels should not be selectable, some of them should just change what the box says
            // and not really be selectable since you'd never want to keep them selected
            _channelSelector.AddItem("Local", (int) ChatChannel.Local);
            _channelSelector.AddItem("Radio", (int) ChatChannel.Radio);
            _channelSelector.AddItem("OOC", (int) ChatChannel.OOC);
            if (_groupController.CanCommand("asay"))
            {
                _channelSelector.AddItem("Admin", (int) ChatChannel.AdminChat);
            }
            else
            {
                // downgrade our selection / saved channel if we lost asay privs
                if (selected == ChatChannel.AdminChat) selected = ChatChannel.OOC;
                if (_savedSelectedChannel == ChatChannel.AdminChat) _savedSelectedChannel = ChatChannel.OOC;
            }
            _channelSelector.AddItem("Emote", (int) ChatChannel.Emotes);
            // technically it's not a chat channel, but it is still possible to send console commands via
            // chatbox
            _channelSelector.AddItem("Console", (int) ChatChannel.Unspecified);

            SafelySelectChannel(selected);
        }


        private void FilterButtonOnOnToggled(BaseButton.ButtonToggledEventArgs obj)
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
        /// (any currently entered text is preserved).
        /// </summary>
        public void SelectChannel(ChatChannel toSelect)
        {
            _savedSelectedChannel = null;
            SafelySelectChannel(toSelect);
        }

        private void SafelySelectChannel(ChatChannel toSelect)
        {
            // in case we try to select admin chat when we can't, default to OOC.
            if (toSelect == ChatChannel.AdminChat && !CanAdminChat())
            {
                toSelect = ChatChannel.OOC;
            }

            if (!_channelSelector.TrySelectId((int) toSelect))
            {
                Logger.Warning("coding error, tried to select chat channel not in the channel selector, defaulting to OOC: {0}",
                    toSelect);
                toSelect = ChatChannel.OOC;
            };
        }

        private bool CanAdminChat()
        {
            return _groupController.CanCommand("asay");
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!args.CanFocus)
            {
                return;
            }

            Input.GrabKeyboardFocus();
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


        private void InputOnTextChanged(LineEdit.LineEditEventArgs obj)
        {
            // switch temporarily to a different channel if an alias prefix has been entered.

            // are we already temporarily switching to a channel?
            if (_savedSelectedChannel.HasValue) return;

            var trimmed = obj.Text.Trim();
            if (trimmed.Length == 0 || trimmed.Length > 1) return;

            var channel = GetChannelFromPrefix(trimmed[0]);
            if (channel == null) return;
            _savedSelectedChannel = SelectedChannel;
            SafelySelectChannel(channel.Value);
            // we "ate" the prefix
            Input.Text = "";
        }

        private ChatChannel? GetChannelFromPrefix(char prefix)
        {
            return prefix switch
            {
                ChatManager.MeAlias => ChatChannel.Emotes,
                ChatManager.RadioAlias => ChatChannel.Radio,
                ChatManager.AdminChatAlias => ChatChannel.AdminChat,
                ChatManager.OOCAlias => ChatChannel.OOC,
                ChatManager.ConCmdSlash => _groupController.CanCommand("asay") ? ChatChannel.Unspecified : null,
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

        private void OnFilterToggled(BaseButton.ButtonToggledEventArgs args)
        {
            FilterToggled?.Invoke(this, args);
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
