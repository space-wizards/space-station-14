using Content.Shared.Administration.Logs;
using Content.Shared.Emag.Systems;
using Content.Shared.Database;
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
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagProviderComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
    }

    /// <summary>
    /// Emag clicked entities that are on the whitelist.
    /// </summary>
    private void OnBeforeInteractHand(EntityUid uid, EmagProviderComponent comp, BeforeInteractHandEvent args)
    {
        // TODO: change this into a generic check event thing
        if (args.Handled || !_gloves.AbilityCheck(uid, args, out var target))
            return;

        // only allowed to emag entities on the whitelist
        if (comp.Whitelist != null && !comp.Whitelist.IsValid(target, EntityManager))
            return;

        // only allowed to emag non-immune entities
        if (_tags.HasTag(target, comp.EmagImmuneTag))
            return;

        var handled = _emag.DoEmagEffect(uid, target);
        if (!handled)
            return;

        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(uid):player} emagged {ToPrettyString(target):target}");
        var ev = new EmaggedSomethingEvent(target);
        RaiseLocalEvent(uid, ref ev);
        args.Handled = true;
    }

    /// <summary>
    /// Set the whitelist for emagging something outside of yaml.
    /// </summary>
    public void SetWhitelist(EntityUid uid, EntityWhitelist? whitelist, EmagProviderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Whitelist = whitelist;
        Dirty(uid, comp);
    }
}

/// <summary>
/// Raised on the player when emagging something.
/// </summary>
[ByRefEvent]
public record struct EmaggedSomethingEvent(EntityUid Target);
