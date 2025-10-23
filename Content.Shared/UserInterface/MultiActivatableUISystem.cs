using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.UserInterface;

public sealed partial class MultiActivatableUISystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(GetActivationVerb);
    }

    private void GetActivationVerb(EntityUid uid, MultiActivatableUIComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || HasComp<GhostComponent>(args.User))
        {
            return;
        }

        for (var i = 0; i < component.Keys.Count; i++)
        {
            var key = component.Keys[i];
            args.Verbs.Add(new ActivationVerb
            {
                Act = () => OpenUI(args.User, uid, key),
                Text = Loc.GetString(component.VerbTexts[i]),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            });
        }
    }

    private void OpenUI(EntityUid user, EntityUid uiEntity, Enum key)
    {
        _uiSystem.OpenUi(uiEntity, key, user);
    }
}
