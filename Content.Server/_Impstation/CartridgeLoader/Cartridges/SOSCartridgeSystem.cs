using System.Threading.Channels;
using Content.Server.Radio.EntitySystems;
using Content.Shared._DV.NanoChat;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Profiling;
using YamlDotNet.Core.Tokens;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class SOSCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SOSCartridgeComponent, CartridgeActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, SOSCartridgeComponent component, CartridgeActivatedEvent args)
    {
        //Get the PDA
        if (!TryComp<PdaComponent>(args.Loader, out var pda))
            return;

        //Get the id container
        if (_container.TryGetContainer(args.Loader, SOSCartridgeComponent.PDAIdContainer, out var idContainer))
        {
            //If theres nothing in id slot, send message anonymously
            if (idContainer.ContainedEntities.Count == 0)
            {
                _radio.SendRadioMessage(uid, component.LocalizedDefaultName + " " + component.LocalizedHelpMessage, component.HelpChannel, uid);
            }
            else
            {
                //Otherwise, send a message with the full name of every id in there
                foreach (var idCard in idContainer.ContainedEntities)
                {
                    if (!TryComp<IdCardComponent>(idCard, out var idCardComp))
                        return;

                    _radio.SendRadioMessage(uid, idCardComp.FullName + " " + component.LocalizedHelpMessage, component.HelpChannel, uid);
                }
            }
        }
    }
}
