using Content.Shared.Database;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private Dictionary<string, int>? _spokenDigits;

    private void InitializeVoiceCommands()
    {
        SubscribeLocalEvent<VoiceCommandsComponent, MapInitEvent>(OnVoiceCommandsMapInit);
        SubscribeLocalEvent<VoiceCommandsComponent, VoiceTriggeredEvent>(OnVoiceCommandsHeard);
    }

    private void OnVoiceCommandsMapInit(Entity<VoiceCommandsComponent> ent, ref MapInitEvent args)
    {
        RebuildVoiceCommandLookup(ent);
    }

    public void RebuildVoiceCommandLookup(Entity<VoiceCommandsComponent> ent)
    {
        var getEv = new VoiceCommandsGetTriggersEvent();
        RaiseLocalEvent(ent, ref getEv);
        foreach (var (phrase, tag) in ent.Comp.Triggers)
            getEv.Triggers[phrase] = tag;

        ent.Comp.Candidates = VoiceCommandMatcher.BuildVoiceCommandCandidates(getEv.Triggers);
    }

    private void OnVoiceCommandsHeard(Entity<VoiceCommandsComponent> ent, ref VoiceTriggeredEvent args)
    {
        if (string.IsNullOrWhiteSpace(args.MessageWithoutPhrase))
            return;

        _spokenDigits ??= VoiceCommandMatcher.BuildSpokenDigits(id => Loc.GetString(id));

        if (!VoiceCommandMatcher.TryMatchVoiceCommand(ent.Comp, args.MessageWithoutPhrase, _spokenDigits, out var tag, out var quantity))
        {
            _adminLogger.Add(LogType.Trigger, LogImpact.Low,
                $"A voice command on {ToPrettyString(ent):entity} from {ToPrettyString(args.Source):speaker} matched no command: '{args.MessageWithoutPhrase}'.");
            return;
        }

        _adminLogger.Add(LogType.Trigger, LogImpact.Medium,
            $"A voice command on {ToPrettyString(ent):entity} from {ToPrettyString(args.Source):speaker} resolved to '{tag}' (x{quantity}).");

        var matched = new VoiceCommandMatchedEvent(args.Source, tag, quantity);
        RaiseLocalEvent(ent, ref matched);
    }
}
