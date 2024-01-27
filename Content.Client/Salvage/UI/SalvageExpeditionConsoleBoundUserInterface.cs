using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Content.Shared.Procedural;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Salvage.UI;

[UsedImplicitly]
public sealed class SalvageExpeditionConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private OfferingWindow? _window;

    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public SalvageExpeditionConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = new OfferingWindow();
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

        _window.Progression = null;
        _window.Cooldown = TimeSpan.FromSeconds(_cfgManager.GetCVar(CCVars.SalvageExpeditionCooldown));
        _window.NextOffer = current.NextOffer;
        _window.Claimed = current.Claimed;
        _window.ClearOptions();
        var salvage = _entManager.System<SalvageSystem>();

        for (var i = 0; i < current.Missions.Count; i++)
        {
            var missionParams = current.Missions[i];

            var offering = new OfferingWindowOption();
            offering.Title = Loc.GetString($"salvage-expedition-type");

            var difficultyId = "Moderate";
            var difficultyProto = _protoManager.Index<SalvageDifficultyPrototype>(difficultyId);
            // TODO: Selectable difficulty soon.
            var mission = salvage.GetMission(difficultyProto, missionParams.Seed);

            // Difficulty
            // Details
            offering.AddContent(new Label()
            {
                Text = Loc.GetString("salvage-expedition-window-difficulty")
            });

            var difficultyColor = difficultyProto.Color;

            offering.AddContent(new Label
            {
                Text = Loc.GetString("salvage-expedition-difficulty-Moderate"),
                FontColorOverride = difficultyColor,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            offering.AddContent(new Label
            {
                Text = Loc.GetString("salvage-expedition-difficulty-players"),
                HorizontalAlignment = Control.HAlignment.Left,
            });

            offering.AddContent(new Label
            {
                Text = difficultyProto.RecommendedPlayers.ToString(),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Details
            offering.AddContent(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-hostiles")
            });

            var faction = mission.Faction;

            offering.AddContent(new Label
            {
                Text = faction,
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Duration
            offering.AddContent(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-duration")
            });

            offering.AddContent(new Label
            {
                Text = mission.Duration.ToString(),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Biome
            offering.AddContent(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-biome")
            });

            var biome = mission.Biome;

            offering.AddContent(new Label
            {
                Text = Loc.GetString(_protoManager.Index<SalvageBiomeModPrototype>(biome).ID),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            // Modifiers
            offering.AddContent(new Label
            {
                Text = Loc.GetString("salvage-expedition-window-modifiers")
            });

            var mods = mission.Modifiers;

            offering.AddContent(new Label
            {
                Text = string.Join("\n", mods.Select(o => "- " + o)).TrimEnd(),
                FontColorOverride = StyleNano.NanoGold,
                HorizontalAlignment = Control.HAlignment.Left,
                Margin = new Thickness(0f, 0f, 0f, 5f),
            });

            offering.ClaimPressed += args =>
            {
                SendMessage(new ClaimSalvageMessage()
                {
                    Index = missionParams.Index,
                });
            };

            offering.Claimed = current.ActiveMission == missionParams.Index;
            offering.Disabled = current.Claimed || current.Cooldown;

            _window.AddOption(offering);
        }
    }
}
