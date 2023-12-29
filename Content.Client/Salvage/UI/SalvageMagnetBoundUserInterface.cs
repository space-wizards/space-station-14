using Content.Shared.Salvage;
using Content.Shared.Salvage.Magnet;

namespace Content.Client.Salvage.UI;

public sealed class SalvageMagnetBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private OfferingWindow? _window;

    public SalvageMagnetBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
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

        foreach (var seed in current.Offers)
        {
            var offer = salvageSystem.GetSalvageOffering(seed);
            var option = new OfferingWindowOption();

            switch (offer)
            {
                case AsteroidOffering asteroid:
                    option.Title = asteroid.DungeonConfig.ID;
                    break;
                case SalvageOffering salvage:
                    option.Title = salvage.SalvageMap.ID;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _window.AddOption(option);
        }
    }
}
