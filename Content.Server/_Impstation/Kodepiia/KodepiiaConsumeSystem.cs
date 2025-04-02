using Content.Server.Atmos.Rotting;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Shared._Impstation.Kodepiia;
using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Body.Components;
using Content.Shared.Changeling;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Kodepiia;

public sealed class KodepiiaConsumeSystem : SharedKodepiiaConsumeSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaConsumeActionComponent, KodepiiaConsumeEvent>(Consume);
        SubscribeLocalEvent<KodepiiaConsumeActionComponent, KodepiiaConsumeDoAfterEvent>(ConsumeDoafter);
    }

    public void Consume(Entity<KodepiiaConsumeActionComponent> ent, ref KodepiiaConsumeEvent args)
    {
        if (!HasComp<AbsorbableComponent>(args.Target) || _rotting.IsRotten(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("kodepiia-consume-fail-inedible", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }

        if (!_mobState.IsIncapacitated(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("kodepiia-consume-fail-incapacitated", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }

        PlayMeatySound(ent);

        if (!TryComp<PhysicsComponent>(args.Target, out var targetPhysics))
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, targetPhysics.Mass/5, new KodepiiaConsumeDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        var popupSelf = Loc.GetString("kodepiia-consume-start-self", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target, EntityManager)));
        var popupOthers = Loc.GetString("kodepiia-consume-start-others", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target, EntityManager)));

        _popup.PopupEntity(popupSelf, ent, ent);
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);

        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;
    }

    public void ConsumeDoafter(Entity<KodepiiaConsumeActionComponent> ent, ref KodepiiaConsumeDoAfterEvent args)
    {
        if (args.Target == null || args.Cancelled || !TryComp<PhysicsComponent>(args.Target, out var targetPhysics))
        {
            return;
        }

        // Drink Bloodstream
        if (_solutionContainer.TryGetSolution(args.User, "chemicals", out var userSolutionComp, out var userSolution)
            && _solutionContainer.TryGetSolution(args.Target.Value, "bloodstream", out var targetSolutionComp, out var targetSolution))
        {
            if (userSolution.Volume < userSolution.MaxVolume)
            {
                var transferAmount = targetSolution.MaxVolume / 10;
                var realTransferAmount = FixedPoint2.Min(transferAmount, userSolution.AvailableVolume);
                // how much protein to add to the mix
                var protein = targetPhysics.Mass / 2;
                if (realTransferAmount.Value > 0)
                {
                    var removedSolution =
                        _solutionContainer.SplitSolution(targetSolutionComp.Value, realTransferAmount);
                    removedSolution.AddReagent("UncookedAnimalProteins",protein);
                    _solutionContainer.TryAddSolution(userSolutionComp.Value, removedSolution);
                }
            }
        }
        // Transfer DNA
        _forensics.TransferDna(args.Target.Value, ent, false);

        // Deal Damage
        _damage.TryChangeDamage(args.Target, ent.Comp.Damage, true, false);

        // Play Sound
        PlayMeatySound(ent);

        var popupSelf = Loc.GetString("kodepiia-consume-end-self", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)));
        var popupOthers = Loc.GetString("kodepiia-consume-end-others", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)));
        _popup.PopupEntity(popupSelf, ent, ent);
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.LargeCaution);

        //Consumed Componentry Stuff lol
        EnsureComp<KodepiiaConsumedComponent>(args.Target.Value, out var consumed);
        consumed.TimesConsumed += 1;
        if (consumed.TimesConsumed >= 12 && TryComp<BodyComponent>(args.Target.Value, out var body) && ent.Comp.CanGib)
        {
            _body.GibBody(args.Target.Value,true,body);
        }
    }

    public void PlayMeatySound(Entity<KodepiiaConsumeActionComponent> ent)
    {
        var soundPool = new SoundCollectionSpecifier("gib");
        _audio.PlayPvs(soundPool, ent, AudioParams.Default.WithVolume(-3f));
    }
}
