using Content.Shared.Store;
using JetBrains.Annotations;
using System.Linq;
using Content.Client.Store.Systems;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Store.Ui;

[UsedImplicitly]
public sealed class StoreBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    [ViewVariables]
    private StoreMenu? _menu;

    [ViewVariables]
    private string _search = string.Empty;

    [ViewVariables]
    private HashSet<ListingDataWithCostModifiers> _listings = new();

    public StoreBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<StoreMenu>();

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            SendPredictedMessage(new StoreBuyListingMessage(listing.ID));
        };

        _menu.OnCategoryButtonPressed += (_, category) =>
        {
            _menu.CurrentCategory = category;
            _menu?.UpdateListing();
        };

        _menu.OnWithdrawAttempt += (_, type, amount) =>
        {
            SendPredictedMessage(new StoreRequestWithdrawMessage(type, amount));
        };

        _menu.SearchTextUpdated += (_, search) =>
        {
            _search = search.Trim().ToLowerInvariant();
            UpdateListingsWithSearchFilter();
        };

        _menu.OnRefundAttempt += _ =>
        {
            SendPredictedMessage(new StoreRequestRefundMessage());
        };

        Update();
    }

    public override void Update()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out StoreComponent? store))
            return;

        var player = _playerMan.LocalEntity;
        if (player == null)
            return;

        var showFooter = EntMan.HasComponent<RingerUplinkComponent>(Owner);
        var storeSystem = EntMan.System<StoreSystem>();

        _menu.Title = Loc.GetString(store.Name);
        _menu.SetFooterVisibility(showFooter);
        _menu.UpdateRefund(store.RefundAllowed);
        _menu.UpdateBalance(store.Balance);
        _listings = storeSystem.GetAvailableListings(player.Value, (Owner, store));

        UpdateListingsWithSearchFilter();
    }

    private void UpdateListingsWithSearchFilter()
    {
        if (_menu == null)
            return;

        var filteredListings = new HashSet<ListingDataWithCostModifiers>(_listings);
        if (!string.IsNullOrEmpty(_search))
        {
            filteredListings.RemoveWhere(listingData => !ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listingData, _prototypeManager).Trim().ToLowerInvariant().Contains(_search) &&
                                                        !ListingLocalisationHelpers.GetLocalisedDescriptionOrEntityDescription(listingData, _prototypeManager).Trim().ToLowerInvariant().Contains(_search));
        }
        _menu.PopulateStoreCategoryButtons(filteredListings);
        _menu.UpdateListing(filteredListings.ToList());
    }
}
