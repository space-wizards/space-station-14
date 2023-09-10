using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Content.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract class SharedSiliconLawSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EmagSiliconLawComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    protected virtual void OnGotEmagged(EntityUid uid, EmagSiliconLawComponent component, ref GotEmaggedEvent args)
    {
        if (component.RequireOpenPanel &&
            TryComp<WiresPanelComponent>(uid, out var panel) &&
            !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("law-emag-require-panel"), uid, args.UserUid);
            return;
        }

        _audio.Play(component.EmagSound, Filter.Pvs(uid), uid, true);
        component.OwnerName = Name(args.UserUid);
        args.Handled = true;
    }
}
