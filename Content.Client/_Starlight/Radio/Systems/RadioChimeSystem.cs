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
        
        Logger.InfoS("radio", "RadioChimeSystem initializing...");
        
        // Force log level to Info for radio messages
        Logger.GetSawmill("radio").Level = LogLevel.Info;
        
        // Get the ChatUIController
        _chatUIController = _userInterfaceManager.GetUIController<ChatUIController>();
        if (_chatUIController != null)
        {
            // Subscribe to the MessageAdded event
            _chatUIController.MessageAdded += OnChatMessage;
            Logger.InfoS("radio", "RadioChimeSystem initialized and subscribed to ChatUIController.MessageAdded");
        }
        else
        {
            Logger.ErrorS("radio", "Failed to get ChatUIController");
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
        Logger.InfoS("radio", $"Received chat message: Channel={message.Channel}");
        
        // Only process radio messages
        if (message.Channel != ChatChannel.Radio)
        {
            Logger.InfoS("radio", $"Ignoring non-radio message: {message.Channel}");
            return;
        }
            
        Logger.InfoS("radio", "Message is a radio message");
        
        // Don't play chimes if the player has muted them
        if (_radioChimeMuteSystem.IsMuted)
        {
            Logger.InfoS("radio", "Radio chimes are muted, not playing sound");
            return;
        }
            
        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == null)
        {
            Logger.InfoS("radio", "Local player is null");
            return;
        }
            
        // Get the sender entity
        var senderEntity = _entityManager.GetEntity(message.SenderEntity);
        
        // If the sender entity doesn't exist, use a default chime sound
        if (!_entityManager.EntityExists(senderEntity))
        {
            Logger.InfoS("radio", "Sender entity does not exist, using default chime sound");
            
            // Use a default radio chime sound
            var defaultChimeSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/Radio/common.ogg");
            _audio.PlayGlobal(defaultChimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
            return;
        }
        
        Logger.InfoS("radio", $"Sender entity: {senderEntity}");
            
        // Handle AI chime sounds
        if (HasComp<StationAiHeldComponent>(senderEntity))
        {
            Logger.InfoS("radio", "Sender is an AI, playing AI chime sound");
            // Play AI chime sound for the local player
            _audio.PlayGlobal(_aiChimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
            return;
        }
        
        // Handle normal radio chimes
        // Check if the sender has a headset with a RadioChimeComponent
        if (!TryGetSenderHeadsetChime(senderEntity, out var chimeSound) || chimeSound == null)
        {
            Logger.InfoS("radio", "Could not find headset with radio chime component, using default chime sound");
            
            // Use a default radio chime sound
            var defaultChimeSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/Radio/common.ogg");
            _audio.PlayGlobal(defaultChimeSound, Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
            return;
        }
        
        Logger.InfoS("radio", "Playing radio chime sound");
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
            Logger.InfoS("radio", "Entity does not have inventory component");
            return false;
        }
            
        // Try to get the headset from the "ears" slot
        if (!_entityManager.System<InventorySystem>().TryGetSlotEntity(senderEntity, "ears", out var headsetEntity))
        {
            Logger.InfoS("radio", "Could not find headset in ears slot");
            return false;
        }
        
        Logger.InfoS("radio", $"Found headset: {headsetEntity}");
            
        // Check if the headset has a RadioChimeComponent
        if (!_entityManager.TryGetComponent<RadioChimeComponent>(headsetEntity.Value, out var radioChime) || radioChime.ChimeSound == null)
        {
            Logger.InfoS("radio", "Headset does not have RadioChimeComponent or ChimeSound is null");
            return false;
        }
            
        chimeSound = radioChime.ChimeSound;
        Logger.InfoS("radio", $"Found chime sound: {chimeSound}");
        return true;
    }
}
