using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Singularity.EntitySystems;

public abstract class SharedEmitterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EmitterComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<EmitterComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        if (TryComp<LockComponent>(ent.Owner, out var lockComp) && lockComp.Locked)
            return;

        if (ent.Comp.SelectableTypes.Count < 2)
            return;

        foreach (var type in ent.Comp.SelectableTypes)
        {
            var proto = _prototype.Index(type);

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = proto.Name,
                Disabled = type == ent.Comp.BoltType,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    ent.Comp.BoltType = type;
                    Dirty(ent);
                    _popup.PopupClient(Loc.GetString("emitter-component-type-set", ("type", proto.Name)), ent.Owner);
                },
            };
            args.Verbs.Add(v);
        }
    }

    private void OnExamined(Entity<EmitterComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.SelectableTypes.Count < 2)
            return;

        var proto = _prototype.Index(ent.Comp.BoltType);
        args.PushMarkup(Loc.GetString("emitter-component-current-type", ("type", proto.Name)));
    }
}
