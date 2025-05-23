using Content.Client._Starlight.Audio;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Log;

namespace Content.Client._Starlight.Radio.Systems;

/// <summary>
/// This system handles playing radio chime sounds on the client side when radio messages are received.
/// </summary>
public sealed class RadioChimeSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RadioChimeMuteSystem _radioChimeMuteSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private readonly SoundPathSpecifier _aiChimeSound = new("/Audio/_Starlight/Effects/Radio/ai.ogg");

    private ChatUIController? _chatUIController;

    public override void Initialize()
    {
        base.Initialize();
        
        // Get the ChatUIController
        _chatUIController = _userInterfaceManager.GetUIController<ChatUIController>();
        if (_chatUIController != null)
        {
            // Subscribe to the MessageAdded event
            _chatUIController.MessageAdded += OnChatMessage;
        }
    }
    
    public override void Shutdown()
    {
        base.Shutdown();
        
        // Unsubscribe from the MessageAdded event
        if (_chatUIController != null)
        {
            _chatUIController.MessageAdded -= OnChatMessage;
        }
    }

    private void OnChatMessage(ChatMessage message)
    {        
        // Only process radio messages
        if (message.Channel != ChatChannel.Radio)
        {
            return;
        }
                    
        // Don't play chimes if the player has muted them
        if (_radioChimeMuteSystem.IsMuted)
        {
            return;
        }
            
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
        {
            return;
        }
            
        // Get the sender entity
        var senderEntity = _entityManager.GetEntity(message.SenderEntity);
        
        // If the sender entity doesn't exist, use a default chime sound
        if (!_entityManager.EntityExists(senderEntity))
        {            
            // Use a default radio chime sound
            var defaultChimeSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/Radio/common.ogg");
            _audio.PlayGlobal(defaultChimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
            return;
        }
                    
        // Handle AI chime sounds
        if (HasComp<StationAiHeldComponent>(senderEntity))
        {
            // Play AI chime sound for the local player
            _audio.PlayGlobal(_aiChimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
            return;
        }
        
        // Handle normal radio chimes
        // Check if the sender has a headset with a RadioChimeComponent
        if (!TryGetSenderHeadsetChime(senderEntity, out var chimeSound) || chimeSound == null)
        {            
            // Use a default radio chime sound
            var defaultChimeSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/Radio/common.ogg");
            _audio.PlayGlobal(defaultChimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
            return;
        }
        
        // Play the chime sound for the local player
        _audio.PlayGlobal(chimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
        
        // Special case: If the local player is an AI, also play the sender's chime
        if (localPlayer.Value != senderEntity && HasComp<StationAiHeldComponent>(localPlayer.Value))
        {
            _audio.PlayGlobal(chimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
        }
    }
    
    /// <summary>
    /// Tries to get the radio chime sound from the sender's headset.
    /// </summary>
    private bool TryGetSenderHeadsetChime(EntityUid senderEntity, out SoundSpecifier? chimeSound)
    {
        chimeSound = null;
        
        // Try to get the inventory system to find the headset
        if (!_entityManager.TryGetComponent<InventoryComponent>(senderEntity, out var inventory))
        {
            return false;
        }
            
        // Try to get the headset from the "ears" slot
        if (!_entityManager.System<InventorySystem>().TryGetSlotEntity(senderEntity, "ears", out var headsetEntity))
        {
            return false;
        }
                    
        // Check if the headset has a RadioChimeComponent
        if (!_entityManager.TryGetComponent<RadioChimeComponent>(headsetEntity.Value, out var radioChime) || radioChime.ChimeSound == null)
        {
            return false;
        }
            
        chimeSound = radioChime.ChimeSound;
        return true;
    }
}
