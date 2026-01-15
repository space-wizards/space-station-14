using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract partial class SharedSiliconLawSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnLawBoundInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, ComponentShutdown>(OnLawBoundShutdown);

        SubscribeLocalEvent<EmagSiliconLawComponent, GotEmaggedEvent>(OnGotEmagged);

        SubscribeLocalEvent<BorgChassisComponent, GetSiliconLawsEvent>(OnChassisGetLaws);

        InitializeUpdater();
        InitializeProvider();
    }

    #region Events

    private void OnLawBoundShutdown(Entity<SiliconLawBoundComponent> ent, ref ComponentShutdown args)
    {
        UnlinkFromProvider(ent.AsNullable());
    }

    private void OnLawBoundInit(Entity<SiliconLawBoundComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.FetchOnInit)
            FetchLawset(ent.AsNullable());
    }

    private void OnToggleLawsScreen(Entity<SiliconLawBoundComponent> ent, ref ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;

        _userInterface.TryToggleUi(ent.Owner, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnGotEmagged(Entity<EmagSiliconLawComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction)
            || _emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (!TryComp<SiliconLawBoundComponent>(ent, out var lawboundComp))
            return;

        // prevent self-emagging
        if (ent.Owner == args.UserUid)
        {
            _popup.PopupClient(Loc.GetString("law-emag-cannot-emag-self"), ent, args.UserUid);
            return;
        }

        if (ent.Comp.RequireOpenPanel &&
            TryComp<WiresPanelComponent>(ent, out var panel) &&
            !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-panel"), ent, args.UserUid);
            return;
        }

        List<SiliconLaw> lawsToSwap = lawboundComp.Lawset.Laws;

        // Add the first emag law before the others
        lawsToSwap.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", Name(args.UserUid)), ("title", Loc.GetString(lawboundComp.Lawset.ObeysTo))),
            Order = 0
        });

        //Add the secrecy law after the others
        lawsToSwap.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-secrecy", ("faction", Loc.GetString(lawboundComp.Lawset.ObeysTo))),
            Order = lawboundComp.Lawset.Laws.Max(law => law.Order) + 1
        });

        if (ent.Comp.AffectProvider && TryComp<SiliconLawProviderComponent>(lawboundComp.LawsetProvider, out var lawProvider))
        {
            lawProvider.Subverted = true;
            SetProviderLaws((lawboundComp.LawsetProvider.Value, lawProvider), lawsToSwap, true);
            Dirty(lawboundComp.LawsetProvider.Value, lawProvider);
        }
        else
        {
            EnsureComp<SiliconLawProviderComponent>(ent, out var ensuredProvider);
            ensuredProvider.Subverted = true;
            SetProviderLaws((ent.Owner, ensuredProvider), lawsToSwap, true);
            LinkToProvider((ent, lawboundComp), (ent, ensuredProvider)); ;
        }

        ent.Comp.OwnerName = Name(args.UserUid);

        NotifyLawsChanged(ent, ent.Comp.EmaggedSound);
        if(_mind.TryGetMind(ent, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        _stunSystem.TryUpdateParalyzeDuration(ent, ent.Comp.StunTime);

        args.Handled = true;
    }

    private void OnChassisGetLaws(Entity<BorgChassisComponent> ent, ref GetSiliconLawsEvent args)
    {
        // Chassis specific laws take priority over brain laws.
        // We want this for things like Syndieborgs or Xenoborgs, who depend on chassis specific laws.
        if (TryComp<SiliconLawProviderComponent>(ent, out var chassisProvider))
        {
            Log.Debug("Fetched chassis laws.");
            args.Laws = chassisProvider.Lawset.Clone();
            args.Handled = true;
        }
        else if(TryComp<SiliconLawProviderComponent>(ent.Comp.BrainContainer.ContainedEntity, out var brainProvider))
        {
            Log.Debug("Fetched brain laws.");
            args.Laws = brainProvider.Lawset.Clone();
            args.LinkedEntity = ent.Comp.BrainContainer.ContainedEntity;
            args.Handled = true;
        }
    }
    #endregion Events
}

/// <summary>
/// An event used to get the laws of silicons roundstart, linking them to potential LawProviders.
/// </summary>
/// <param name="Entity">The entity we are gathering the laws for.</param>
/// <param name="LinkedEntity">The entity to to link to, if null, uses the entity this event was raised on.</param>
[ByRefEvent]
public record struct GetSiliconLawsEvent(EntityUid Entity, EntityUid? LinkedEntity = null)
{
    public SiliconLawset Laws = new();

    public bool Handled = false;
}

public sealed partial class ToggleLawsScreenEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SiliconLawsUiKey : byte
{
    Key
}
