using Content.Client.Interactable;
using Content.Client.UserInterface.Systems.MedicalMenu;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client.Medical;

public sealed class MedicalMenuSystem : EntitySystem
{
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    private MedicalMenuUIController _medMenuController = default!;

    //TODO: make this a CVAR
    private readonly float MaxMedInspectRange = 15f;

    public override void Initialize()
    {
        _medMenuController = _uiManager.GetUIController<MedicalMenuUIController>();
        SubscribeLocalEvent<WoundableRootComponent, GetVerbsEvent<Verb>>(SetupMedicalUI);

    }

    private void SetupMedicalUI(EntityUid uid, WoundableRootComponent component, GetVerbsEvent<Verb> args)
    {
            if (!args.CanInteract
                || !_containerSystem.IsInSameOrParentContainer(args.User, args.Target)
                || !_interaction.InRangeUnobstructed(args.User, args.Target, MaxMedInspectRange))
                return;

            args.Verbs.Add(new Verb()
            {
                Text = "Open Medical Menu", //TODO localize
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
                Act = () => { OpenUI(args.Target);},
                ClientExclusive = true
            });
    }

    private void OpenUI(EntityUid target)
    {
        if (!_medMenuController.IsOpen)
        {
            //Opens a window and sets target
            _medMenuController.OpenWindow(target);
            return;
        }
        //updates our target if the window is open already
        _medMenuController.SetTarget(target);
    }
}
