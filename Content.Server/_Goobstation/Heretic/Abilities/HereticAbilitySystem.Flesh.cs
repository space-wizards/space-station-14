using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Heretic;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    private void SubscribeFlesh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);
        SubscribeLocalEvent<HereticComponent, EventHereticFleshAscend>(OnFleshAscendPolymorph);
    }

    private void OnFleshSurgery(Entity<HereticComponent> ent, ref EventHereticFleshSurgery args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (HasComp<GhoulComponent>(args.Target)
        || (TryComp<HereticComponent>(args.Target, out var th) && th.CurrentPath == ent.Comp.CurrentPath))
        {
            var dargs = new DoAfterArgs(EntityManager, ent, 10f, new EventHereticFleshSurgeryDoAfter(args.Target), ent, args.Target)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnHandChange = false,
                BreakOnDropItem = false,
            };
            _doafter.TryStartDoAfter(dargs);
            args.Handled = true;
            return;
        }

        // temporarily disable a random organ
        // the fucking goob coders were literally just deleting organ components. who thought that was okay
        // TODO: change this to actually remove/disable organs once newmed comes out next week
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            //i should really make these their own methods. but i dont want to
            switch (_random.Next(0, 3))
            {
                // case 0: barf
                case 0:
                    _popup.PopupEntity(Loc.GetString("heretic-fleshsurgery-barf"), args.Target, args.Target, PopupType.LargeCaution);
                    _vomit.Vomit(args.Target, -1000, -1000); // i frew up
                    break;

                // case 1: blind (fixable!)
                case 1:
                    if (!TryComp<BlindableComponent>(args.Target, out var blindable) || blindable.IsBlind)
                        return;

                    _popup.PopupEntity(Loc.GetString("heretic-fleshsurgery-eyes"), args.Target, args.Target, PopupType.LargeCaution);
                    _blindable.AdjustEyeDamage((args.Target, blindable), 7); //same as rawdogging a welder 7 times. fixable but definitely a pain
                    break;

                // case 2: mute for 2.5 minutes
                case 2:
                    _popup.PopupEntity(Loc.GetString("heretic-fleshsurgery-mute"), args.Target, args.Target, PopupType.LargeCaution);
                    _statusEffect.TryAddStatusEffect<MutedComponent>(args.Target, "Muted", TimeSpan.FromSeconds(150), false);
                    break;

                default:
                    break;
            }
        }

        args.Handled = true;
    }
    private void OnFleshSurgeryDoAfter(Entity<HereticComponent> ent, ref EventHereticFleshSurgeryDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null) // shouldn't really happen. just in case
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var dmg))
            return;

        // heal teammates, mostly ghouls
        _dmg.SetAllDamage((EntityUid) args.Target, dmg, 0);
        args.Handled = true;
    }
    private void OnFleshAscendPolymorph(Entity<HereticComponent> ent, ref EventHereticFleshAscend args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var urist = _poly.PolymorphEntity(ent, "EldritchHorror");
        if (urist == null)
            return;

        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg"), (EntityUid) urist, AudioParams.Default.AddVolume(2f));

        args.Handled = true;
    }
}
