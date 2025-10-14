using Content.Server.Audio;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// System that handles playing sounds at round start based on game rules.
/// This system listens for active RoundstartPlaySoundRule components and plays
/// their configured sounds when a round starts.
/// </summary>
public sealed class RoundstartPlaySoundRuleSystem : GameRuleSystem<RoundstartPlaySoundRuleComponent>
{
    [Dependency] private readonly ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        Logger.Info("RoundstartPlaySoundRuleSystem initialized");
        SubscribeLocalEvent<GameRuleAddedEvent>(OnAnyGameRuleAdded);
        Logger.Info("Subscribed to GameRuleAddedEvent");
    }

    

    

    /// <summary>
    /// Handles any game rule being added - this will help us see when rules are loaded.
    /// </summary>
    /// <param name="ev">The game rule added event</param>
    private void OnAnyGameRuleAdded(ref GameRuleAddedEvent ev)
    {
        Logger.Info($"GameRuleAddedEvent received: RuleId={ev.RuleId}, Entity={ToPrettyString(ev.RuleEntity)}");
        
        // Check if this is our specific rule
        if (TryComp<RoundstartPlaySoundRuleComponent>(ev.RuleEntity, out var comp))
        {
            Logger.Info($"RoundstartPlaySoundRule detected in GameRuleAddedEvent: {ToPrettyString(ev.RuleEntity)}");
            Logger.Info($"Sound: {comp.Sound}, Volume: {comp.Volume}");
        }
    }

    /// <summary>
    /// Test method to manually play a sound for debugging purposes.
    /// </summary>
    public void TestPlaySound(string soundPath, float volume = -8f)
    {
        Logger.Info($"Testing sound playback: {soundPath} at volume {volume}");
        
        try
        {
            var audioParams = AudioParams.Default.WithVolume(volume);
            _globalSound.PlayAdminGlobal(Filter.Broadcast(), soundPath, audioParams, replay: true);
            Logger.Info($"Successfully played test sound: {soundPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to play test sound {soundPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when the game rule is added to the game.
    /// </summary>
    /// <param name="uid">The entity UID of the rule</param>
    /// <param name="component">The roundstart play sound rule component</param>
    /// <param name="gameRule">The game rule component</param>
    /// <param name="args">The game rule added event</param>
    protected override void Added(EntityUid uid, RoundstartPlaySoundRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        Logger.Info($"RoundstartPlaySoundRule added: {ToPrettyString(uid)}, RuleId: {args.RuleId}");
        Logger.Info($"Sound: {component.Sound}, Volume: {component.Volume}");
        
        // Validate the sound specification
        if (component.Sound == null)
        {
            Logger.Warning($"RoundstartPlaySoundRule on {ToPrettyString(uid)} has no sound configured!");
            return;
        }

        // Validate volume is within reasonable bounds
        if (component.Volume < -100f || component.Volume > 100f)
        {
            Logger.Warning($"RoundstartPlaySoundRule on {ToPrettyString(uid)} has volume {component.Volume} outside reasonable bounds (-100 to 100), clamping to -8");
            component.Volume = -8f;
        }
        
        Logger.Info($"RoundstartPlaySoundRule validation completed for {ToPrettyString(uid)}");
    }

    /// <summary>
    /// Called when the game rule starts.
    /// </summary>
    /// <param name="uid">The entity UID of the rule</param>
    /// <param name="component">The roundstart play sound rule component</param>
    /// <param name="gameRule">The game rule component</param>
    /// <param name="args">The game rule started event</param>
    protected override void Started(EntityUid uid, RoundstartPlaySoundRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        Logger.Info($"RoundstartPlaySoundRule started: {ToPrettyString(uid)}, RuleId: {args.RuleId}");
        Logger.Info($"Sound: {component.Sound}, Volume: {component.Volume}");
        
        // Ensure the rule is active before playing.
        if (!GameTicker.IsGameRuleActive(uid, gameRule))
        {
            Logger.Info($"Rule {ToPrettyString(uid)} not active on Started, skipping playback");
            return;
        }

        try
        {
            var resolved = _audio.ResolveSound(component.Sound);
            var audioParams = AudioParams.Default.WithVolume(component.Volume);
            _globalSound.PlayAdminGlobal(Filter.Broadcast(), resolved, audioParams, replay: true);
            Logger.Info($"Played roundstart sound for rule {ToPrettyString(uid)}");
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to play resolved sound in Started, trying direct: {ex.Message}");
            try
            {
                var audioParams = AudioParams.Default.WithVolume(component.Volume);
                _globalSound.PlayAdminGlobal(Filter.Broadcast(), component.Sound.ToString()!, audioParams, replay: true);
                Logger.Info($"Played roundstart sound (direct) for rule {ToPrettyString(uid)}");
            }
            catch (Exception ex2)
            {
                Logger.Error($"Failed to play roundstart sound in Started for rule {ToPrettyString(uid)}: {ex2.Message}");
            }
        }
    }

    /// <summary>
    /// Called when the game rule ends.
    /// </summary>
    /// <param name="uid">The entity UID of the rule</param>
    /// <param name="component">The roundstart play sound rule component</param>
    /// <param name="gameRule">The game rule component</param>
    /// <param name="args">The game rule ended event</param>
    protected override void Ended(EntityUid uid, RoundstartPlaySoundRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        // This rule doesn't need special handling when ended
    }

    /// <summary>
    /// Called at the end of a round when text needs to be added for this game rule.
    /// </summary>
    /// <param name="uid">The entity UID of the rule</param>
    /// <param name="component">The roundstart play sound rule component</param>
    /// <param name="gameRule">The game rule component</param>
    /// <param name="args">The round end text append event</param>
    protected override void AppendRoundEndText(EntityUid uid, RoundstartPlaySoundRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        // This rule doesn't add any text to the round end summary
    }
}
