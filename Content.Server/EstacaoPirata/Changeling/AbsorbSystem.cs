using Content.Server.Mind.Components;
using Content.Server.Changeling.Components;
using Content.Server.Changeling;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Changeling;
using Robust.Shared.Random;

namespace Content.Server.Changeling.EntitySystems;

/// <summary>
/// Attached to entities when a revenant drains them in order to
/// manage their essence.
/// </summary>
public sealed class AbsorbSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbComponent, ComponentStartup>(OnAbsorbEventReceived);
        SubscribeLocalEvent<AbsorbComponent, MobStateChangedEvent>(OnMobstateChanged);
        SubscribeLocalEvent<AbsorbComponent, MindAddedMessage>(OnAbsorbEventReceived);
        SubscribeLocalEvent<AbsorbComponent, MindRemovedMessage>(OnAbsorbEventReceived);
        SubscribeLocalEvent<AbsorbComponent, ExaminedEvent>(OnExamine);
        //SubscribeLocalEvent<AbsorbComponent, AbsorbDNAEvent>(OnAbsorb);

    }

    private void OnMobstateChanged(EntityUid uid, AbsorbComponent component, MobStateChangedEvent args)
    {
        UpdateDNAAmount(uid, component);
    }

    private void OnExamine(EntityUid uid, AbsorbComponent component, ExaminedEvent args)
    {
        if (!component.AbsorbComplete || !HasComp<ChangelingComponent>(args.Examiner))
            return;


        string message = "";
        if(component.Absorbed)
        {
            message = "changeling-already-absorbed";
            args.PushMarkup(Loc.GetString(message, ("target", uid)));
        }
    }

    private void OnAbsorbEventReceived(EntityUid uid, AbsorbComponent component, EntityEventArgs args)
    {
        UpdateDNAAmount(uid, component);
    }

    private void UpdateDNAAmount(EntityUid uid, AbsorbComponent component)
    {
        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;

        if(!TryComp<ChangelingComponent>(uid, out var changComp))
            return;
        
        if(changComp.DNAStrandBalance < changComp.DNAStrandCap)
        {
            changComp.DNAStrandBalance = changComp.DNAStrandBalance + 1;
        }
        // // fazer com que mostre uma mensagem que fale que o DNA ta no maximo
        // else
        // {
        //     string message = "";
        //     message = "changeling-DNA-full";
        //     args.PushMarkup(Loc.GetString(message, ("target", uid)));
        // }

        // switch (mob.CurrentState)
        // {
        //     case MobState.Alive:
        //         if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
        //             component.EssenceAmount = _random.NextFloat(75f, 100f);
        //         else
        //             component.EssenceAmount = _random.NextFloat(45f, 70f);
        //         break;
        //     case MobState.Critical:
        //         component.EssenceAmount = _random.NextFloat(35f, 50f);
        //         break;
        //     case MobState.Dead:
        //         component.EssenceAmount = _random.NextFloat(15f, 20f);
        //         break;
        // }
    }

    private void OnAbsorb(EntityUid uid, AbsorbComponent component, AbsorbDNAEvent args)
    {
        if(!TryComp<ChangelingComponent>(args.self, out var changComp))
            return;

        
    }

    
}

public sealed class AbsorbDNAEvent : EntityEventArgs
{
    /// <summary>
    ///     The entity performing the absortion.
    /// </summary>
    public EntityUid self { get; }

    /// <summary>
    ///     Entity being absorbed.
    /// </summary>
    public EntityUid target { get; }

    public AbsorbDNAEvent(EntityUid _self, EntityUid _target)
    {
        self = _self;
        target = _target;
    }
}
