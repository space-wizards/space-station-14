using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Bar;

public abstract class SharedPolymorphGlassSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphGlassComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    protected virtual void ChangeGlass(EntityUid uid, PolymorphGlassComponent component, EntityPrototype prototype, GetVerbsEvent<Verb> args)
    {
        //  Server side
    }

    private void OnGetVerb(EntityUid uid, PolymorphGlassComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var thisProto = Prototype(uid, Comp<MetaDataComponent>(uid));

        foreach (var glass in component.Glasses)
        {
            var proto = _prototypeManager.Index<EntityPrototype>(glass.Key);
            if (proto == thisProto)
                continue;

            var v = new Verb
            {
                Priority = 2,
                Category = new VerbCategory(Loc.GetString("polymorphic-glass-verb-category"), "Objects/Consumable/Drinks/glass_clear.rsi/icon.png"),
                Text = proto.Name,
                DoContactInteraction = true,
                Icon = glass.Value,
                Act = () =>
                {
                    ChangeGlass(uid, component, proto, args);
                }
            };
            args.Verbs.Add(v);
        }
    }
}
