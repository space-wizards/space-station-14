using Content.Server.Construction;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class TraversalDistorterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TraversalDistorterComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<TraversalDistorterComponent, ActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<TraversalDistorterComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TraversalDistorterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<TraversalDistorterComponent, UpgradeExamineEvent>(OnUpgradeExamine);

        SubscribeLocalEvent<TraversalDistorterComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<TraversalDistorterComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnInit(EntityUid uid, TraversalDistorterComponent component, MapInitEvent args)
    {
        component.NextActivation = _timing.CurTime;
    }

    private void OnInteract(EntityUid uid, TraversalDistorterComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !this.IsPowered(uid, EntityManager))
            return;
        if (_timing.CurTime < component.NextActivation)
            return;
        args.Handled = true;
        component.NextActivation = _timing.CurTime + component.ActivationDelay;

        component.BiasDirection = component.BiasDirection == BiasDirection.In
            ? BiasDirection.Out
            : BiasDirection.In;

        var toPopup = string.Empty;
        switch (component.BiasDirection)
        {
            case BiasDirection.In:
                toPopup = Loc.GetString("traversal-distorter-set-in");
                break;
            case BiasDirection.Out:
                toPopup = Loc.GetString("traversal-distorter-set-out");
                break;
        }
        _popup.PopupEntity(toPopup, uid);
    }

    private void OnExamine(EntityUid uid, TraversalDistorterComponent component, ExaminedEvent args)
    {
        string examine = string.Empty;
        switch (component.BiasDirection)
        {
            case BiasDirection.In:
                examine = Loc.GetString("traversal-distorter-desc-in");
                break;
            case BiasDirection.Out:
                examine = Loc.GetString("traversal-distorter-desc-out");
                break;
        }
        
        args.PushMarkup(examine);
    }

    private void OnRefreshParts(EntityUid uid, TraversalDistorterComponent component, RefreshPartsEvent args)
    {
        var biasRating = args.PartRatings[component.MachinePartBiasChance];

        component.BiasChance = component.BaseBiasChance * MathF.Pow(component.PartRatingBiasChance, biasRating - 1);
    }

    private void OnUpgradeExamine(EntityUid uid, TraversalDistorterComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("traversal-distorter-upgrade-bias", component.BiasChance / component.BaseBiasChance);
    }

    private void OnItemPlaced(EntityUid uid, TraversalDistorterComponent component, ref ItemPlacedEvent args)
    {
        var bias = EnsureComp<BiasedArtifactComponent>(args.OtherEntity);
        bias.Provider = uid;
    }

    private void OnItemRemoved(EntityUid uid, TraversalDistorterComponent component, ref ItemRemovedEvent args)
    {
        var otherEnt = args.OtherEntity;
        if (TryComp<BiasedArtifactComponent>(otherEnt, out var bias) && bias.Provider == uid)
            RemComp(otherEnt, bias);
    }
}
