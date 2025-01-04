using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Eui;
using Content.Client.Stylesheets;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Shared.Administration.PermissionsEuiMsg;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Administration.UI
{
    [UsedImplicitly]
    public sealed class PermissionsEui : BaseEui
    {
        private const int NoRank = -1;

        [Dependency] private readonly IClientAdminManager _adminManager = default!;

        private readonly Menu _menu;
        private readonly List<DefaultWindow> _subWindows = new();

        private Dictionary<int, PermissionsEuiState.AdminRankData> _ranks =
            new();

        public PermissionsEui()
        {
            IoCManager.InjectDependencies(this);

            _menu = new Menu(this);
            _menu.AddAdminButton.OnPressed += AddAdminPressed;
            _menu.AddAdminRankButton.OnPressed += AddAdminRankPressed;
            _menu.OnClose += CloseEverything;
        }

        public override void Closed()
        {
            base.Closed();

            SendMessage(new CloseEuiMessage());
            CloseEverything();
        }

        private void CloseEverything()
        {
            foreach (var subWindow in _subWindows.ToArray())
            {
                subWindow.Close();
            }

            _menu.Close();
        }

        private void AddAdminPressed(BaseButton.ButtonEventArgs obj)
        {
            OpenEditWindow(null);
        }

        private void AddAdminRankPressed(BaseButton.ButtonEventArgs obj)
        {
            OpenRankEditWindow(null);
        }


        private void OnEditPressed(PermissionsEuiState.AdminData admin)
        {
            OpenEditWindow(admin);
        }

        private void OpenEditWindow(PermissionsEuiState.AdminData? data)
        {
            var window = new EditAdminWindow(this, data);
            window.SaveButton.OnPressed += _ => SaveAdminPressed(window);
            window.OpenCentered();
            window.OnClose += () => _subWindows.Remove(window);
            if (data != null)
            {
                window.RemoveButton!.OnPressed += _ => RemoveButtonPressed(window);
            }

            _subWindows.Add(window);
        }


        private void OpenRankEditWindow(KeyValuePair<int, PermissionsEuiState.AdminRankData>? rank)
        {
            var window = new EditAdminRankWindow(this, rank);
            window.SaveButton.OnPressed += _ => SaveAdminRankPressed(window);
            window.OpenCentered();
            window.OnClose += () => _subWindows.Remove(window);
            if (rank != null)
            {
                window.RemoveButton!.OnPressed += _ => RemoveRankButtonPressed(window);
            }

            _subWindows.Add(window);
        }

        private void RemoveButtonPressed(EditAdminWindow window)
        {
            SendMessage(new RemoveAdmin { UserId = window.SourceData!.Value.UserId });

            window.Close();
        }

        private void RemoveRankButtonPressed(EditAdminRankWindow window)
        {
            SendMessage(new RemoveAdminRank { Id = window.SourceId!.Value });

            window.Close();
        }

        private void SaveAdminPressed(EditAdminWindow popup)
        {
            popup.CollectSetFlags(out var pos, out var neg);

            int? rank = popup.RankButton.SelectedId;
            if (rank == NoRank)
            {
                rank = null;
            }

            var title = string.IsNullOrWhiteSpace(popup.TitleEdit.Text) ? null : popup.TitleEdit.Text;
            var suspended = popup.SuspendedCheckbox.Pressed;

            if (popup.SourceData is { } src)
            {
                SendMessage(new UpdateAdmin
                {
                    UserId = src.UserId,
                    Title = title,
                    PosFlags = pos,
                    NegFlags = neg,
                    RankId = rank,
                    Suspended = suspended,
                });
            }
            else
            {
                DebugTools.AssertNotNull(popup.NameEdit);

                SendMessage(new AddAdmin
                {
                    UserNameOrId = popup.NameEdit!.Text,
                    Title = title,
                    PosFlags = pos,
                    NegFlags = neg,
                    RankId = rank,
                    Suspended = suspended,
                });
            }

            popup.Close();
        }


        private void SaveAdminRankPressed(EditAdminRankWindow popup)
        {
            var flags = popup.CollectSetFlags();
            var name = popup.NameEdit.Text;

            if (popup.SourceId is { } src)
            {
                SendMessage(new UpdateAdminRank
                {
                    Id = src,
                    Flags = flags,
                    Name = name,
                });
            }
            else
            {
                SendMessage(new AddAdminRank
                {
                    Flags = flags,
                    Name = name
                });
            }

            popup.Close();
        }

        public override void Opened()
        {
            _menu.OpenCentered();
        }

        public override void HandleState(EuiStateBase state)
        {
            var s = (PermissionsEuiState) state;

            if (s.IsLoading)
            {
                return;
            }

            _ranks = s.AdminRanks;

            _menu.AdminsList.RemoveAllChildren();
            foreach (var admin in s.Admins.OrderBy(d => d.UserName))
            {
                var al = _menu.AdminsList;
                var name = admin.UserName ?? admin.UserId.ToString();

                al.AddChild(new Label { Text = name });

                var titleControl = new Label { Text = admin.Title ?? Loc.GetString("permissions-eui-edit-admin-title-control-text").ToLowerInvariant() };
                if (admin.Title == null) // none
                {
                    titleControl.StyleClasses.Add(StyleBase.StyleClassItalic);
                }

                al.AddChild(titleControl);

                bool italic;
                string rank;
                var combinedFlags = admin.PosFlags;
                if (admin.RankId is { } rankId)
                {
                    italic = false;
                    var rankData = s.AdminRanks[rankId];
                    rank = rankData.Name;
                    combinedFlags |= rankData.Flags;
                }
                else
                {
                    italic = true;
                    rank = Loc.GetString("permissions-eui-edit-no-rank-text").ToLowerInvariant();
                }

                var rankControl = new Label { Text = rank };
                if (italic)
                {
                    rankControl.StyleClasses.Add(StyleBase.StyleClassItalic);
                }

                al.AddChild(rankControl);

                var flagsText = AdminFlagsHelper.PosNegFlagsText(admin.PosFlags, admin.NegFlags);

                al.AddChild(new Label
                {
                    Text = flagsText,
                    HorizontalExpand = true,
                    HorizontalAlignment = Control.HAlignment.Center,
                });

                var editButton = new Button { Text = Loc.GetString("permissions-eui-edit-title-button") };
                editButton.OnPressed += _ => OnEditPressed(admin);
                al.AddChild(editButton);

                if (!_adminManager.HasFlag(combinedFlags))
                {
                    editButton.Disabled = true;
                    editButton.ToolTip = Loc.GetString("permissions-eui-do-not-have-required-flags-to-edit-admin-tooltip");
                }
            }

            _menu.AdminRanksList.RemoveAllChildren();
            foreach (var kv in s.AdminRanks)
            {
                var rank = kv.Value;
                var flagsText = string.Join(' ', AdminFlagsHelper.FlagsToNames(rank.Flags).Select(f => $"+{f}"));
                _menu.AdminRanksList.AddChild(new Label { Text = rank.Name });
                _menu.AdminRanksList.AddChild(new Label
                {
                    Text = flagsText,
                    HorizontalExpand = true,
                    HorizontalAlignment = Control.HAlignment.Center,
                });
                var editButton = new Button { Text = Loc.GetString("permissions-eui-edit-admin-rank-button") };
                editButton.OnPressed += _ => OnEditRankPressed(kv);
                _menu.AdminRanksList.AddChild(editButton);

                if (!_adminManager.HasFlag(rank.Flags))
                {
                    editButton.Disabled = true;
                    editButton.ToolTip = Loc.GetString("permissions-eui-do-not-have-required-flags-to-edit-rank-tooltip");
                }
            }
        }

        private void OnEditRankPressed(KeyValuePair<int, PermissionsEuiState.AdminRankData> rank)
        {
            OpenRankEditWindow(rank);
        }

        private sealed class Menu : DefaultWindow
        {
            private readonly PermissionsEui _ui;
            public readonly GridContainer AdminsList;
            public readonly GridContainer AdminRanksList;
            public readonly Button AddAdminButton;
            public readonly Button AddAdminRankButton;

            public Menu(PermissionsEui ui)
            {
                _ui = ui;
                Title = Loc.GetString("permissions-eui-menu-title");

                var tab = new TabContainer();

                AddAdminButton = new Button
                {
                    Text = Loc.GetString("permissions-eui-menu-add-admin-button"),
                    HorizontalAlignment = HAlignment.Right
                };

                AddAdminRankButton = new Button
                {
                    Text = Loc.GetString("permissions-eui-menu-add-admin-rank-button"),
                    HorizontalAlignment = HAlignment.Right
                };

                AdminsList = new GridContainer { Columns = 5, VerticalExpand = true };
                var adminVBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children = { new ScrollContainer() { VerticalExpand = true, Children = { AdminsList } }, AddAdminButton },
                };
                TabContainer.SetTabTitle(adminVBox, Loc.GetString("permissions-eui-menu-admins-tab-title"));

                AdminRanksList = new GridContainer { Columns = 3, VerticalExpand = true };
                var rankVBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children = { new ScrollContainer() { VerticalExpand = true, Children = { AdminRanksList } }, AddAdminRankButton }
                };
                TabContainer.SetTabTitle(rankVBox, Loc.GetString("permissions-eui-menu-admin-ranks-tab-title"));

                tab.AddChild(adminVBox);
                tab.AddChild(rankVBox);

                Contents.AddChild(tab);
            }

            protected override Vector2 ContentsMinimumSize => new Vector2(600, 400);
        }

        private sealed class EditAdminWindow : DefaultWindow
        {
            public readonly PermissionsEuiState.AdminData? SourceData;
            public readonly LineEdit? NameEdit;
            public readonly LineEdit TitleEdit;
            public readonly OptionButton RankButton;
            public readonly Button SaveButton;
            public readonly Button? RemoveButton;
            public readonly CheckBox SuspendedCheckbox;

            public readonly Dictionary<AdminFlags, (Button inherit, Button sub, Button plus)> FlagButtons
                = new();

            public EditAdminWindow(PermissionsEui ui, PermissionsEuiState.AdminData? data)
            {
                MinSize = new Vector2(600, 400);
                SourceData = data;

                Control nameControl;

                if (data is { } dat)
                {
                    var name = dat.UserName ?? dat.UserId.ToString();
                    Title = Loc.GetString("permissions-eui-edit-admin-window-edit-admin-label",
                                          ("admin", name));

                    nameControl = new Label { Text = name };
                }
                else
                {
                    Title = Loc.GetString("permissions-eui-menu-add-admin-button");

                    nameControl = NameEdit = new LineEdit { PlaceHolder = Loc.GetString("permissions-eui-edit-admin-window-name-edit-placeholder") };
                }

                TitleEdit = new LineEdit { PlaceHolder = Loc.GetString("permissions-eui-edit-admin-window-title-edit-placeholder") };
                RankButton = new OptionButton();
                SaveButton = new Button { Text = Loc.GetString("permissions-eui-edit-admin-window-save-button"), HorizontalAlignment = HAlignment.Right };

                SuspendedCheckbox = new CheckBox
                {
                    Text = Loc.GetString("permissions-eui-edit-admin-window-suspended"),
                    Pressed = data?.Suspended ?? false,
                };

                RankButton.AddItem(Loc.GetString("permissions-eui-edit-admin-window-no-rank-button"), NoRank);
                foreach (var (rId, rank) in ui._ranks)
                {
                    RankButton.AddItem(rank.Name, rId);
                }

                RankButton.SelectId(data?.RankId ?? NoRank);
                RankButton.OnItemSelected += RankSelected;

                var permGrid = new GridContainer
                {
                    Columns = 4,
                    HSeparationOverride = 0,
                    VSeparationOverride = 0
                };

                foreach (var flag in AdminFlagsHelper.AllFlags)
                {
                    // Can only grant out perms you also have yourself.
                    // Primarily intended to prevent people giving themselves +HOST with +PERMISSIONS but generalized.
                    var disable = !ui._adminManager.HasFlag(flag);
                    var flagName = flag.ToString().ToUpper();

                    var group = new ButtonGroup();

                    var inherit = new Button
                    {
                        Text = "I",
                        StyleClasses = { StyleBase.ButtonOpenRight },
                        Disabled = disable,
                        Group = group,
                    };
                    var sub = new Button
                    {
                        Text = "-",
                        StyleClasses = { StyleBase.ButtonOpenBoth },
                        Disabled = disable,
                        Group = group
                    };
                    var plus = new Button
                    {
                        Text = "+",
                        StyleClasses = { StyleBase.ButtonOpenLeft },
                        Disabled = disable,
                        Group = group
                    };

                    if (data is { } d)
                    {
                        if ((d.NegFlags & flag) != 0)
                        {
                            sub.Pressed = true;
                        }
                        else if ((d.PosFlags & flag) != 0)
                        {
                            plus.Pressed = true;
                        }
                        else
                        {
                            inherit.Pressed = true;
                        }
                    }
                    else
                    {
                        inherit.Pressed = true;
                    }

                    permGrid.AddChild(new Label { Text = flagName });
                    permGrid.AddChild(inherit);
                    permGrid.AddChild(sub);
                    permGrid.AddChild(plus);

                    FlagButtons.Add(flag, (inherit, sub, plus));
                }

                var bottomButtons = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal
                };
                if (data != null)
                {
                    // show remove button.
                    RemoveButton = new Button { Text = Loc.GetString("permissions-eui-edit-admin-window-remove-flag-button") };
                    bottomButtons.AddChild(RemoveButton);
                }

                bottomButtons.AddChild(SaveButton);

                Contents.AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            SeparationOverride = 2,
                            Children =
                            {
                                new BoxContainer
                                {
                                    Orientation = LayoutOrientation.Vertical,
                                    HorizontalExpand = true,
                                    Children =
                                    {
                                        nameControl,
                                        TitleEdit,
                                        RankButton,
                                        SuspendedCheckbox,
                                    }
                                },
                                permGrid
                            },
                            VerticalExpand = true
                        },
                        bottomButtons
                    }
                });
            }

            private void RankSelected(OptionButton.ItemSelectedEventArgs obj)
            {
                RankButton.SelectId(obj.Id);
            }

            public void CollectSetFlags(out AdminFlags pos, out AdminFlags neg)
            {
                pos = default;
                neg = default;

                foreach (var (flag, (_, s, p)) in FlagButtons)
                {
                    if (s.Pressed)
                    {
                        neg |= flag;
                    }
                    else if (p.Pressed)
                    {
                        pos |= flag;
                    }
                }
            }
        }

        private sealed class EditAdminRankWindow : DefaultWindow
        {
            public readonly int? SourceId;
            public readonly LineEdit NameEdit;
            public readonly Button SaveButton;
            public readonly Button? RemoveButton;
            public readonly Dictionary<AdminFlags, CheckBox> FlagCheckBoxes = new();

            public EditAdminRankWindow(PermissionsEui ui, KeyValuePair<int, PermissionsEuiState.AdminRankData>? data)
            {
                Title = Loc.GetString("permissions-eui-edit-admin-rank-window-title");
                MinSize = new Vector2(600, 400);
                SourceId = data?.Key;

                NameEdit = new LineEdit
                {
                    PlaceHolder = Loc.GetString("permissions-eui-edit-admin-rank-window-name-edit-placeholder"),
                };

                if (data != null)
                {
                    NameEdit.Text = data.Value.Value.Name;
                }

                SaveButton = new Button
                {
                    Text = Loc.GetString("permissions-eui-menu-save-admin-rank-button"),
                    HorizontalAlignment = HAlignment.Right,
                    HorizontalExpand = true,
                };
                var flagsBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical
                };

                foreach (var flag in AdminFlagsHelper.AllFlags)
                {
                    // Can only grant out perms you also have yourself.
                    // Primarily intended to prevent people giving themselves +HOST with +PERMISSIONS but generalized.
                    var disable = !ui._adminManager.HasFlag(flag);
                    var flagName = flag.ToString().ToUpper();

                    var checkBox = new CheckBox
                    {
                        Disabled = disable,
                        Text = flagName
                    };

                    if (data != null && (data.Value.Value.Flags & flag) != 0)
                    {
                        checkBox.Pressed = true;
                    }

                    FlagCheckBoxes.Add(flag, checkBox);
                    flagsBox.AddChild(checkBox);
                }

                var bottomButtons = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal
                };
                if (data != null)
                {
                    // show remove button.
                    RemoveButton = new Button { Text = Loc.GetString("permissions-eui-menu-remove-admin-rank-button") };
                    bottomButtons.AddChild(RemoveButton);
                }

                bottomButtons.AddChild(SaveButton);

                Contents.AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        NameEdit,
                        flagsBox,
                        bottomButtons
                    }
                });
            }

            public AdminFlags CollectSetFlags()
            {
                AdminFlags flags = default;
                foreach (var (flag, chk) in FlagCheckBoxes)
                {
                    if (chk.Pressed)
                    {
                        flags |= flag;
                    }
                }

                return flags;
            }
        }
    }
}
