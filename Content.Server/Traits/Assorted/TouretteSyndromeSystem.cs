using Content.Server.Speech;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles making character randomly scream/speak with no reason.
/// </summary>
public sealed class TouretteSyndromeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly VocalSystem _vocalSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

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

            // Set the new time.
            tourette.NextIncidentTime +=
                _random.NextFloat(tourette.TimeBetweenIncidents.X, tourette.TimeBetweenIncidents.Y);

            if (tourette.NextIncidentTime <= 3)
            {
                
                _chat.TrySendInGameICMessage(tourette.Owner, "test message", InGameICChatType.Speak, hideChat: false, hideGlobalGhostChat: true);
            }
            else
            {
                _vocalSystem.TryScream(tourette.Owner);
            }

        }
    }

}
