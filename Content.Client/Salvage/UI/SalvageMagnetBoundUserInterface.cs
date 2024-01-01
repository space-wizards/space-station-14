using Content.Shared.Salvage;
using Content.Shared.Salvage.Magnet;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Salvage.UI;

public sealed class SalvageMagnetBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private OfferingWindow? _window;

    public SalvageMagnetBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = new OfferingWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SalvageMagnetBoundUserInterfaceState current || _window == null)
            return;

        _window.ClearOptions();

        var salvageSystem = _entManager.System<SharedSalvageSystem>();
        _window.NextOffer = current.NextOffer;
        _window.Progression = current.EndTime;
        _window.Claimed = current.EndTime != null;
        _window.Cooldown = current.Cooldown;
        _window.ProgressionCooldown = current.Duration;

        for (var i = 0; i < current.Offers.Count; i++)
        {
            var seed = current.Offers[i];
            var offer = salvageSystem.GetSalvageOffering(seed);
            var option = new OfferingWindowOption();
            option.Disabled = current.EndTime != null;
            option.Claimed = current.ActiveSeed == seed;

            switch (offer)
            {
                case AsteroidOffering asteroid:
                    option.Title = Loc.GetString($"dungeon-config-proto-{asteroid.DungeonConfig.ID}");

                    foreach (var (resource, count) in asteroid.MarkerLayers)
                    {
                        option.AddContent(new Label()
                        {
                            Text = Loc.GetString("salvage-magnet-resources",
                                ("resource", resource),
                                ("count", count))
                        });
                    }

                    break;
                case SalvageOffering salvage:
                    option.Title = Loc.GetString($"salvage-map-proto-{salvage.SalvageMap.ID}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _window.AddOption(option);
        }
    }
}
