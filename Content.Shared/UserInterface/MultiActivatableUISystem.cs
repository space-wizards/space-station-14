using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.UserInterface;

/// <summary>
/// System for multiple verb-only user interfaces on one entity
/// </summary>
public sealed partial class MultiActivatableUISystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(GetActivationVerb);
    }

    private void GetActivationVerb(Entity<MultiActivatableUIComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || HasComp<GhostComponent>(args.User))
        {
            return;
        }

        var user = args.User;

        foreach (var dkey in ent.Comp.KeyVerbs)
        {
            args.Verbs.Add(new ActivationVerb
            {
                Act = () => _uiSystem.OpenUi(ent.Owner, dkey.Key, user),
                Text = Loc.GetString(dkey.Value),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            });
        }
    }

    public void AddUI(Entity<MultiActivatableUIComponent?> ent, Enum key, LocId verbText)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
        {
            return;
        }

        ent.Comp.KeyVerbs.Add(key, verbText);

        Dirty(ent);
    }
}
