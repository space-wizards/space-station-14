using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
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

        SubscribeLocalEvent<SiliconLawProviderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawProviderComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);

        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, SiliconEmaggedEvent>(OnEmagLawsAdded);
        SubscribeLocalEvent<EmagSiliconLawComponent, GotEmaggedEvent>(OnGotEmagged);

        InitializeUpdater();
    }

    private void OnMapInit(Entity<SiliconLawProviderComponent> ent, ref MapInitEvent args)
    {
        // Are we supposed to fetch laws on init?
        if (ent.Comp.FetchOnInit)
        {
            FetchLawset(ent); // If so, we raise the necessary event and get whatever it returns to be our lawset.
            return;
        }

        // If we aren't supposed to fetch the lawset, we use the one provided in the component.
        if (_prototype.TryIndex(ent.Comp.Laws, out var lawset))
        {
            ent.Comp.Lawset = GetLawset(lawset); // If the lawset is valid, we save it as the currently used one.
            return;
        }

        // We don't try to get a lawset at all? Guess it'll be empty.
        ent.Comp.Lawset = new SiliconLawset();
    }

    private void OnToggleLawsScreen(Entity<SiliconLawProviderComponent> ent, ref ToggleLawsScreenEvent args)
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
    /// Refreshes the laws of target entity using an event and stores them on the <see cref="SiliconLawProviderComponent"/>
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void FetchLawset(EntityUid uid, SiliconLawProviderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var ev = new GetSiliconLawsEvent(uid);

        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
        {
            component.Lawset = ev.Laws;
            return;
        }

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                component.Lawset = ev.Laws;
                return;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                component.Lawset = ev.Laws;
                return;
            }
        }

        RaiseLocalEvent(ref ev);
        if (ev.Handled)
            component.Lawset = ev.Laws;
    }

    /// <summary>
    /// Get the current laws of this silicon.
    /// </summary>
    /// <param name="uid">The silicon to get the laws of.</param>
    /// <param name="component">Nullable SiliconLawProviderComponent, which gets resolved.</param>
    /// <param name="refresh">Whether to run FetchLawset before getting the laws, effectively finding them via an event instead.</param>
    /// <returns></returns>
    public SiliconLawset GetLaws(EntityUid uid, SiliconLawProviderComponent? component = null, bool refresh = false)
    {
        if (!Resolve(uid, ref component))
            return new SiliconLawset();

        if (refresh)
            FetchLawset(uid, component);

        return component.Lawset;
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

    /// <summary>
    /// Set the laws of a silicon entity while notifying the player.
    /// </summary>
    public void SetLaws(List<SiliconLaw> newLaws, EntityUid target, SoundSpecifier? cue = null)
    {
        if (!TryComp<SiliconLawProviderComponent>(target, out var component))
            return;

        if (component.Lawset == null)
            component.Lawset = new SiliconLawset();

        component.Lawset.Laws = newLaws;
        Dirty(target, component);
        NotifyLawsChanged(target, cue);
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

[ByRefEvent]
public record struct GetSiliconLawsEvent(EntityUid Entity)
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
