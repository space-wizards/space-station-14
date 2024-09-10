using System.Linq;
using Content.Client.Message;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Magnet;
using Robust.Client.UserInterface;
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

        _window = this.CreateWindow<OfferingWindow>();
        _window.Title = Loc.GetString("salvage-magnet-window-title");
        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SalvageMagnetBoundUserInterfaceState current || _window == null)
            return;

        _window.ClearOptions();

        var salvageSystem = _entManager.System<SharedSalvageSystem>();
        _window.NextOffer = current.NextOffer;
        _window.Progression = current.EndTime ?? TimeSpan.Zero;
        _window.Claimed = current.EndTime != null;
        _window.Cooldown = current.Cooldown;
        _window.ProgressionCooldown = current.Duration;

        for (var i = 0; i < current.Offers.Count; i++)
        {
            var seed = current.Offers[i];
            var offer = salvageSystem.GetSalvageOffering(seed);
            var option = new OfferingWindowOption();
            option.MinWidth = 210f;
            option.Disabled = current.EndTime != null;
            option.Claimed = current.ActiveSeed == seed;
            var claimIndex = i;

            option.ClaimPressed += _ =>
            {
                SendMessage(new MagnetClaimOfferEvent
                {
                    Index = claimIndex
                });
            };

            switch (offer)
            {
                case AsteroidOffering asteroid:
                    option.Title = Loc.GetString($"dungeon-config-proto-{asteroid.Id}");
                    var layerKeys = asteroid.MarkerLayers.Keys.ToList();
                    layerKeys.Sort();

                    foreach (var resource in layerKeys)
                    {
                        var count = asteroid.MarkerLayers[resource];

                        var container = new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            HorizontalExpand = true,
                        };

                        var resourceLabel = new Label
                        {
                            Text = Loc.GetString("salvage-magnet-resources",
                                ("resource", resource)),
                            HorizontalAlignment = Control.HAlignment.Left,
                        };

                        var countLabel = new Label
                        {
                            Text = Loc.GetString("salvage-magnet-resources-count", ("count", count)),
                            HorizontalAlignment = Control.HAlignment.Right,
                            HorizontalExpand = true,
                        };

                        container.AddChild(resourceLabel);
                        container.AddChild(countLabel);

                        option.AddContent(container);
                    }

                    break;
                case DebrisOffering debris:
                    option.Title = Loc.GetString($"salvage-magnet-debris-{debris.Id}");
                    break;
                case SalvageOffering salvage:
                    option.Title = Loc.GetString($"salvage-map-wreck");

                    var salvContainer = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        HorizontalExpand = true,
                    };

                    var sizeLabel = new Label
                    {
                        Text = Loc.GetString("salvage-map-wreck-desc-size"),
                        HorizontalAlignment = Control.HAlignment.Left,
                    };

                    var sizeValueLabel = new RichTextLabel
                    {
                        HorizontalAlignment = Control.HAlignment.Right,
                        HorizontalExpand = true,
                    };
                    sizeValueLabel.SetMarkup(Loc.GetString(salvage.SalvageMap.SizeString));

                    salvContainer.AddChild(sizeLabel);
                    salvContainer.AddChild(sizeValueLabel);

                    option.AddContent(salvContainer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _window.AddOption(option);
        }
    }
}
