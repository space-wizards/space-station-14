using Content.Server.Speech;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Popups;
using System.Text;
using Content.Shared.StatusEffect;
using Content.Shared.Jittering;
//using Content.Shared.Actions;
//using Content.Shared.CombatMode;

namespace Content.Server.Traits.Assorted;

// ToDO:
//+1. After timer expired generate from 1 to N number to choose random event.
// Events:
//+2. Random Scream
//+3. Random Phrase:
//   +3.1) Barking
//   +3.2) Grunting
//   +3.3) Vulgar words
//-4. Random PVP mode
//+5. Character twitching
//+6. Drop item from hand
//+7. Use item in hand
// 8. Set final disability modifiers

/// <summary>
/// This handles character tourette syndrome: randomly scream,speak,drop/use,jittering,combat mode.
/// </summary>
public sealed class TouretteSyndromeSystem : EntitySystem
{
    private const string StatusEffectKey = "Jitter";

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VocalSystem _vocalSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHands = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    //[Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

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
                    // Get random phrase from tourette.TourettePhrases
                    int random_phrase_index = _random.Next(tourette.TourettePhrases.Count);
                    // Get random value to duplicate same message n-times
                    int repeate_amount = _random.Next(1, 3);
                    string tourette_final_message;
                    if (repeate_amount > 1)
                    {
                        // Create final message: get phrase from tourette.TourettePhrases and repeat it repeate_amount times
                        string repeated_phrase = new StringBuilder().Insert(0, tourette.TourettePhrases[random_phrase_index] + '-', repeate_amount).ToString();
                        // Remove last character
                        tourette_final_message = repeated_phrase.Remove(repeated_phrase.Length - 1, 1);
                    }
                    else
                    {
                        tourette_final_message = tourette.TourettePhrases[random_phrase_index];
                    }
                    // Send final message
                    _chat.TrySendInGameICMessage(
                                            tourette.Owner,
                                            tourette_final_message,
                                            InGameICChatType.Speak,
                                            hideChat: false,
                                            hideGlobalGhostChat: true
                                            );
                    break;

                case "Jittering":
                    var jittering_duration = _random.NextFloat(tourette.TouretteJitteringDuration.X, tourette.TouretteJitteringDuration.Y);
                    _statusEffects.TryAddStatusEffect<JitteringComponent>(
                                                        tourette.Owner,
                                                        StatusEffectKey,
                                                        TimeSpan.FromSeconds(jittering_duration),
                                                        false);
                    break;

                //case "Agression":
                //    if (!TryComp<SharedCombatModeComponent>(tourette.Owner, out var combatMode))
                //        return;
                //    Console.WriteLine("DEBUG BEFORE");
                //    if (combatMode.CombatToggleAction != null)
                //    {
                //        if (combatMode.IsInCombatMode == false)
                //        {
                //            Console.WriteLine("DEBUG AFTER");
                //            _actionsSystem.SetToggled(combatMode.CombatToggleAction, true);
                //        }
                //    }
                //    break;

                case "DropItemInHand":
                    _popupSystem.PopupEntity(Loc.GetString("trait-tourette-drop-item-in-hand-examine"), tourette.Owner);
                    // Try drop item from active hand
                    _sharedHands.TryDrop(tourette.Owner);
                    break;
                    
                case "UseItemInHand":
                    _popupSystem.PopupEntity(Loc.GetString("trait-tourette-use-item-in-hand-examine"), tourette.Owner);
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
