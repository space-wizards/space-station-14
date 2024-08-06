using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Handles emagging whitelisted objects when clicked.
/// </summary>
public sealed class EmagProviderSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagProviderComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
    }

    /// <summary>
    /// Emag clicked entities that are on the whitelist.
    /// </summary>
    private void OnBeforeInteractHand(Entity<EmagProviderComponent> ent, ref BeforeInteractHandEvent args)
    {
        // TODO: change this into a generic check event thing
        if (args.Handled || !_gloves.AbilityCheck(ent, args, out var target))
            return;

        var (uid, comp) = ent;

        // only allowed to emag entities on the whitelist
        if (_whitelist.IsWhitelistFail(comp.Whitelist, target))
            return;

        // only allowed to emag non-immune entities
        if (_tag.HasTag(target, comp.EmagImmuneTag))
            return;

        var handled = _emag.DoEmagEffect(uid, target);
        if (!handled)
            return;

        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(uid):player} emagged {ToPrettyString(target):target}");
        var ev = new EmaggedSomethingEvent(target);
        RaiseLocalEvent(uid, ref ev);
        args.Handled = true;
    }
}

/// <summary>
/// Raised on the player when emagging something.
/// </summary>
[ByRefEvent]
public record struct EmaggedSomethingEvent(EntityUid Target);
