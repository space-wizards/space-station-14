# nullable enable

using System;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    public class ChatBox : MarginContainer
    {
        public delegate void TextSubmitHandler(ChatBox chatBox, string text);

        public delegate void FilterToggledHandler(ChatBox chatBox, BaseButton.ButtonToggledEventArgs e);

        public event TextSubmitHandler? TextSubmitted;

        public event FilterToggledHandler? FilterToggled;

        // TODO: Maybe don't expose so many of our controls if we don't really need to, makes this
        // control a bit harder to understand
        public HistoryLineEdit Input { get; private set; }
        public OutputPanel Contents { get; }

        // Buttons for filtering
        public Button AllButton { get; }
        public Button LocalButton { get; }
        public Button OOCButton { get; }
        public Button AdminButton { get; }
        public Button DeadButton { get;  }

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

        public ChatBox()
        {
            /*MarginLeft = -475.0f;
            MarginTop = 10.0f;
            MarginRight = -10.0f;
            MarginBottom = 235.0f;

            AnchorLeft = 1.0f;
            AnchorRight = 1.0f;*/
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
                                                        StyleClasses = { StyleNano.StyleClassChatFilterOptionButton },
                                                        OptionStyleClasses = { StyleNano.StyleClassChatFilterOptionButton }
                                                    }),
                                                    (Input = new HistoryLineEdit
                                                    {
                                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                        StyleClasses = { StyleNano.StyleClassChatLineEdit }
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


            // TODO: possibly some of the channels should not be selectable, some of them should just change what the box says
            // and not really be selectable since you'd never want to keep them selected
            _channelSelector.AddItem("Local", (int) ChatChannel.Local);
            _channelSelector.AddItem("Radio", (int) ChatChannel.Radio);
            _channelSelector.AddItem("OOC", (int) ChatChannel.OOC);
            // TODO: Only allow this to show up if we are admin.
            _channelSelector.AddItem("Admin", (int) ChatChannel.AdminChat);
            _channelSelector.AddItem("Emote", (int) ChatChannel.Emotes);
            // technically it's not a chat channel, but it is still possible to send console commands via
            // chatbox
            _channelSelector.AddItem("Console", (int) ChatChannel.Unspecified);


            AllButton = new Button
            {
                Text = Loc.GetString("All"),
                Name = "ALL",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand,
                ToggleMode = true,
            };

            LocalButton = new Button
            {
                Text = Loc.GetString("Local"),
                Name = "Local",
                ToggleMode = true,
            };

            OOCButton = new Button
            {
                Text = Loc.GetString("OOC"),
                Name = "OOC",
                ToggleMode = true,
            };

            AdminButton = new Button
            {
                Text = Loc.GetString("Admin"),
                Name = "Admin",
                ToggleMode = true,
                Visible = false
            };

            DeadButton = new Button
            {
                Text = Loc.GetString("Dead"),
                Name = "Dead",
                ToggleMode = true,
                Visible = false
            };

            AllButton.OnToggled += OnFilterToggled;
            LocalButton.OnToggled += OnFilterToggled;
            OOCButton.OnToggled += OnFilterToggled;
            AdminButton.OnToggled += OnFilterToggled;
            DeadButton.OnToggled += OnFilterToggled;
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _channelSelector.OnItemSelected += OnChannelItemSelected;
            Input.OnKeyBindDown += InputKeyBindDown;
            Input.OnTextEntered += Input_OnTextEntered;
            Input.OnTextChanged += InputOnTextChanged;
            Input.OnFocusExit += InputOnFocusExit;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _channelSelector.OnItemSelected -= OnChannelItemSelected;
            Input.OnKeyBindDown -= InputKeyBindDown;
            Input.OnTextEntered -= Input_OnTextEntered;
            Input.OnTextChanged -= InputOnTextChanged;
            Input.OnFocusExit -= InputOnFocusExit;
        }

        /// <summary>
        /// Selects the indicated channel, clearing out any temporarily-selected channel
        /// (any currently entered text is preserved).
        /// </summary>
        public void SelectChannel(ChatChannel toSelect)
        {
            // TODO: validate if allowed to chat in admin mode if selecting admin channel
            _savedSelectedChannel = null;
            _channelSelector.SelectId((int) toSelect);
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
                _channelSelector.SelectId((int) _savedSelectedChannel);
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
            _channelSelector.SelectId((int) channel);
            // we "ate" the prefix
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
            _channelSelector.SelectId(args.Id);
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
            _channelSelector.SelectId((int) _savedSelectedChannel);
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
                    _channelSelector.SelectId((int) _savedSelectedChannel);
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
}
