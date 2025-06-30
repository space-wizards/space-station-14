using System.Diagnostics.CodeAnalysis;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Audio;

namespace Content.Server._Starlight.Radio.Systems;

public sealed class RadioChimeSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    private readonly SoundPathSpecifier _aiChimeSound = new("/Audio/_Starlight/Effects/Radio/ai.ogg");

    /// <summary>
    /// Tries to get the radio chime sound from the sender's headset.
    /// </summary>
    public bool TryGetSenderHeadsetChime(EntityUid client, [NotNullWhen(true)] out SoundSpecifier? chime)
    {
        chime = null;

        // Handle AI chime sounds
        if (HasComp<StationAiHeldComponent>(client))
        {
            chime = _aiChimeSound;
            return true;
        }

        // Try to get the inventory system to find the headset
        if (!TryComp<InventoryComponent>(client, out var inventory))
            return false;

        // Try to get the headset from the "ears" slot
        if (!_inventory.TryGetSlotEntity(client, "ears", out var headsetEntity))
            return false;

        // Check if the headset has a RadioChimeComponent
        if (!TryComp<RadioChimeComponent>(headsetEntity.Value, out var radioChime) 
            || radioChime.Sound is null)
            return false;

        chime = radioChime.Sound;
        return true;
    }

}
