using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.SelectableComponentAdder;

public sealed partial class SelectableComponentAdderSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelectableComponentAdderComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<SelectableComponentAdderComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Selections <= 0)
            return;

        var target = args.Target;
        var user = args.User;
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
                    Dirty(ent);
                    if (entry.Popup == null)
                        return;
                    var message = Loc.GetString(entry.Popup.Value, ("target", target));
                    _popup.PopupClient(message, target, user);
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

        switch (setting)
        {
            case ComponentExistsSetting.Skip:
                // disable the verb if all components already exist
                foreach (var component in registry)
                {
                    if (!EntityManager.HasComponent(target, Factory.GetComponent(component.Key).GetType()))
                        return false;
                }
                return true;
            case ComponentExistsSetting.Replace:
                // always allow the verb
                return false;
            case ComponentExistsSetting.Block:
                // disable the verb if any component already exists.
                foreach (var component in registry)
                {
                    if (EntityManager.HasComponent(target, Factory.GetComponent(component.Key).GetType()))
                        return true;
                }
                return false;
            default:
                throw new NotImplementedException();
        }
    }

    private void AddComponents(EntityUid target, ComponentRegistry? registry, ComponentExistsSetting setting)
    {
        if (registry == null || CheckDisabled(target, registry, setting))
            return;

        foreach (var component in registry)
        {
            if (EntityManager.HasComponent(target, Factory.GetComponent(component.Key).GetType()) &&
                setting is ComponentExistsSetting.Skip or ComponentExistsSetting.Block)
                continue;

            EntityManager.AddComponent(target, component.Value, true);
        }
    }
}
