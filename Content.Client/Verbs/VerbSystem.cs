using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Client.ContextMenu.UI;
using Content.Client.Resources;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Verbs
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public event EventHandler<PointerInputCmdHandler.PointerInputCmdArgs>? ToggleContextMenu;
        public event EventHandler<bool>? ToggleContainerVisibility;

        private ContextMenuPresenter _contextMenuPresenter = default!;
        private VerbPopup? _currentVerbListRoot;
        private EntityUid _currentEntity;
        private List<Verb>? _currentVerbs;

        private VerbPopup? _currentCategoryPopup;
        public VerbPopup? CurrentCategoryPopup
        {
            get => _currentCategoryPopup;
            set 
            {
                _currentCategoryPopup?.Close();
                _currentCategoryPopup = value;
            }
        }

        // TODO VERBS Move presenter out of the system
        // TODO VERBS Separate the rest of the UI from the logic
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeNetworkEvent<VerbsResponseEvent>(HandleVerbResponse);
            SubscribeNetworkEvent<PlayerContainerVisibilityMessage>(HandleContainerVisibilityMessage);

            _contextMenuPresenter = new ContextMenuPresenter(this);
            SubscribeLocalEvent<MoveEvent>(_contextMenuPresenter.HandleMoveEvent);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                    new PointerInputCmdHandler(HandleOpenContextMenu))
                .Register<VerbSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _contextMenuPresenter?.Dispose();

            CommandBinds.Unregister<VerbSystem>();
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ToggleContainerVisibility?.Invoke(this, false);
        }

        private bool HandleOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State == BoundKeyState.Down)
            {
                ToggleContextMenu?.Invoke(this, args);
            }
            return true;
        }
        private void HandleContainerVisibilityMessage(PlayerContainerVisibilityMessage ev)
        {
            ToggleContainerVisibility?.Invoke(this, ev.CanSeeThrough);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _contextMenuPresenter?.Update();
        }

        public void OpenContextMenu(IEntity entity, ScreenCoordinates screenCoordinates)
        {
            if (_currentVerbListRoot != null)
            {
                CloseContextMenu();
            }

            _currentEntity = entity.Uid;
            _currentVerbListRoot = new VerbPopup();
            _userInterfaceManager.ModalRoot.AddChild(_currentVerbListRoot);
            _currentVerbListRoot.OnPopupHide += CloseContextMenu;

            // Add text informing user that the verb list may be incomplete.
            if (!entity.Uid.IsClientSide())
            {
                _currentVerbListRoot.List.AddChild(new Label { Text = Loc.GetString("verb-system-waiting-on-server-text") });
            }

            // Get verb lists from client-side / shared systems
            var user = _playerManager.LocalPlayer!.ControlledEntity!;
            GetOtherVerbsEvent assembleVerbs = new(user, entity, prepareGUI: true);
            RaiseLocalEvent(entity.Uid, assembleVerbs);
            _currentVerbs = assembleVerbs.Verbs;

            // Show the menu
            FillVerbMenu();
            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            _currentVerbListRoot.Open(box);

            //ask server for remaining verbs
            RaiseNetworkEvent(new RequestServerVerbsEvent(_currentEntity));
        }

        public void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, _userInterfaceManager.MousePositionScaled);
        }

        private void HandleVerbResponse(VerbsResponseEvent msg)
        {
            if (_currentEntity != msg.Entity)
            {
                return;
            }

            if (_currentVerbs == null)
            {
                _currentVerbs = msg.Verbs;
            }
            else if (msg.Verbs != null)
            {
                // Merge message verbs with client side verb list
                foreach (var verb in msg.Verbs)
                {
                    _currentVerbs.Add(verb);
                }
            }

            // Clear currently shown verbs
            var vBox = _currentVerbListRoot!.List;
            vBox.DisposeAllChildren();

            // Show verbs, if there are any to show
            if (_currentVerbs != null && _currentVerbs.Count() > 0)
            {
                FillVerbMenu();
            }
            else
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label { Text = Loc.GetString("verb-system-no-verbs-text") });
                vBox.AddChild(panel);
            }
        }

        /// <summary>
        ///     Iterates over the current verbs list and creates GUI buttons.
        /// </summary>
        private void FillVerbMenu()
        {
            if (!EntityManager.TryGetEntity(_currentEntity, out var target) || _currentVerbs == null)
                return;

            _currentVerbs.Sort();

            DebugTools.AssertNotNull(_currentVerbListRoot);
            var vBox = _currentVerbListRoot!.List;

            HashSet<string> categories = new();
            var addLine = false;

            foreach (var verb in _currentVerbs)
            {
                // Add a small horizontal line between verbs
                if (addLine)
                {
                    vBox.AddChild(new PanelContainer
                    {
                        MinSize = (0, 2),
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                    });
                }
                addLine = true;

                if (verb.Category == null)
                {
                    // Lone verb. just create a button for it
                    vBox.AddChild(new VerbButton(this, verb, target));
                    addLine = true;
                }
                else if (categories.Add(verb.Category.Text))
                {
                    // This verb belong to a category that was not yet listed in the categories HashSet.

                    // Get the actual verbs in this category
                    var categoryVerbs = _currentVerbs.Where(v => v.Category?.Text == verb.Category.Text);

                    if (categoryVerbs.Count() > 1 || !verb.Category.Contractible)
                    {
                        // Create a new verb category button,
                        vBox.AddChild(
                            new VerbCategoryButton(this, verb.Category, categoryVerbs, target));
                    }
                    else
                    {
                        // This category only contains a single verb, and the verb is flagged as collapsible. Add a
                        // single modified verb instead.

                        if (verb.Text == string.Empty)
                            verb.Text = verb.Category.Text;
                        else
                            verb.Text = verb.Category.Text + " " + verb.Text;

                        verb.Icon = verb.Category.Icon;
                        vBox.AddChild(new VerbButton(this, verb, target));
                    }
                }
                else
                {
                    // This verb belongs to a category that was already drawn.
                    // Dont draw extra line-spacings for this
                    addLine = false;
                }
            }
        }

        public void CloseContextMenu()
        {
            _currentVerbListRoot?.Dispose();
            _currentVerbListRoot = null;
            CurrentCategoryPopup?.Dispose();
            CurrentCategoryPopup = null;
            _currentEntity = EntityUid.Invalid;
            _currentVerbs = null;
        }

        public class VerbPopup : Popup
        {
            public BoxContainer List { get; }

            public VerbPopup(LayoutOrientation orientation = LayoutOrientation.Vertical)
            {
                AddChild(new PanelContainer
                {
                    Children = {(List = new BoxContainer
                    {
                        Orientation = orientation
                    })},
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#111E")}
                });
            }
        }


        /// <summary>
        ///     These are the popups that appears when hovering over a verb category in the context menu.
        /// </summary>
        public sealed class VerbCategoryPopup : VerbPopup
        {
            public VerbCategoryPopup(VerbSystem system, IEnumerable<Verb> verbs, IEntity target, bool drawOnlyIcons, bool drawVerbIcons)
                : base(drawOnlyIcons ? LayoutOrientation.Horizontal : LayoutOrientation.Vertical)
            {
                if (drawOnlyIcons)
                    drawVerbIcons = true;

                var first = true;
                foreach (var verb in verbs)
                {
                    if (!first)
                    {
                        List.AddChild(new PanelContainer
                        {
                            MinSize = (0, 2),
                            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                        });
                    }

                    first = false;

                    List.AddChild(new VerbButton(system, verb, target, drawVerbIcons));
                }
            }
        }

        private sealed class VerbButton : BaseButton
        {
            public VerbButton(VerbSystem system, Verb verb, IEntity target, bool drawIcons = true)
            {
                Disabled = verb.IsDisabled;

                var buttonContents = new BoxContainer { Orientation = LayoutOrientation.Horizontal };

                // maybe draw verb icons
                if (drawIcons)
                {
                    TextureRect icon = new()
                    {
                        MinSize = (32, 32),
                        Stretch = TextureRect.StretchMode.KeepCentered,
                        TextureScale = (0.5f, 0.5f)
                    };

                    // Even though we are drawing icons, the icon for this specific verb may be null.
                    if (verb.Icon != null)
                    {
                        icon.Texture = verb.Icon.Frame0();
                    }

                    buttonContents.AddChild(icon);
                }

                // maybe add a label
                if (verb.Text != string.Empty)
                {
                    var label = new RichTextLabel();
                    label.SetMessage(FormattedMessage.FromMarkupPermissive(verb.Text));
                    buttonContents.AddChild(label);

                    // If we added a label, also add some padding
                    buttonContents.AddChild(new Control { MinSize = (8, 0) });
                }

                AddChild(buttonContents);

                // give the button functionality!
                if (!Disabled)
                {
                    OnPressed += _ =>
                    {
                        system.CloseContextMenu();
                        try
                        {
                            // Try run the verb locally. Else, ask the server to run it.
                            if (!system.TryExecuteVerb(verb))
                            {
                                system.RaiseNetworkEvent(new UseVerbEvent(target.Uid, verb.Key));
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", verb.Key, target.ToString(), e);
                        }
                    };
                }
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (Disabled)
                {
                    // somewhat darker background
                    handle.DrawRect(PixelSizeBox, new Color(0,0,0,155)); 
                }    
                else if (DrawMode == DrawModeEnum.Hover)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }
        }

        private sealed class VerbCategoryButton : Control
        {
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            private readonly VerbSystem _system;

            private CancellationTokenSource? _openCancel;

            private readonly IEntity _owner;

            private readonly IEnumerable<Verb> _verbs;

            /// <summary>
            ///     Whether or not the leave space to draw verb icons when showing the verbs in the group.
            /// </summary>
            /// <remarks>
            ///     If no verbs in this group have icons, default to hiding them. Alternative would be to leave blank
            ///     space, or duplicate the Category icon repeatedly.
            /// </remarks>
            private readonly bool _drawVerbIcons;

            /// <summary>
            ///     Whether or not to hide member verb text and just show icons.
            /// </summary>
            private readonly bool _drawOnlyIcons;

            /// <summary>
            ///     The popup that appears when hovering over this verb group.
            /// </summary>
            private readonly VerbCategoryPopup _popup;

            public VerbCategoryButton(VerbSystem system, VerbCategoryData category, IEnumerable<Verb> verbs, IEntity target)
            {
                _system = system;
                _owner = target;
                _verbs = verbs;
                _drawOnlyIcons = category.IconsOnly;

                var label = new RichTextLabel();
                label.SetMessage(FormattedMessage.FromMarkupPermissive(category.Text));
                label.HorizontalExpand = true;

                // the icon for the verb group
                var icon = new TextureRect
                {
                    MinSize = (32, 32),
                    TextureScale = (0.5f, 0.5f),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };
                if (category.Icon != null)
                {
                    icon.Texture = category.Icon.Frame0();
                }

                // The little ">" icon that tells you it's a group of verbs
                var groupIndicatorIcon = new TextureRect
                {
                    Texture = IoCManager.Resolve<IResourceCache>()
                                .GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png"),
                    TextureScale = (0.5f, 0.5f),
                    Stretch = TextureRect.StretchMode.KeepCentered,
                };

                // Do any verbs have icons
                foreach (var verb in verbs)
                {
                    if (verb.Icon != null)
                    {
                        _drawVerbIcons = true;
                        break;
                    }
                }

                MouseFilter = MouseFilterMode.Stop;

                // The verb button itself
                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        icon,
                        label,
                        // Padding
                        new Control {MinSize = (8, 0)},
                        groupIndicatorIcon
                    }
                });

                // The popup that appears when hovering over the button
                _popup = new VerbCategoryPopup(_system, _verbs, _owner, _drawOnlyIcons, _drawVerbIcons);
                UserInterfaceManager.ModalRoot.AddChild(_popup);
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (this == UserInterfaceManager.CurrentlyHovered)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                _openCancel = new CancellationTokenSource();

                Timer.Spawn(HoverDelay, () =>
                {
                    _system.CurrentCategoryPopup = _popup;
                    _popup.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), (1, 1)), GlobalPosition);
                }, _openCancel.Token);
            }

            protected override void MouseExited()
            {
                base.MouseExited();

                _openCancel?.Cancel();
                _openCancel = null;
            }
        }

        /// <summary>
        ///     This returns the popup that appears when hovering over a verb category in the context menu.
        /// </summary>
        private static VerbPopup GetVerbCategoryPopup(VerbSystem system, IEnumerable<Verb> verbs, IEntity target, bool drawOnlyIcons, bool drawVerbIcons)
        {
            // We need to show at least something
            if (drawOnlyIcons)
                drawVerbIcons = true;

            var popup = system.CurrentCategoryPopup = new VerbPopup(orientation: drawOnlyIcons?  LayoutOrientation.Horizontal : LayoutOrientation.Vertical);

            var first = true;
            foreach (var verb in verbs)
            {
                if (!first)
                {
                    popup.List.AddChild(new PanelContainer
                    {
                        MinSize = (0, 2),
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                    });
                }

                first = false;

                popup.List.AddChild(new VerbButton(system, verb, target, drawVerbIcons));
            }

            return popup;
        }
    }
}
