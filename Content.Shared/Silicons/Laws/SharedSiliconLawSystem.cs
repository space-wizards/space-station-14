using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Shared.Audio;
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

        SubscribeLocalEvent<SiliconLawProviderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, SiliconEmaggedEvent>(OnEmagLawsAdded);
        SubscribeLocalEvent<SiliconLawProviderComponent, ComponentShutdown>(OnProviderShutdown);
        SubscribeLocalEvent<EmagSiliconLawComponent, GotEmaggedEvent>(OnGotEmagged);

        SubscribeLocalEvent<BorgChassisComponent, GetSiliconLawsEvent>(OnChassisGetLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnProviderGetLaws);

        InitializeUpdater();
    }

    #region Events

    private void OnMapInit(Entity<SiliconLawProviderComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Lawset = GetLawset(ent.Comp.Laws);

        // Don't dirty here, LawProvider gets dirtied in SyncToLawBound.
        SyncToLawBound(ent.AsNullable());
    }

    private void OnProviderShutdown(Entity<SiliconLawProviderComponent> ent, ref ComponentShutdown args)
    {
        var iterateEntities = ent.Comp.ExternalLawsets;
        foreach (var lawbound in iterateEntities)
        {
            UnlinkFromProvider(lawbound, ent.AsNullable());
        }
    }

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

    private void OnIonStormLaws(Entity<SiliconLawProviderComponent> ent, ref IonStormLawsEvent args)
    {
        // Emagged borgs are immune to ion storm
        if (!_emag.CheckFlag(ent, EmagType.Interaction))
        {
            ent.Comp.Lawset = args.Lawset;

            // gotta tell player to check their laws
            NotifyLawsChanged(ent, ent.Comp.LawUploadSound);

            // Show the silicon has been subverted.
            ent.Comp.Subverted = true;

            // new laws may allow antagonist behaviour so make it clear for admins
            if(_mind.TryGetMind(ent.Owner, out var mindId, out _))
                EnsureSubvertedSiliconRole(mindId);
        }

        Dirty(ent);
    }

    private void OnEmagLawsAdded(Entity<SiliconLawProviderComponent> ent, ref SiliconEmaggedEvent args)
    {
        // Show the silicon has been subverted.
        ent.Comp.Subverted = true;

        // Add the first emag law before the others
        ent.Comp.Lawset.Laws.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", Name(args.user)), ("title", Loc.GetString(ent.Comp.Lawset.ObeysTo))),
            Order = 0
        });

        //Add the secrecy law after the others
        ent.Comp.Lawset.Laws.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-secrecy", ("faction", Loc.GetString(ent.Comp.Lawset.ObeysTo))),
            Order = ent.Comp.Lawset.Laws.Max(law => law.Order) + 1
        });

        Dirty(ent);
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

    private void OnProviderGetLaws(Entity<SiliconLawProviderComponent> ent, ref GetSiliconLawsEvent args)
    {
        args.Laws = ent.Comp.Lawset.Clone();
        args.Handled = true;
    }

    #endregion Events

    protected void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        if (!_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSubvertedSilicon", silent: true);
    }

    protected void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindRemoveRole<SubvertedSiliconRoleComponent>(mindId);
    }

    /// <summary>
    /// Refreshes the laws of target entity and tries to link their <see cref="SiliconLawBoundComponent"/> to a <see cref="SiliconLawProviderComponent"/>
    /// </summary>
    /// <param name="ent"></param>
    public void FetchLawset(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new GetSiliconLawsEvent(ent);

        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
        {
            LinkToProvider(ent, ev.LinkedEntity ?? ent);
            return;
        }

        var xform = Transform(ent);

        if (_station.GetOwningStation(ent, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                LinkToProvider(ent, ev.LinkedEntity ?? ent);
                return;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                LinkToProvider(ent, ev.LinkedEntity ?? ent);
                return;
            }
        }

        RaiseLocalEvent(ref ev);
        if (ev.Handled)
        {
            LinkToProvider(ent, ev.LinkedEntity ?? ent);
        }
    }

    /// <summary>
    /// Get the current laws of this silicon.
    /// </summary>
    /// <param name="ent">The silicon to get the laws of.</param>
    /// <returns>The lawset.</returns>
    public SiliconLawset GetProviderLaws(Entity<SiliconLawProviderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new SiliconLawset();

        return ent.Comp.Lawset;
    }

    /// <summary>
    /// Get the current laws of this silicon.
    /// </summary>
    /// <param name="ent">The silicon to get the laws of.</param>
    /// <returns>The lawset.</returns>
    public SiliconLawset GetBoundLaws(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new SiliconLawset();

        return ent.Comp.Lawset;
    }

    private void OnGotEmagged(Entity<EmagSiliconLawComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
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

        var ev = new SiliconEmaggedEvent(args.UserUid);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.OwnerName = Name(args.UserUid);

        NotifyLawsChanged(ent, ent.Comp.EmaggedSound);
        if(_mind.TryGetMind(ent, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        _stunSystem.TryUpdateParalyzeDuration(ent, ent.Comp.StunTime);

        args.Handled = true;
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(ProtoId<SiliconLawsetPrototype>? lawset)
    {
        if (!_prototype.TryIndex(lawset, out var proto))
            return new SiliconLawset();

        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(_prototype.Index<SiliconLawPrototype>(law).ShallowClone());
        }
        laws.ObeysTo = proto.ObeysTo;

        return laws;
    }

    public void SetProviderLaws(Entity<SiliconLawProviderComponent?> ent, List<SiliconLaw> newLaws, SoundSpecifier? cue = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Lawset.Laws = newLaws;
        SyncToLawBound(ent);
    }

    /// <summary>
    /// Set the laws of a silicon entity while notifying the player.
    /// </summary>
    public void UpdateLaws(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<SiliconLawProviderComponent>(ent.Comp.LawsetProvider, out var provider))
            return;

        ent.Comp.Lawset = provider.Lawset.Clone();
        Dirty(ent);
    }

    public void LinkToProvider(Entity<SiliconLawBoundComponent?> lawboundEnt,
        Entity<SiliconLawProviderComponent?> providerEnt)
    {
        if (!Resolve(providerEnt, ref providerEnt.Comp))
            return;

        if (!Resolve(lawboundEnt, ref lawboundEnt.Comp))
            return;

        lawboundEnt.Comp.LawsetProvider = providerEnt;
        providerEnt.Comp.ExternalLawsets.Add(lawboundEnt.Owner);
        UpdateLaws(lawboundEnt);
        Dirty(providerEnt);
    }

    public void UnlinkFromProvider(Entity<SiliconLawBoundComponent?> lawboundEnt,
        Entity<SiliconLawProviderComponent?> providerEnt)
    {
        if (!Resolve(providerEnt, ref providerEnt.Comp))
            return;

        if (!Resolve(lawboundEnt, ref lawboundEnt.Comp))
            return;

        lawboundEnt.Comp.LawsetProvider = null;
        providerEnt.Comp.ExternalLawsets.Remove(lawboundEnt);
        Dirty(lawboundEnt);
        Dirty(providerEnt);
    }

    public void UnlinkFromProvider(Entity<SiliconLawBoundComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (TryComp<SiliconLawProviderComponent>(ent.Comp.LawsetProvider, out var provider))
        {
            provider.ExternalLawsets.Remove(ent);
            ent.Comp.LawsetProvider = null;
            Dirty(ent);
        }
    }

    private void SyncToLawBound(Entity<SiliconLawProviderComponent?> ent, SoundSpecifier? cue = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // We don't wanna iterate on the pure external lawsets cause we remove them in iteration.
        var iteratedEntities = ent.Comp.ExternalLawsets;

        foreach (var lawboundEnt in iteratedEntities)
        {
            if (!TryComp<SiliconLawBoundComponent>(lawboundEnt, out var lawboundComp))
            {
                UnlinkFromProvider((lawboundEnt, lawboundComp));
                continue;
            }

            lawboundComp.Lawset = ent.Comp.Lawset.Clone();
            lawboundComp.LawsetProvider = ent.Owner;
            Dirty(lawboundEnt, lawboundComp);
            NotifyLawsChanged(lawboundEnt, cue);
        }

        Dirty(ent);
    }

    public virtual void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {

    }

    public virtual void NotifyLaws(EntityUid uid, SoundSpecifier? cue = null)
    {

    }
}

[ByRefEvent]
public record struct SiliconEmaggedEvent(EntityUid user);

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
