using System;
using System.Collections.Generic;
using Content.Shared.Chemistry;
using Content.Shared.Kitchen;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GameObjects.Components.Kitchen
{
    [UsedImplicitly]
    public class MicrowaveBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private MicrowaveMenu? _menu;

        private readonly Dictionary<int, EntityUid> _solids = new();
        private readonly Dictionary<int, Solution.ReagentQuantity> _reagents =new();

        public MicrowaveBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner,uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();
            _menu = new MicrowaveMenu(this);
            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.StartButton.OnPressed += _ => SendMessage(new SharedMicrowaveComponent.MicrowaveStartCookMessage());
            _menu.EjectButton.OnPressed += _ => SendMessage(new SharedMicrowaveComponent.MicrowaveEjectMessage());
            _menu.IngredientsList.OnItemSelected += args =>
            {
                SendMessage(new SharedMicrowaveComponent.MicrowaveEjectSolidIndexedMessage(_solids[args.ItemIndex]));

            };

            _menu.IngredientsListReagents.OnItemSelected += args =>
            {
                SendMessage(
                    new SharedMicrowaveComponent.MicrowaveVaporizeReagentIndexedMessage(_reagents[args.ItemIndex]));
            };

            _menu.OnCookTimeSelected += (args,buttonIndex) =>
            {
                var actualButton = (MicrowaveMenu.MicrowaveCookTimeButton) args.Button ;
                SendMessage(new SharedMicrowaveComponent.MicrowaveSelectCookTimeMessage(buttonIndex,actualButton.CookTime));
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            _solids.Clear();
            _menu?.Dispose();
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not MicrowaveUpdateUserInterfaceState cState)
            {
                return;
            }

            _menu?.ToggleBusyDisableOverlayPanel(cState.IsMicrowaveBusy);
            RefreshContentsDisplay(cState.ReagentQuantities, cState.ContainedSolids);

            if (_menu != null)
            {
                var currentlySelectedTimeButton = (Button) _menu.CookTimeButtonVbox.GetChild(cState.ActiveButtonIndex);
                currentlySelectedTimeButton.Pressed = true;
                var label = cState.ActiveButtonIndex <= 0 ? Loc.GetString("INSTANT") : cState.CurrentCookTime.ToString();
                _menu._cookTimeInfoLabel.Text = $"{Loc.GetString("COOK TIME")}: {label}";
            }
        }

        private void RefreshContentsDisplay(Solution.ReagentQuantity[] reagents, EntityUid[] containedSolids)
        {
            _reagents.Clear();

            if (_menu == null) return;

            _menu.IngredientsListReagents.Clear();
            for (var i = 0; i < reagents.Length; i++)
            {
                if (_prototypeManager.TryIndex(reagents[i].ReagentId, out ReagentPrototype? proto))
                {
                    var reagentAdded = _menu.IngredientsListReagents.AddItem($"{reagents[i].Quantity} {proto.Name}");
                    var reagentIndex = _menu.IngredientsListReagents.IndexOf(reagentAdded);
                    _reagents.Add(reagentIndex, reagents[i]);
                }
            }

            _solids.Clear();
            _menu.IngredientsList.Clear();
            for (var j = 0; j < containedSolids.Length; j++)
            {
                if (!_entityManager.TryGetEntity(containedSolids[j], out var entity))
                {
                    return;
                }

                if (entity.Deleted)
                {
                    continue;
                }

                Texture? texture;
                if (entity.TryGetComponent(out IconComponent? iconComponent))
                {
                    texture = iconComponent.Icon?.Default;
                }
                else if (entity.TryGetComponent(out SpriteComponent? spriteComponent))
                {
                    texture = spriteComponent.Icon?.Default;
                }
                else
                {
                    continue;
                }

                var solidItem = _menu.IngredientsList.AddItem(entity.Name, texture);
                var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                _solids.Add(solidIndex, containedSolids[j]);
            }
        }

        public class MicrowaveMenu : SS14Window
        {
            public class MicrowaveCookTimeButton : Button
            {
                public uint CookTime;
            }


            private MicrowaveBoundUserInterface Owner { get; set; }

            public event Action<ButtonEventArgs, int>? OnCookTimeSelected;

            public Button StartButton { get; }
            public Button EjectButton { get; }

            public PanelContainer TimerFacePlate { get; }

            public ButtonGroup CookTimeButtonGroup { get; }

            public VBoxContainer CookTimeButtonVbox { get; }

            private VBoxContainer ButtonGridContainer { get; }

            private PanelContainer DisableCookingPanelOverlay { get; }

            public ItemList IngredientsList { get; }

            public ItemList IngredientsListReagents { get; }
            public Label _cookTimeInfoLabel { get; }

            public MicrowaveMenu(MicrowaveBoundUserInterface owner)
            {
                SetSize = MinSize = (512, 256);

                Owner = owner;
                Title = Loc.GetString("Microwave");
                DisableCookingPanelOverlay = new PanelContainer
                {
                    MouseFilter = MouseFilterMode.Stop,
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.60f)},
                };

                var hSplit = new HBoxContainer();

                IngredientsListReagents = new ItemList
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    SelectMode = ItemList.ItemListSelectMode.Button,
                    SizeFlagsStretchRatio = 2,
                    MinSize = (100, 128)
                };

                IngredientsList = new ItemList
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    SelectMode = ItemList.ItemListSelectMode.Button,
                    SizeFlagsStretchRatio = 2,
                    MinSize = (100, 128)
                };

                hSplit.AddChild(IngredientsListReagents);
                //Padding between the lists.
                hSplit.AddChild(new Control
                {
                    MinSize = (0, 5),
                });

                hSplit.AddChild(IngredientsList);

                var vSplit = new VBoxContainer
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                };

                hSplit.AddChild(vSplit);

                ButtonGridContainer = new VBoxContainer
                {
                    Align = BoxContainer.AlignMode.Center,
                    SizeFlagsStretchRatio = 3
                };

                StartButton = new Button
                {
                    Text = Loc.GetString("Start"),
                    TextAlign = Label.AlignMode.Center,
                };

                EjectButton = new Button
                {
                    Text = Loc.GetString("Eject All Contents"),
                    ToolTip = Loc.GetString("This vaporizes all reagents, but ejects any solids."),
                    TextAlign = Label.AlignMode.Center,
                };

                ButtonGridContainer.AddChild(StartButton);
                ButtonGridContainer.AddChild(EjectButton);
                vSplit.AddChild(ButtonGridContainer);

                //Padding
                vSplit.AddChild(new Control
                {
                    MinSize = (0, 15),
                });

                CookTimeButtonGroup = new ButtonGroup();
                CookTimeButtonVbox = new VBoxContainer
                {
                    VerticalExpand = true,
                    Align = BoxContainer.AlignMode.Center,
                };

                var index = 0;
                for (var i = 0; i <= 6; i++)
                {
                    var newButton = new MicrowaveCookTimeButton
                    {
                        Text = index <= 0 ? Loc.GetString("INSTANT") : index.ToString(),
                        CookTime = (uint)index,
                        TextAlign = Label.AlignMode.Center,
                        ToggleMode = true,
                        Group = CookTimeButtonGroup,
                    };
                    CookTimeButtonVbox.AddChild(newButton);
                    newButton.OnToggled += args =>
                    {
                        OnCookTimeSelected?.Invoke(args, newButton.GetPositionInParent());

                    };
                    index += 5;
                }

                var cookTimeOneSecondButton = (Button) CookTimeButtonVbox.GetChild(0);
                cookTimeOneSecondButton.Pressed = true;

                _cookTimeInfoLabel = new Label
                {
                    Text = Loc.GetString("COOK TIME: 1"),
                    Align = Label.AlignMode.Center,
                    Modulate = Color.White,
                    VerticalAlignment = VAlignment.Center
                };

                var innerTimerPanel = new PanelContainer
                {
                    VerticalExpand = true,
                    ModulateSelfOverride = Color.Red,
                    MinSize = (100, 128),
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.Black.WithAlpha(0.5f)},

                    Children =
                    {
                        new VBoxContainer
                        {
                            Children =
                            {
                                new PanelContainer
                                {
                                    PanelOverride = new StyleBoxFlat() {BackgroundColor = Color.Gray.WithAlpha(0.2f)},

                                    Children =
                                    {
                                        _cookTimeInfoLabel
                                    }
                                },

                                new ScrollContainer()
                                {
                                    VerticalExpand = true,

                                    Children =
                                    {
                                        CookTimeButtonVbox,
                                    }
                                },
                            }
                        }
                    }
                };

                TimerFacePlate = new PanelContainer()
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    Children =
                    {
                        innerTimerPanel
                    },
                };

                vSplit.AddChild(TimerFacePlate);
                Contents.AddChild(hSplit);
                Contents.AddChild(DisableCookingPanelOverlay);
            }

            public void ToggleBusyDisableOverlayPanel(bool shouldDisable)
            {
                DisableCookingPanelOverlay.Visible = shouldDisable;
            }
        }
    }
}
