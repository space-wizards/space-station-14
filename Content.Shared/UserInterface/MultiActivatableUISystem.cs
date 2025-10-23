using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.UserInterface;

public sealed partial class MultiActivatableUISystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<MultiActivatableUIComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<MultiActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(GetActivationVerb);
    }

    private void GetActivationVerb(EntityUid uid, MultiActivatableUIComponent component, GetVerbsEvent<ActivationVerb> args)
    {
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

    private void OnActivate(EntityUid uid, MultiActivatableUIComponent component, ActivateInWorldEvent args)
    {
        args.Handled = InteractUI(args.User, uid, component);
    }

    private void OpenUI(EntityUid user, EntityUid uiEntity, Enum key)
    {
        _uiSystem.OpenUi(uiEntity, key, user);
    }

    private bool InteractUI(EntityUid user, EntityUid uiEntity, MultiActivatableUIComponent maui)
    {
        for (var i = 0; i < maui.Keys.Count; i++)
        {
            _uiSystem.OpenUi(uiEntity, maui.Keys[i], user);
        }

        return false; // We don't want to hijack the ActivateInWorldEvent from other components
    }
}
