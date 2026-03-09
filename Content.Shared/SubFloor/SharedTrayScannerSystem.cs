using Content.Shared.Database;
using Content.Shared.Eye;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SubFloor;

public abstract class SharedTrayScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public const float SubfloorRevealAlpha = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);
        SubscribeLocalEvent<TrayScannerComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);
        SubscribeLocalEvent<TrayScannerComponent, GotEquippedHandEvent>(OnTrayHandEquipped);
        SubscribeLocalEvent<TrayScannerComponent, GotUnequippedHandEvent>(OnTrayHandUnequipped);
        SubscribeLocalEvent<TrayScannerComponent, GotEquippedEvent>(OnTrayEquipped);
        SubscribeLocalEvent<TrayScannerComponent, GotUnequippedEvent>(OnTrayUnequipped);
        SubscribeLocalEvent<TrayScannerUserComponent, GetVisMaskEvent>(OnUserGetVis);
    }

    private void OnAddSwitchModeVerb(Entity<TrayScannerComponent> scanner, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !HasComp<TrayScannerComponent>(args.Target) || !scanner.Comp.Enabled)
            return;

        var user = args.User;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("tray-scanner-switch-mode"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => SwitchMode(scanner, user),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Returns true if the last time this method was called is earlier than the scanner use delay.
    /// </summary>
    private bool Delay(Entity<TrayScannerComponent> scanner)
    {
        var currentTime = _gameTiming.CurTime;
        if (currentTime < scanner.Comp.LastUseAttempt + scanner.Comp.UseDelay)
            return true;

        scanner.Comp.LastUseAttempt = currentTime;
        return false;
    }

    private static TrayScannerMode Next(TrayScannerMode mode)
    {
        return mode switch
        {
            TrayScannerMode.All => TrayScannerMode.Wiring,
            TrayScannerMode.Wiring => TrayScannerMode.Piping,
            TrayScannerMode.Piping => TrayScannerMode.All,
            _ => TrayScannerMode.All,
        };
    }

    private void SwitchMode(Entity<TrayScannerComponent> scanner, EntityUid? userUid)
    {
        if (!userUid.HasValue)
            return;

        if (Delay(scanner))
            return;

        scanner.Comp.Mode = Next(scanner.Comp.Mode);
        Dirty(scanner);

        var pitch = scanner.Comp.Mode == TrayScannerMode.All ? 1 : 0.8f;
        _audio.PlayPredicted(scanner.Comp.SoundSwitchMode, scanner, userUid, AudioParams.Default.WithVolume(1.5f).WithPitchScale(pitch));
    }

    private void OnUserGetVis(Entity<TrayScannerUserComponent> scanner, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    private void OnEquip(EntityUid user)
    {
        if (_netMan.IsClient)
            return;

        var comp = EnsureComp<TrayScannerUserComponent>(user);
        comp.Count++;

        if (comp.Count > 1)
            return;

        _eye.RefreshVisibilityMask(user);
    }

    private void OnUnequip(EntityUid user)
    {
        if (_netMan.IsClient)
            return;

        if (!TryComp(user, out TrayScannerUserComponent? comp))
            return;

        comp.Count--;

        if (comp.Count > 0)
            return;

        RemComp<TrayScannerUserComponent>(user);
        _eye.RefreshVisibilityMask(user);
    }

    private void OnTrayHandUnequipped(Entity<TrayScannerComponent> ent, ref GotUnequippedHandEvent args)
    {
        OnUnequip(args.User);
    }

    private void OnTrayHandEquipped(Entity<TrayScannerComponent> ent, ref GotEquippedHandEvent args)
    {
        OnEquip(args.User);
    }

    private void OnTrayUnequipped(Entity<TrayScannerComponent> ent, ref GotUnequippedEvent args)
    {
        OnUnequip(args.Equipee);
    }

    private void OnTrayEquipped(Entity<TrayScannerComponent> ent, ref GotEquippedEvent args)
    {
        OnEquip(args.Equipee);
    }

    private void OnTrayScannerActivate(Entity<TrayScannerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            _appearance.SetData(ent, TrayScannerVisual.Visual, ent.Comp.Enabled ? TrayScannerVisual.On : TrayScannerVisual.Off, appearance);
        }

        args.Handled = true;
    }
}
