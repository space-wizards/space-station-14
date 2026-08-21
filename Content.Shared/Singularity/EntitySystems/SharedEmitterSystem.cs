using Content.Shared.Database;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Singularity.EntitySystems;

public abstract partial class SharedEmitterSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitterComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<EmitterComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        if (TryComp<LockComponent>(ent.Owner, out var lockComp) && lockComp.Locked)
            return;

        if (ent.Comp.SelectableTypes.Count < 2 || !TryComp<NetworkPoweredAmmoProviderComponent>(ent, out var ammoProvider))
            return;

        foreach (var type in ent.Comp.SelectableTypes)
        {
            var proto = ProtoMan.Index(type);
            
            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = proto.Name,
                Disabled = type == ammoProvider.Prototype,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    ammoProvider.Prototype = type;
                    Dirty(ent);
                    _popup.PopupEntity(Loc.GetString("emitter-component-type-set", ("type", proto.Name)), ent.Owner, ent.Owner);
                },
            };
            args.Verbs.Add(v);
        }
    }
}
