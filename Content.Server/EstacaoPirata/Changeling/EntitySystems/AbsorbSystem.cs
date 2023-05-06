using Content.Server.Mind.Components;
using Content.Server.EstacaoPirata.Changeling.Components;
using Content.Server.EstacaoPirata.Changeling;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.EstacaoPirata.Changeling;
using Robust.Shared.Random;
using Content.Server.Forensics;
using Robust.Shared.Utility;

namespace Content.Server.EstacaoPirata.Changeling.EntitySystems;

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
        //SubscribeLocalEvent<AbsorbComponent, MobStateChangedEvent>(OnMobstateChanged);
        //SubscribeLocalEvent<AbsorbComponent, MindAddedMessage>(OnAbsorbEventReceived);
        //SubscribeLocalEvent<AbsorbComponent, MindRemovedMessage>(OnAbsorbEventReceived);
        //SubscribeLocalEvent<AbsorbComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<AbsorbComponent, AbsorbDNAEventTest>(OnAbsorb);

    }

    // private void OnMobstateChanged(EntityUid uid, AbsorbComponent component, MobStateChangedEvent args)
    // {
    //     UpdateDNAAmount(uid, component);
    // }

    private void OnExamine(EntityUid uid, AbsorbComponent component, ExaminedEvent args)
    {
        if (!component.Absorbed || !HasComp<ChangelingComponent>(args.Examiner))
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

    private void OnAbsorb(EntityUid uid, AbsorbComponent component, AbsorbDNAEventTest args)
    {
        if(!TryComp<ChangelingComponent>(args.self, out var changComp))
            return;



        string message = "";
        if(component.Absorbed)
        {
            message = "changeling-already-absorbed";
            args.PushMarkup(Loc.GetString(message, ("target", uid)));
        }

        UpdateDNAAmount(args.self, component);
    }


}

public sealed class AbsorbDNAEventTest : EntityEventArgs
{
    public FormattedMessage Message { get; }

    /// <summary>
    ///     The entity performing the absortion.
    /// </summary>
    public EntityUid self { get; }

    /// <summary>
    ///     Entity being absorbed.
    /// </summary>
    public EntityUid target { get; }

    ///// <summary>
    /////     Whether the examiner is in range of the entity to get some extra details.
    ///// </summary>
    //public bool IsInDetailsRange { get; }

    private bool _doNewLine;

    public AbsorbDNAEventTest(FormattedMessage _message, EntityUid _self, EntityUid _target)
    {
        Message = _message;
        self = _self;
        target = _target;
    }

    /// <summary>
    /// Push another message into this examine result, on its own line.
    /// </summary>
    /// <seealso cref="PushMarkup"/>
    /// <seealso cref="PushText"/>
    public void PushMessage(FormattedMessage message)
    {
        if (message.Nodes.Count == 0)
            return;

        if (_doNewLine)
            Message.AddText("\n");

        Message.AddMessage(message);
        _doNewLine = true;
    }

    /// <summary>
    /// Push another message parsed from markup into this examine result, on its own line.
    /// </summary>
    /// <seealso cref="PushText"/>
    /// <seealso cref="PushMessage"/>
    public void PushMarkup(string markup)
    {
        PushMessage(FormattedMessage.FromMarkup(markup));
    }

    /// <summary>
    /// Push another message containing raw text into this examine result, on its own line.
    /// </summary>
    /// <seealso cref="PushMarkup"/>
    /// <seealso cref="PushMessage"/>
    public void PushText(string text)
    {
        var msg = new FormattedMessage();
        msg.AddText(text);
        PushMessage(msg);
    }
}
