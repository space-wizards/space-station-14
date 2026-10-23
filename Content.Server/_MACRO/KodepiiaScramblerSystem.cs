using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._MACRO.Species.Kodepiia;
using Content.Shared._MACRO.Species.Kodepiia.Components;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._MACRO.Species.Kodepiia;
/// <inheritdoc/>
public sealed partial class KodepiiaScramblerSystem : SharedKodepiiaScramblerSystem
{
    [Dependency] private ActionsSystem _actionsSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaScramblerComponent, KodepiiaScramblerEvent>(Scramble);
        SubscribeLocalEvent<KodepiiaScramblerComponent, KodepiiaScramblerDoAfterEvent>(OnScrambleDoAfter);
    }
    private void Scramble(Entity<KodepiiaScramblerComponent> ent, ref KodepiiaScramblerEvent args)
    {
        // Setup the doafter.
        var doargs = new DoAfterArgs(EntityManager, ent, 4, new KodepiiaScramblerDoAfterEvent(), ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        //Give the whole "im scrambling here" popup.
        var popupOthers = Loc.GetString(ent.Comp.OnScrambleStart, ("name", Identity.Entity(ent, EntityManager)), ("ent", ent));
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);

        //Play your sound.
        _audio.PlayEntity(ent.Comp.ScramblerSound, ent, ent);

        //Start the doafter.
        _doAfter.TryStartDoAfter(doargs);

        args.Handled = true;
    }

    /// <summary>
    /// Function triggered after the doafter is completed
    /// </summary>
    private void OnScrambleDoAfter(Entity<KodepiiaScramblerComponent> ent, ref KodepiiaScramblerDoAfterEvent args)
    {
        // If the scramble is cancelled, reset the cooldown.. But not to none so you can't spam the sound.
        if (args.Cancelled)
        {
            _actionsSystem.SetCooldown(ent.Comp.ScramblerAction, TimeSpan.FromSeconds(10));
            return;
        }

        if (args.Handled)
            return;

        // If we aren't a humanoid with a profile we can't scramble shit.
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid))
            return;

        // Get a random profile with the species this entity is.
        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);

        // Apply the profile to the visual body and regular body,
        _visualBody.ReplaceProfileWith(ent.Owner, profile);
        _humanoidProfile.ApplyProfileTo(ent.Owner, profile);

        // Handle more popping up stuff.
        var popupSelf = Loc.GetString(ent.Comp.OnScrambleCompleted, ("name", Identity.Entity(ent, EntityManager)));
        _popup.PopupEntity(popupSelf, ent, ent);
        args.Handled = true;
    }
}
