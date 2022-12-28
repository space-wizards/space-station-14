using Content.Server.Speech;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Traits.Assorted;

// ToDO:
//+1. After timer expired generate from 1 to N number to choose random event.
// Events:
//+2. Random Scream
// 3. Random Phrase:
//   3.1) Barking
//   3.2) Grunting
//   3.3) Vulgar words
// 4. Random PVP mode
// 5. Character twitching
//+6. Drop item from hand
//+7. Use item in hand

/// <summary>
/// This handles making character randomly scream/speak with no reason.
/// </summary>
public sealed class TouretteSyndromeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VocalSystem _vocalSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TouretteSyndromeComponent, ComponentStartup>(SetupTourette);
    }

    private void SetupTourette(EntityUid uid, TouretteSyndromeComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var tourette in EntityQuery<TouretteSyndromeComponent>())
        {
            tourette.NextIncidentTime -= frameTime;

            if (tourette.NextIncidentTime >= 0)
                continue;

            // Set the new time
            tourette.NextIncidentTime +=
                _random.NextFloat(tourette.TimeBetweenIncidents.X, tourette.TimeBetweenIncidents.Y);

            // Generate random value in range of TouretteSymptoms list size
            int random_value = _random.Next(tourette.TouretteSymptoms.Count);

            // Get random value from TouretteSymptoms
            string tourette_event = tourette.TouretteSymptoms[random_value];

            switch (tourette_event)
            {
                case "Scream":
                    _vocalSystem.TryScream(tourette.Owner);
                    break;

                case "Phrase":
                    _chat.TrySendInGameICMessage(tourette.Owner, "test message", InGameICChatType.Speak, hideChat: false, hideGlobalGhostChat: true);
                    break;

                case "DropItemInHand":
                    _chat.TrySendInGameICMessage(tourette.Owner, tourette.TouretteDropItemMessage, InGameICChatType.Emote, hideChat: false, hideGlobalGhostChat: true);
                    // Try drop item from active hand
                    _sharedHands.TryDrop(tourette.Owner);
                    break;

                case "UseItemInHand":
                    _chat.TrySendInGameICMessage(tourette.Owner, tourette.TouretteUseItemMessage, InGameICChatType.Emote, hideChat: false, hideGlobalGhostChat: true);
                    // Try use item in active hand
                    _sharedHands.TryUseItemInHand(tourette.Owner);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"{nameof(tourette.TouretteSymptoms)} switch doesn't have behavior for {tourette.TouretteSymptoms[random_value]} block");
            }
        }
    }
}
