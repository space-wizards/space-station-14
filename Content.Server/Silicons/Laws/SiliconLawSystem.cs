using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Overlays;
using Content.Shared.Radio.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed partial class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IonLawSystem _ionLaw = default!;

    private static readonly ProtoId<SiliconLawsetPrototype> DefaultCrewLawset = "Crewsimov";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SiliconLawBoundComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, MindAddedMessage>(OnLawProviderMindAdded);
        SubscribeLocalEvent<SiliconLawProviderComponent, MindRemovedMessage>(OnLawProviderMindRemoved);
        SubscribeLocalEvent<SiliconLawProviderComponent, SiliconEmaggedEvent>(OnEmagLawsAdded);

        SubscribeLocalEvent<IonStormSiliconLawComponent, IonStormEvent>(OnIonStormEvent);
    }

    private void OnMapInit(EntityUid uid, SiliconLawBoundComponent component, MapInitEvent args)
    {
        GetLaws(uid, component);
    }

    private void OnMindAdded(EntityUid uid, SiliconLawBoundComponent component, MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.FromHex("#5ed7aa"));

        if (!TryComp<SiliconLawProviderComponent>(uid, out var lawcomp))
            return;

        if (!lawcomp.Subverted)
            return;

        var modifedLawMsg = Loc.GetString("laws-notify-subverted");
        var modifiedLawWrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", modifedLawMsg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, modifedLawMsg, modifiedLawWrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);
    }

    private void OnLawProviderMindAdded(Entity<SiliconLawProviderComponent> ent, ref MindAddedMessage args)
    {
        if (!ent.Comp.Subverted)
            return;
        EnsureSubvertedSiliconRole(args.Mind);
    }

    private void OnLawProviderMindRemoved(Entity<SiliconLawProviderComponent> ent, ref MindRemovedMessage args)
    {
        if (!ent.Comp.Subverted || args.TransferEntity == null)
            return;

        RemoveSubvertedSiliconRole(args.Mind);
    }


    private void OnToggleLawsScreen(EntityUid uid, SiliconLawBoundComponent component, ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(EntityUid uid, SiliconLawBoundComponent component, BoundUIOpenedEvent args)
    {
        TryComp(uid, out IntrinsicRadioTransmitterComponent? intrinsicRadio);
        var radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(uid).Laws, radioChannels);
        _userInterface.SetUiState(args.Entity, SiliconLawsUiKey.Key, state);
    }

    private void OnPlayerSpawnComplete(EntityUid uid, SiliconLawBoundComponent component, PlayerSpawnCompleteEvent args)
    {
        component.LastLawProvider = args.Station;
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled)
            return;

        if (component.Lawset == null)
            component.Lawset = GetLawset(component.Laws);

        args.Laws = component.Lawset;

        args.Handled = true;
    }

    private void OnEmagLawsAdded(EntityUid uid, SiliconLawProviderComponent component, ref SiliconEmaggedEvent args)
    {
        if (component.Lawset == null)
            component.Lawset = GetLawset(component.Laws);

        // Show the silicon has been subverted.
        component.Subverted = true;

        // Add the first emag law before the others
        component.Lawset?.Laws.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", Name(args.user)), ("title", Loc.GetString(component.Lawset.ObeysTo))),
            Order = 0
        });

        //Add the secrecy law after the others
        component.Lawset?.Laws.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-secrecy", ("faction", Loc.GetString(component.Lawset.ObeysTo))),
            Order = component.Lawset.Laws.Max(law => law.Order) + 1
        });
    }

    protected override void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        base.EnsureSubvertedSiliconRole(mindId);

        if (!_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSubvertedSilicon", silent: true);
    }

    protected override void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        base.RemoveSubvertedSiliconRole(mindId);

        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindRemoveRole<SubvertedSiliconRoleComponent>(mindId);
    }

    public SiliconLawset GetLaws(EntityUid uid, SiliconLawBoundComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new SiliconLawset();

        var ev = new GetSiliconLawsEvent(uid);

        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
        {
            component.LastLawProvider = uid;
            return ev.Laws;
        }

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = station;
                return ev.Laws;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = grid;
                return ev.Laws;
            }
        }

        if (component.LastLawProvider == null ||
            Deleted(component.LastLawProvider) ||
            Terminating(component.LastLawProvider.Value))
        {
            component.LastLawProvider = null;
        }
        else
        {
            RaiseLocalEvent(component.LastLawProvider.Value, ref ev);
            if (ev.Handled)
            {
                return ev.Laws;
            }
        }

        RaiseLocalEvent(ref ev);
        return ev.Laws;
    }

    public override void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(uid, cue);

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(ProtoId<SiliconLawsetPrototype> lawset)
    {
        var proto = ProtoMan.Index(lawset);
        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(ProtoMan.Index<SiliconLawPrototype>(law).ShallowClone());
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
        NotifyLawsChanged(target, cue);
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // TODO: Prediction dump this
        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out var provider))
            return;

        var lawset = provider.Lawset ?? GetLawset(provider.Laws);

        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
        {
            if (TryComp<ShowCrewIconsComponent>(update, out var crewIconComp))
            {
                crewIconComp.UncertainCrewBorder = DefaultCrewLawset != provider.Laws;
                Dirty(update, crewIconComp);
            }
            SetLaws(lawset.Laws, update, provider.LawUploadSound);
        }
    }

    private void OnIonStormEvent(EntityUid uid, IonStormSiliconLawComponent component, ref IonStormEvent args)
    {
        if (!TryComp<SiliconLawBoundComponent>(uid, out var lawBound))
            return;

        if (!TryComp<IonStormSiliconLawComponent>(uid, out var target))
            return;

        var laws = GetLaws(uid, lawBound);
        if (laws.Laws.Count == 0)
            return;

        // clone it so not modifying stations lawset
        laws = laws.Clone();

        SwapRandomLawset(target.RandomLawsetChance, ref laws, target.IonRandomLawsets);

        ShuffleLaws(target.ShuffleChance, ref laws);

        RemoveRandomLaw(target.RemoveChance, ref laws);

        // generate a new law...
        var newLaw = _ionLaw.GetIonLaw();

        if (string.IsNullOrEmpty(newLaw))
            return;

        RandomReplaceOrAddLaw(target.ReplaceChance, ref laws, newLaw);

        ReorderObfuscatedLaws(ref laws);

        // adminlog is used to prevent adminlog spam.
        if (args.Adminlog)
            _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(uid):silicon} had its laws changed by an ion storm to {laws.LoggingString()}");

        // laws unique to this silicon, dont use station laws anymore
        var lawProvider = EnsureComp<SiliconLawProviderComponent>(uid);

        SetAlteredLawset(uid, lawProvider, laws);
    }

    /// <summary>
    /// Has a chance to swap referenced lawset with provided lawset list
    /// </summary>
    /// <param name="chance">The chance it will swap the lawset to a new one</param>
    /// <param name="laws">The lawset you might swap</param>
    /// <param name="randomLawset">The lookup table for acceptable lawsets</param>
    public void SwapRandomLawset(float chance, ref SiliconLawset laws, ProtoId<WeightedRandomPrototype> randomLawset)
    {
        // try to swap it out with a random lawset
        if (!_robustRandom.Prob(chance))
            return;

        var lawsets = ProtoMan.Index<WeightedRandomPrototype>(randomLawset);
        var lawset = lawsets.Pick(_robustRandom);
        laws = GetLawset(lawset);
    }

    /// <summary>
    /// Has a chance to shuffle referenced lawset
    /// </summary>
    /// <param name="chance">The chance to shuffle</param>
    /// <param name="laws">The lawset you might shuffle</param>
    public void ShuffleLaws(float chance, ref SiliconLawset laws)
    {
        // shuffle them all
        if (!_robustRandom.Prob(chance))
            return;

        // hopefully work with existing glitched laws if there are multiple ion storms
        var baseOrder = FixedPoint2.New(1);
        foreach (var law in laws.Laws)
        {
            if (law.Order < baseOrder)
                baseOrder = law.Order;
        }

        _robustRandom.Shuffle(laws.Laws);

        // change order based on shuffled position
        for (int i = 0; i < laws.Laws.Count; i++)
        {
            laws.Laws[i].Order = baseOrder + i;
        }
    }

    /// <summary>
    /// Has a chance to remove a random law from a referenced lawset
    /// </summary>
    /// <param name="chance">The chance to remove a law</param>
    /// <param name="laws">The lawset you might remove from</param>
    public void RemoveRandomLaw(float chance, ref SiliconLawset laws)
    {
        // see if we can remove a random law
        if (laws.Laws.Count <= 0 || !_robustRandom.Prob(chance))
            return;

        var i = _robustRandom.Next(laws.Laws.Count);
        laws.Laws.RemoveAt(i);
    }

    /// <summary>
    /// Has a chance to replace or add a given law to the referenced lawset
    /// </summary>
    /// <param name="chance">The chance to do the replacement</param>
    /// <param name="laws">The lawset you might replace from</param>
    /// <param name="newLaw">The string of the new law</param>
    /// <returns></returns>
    public void RandomReplaceOrAddLaw(float chance, ref SiliconLawset laws, string newLaw)
    {
        // see if the law we add will replace a random existing law or be a new glitched order one
        if (laws.Laws.Count > 0 && _robustRandom.Prob(chance))
        {
            var i = _robustRandom.Next(laws.Laws.Count);
            laws.Laws[i] = new SiliconLaw()
            {
                LawString = newLaw,
                Order = laws.Laws[i].Order
            };
        }
        else
        {
            laws.Laws.Insert(0, new SiliconLaw
            {
                LawString = newLaw,
                Order = -1,
                LawIdentifierOverride = Loc.GetString("ion-storm-law-scrambled-number", ("length", _robustRandom.Next(5, 10)))
            });
        }
    }

    /// <summary>
    /// Reorders the obfuscated laws in referenced lawset so it properly follows law priority in the display
    /// </summary>
    /// <param name="laws">The lawset you would like to reorder</param>
    public static void ReorderObfuscatedLaws(ref SiliconLawset laws)
    {
        // sets all unobfuscated laws' indentifier in order from highest to lowest priority
        // This could technically override the Obfuscation from the code above, but it seems unlikely enough to basically never happen
        int orderDeduction = -1;

        for (int i = 0; i < laws.Laws.Count; i++)
        {
            var notNullIdentifier = laws.Laws[i].LawIdentifierOverride ?? (i - orderDeduction).ToString();

            if (notNullIdentifier.Any(char.IsSymbol))
            {
                orderDeduction += 1;
            }
            else
            {
                laws.Laws[i].LawIdentifierOverride = (i - orderDeduction).ToString();
            }
        }
    }

    /// <summary>
    /// Sets and notifies the lawset of law'd entity.
    /// Prefer using <see cref="SetLaws"/> on non silicon.
    /// </summary>
    /// <param name="uid">Entity you would like to set</param>
    /// <param name="lawProvider">The law provider of the entity</param>
    /// <param name="laws">The lawset you would like to set the provider to</param>
    public void SetAlteredLawset(EntityUid uid, SiliconLawProviderComponent lawProvider, SiliconLawset laws)
    {
        // Emagged borgs are immune to ion storm
        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        lawProvider.Lawset = laws;

        // gotta tell player to check their laws
        NotifyLawsChanged(uid, lawProvider.LawUploadSound);

        // Show the silicon has been subverted.
        lawProvider.Subverted = true;

        // new laws may allow antagonist behaviour so make it clear for admins
        if(_mind.TryGetMind(uid, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawBoundComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetLaws(lawbound).Laws)
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
