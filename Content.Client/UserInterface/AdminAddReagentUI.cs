using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Chemistry;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface
{
    [UsedImplicitly]
    public sealed class AdminAddReagentEui : BaseEui
    {
        [Dependency] private readonly IPrototypeManager _prototypes = default!;

        private readonly Menu _window;
        private bool _closed;

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
            _closed = true;

            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            _window.HandleState((AdminAddReagentEuiState) state);
        }

        private void DoAdd(bool close, string reagentId, ReagentUnit amount)
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

                Title = Loc.GetString("Add reagent...");

                Contents.AddChild(new VBoxContainer
                {
                    Children =
                    {
                        new GridContainer
                        {
                            Columns = 2,
                            Children =
                            {
                                new Label {Text = Loc.GetString("Cur volume: ")},
                                (_volumeLabel = new Label()),
                                new Label {Text = Loc.GetString("Reagent: ")},
                                (_reagentIdEdit = new LineEdit {PlaceHolder = Loc.GetString("Reagent ID...")}),
                                new Label {Text = Loc.GetString("Amount: ")},
                                (_amountEdit = new LineEdit
                                {
                                    PlaceHolder = Loc.GetString("A number..."),
                                    SizeFlagsHorizontal = SizeFlags.FillExpand
                                }),
                            },
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            SizeFlagsVertical = SizeFlags.FillExpand
                        },
                        new HBoxContainer
                        {
                            Children =
                            {
                                (_errorLabel = new Label
                                {
                                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                                    ClipText = true
                                }),

                                (_addButton = new Button {Text = Loc.GetString("Add")}),
                                (_addCloseButton = new Button {Text = Loc.GetString("Add & Close")})
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
                    ReagentUnit.New(float.Parse(_amountEdit.Text)));
            }

            private void CheckErrors()
            {
                if (string.IsNullOrWhiteSpace(_reagentIdEdit.Text))
                {
                    DoError(Loc.GetString("Must specify reagent ID"));
                    return;
                }

                if (!_eui._prototypes.HasIndex<ReagentPrototype>(_reagentIdEdit.Text))
                {
                    DoError(Loc.GetString("'{0}' does not exist.", _reagentIdEdit.Text));
                    return;
                }

                if (string.IsNullOrWhiteSpace(_amountEdit.Text))
                {
                    DoError(Loc.GetString("Must specify reagent amount"));
                    return;
                }

                if (!float.TryParse(_amountEdit.Text, out _))
                {
                    DoError(Loc.GetString("Invalid amount"));
                    return;
                }

                _addButton.Disabled = false;
                _addCloseButton.Disabled = false;
                _errorLabel.Text = "";

                void DoError(string text)
                {
                    _errorLabel.Text = text;

                    _addButton.Disabled = true;
                    _addCloseButton.Disabled = true;
                }
            }

            public void HandleState(AdminAddReagentEuiState state)
            {
                _volumeLabel.Text = Loc.GetString("{0}/{1}u", state.CurVolume, state.MaxVolume);
            }
        }
    }
}
