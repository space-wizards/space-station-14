using Content.Shared.Lathe;
using Content.Shared.Verbs;
using Content.Shared.Xenoborgs;
using Content.Shared.Xenoborgs.Components;
using System.Linq;

namespace Content.Client.Xenoborgs;

public sealed class XenoborgFactorySystem : SharedXenoborgFactorySystem
{
    [Dependency] private readonly SharedLatheSystem _lathe = default!;

    protected override void OnGetVerb(EntityUid uid, XenoborgFactoryComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!Proto.TryIndex(component.BorgRecipePack, out var recipePack))
            return;

        foreach (var v in from type in recipePack.Recipes
                 let proto = Proto.Index(type)
                 select new Verb
                 {
                     Category = VerbCategory.SelectType,
                     Text = _lathe.GetRecipeName(proto),
                     Disabled = type == component.Recipe,
                     DoContactInteraction = true,
                     Icon = proto.Icon,
                 })
        {
            args.Verbs.Add(v);
        }
    }
}
