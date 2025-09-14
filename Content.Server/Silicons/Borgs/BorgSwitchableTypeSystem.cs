// Modifications ported by Ronstation from CorvaxNext, therefore this file is licensed as MIT sublicensed with AGPL-v3.0.
using Content.Server.Inventory;
using Content.Server.Radio.Components;
using Content.Shared._CorvaxNext.Silicons.Borgs.Components;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// Server-side logic for borg type switching. Handles more heavyweight and server-specific switching logic.
/// </summary>
public sealed class BorgSwitchableTypeSystem : SharedBorgSwitchableTypeSystem
{
    [Dependency] private readonly BorgSystem _borgSystem = default!;
    [Dependency] private readonly ServerInventorySystem _inventorySystem = default!;

    protected override void SelectBorgModule(Entity<BorgSwitchableTypeComponent> ent, ProtoId<BorgTypePrototype> borgType)
    {
        var prototype = Prototypes.Index(borgType);

        // Assign radio channels
        string[] radioChannels = [.. ent.Comp.InherentRadioChannels, .. prototype.RadioChannels];
        if (TryComp(ent, out IntrinsicRadioTransmitterComponent? transmitter))
            transmitter.Channels = [.. radioChannels];

        if (TryComp(ent, out ActiveRadioComponent? activeRadio))
            activeRadio.Channels = [.. radioChannels];

        // Corvax-Next-AiRemoteControl-Start
        if (TryComp(ent, out AiRemoteControllerComponent? aiRemoteComp))
        {
            if (TryComp(aiRemoteComp.AiHolder, out IntrinsicRadioTransmitterComponent? stationAiTransmitter) && transmitter != null)
            {
                aiRemoteComp.PreviouslyTransmitterChannels = [.. radioChannels];
                transmitter.Channels = [.. stationAiTransmitter.Channels];
            }

            if (TryComp(aiRemoteComp.AiHolder, out ActiveRadioComponent? stationAiActiveRadio) && activeRadio != null)
            {
                aiRemoteComp.PreviouslyActiveRadioChannels = [.. radioChannels];
                activeRadio.Channels = [.. stationAiActiveRadio.Channels];
            }
        }
        // Corvax-Next-AiRemoteControl-End

        // Borg transponder for the robotics console
        if (TryComp(ent, out BorgTransponderComponent? transponder))
        {
            _borgSystem.SetTransponderSprite(
                (ent.Owner, transponder),
                new SpriteSpecifier.Rsi(new ResPath("Mobs/Silicon/chassis.rsi"), prototype.SpriteBodyState));

            _borgSystem.SetTransponderName(
                (ent.Owner, transponder),
                Loc.GetString($"borg-type-{borgType}-transponder"));
        }

        // Configure modules
        if (TryComp(ent, out BorgChassisComponent? chassis))
        {
            var chassisEnt = (ent.Owner, chassis);
            _borgSystem.SetMaxModules(
                chassisEnt,
                prototype.ExtraModuleCount + prototype.DefaultModules.Length);

            _borgSystem.SetModuleWhitelist(chassisEnt, prototype.ModuleWhitelist);

            foreach (var module in prototype.DefaultModules)
            {
                var moduleEntity = Spawn(module);
                var borgModule = Comp<BorgModuleComponent>(moduleEntity);
                _borgSystem.SetBorgModuleDefault((moduleEntity, borgModule), true);
                _borgSystem.InsertModule(chassisEnt, moduleEntity);
            }
        }

        // Configure special components
        if (Prototypes.Resolve(ent.Comp.SelectedBorgType, out var previousPrototype))
        {
            if (previousPrototype.AddComponents is { } removeComponents)
                EntityManager.RemoveComponents(ent, removeComponents);
        }

        if (prototype.AddComponents is { } addComponents)
        {
            EntityManager.AddComponents(ent, addComponents);
        }

        // Configure inventory template (used for hat spacing)
        if (TryComp(ent, out InventoryComponent? inventory))
        {
            _inventorySystem.SetTemplateId((ent.Owner, inventory), prototype.InventoryTemplateId);
        }

        base.SelectBorgModule(ent, borgType);
    }
}
