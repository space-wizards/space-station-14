using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Shared._Impstation.Kodepiia;
using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Kodepiia;

public sealed class KodepiiaScramblerSystem : SharedKodepiiaScramblerSystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaScramblerComponent, KodepiiaScramblerEvent>(Scramble);
        SubscribeLocalEvent<KodepiiaScramblerComponent, KodepiiaScramblerDoAfterEvent>(OnScrambleDoAfter);
    }
    private void Scramble(Entity<KodepiiaScramblerComponent> ent, ref KodepiiaScramblerEvent args)
    {
        var doargs = new DoAfterArgs(EntityManager, ent, 4, new KodepiiaScramblerDoAfterEvent(), ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };
        var popupOthers = Loc.GetString("kodepiia-scramble-others", ("name", Identity.Entity(ent, EntityManager)), ("ent", ent));
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.ScramblerSound, ent);
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;
    }

    private void OnScrambleDoAfter(Entity<KodepiiaScramblerComponent> ent, ref KodepiiaScramblerDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            _actionsSystem.SetCooldown(ent.Comp.ScramblerAction,TimeSpan.FromSeconds(10));
            return;
        }

        if (args.Handled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;
        var popupSelf = Loc.GetString("kodepiia-scramble-self", ("name", Identity.Entity(ent, EntityManager)));
        _humanoidAppearance.LoadProfile(ent, HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species), humanoid);
        _popup.PopupEntity(popupSelf, ent, ent);
        args.Handled = true;
    }
}
