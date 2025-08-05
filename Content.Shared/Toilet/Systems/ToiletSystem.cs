using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Plunger.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Toilet.Components;

namespace Content.Shared.Toilet.Systems;

/// <summary>
/// Handles sprite changes for both toilet seat up and down as well as for lid
/// open and closed. Handles interactions with hidden stash
/// </summary>
public sealed class ToiletSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToiletComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToiletComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleSeatVerb);
        SubscribeLocalEvent<ToiletComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnMapInit(Entity<ToiletComponent> ent, ref MapInitEvent args)
    {
        if (_random.Prob(0.5f))
        {
            ent.Comp.ToggleSeat = true;
            Dirty(ent);
        }

        if (_random.Prob(0.3f)
            && TryComp<PlungerUseComponent>(ent, out var plunger))
        {
            plunger.NeedsPlunger = true;
            Dirty(ent, plunger);
        }

        UpdateAppearance(ent);
    }

    public bool CanToggle(EntityUid uid)
    {
        return TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count == 0;
    }

    private void OnToggleSeatVerb(Entity<ToiletComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null || !CanToggle(ent))
            return;

        var user = args.User;
        AlternativeVerb toggleVerb = new() { Act = () => ToggleToiletSeat(ent.AsNullable(), user) };

        if (ent.Comp.ToggleSeat)
        {
            toggleVerb.Text = Loc.GetString("toilet-seat-close");
            toggleVerb.Icon =
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            toggleVerb.Text = Loc.GetString("toilet-seat-open");
            toggleVerb.Icon =
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }
        args.Verbs.Add(toggleVerb);
    }

    private void OnActivateInWorld(Entity<ToiletComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        ToggleToiletSeat(ent.AsNullable(), args.User);
    }

    public void ToggleToiletSeat(Entity<ToiletComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.ToggleSeat = !ent.Comp.ToggleSeat;

        _audio.PlayPredicted(ent.Comp.SeatSound, ent, user);
        UpdateAppearance((ent, ent.Comp));
        Dirty(ent);
    }

    private void UpdateAppearance(Entity<ToiletComponent> ent)
    {
        _appearance.SetData(ent,
            ToiletVisuals.SeatVisualState,
            ent.Comp.ToggleSeat ? SeatVisualState.SeatUp : SeatVisualState.SeatDown);
    }
}
