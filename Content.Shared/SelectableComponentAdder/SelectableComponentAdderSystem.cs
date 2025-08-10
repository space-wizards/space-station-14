using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.SelectableComponentAdder;

public sealed partial class SelectableComponentAdderSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelectableComponentAdderComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<SelectableComponentAdderComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Selections != null && ent.Comp.Selections <= 0)
            return;

        var target = args.Target;
        var verbCategory = new VerbCategory(ent.Comp.VerbCategoryName, null);

        foreach (var entry in ent.Comp.Entries)
        {
            var verb = new Verb
            {
                Priority = entry.Priority,
                Category = verbCategory,
                Disabled = CheckDisabled(target, entry.ComponentsToAdd, entry.ComponentExistsBehavior),
                Act = () =>
                {
                    AddComponents(target, entry.ComponentsToAdd, entry.ComponentExistsBehavior);
                    ent.Comp.Selections--;
                },
                Text = Loc.GetString(entry.VerbName),
            };
            args.Verbs.Add(verb);
        }
    }

    private bool CheckDisabled(EntityUid target, ComponentRegistry? registry, ComponentExistsSetting setting)
    {
        if (registry == null)
            return false;

        foreach (var component in registry)
        {
            if (!EntityManager.HasComponent(target, _factory.GetComponent(component.Key).GetType()))
                continue;

            if (setting == ComponentExistsSetting.Block)
                return true;
        }

        return false;
    }

    private void AddComponents(EntityUid target, ComponentRegistry? registry, ComponentExistsSetting setting)
    {
        if (registry == null)
            return;

        foreach (var component in registry)
        {
            if (EntityManager.HasComponent(target, _factory.GetComponent(component.Key).GetType()) &&
                setting is ComponentExistsSetting.Skip or ComponentExistsSetting.Block)
                continue;

            EntityManager.AddComponent(target, component.Value, true);
        }
    }
}
