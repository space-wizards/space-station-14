using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.CCVar;
using Content.Shared.Procedural;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Salvage.UI;

[UsedImplicitly]
public sealed class SalvageExpeditionConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private OfferingWindow? _window;

    public SalvageExpeditionConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new OfferingWindow();
        _window.ClaimOption += index =>
        {
            SendMessage(new ClaimSalvageMessage()
            {
                Index = index,
            });
        };
        _window.OnClose += Close;
        _window?.OpenCenteredLeft();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
        _window = null;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SalvageExpeditionConsoleState current || _window == null)
            return;

        _window.Cooldown = TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.SalvageExpeditionCooldown));
        _window.NextOffer = current.NextOffer;
        _window.Claimed = current.Claimed;

        for (var i = 0; i < current.Missions.Count; i++)
        {
            var missionParams = current.Missions[i];

            var offering = new OfferingWindowOption();
            offering.Title = Loc.GetString($"salvage-expedition-type");

            var difficultyId = "Moderate";
            var difficultyProto = _prototype.Index<SalvageDifficultyPrototype>(difficultyId);
            // TODO: Selectable difficulty soon.
            var mission = _salvage.GetMission(difficultyProto, missionParams.Seed);

            var lBox = new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical
            };

            // Difficulty
            // Details
            lBox.AddChild(new Label()
            {
                Text = Loc.GetString("salvage-expedition-window-difficulty")
            });

            var difficultyColor = difficultyProto.Color;

            lBox.AddChild(new Label
            {
                Text = Loc.GetString("salvage-expedition-difficulty-Moderate"),
                FontColorOverride = difficultyColor,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            lBox.AddChild(new Label
            {
                Text = Loc.GetString("salvage-expedition-difficulty-players"),
                HorizontalAlignment = Control.HAlignment.Left,
            });

            lBox.AddChild(new Label
            {
                Text = difficultyProto.RecommendedPlayers.ToString(),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Details
            lBox.AddChild(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-hostiles")
            });

            var faction = mission.Faction;

            lBox.AddChild(new Label
            {
                Text = faction,
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Duration
            lBox.AddChild(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-duration")
            });

            lBox.AddChild(new Label
            {
                Text = mission.Duration.ToString(),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Biome
            lBox.AddChild(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-biome")
            });

            var biome = mission.Biome;

            lBox.AddChild(new Label
            {
                Text = Loc.GetString(_prototype.Index<SalvageBiomeModPrototype>(biome).ID),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Modifiers
            lBox.AddChild(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-modifiers")
            });

            var mods = mission.Modifiers;

            lBox.AddChild(new Label
            {
                Text = string.Join("\n", mods.Select(o => "- " + o)).TrimEnd(),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Claim
            var claimButton = new Button()
            {
                HorizontalExpand = true,
                VerticalAlignment = Control.VAlignment.Bottom,
                Pressed = current.ActiveMission == missionParams.Index,
                ToggleMode = true,
                Disabled = current.Claimed || current.Cooldown,
            };

            claimButton.Label.Margin = new Thickness(0f, 5f);

            claimButton.OnPressed += args =>
            {
                ClaimOption?.Invoke(missionParams.Index);
            };

            if (current.ActiveMission == missionParams.Index)
            {
                claimButton.Text = Loc.GetString("salvage-expedition-window-claimed");
                claimButton.AddStyleClass(StyleBase.ButtonCaution);
            }
            else
            {
                claimButton.Text = Loc.GetString("salvage-expedition-window-claim");
            }

            var box = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat(new Color(30, 30, 34)),
                HorizontalExpand = true,
                Margin = new Thickness(5f, 0f),
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        Children =
                        {
                            missionStripe,
                            lBox,
                            new Control() {VerticalExpand = true},
                            claimButton,
                        },
                        Margin = new Thickness(5f, 5f)
                    }
                }
            };

            LayoutContainer.SetAnchorPreset(box, LayoutContainer.LayoutPreset.Wide);

            Container.AddChild(box);
        }

        _window?.UpdateState(current);
    }
}
