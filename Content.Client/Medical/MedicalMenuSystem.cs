using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Interactable;
using Content.Client.UserInterface.Systems.MedicalMenu;
using Content.Shared.Administration;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Verbs;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Client.Medical;

public sealed class MedicalMenuSystem : EntitySystem
{
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private MedicalMenuUIController _medMenuController = default!;

    //TODO: make this a CVAR
    private readonly float MaxMedInspectRange = 15f;

    public override void Initialize()
    {
        _medMenuController = _uiManager.GetUIController<MedicalMenuUIController>();
        SubscribeLocalEvent<MedicalDataComponent, GetVerbsEvent<Verb>>(SetupMedicalUI);

    }

    private void SetupMedicalUI(EntityUid uid, MedicalDataComponent medData, GetVerbsEvent<Verb> args)
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

            // View variables verbs
            if (_adminManager.HasFlag(AdminFlags.Debug) && _player.LocalSession != null)
            {
                var verb = new VvVerb()
                {
                    Text = Loc.GetString("Print All Wounds"),
                    Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
                    Act = () => _console.RemoteExecuteCommand(_player.LocalSession, $"PrintAllWounds \"{_entityManager.GetNetEntity(args.Target)}\""),
                    ClientExclusive = true // opening VV window is client-side. Don't ask server to run this verb.
                };
                args.Verbs.Add(verb);
            }
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
