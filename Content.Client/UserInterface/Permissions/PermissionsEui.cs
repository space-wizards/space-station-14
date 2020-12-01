using System.Collections.Generic;
using System.Linq;
using Content.Client.Administration;
using Content.Client.Eui;
using Content.Client.UserInterface.Stylesheets;
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

#nullable enable

namespace Content.Client.UserInterface.Permissions
{
    [UsedImplicitly]
    public sealed class PermissionsEui : BaseEui
    {
        private const int NoRank = -1;

        [Dependency] private readonly IClientAdminManager _adminManager = default!;

        private readonly Menu _menu;
        private readonly List<SS14Window> _subWindows = new();

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
            SendMessage(new RemoveAdmin {UserId = window.SourceData!.Value.UserId});

            window.Close();
        }

        private void RemoveRankButtonPressed(EditAdminRankWindow window)
        {
            SendMessage(new RemoveAdminRank {Id = window.SourceId!.Value});

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

            if (popup.SourceData is { } src)
            {
                SendMessage(new UpdateAdmin
                {
                    UserId = src.UserId,
                    Title = title,
                    PosFlags = pos,
                    NegFlags = neg,
                    RankId = rank
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
                    RankId = rank
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
                    Name = name
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
            foreach (var admin in s.Admins)
            {
                var al = _menu.AdminsList;
                var name = admin.UserName ?? admin.UserId.ToString();

                al.AddChild(new Label {Text = name});

                var titleControl = new Label {Text = admin.Title ?? Loc.GetString("none")};
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
                    rank = Loc.GetString("none");
                }

                var rankControl = new Label {Text = rank};
                if (italic)
                {
                    rankControl.StyleClasses.Add(StyleBase.StyleClassItalic);
                }

                al.AddChild(rankControl);

                var flagsText = AdminFlagsHelper.PosNegFlagsText(admin.PosFlags, admin.NegFlags);

                al.AddChild(new Label
                {
                    Text = flagsText,
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter | Control.SizeFlags.Expand
                });

                var editButton = new Button {Text = Loc.GetString("Edit")};
                editButton.OnPressed += _ => OnEditPressed(admin);
                al.AddChild(editButton);

                if (!_adminManager.HasFlag(combinedFlags))
                {
                    editButton.Disabled = true;
                    editButton.ToolTip = Loc.GetString("You do not have the required flags to edit this admin.");
                }
            }

            _menu.AdminRanksList.RemoveAllChildren();
            foreach (var kv in s.AdminRanks)
            {
                var rank = kv.Value;
                var flagsText = string.Join(' ', AdminFlagsHelper.FlagsToNames(rank.Flags).Select(f => $"+{f}"));
                _menu.AdminRanksList.AddChild(new Label {Text = rank.Name});
                _menu.AdminRanksList.AddChild(new Label
                {
                    Text = flagsText,
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter | Control.SizeFlags.Expand
                });
                var editButton = new Button {Text = Loc.GetString("Edit")};
                editButton.OnPressed += _ => OnEditRankPressed(kv);
                _menu.AdminRanksList.AddChild(editButton);

                if (!_adminManager.HasFlag(rank.Flags))
                {
                    editButton.Disabled = true;
                    editButton.ToolTip = Loc.GetString("You do not have the required flags to edit this rank.");
                }
            }
        }

        private void OnEditRankPressed(KeyValuePair<int, PermissionsEuiState.AdminRankData> rank)
        {
            OpenRankEditWindow(rank);
        }

        private sealed class Menu : SS14Window
        {
            private readonly PermissionsEui _ui;
            public readonly GridContainer AdminsList;
            public readonly GridContainer AdminRanksList;
            public readonly Button AddAdminButton;
            public readonly Button AddAdminRankButton;

            public Menu(PermissionsEui ui)
            {
                _ui = ui;
                Title = Loc.GetString("Permissions Panel");

                var tab = new TabContainer();

                AddAdminButton = new Button
                {
                    Text = Loc.GetString("Add Admin"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                };

                AddAdminRankButton = new Button
                {
                    Text = Loc.GetString("Add Admin Rank"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkEnd
                };

                AdminsList = new GridContainer {Columns = 5, SizeFlagsVertical = SizeFlags.FillExpand};
                var adminVBox = new VBoxContainer
                {
                    Children = {AdminsList, AddAdminButton},
                };
                TabContainer.SetTabTitle(adminVBox, Loc.GetString("Admins"));

                AdminRanksList = new GridContainer {Columns = 3};
                var rankVBox = new VBoxContainer
                {
                    Children = { AdminRanksList, AddAdminRankButton}
                };
                TabContainer.SetTabTitle(rankVBox, Loc.GetString("Admin Ranks"));

                tab.AddChild(adminVBox);
                tab.AddChild(rankVBox);

                Contents.AddChild(tab);
            }

            protected override Vector2 ContentsMinimumSize => (600, 400);
        }

        private sealed class EditAdminWindow : SS14Window
        {
            public readonly PermissionsEuiState.AdminData? SourceData;
            public readonly LineEdit? NameEdit;
            public readonly LineEdit TitleEdit;
            public readonly OptionButton RankButton;
            public readonly Button SaveButton;
            public readonly Button? RemoveButton;

            public readonly Dictionary<AdminFlags, (Button inherit, Button sub, Button plus)> FlagButtons
                = new();

            public EditAdminWindow(PermissionsEui ui, PermissionsEuiState.AdminData? data)
            {
                SourceData = data;

                Control nameControl;

                if (data is { } dat)
                {
                    var name = dat.UserName ?? dat.UserId.ToString();
                    Title = Loc.GetString("Edit admin {0}", name);

                    nameControl = new Label {Text = name};
                }
                else
                {
                    Title = Loc.GetString("Add admin");

                    nameControl = NameEdit = new LineEdit {PlaceHolder = Loc.GetString("Username/User ID")};
                }

                TitleEdit = new LineEdit {PlaceHolder = Loc.GetString("Custom title, leave blank to inherit rank title.")};
                RankButton = new OptionButton();
                SaveButton = new Button {Text = Loc.GetString("Save"), SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand};

                RankButton.AddItem(Loc.GetString("No rank"), NoRank);
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
                        StyleClasses = {StyleBase.ButtonOpenRight},
                        Disabled = disable,
                        Group = group,
                    };
                    var sub = new Button
                    {
                        Text = "-",
                        StyleClasses = {StyleBase.ButtonOpenBoth},
                        Disabled = disable,
                        Group = group
                    };
                    var plus = new Button
                    {
                        Text = "+",
                        StyleClasses = {StyleBase.ButtonOpenLeft},
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

                    permGrid.AddChild(new Label {Text = flagName});
                    permGrid.AddChild(inherit);
                    permGrid.AddChild(sub);
                    permGrid.AddChild(plus);

                    FlagButtons.Add(flag, (inherit, sub, plus));
                }

                var bottomButtons = new HBoxContainer();
                if (data != null)
                {
                    // show remove button.
                    RemoveButton = new Button {Text = Loc.GetString("Remove")};
                    bottomButtons.AddChild(RemoveButton);
                }

                bottomButtons.AddChild(SaveButton);

                Contents.AddChild(new VBoxContainer
                {
                    Children =
                    {
                        new HBoxContainer
                        {
                            SeparationOverride = 2,
                            Children =
                            {
                                new VBoxContainer
                                {
                                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                                    Children =
                                    {
                                        nameControl,
                                        TitleEdit,
                                        RankButton
                                    }
                                },
                                permGrid
                            },
                            SizeFlagsVertical = SizeFlags.FillExpand
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

            protected override Vector2? CustomSize => (600, 400);
        }

        private sealed class EditAdminRankWindow : SS14Window
        {
            public readonly int? SourceId;
            public readonly LineEdit NameEdit;
            public readonly Button SaveButton;
            public readonly Button? RemoveButton;
            public readonly Dictionary<AdminFlags, CheckBox> FlagCheckBoxes = new();

            public EditAdminRankWindow(PermissionsEui ui, KeyValuePair<int, PermissionsEuiState.AdminRankData>? data)
            {
                SourceId = data?.Key;

                NameEdit = new LineEdit
                {
                    PlaceHolder = Loc.GetString("Rank name"),
                };

                if (data != null)
                {
                    NameEdit.Text = data.Value.Value.Name;
                }

                SaveButton = new Button {Text = Loc.GetString("Save"), SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand};
                var flagsBox = new VBoxContainer();

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

                var bottomButtons = new HBoxContainer();
                if (data != null)
                {
                    // show remove button.
                    RemoveButton = new Button {Text = Loc.GetString("Remove")};
                    bottomButtons.AddChild(RemoveButton);
                }

                bottomButtons.AddChild(SaveButton);

                Contents.AddChild(new VBoxContainer
                {
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

            protected override Vector2? CustomSize => (600, 400);
        }
    }
}
