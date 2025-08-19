using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components; // Starlight
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes; // Starlight
using Content.Shared.Emag.Components; //Starlight

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract partial class SharedSiliconLawSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!; // Starlight
    [Dependency] private readonly IEntityManager _entMan = default!; // Starlight

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeUpdater();
        SubscribeLocalEvent<EmagSiliconLawComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    private void OnGotEmagged(EntityUid uid, EmagSiliconLawComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        // prevent self-emagging
        if (uid == args.UserUid)
        {
            _popup.PopupClient(Loc.GetString("law-emag-cannot-emag-self"), uid, args.UserUid);
            return;
        }

        if (component.RequireOpenPanel &&
            TryComp<WiresPanelComponent>(uid, out var panel) &&
            !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-panel"), uid, args.UserUid);
            return;
        }

        //#region Starlight
        SiliconLawsetPrototype? proto = null;
        if (args.EmagComponent != null)
        {
            if (args.EmagComponent.DestroyTransponder)
            {
                RemComp<BorgTransponderComponent>(uid);
            }

            if (args.EmagComponent.Components != null)
                _entMan.AddComponents(uid, args.EmagComponent.Components);
        }
        //#endregion Starlight

        var ev = new SiliconEmaggedEvent(args.UserUid, args.EmagComponent); // Starlight
        RaiseLocalEvent(uid, ref ev);

        component.OwnerName = Name(args.UserUid);

        NotifyLawsChanged(uid, component.EmaggedSound);
        if (_mind.TryGetMind(uid, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        _stunSystem.TryUpdateParalyzeDuration(uid, component.StunTime);

        args.Handled = true;
    }

    public virtual void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {

    }

    protected virtual void EnsureSubvertedSiliconRole(EntityUid mindId)
    {

    }

    protected virtual void RemoveSubvertedSiliconRole(EntityUid mindId)
    {

    }

    #region Starlight
    public void SetLawset(EntityUid entity, SiliconLawset? laws)
    {
        if (!TryComp<SiliconLawProviderComponent>(entity, out var provider))
            return;
        provider.Lawset = laws;
    }
    #endregion
}

[ByRefEvent]
public record struct SiliconEmaggedEvent(EntityUid user, EmagComponent? emagComp);
