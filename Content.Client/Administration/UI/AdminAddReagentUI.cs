using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eui;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Administration.UI
{
    [UsedImplicitly]
    public sealed class AdminAddReagentEui : BaseEui
    {
        [Dependency] private readonly IPrototypeManager _prototypes = default!;

        private readonly Menu _window;

        public AdminAddReagentEui()
        {
            _window = new Menu(this);
            _window.OnClose += () => SendMessage(new AdminAddReagentEuiMsg.Close());
        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            _window.HandleState((AdminAddReagentEuiState) state);
        }

        private void DoAdd(bool close, string reagentId, FixedPoint2 amount)
        {
            SendMessage(new AdminAddReagentEuiMsg.DoAdd
            {
                Amount = amount,
                ReagentId = reagentId,
                CloseAfter = close
            });
        }

        private sealed class Menu : SS14Window
        {
            private readonly AdminAddReagentEui _eui;
            private readonly Label _volumeLabel;
            private readonly LineEdit _reagentIdEdit;
            private readonly LineEdit _amountEdit;
            private readonly Label _errorLabel;
            private readonly Button _addButton;
            private readonly Button _addCloseButton;

            public Menu(AdminAddReagentEui eui)
            {
                _eui = eui;

                Title = Loc.GetString("admin-add-reagent-eui-title");

                Contents.AddChild(new BoxContainer
                {
	                Orientation = LayoutOrientation.Vertical,
                    Children =
                    {
                        new GridContainer
                        {
                            Columns = 2,
                            Children =
                            {
                                new Label {Text = Loc.GetString("admin-add-reagent-eui-current-volume-label") + " "},
                                (_volumeLabel = new Label()),
                                new Label {Text = Loc.GetString("admin-add-reagent-eui-reagent-label") + " "},
                                (_reagentIdEdit = new LineEdit {PlaceHolder = Loc.GetString("admin-add-reagent-eui-reagent-id-edit")}),
                                new Label {Text = Loc.GetString("admin-add-reagent-eui-amount-label") + " "},
                                (_amountEdit = new LineEdit
                                {
                                    PlaceHolder = Loc.GetString("admin-add-reagent-eui-amount-edit"),
                                    HorizontalExpand = true
                                }),
                            },
                            HorizontalExpand = true,
                            VerticalExpand = true
                        },
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Children =
                            {
                                (_errorLabel = new Label
                                {
                                    HorizontalExpand = true,
                                    ClipText = true
                                }),

                                (_addButton = new Button {Text = Loc.GetString("admin-add-reagent-eui-add-button")}),
                                (_addCloseButton = new Button {Text = Loc.GetString("admin-add-reagent-eui-add-close-button")})
                            }
                        }
                    }
                });

                _reagentIdEdit.OnTextChanged += _ => CheckErrors();
                _amountEdit.OnTextChanged += _ => CheckErrors();
                _addButton.OnPressed += _ => DoAdd(false);
                _addCloseButton.OnPressed += _ => DoAdd(true);

                CheckErrors();
            }

            private void DoAdd(bool close)
            {
                _eui.DoAdd(
                    close,
                    _reagentIdEdit.Text,
                    FixedPoint2.New(float.Parse(_amountEdit.Text)));
            }

            private void CheckErrors()
            {
                if (string.IsNullOrWhiteSpace(_reagentIdEdit.Text))
                {
                    DoError(Loc.GetString("admin-add-reagent-eui-no-reagent-id-error"));
                    return;
                }

                if (!_eui._prototypes.HasIndex<ReagentPrototype>(_reagentIdEdit.Text))
                {
                    DoError(Loc.GetString("admin-add-reagent-eui-reagent-does-not-exist-error",
                                         ("reagent", _reagentIdEdit.Text)));
                    return;
                }

                if (string.IsNullOrWhiteSpace(_amountEdit.Text))
                {
                    DoError(Loc.GetString("admin-add-reagent-eui-no-reagent-amount-specified-error"));
                    return;
                }

                if (!float.TryParse(_amountEdit.Text, out _))
                {
                    DoError(Loc.GetString("admin-add-reagent-eui-invalid-amount-error"));
                    return;
                }

                _addButton.Disabled = false;
                _addCloseButton.Disabled = false;
                _errorLabel.Text = string.Empty;

                void DoError(string text)
                {
                    _errorLabel.Text = text;

                    _addButton.Disabled = true;
                    _addCloseButton.Disabled = true;
                }
            }

            public void HandleState(AdminAddReagentEuiState state)
            {
                _volumeLabel.Text = Loc.GetString("admin-add-reagent-eui-current-and-max-volume-label",
                                                  ("currentVolume", state.CurVolume),
                                                  ("maxVolume" ,state.MaxVolume));
            }
        }
    }
}
