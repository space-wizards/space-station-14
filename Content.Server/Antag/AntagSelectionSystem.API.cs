using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    /// <summary>
    /// Tries to get the next non-filled definition based on the current amount of selected minds and other factors.
    /// </summary>
    public bool TryGetNextAvailableDefinition(Entity<AntagSelectionComponent> ent,
        [NotNullWhen(true)] out AntagSelectionDefinition? definition)
    {
        definition = null;

        var totalTargetCount = GetTargetAntagCount(ent);
        var mindCount = ent.Comp.SelectedMinds.Count;
        if (mindCount >= totalTargetCount)
            return false;

        // TODO ANTAG fix this
        // If here are two definitions with 1/10 and 10/10 slots filled, this will always return the second definition
        // even though it has already met its target
        // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA I fucking hate game ticker code.
        // It needs to track selected minds for each definition independently.
        foreach (var def in ent.Comp.Definitions)
        {
            var target = GetTargetAntagCount(ent, null, def);

            if (mindCount < target)
            {
                definition = def;
                return true;
            }

            mindCount -= target;
        }

        return false;
    }

    /// <summary>
    /// Gets the number of antagonists that should be present for a given rule based on the provided pool.
    /// A null pool will simply use the player count.
    /// </summary>
    public int GetTargetAntagCount(Entity<AntagSelectionComponent> ent, int? playerCount = null)
    {
        var count = 0;
        foreach (var def in ent.Comp.Definitions)
        {
            count += GetTargetAntagCount(ent, playerCount, def);
        }

        return count;
    }

    public int GetTotalPlayerCount(IList<ICommonSession> pool)
    {
        var count = 0;
        foreach (var session in pool)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;

            count++;
        }

        return count;
    }

    // goob edit
    public List<ICommonSession> GetAliveConnectedPlayers(IList<ICommonSession> pool)
    {
        var l = new List<ICommonSession>();
        foreach (var session in pool)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;
            l.Add(session);
        }
        return l;
    }
    // goob edit end

    /// <summary>
    /// Gets the number of antagonists that should be present for a given antag definition based on the provided pool.
    /// A null pool will simply use the player count.
    /// </summary>
    public int GetTargetAntagCount(Entity<AntagSelectionComponent> ent, int? playerCount, AntagSelectionDefinition def)
    {
        // TODO ANTAG
        // make pool non-nullable
        // Review uses and ensure that people are INTENTIONALLY including players in the lobby if this is a mid-round
        // antag selection.
        var poolSize = playerCount ?? GetTotalPlayerCount(_playerManager.Sessions);

        // factor in other definitions' affect on the count.
        var countOffset = 0;
        foreach (var otherDef in ent.Comp.Definitions)
        {
            countOffset += Math.Clamp((poolSize - countOffset) / otherDef.PlayerRatio, otherDef.Min, otherDef.Max) * otherDef.PlayerRatio;
        }
        // make sure we don't double-count the current selection
        countOffset -= Math.Clamp(poolSize / def.PlayerRatio, def.Min, def.Max) * def.PlayerRatio;

        return Math.Clamp((poolSize - countOffset) / def.PlayerRatio, def.Min, def.Max);
    }

    /// <summary>
    /// Returns identifiable information for all antagonists to be used in a round end summary.
    /// </summary>
    /// <returns>
    /// A list containing, in order, the antag's mind, the session data, and the original name stored as a string.
    /// </returns>
    public List<(EntityUid, SessionData, string)> GetAntagIdentifiers(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new List<(EntityUid, SessionData, string)>();

        var output = new List<(EntityUid, SessionData, string)>();
        foreach (var (mind, name) in ent.Comp.SelectedMinds)
        {
            if (!TryComp<MindComponent>(mind, out var mindComp) || mindComp.OriginalOwnerUserId == null)
                continue;

            if (!_playerManager.TryGetPlayerData(mindComp.OriginalOwnerUserId.Value, out var data))
                continue;

            output.Add((mind, data, name));
        }
        return output;
    }

    /// <summary>
    /// Returns all the minds of antagonists.
    /// </summary>
    public List<Entity<MindComponent>> GetAntagMinds(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new();

        var output = new List<Entity<MindComponent>>();
        foreach (var (mind, _) in ent.Comp.SelectedMinds)
        {
            if (!TryComp<MindComponent>(mind, out var mindComp) || mindComp.OriginalOwnerUserId == null)
                continue;

            output.Add((mind, mindComp));
        }
        return output;
    }

    /// <remarks>
    /// Helper to get just the mind entities and not names.
    /// </remarks>
    public List<EntityUid> GetAntagMindEntityUids(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new();

        return ent.Comp.SelectedMinds.Select(p => p.Item1).ToList();
    }

    /// <summary>
    /// Checks if a given session has the primary antag preferences for a given definition
    /// </summary>
    public bool HasPrimaryAntagPreference(ICommonSession? session, AntagSelectionDefinition def)
    {
        if (session == null)
            return true;

        if (def.PrefRoles.Count == 0)
            return false;

        var pref = (HumanoidCharacterProfile) _pref.GetPreferences(session.UserId).SelectedCharacter;
        return pref.AntagPreferences.Any(p => def.PrefRoles.Contains(p));
    }

    /// <summary>
    /// Checks if a given session has the fallback antag preferences for a given definition
    /// </summary>
    public bool HasFallbackAntagPreference(ICommonSession? session, AntagSelectionDefinition def)
    {
        if (session == null)
            return true;

        if (def.FallbackRoles.Count == 0)
            return false;

        var pref = (HumanoidCharacterProfile) _pref.GetPreferences(session.UserId).SelectedCharacter;
        return pref.AntagPreferences.Any(p => def.FallbackRoles.Contains(p));
    }

    /// <summary>
    /// Returns all the antagonists for this rule who are currently alive
    /// </summary>
    public IEnumerable<EntityUid> GetAliveAntags(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        var minds = GetAntagMinds(ent);
        foreach (var mind in minds)
        {
            if (_mind.IsCharacterDeadIc(mind))
                continue;

            if (mind.Comp.OriginalOwnedEntity != null)
                yield return GetEntity(mind.Comp.OriginalOwnedEntity.Value);
        }
    }

    /// <summary>
    /// Returns the number of alive antagonists for this rule.
    /// </summary>
    public int GetAliveAntagCount(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        var numbah = 0;
        var minds = GetAntagMinds(ent);
        foreach (var mind in minds)
        {
            if (_mind.IsCharacterDeadIc(mind))
                continue;

            numbah++;
        }

        return numbah;
    }

    /// <summary>
    /// Returns if there are any remaining antagonists alive for this rule.
    /// </summary>
    public bool AnyAliveAntags(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return GetAliveAntags(ent).Any();
    }

    /// <summary>
    /// Checks if all the antagonists for this rule are alive.
    /// </summary>
    public bool AllAntagsAlive(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return GetAliveAntagCount(ent) == ent.Comp.SelectedMinds.Count;
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a player entity
    /// </summary>
    /// <param name="entity">The entity chosen to be antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(EntityUid entity, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (!_mind.TryGetMind(entity, out _, out var mindComponent))
            return;

        if (mindComponent.Session == null)
            return;

        SendBriefing(mindComponent.Session, briefing, briefingColor, briefingSound);
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a list of sessions
    /// </summary>
    /// <param name="sessions">The sessions that will be sent the briefing</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    [PublicAPI]
    public void SendBriefing(List<ICommonSession> sessions, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        foreach (var session in sessions)
        {
            SendBriefing(session, briefing, briefingColor, briefingSound);
        }
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a session
    /// </summary>
    /// <param name="session">The player chosen to be an antag</param>
    /// <param name="data">The briefing data</param>
    public void SendBriefing(
        ICommonSession? session,
        BriefingData? data)
    {
        if (session == null || data == null)
            return;

        var text = data.Value.Text == null ? string.Empty : Loc.GetString(data.Value.Text);
        SendBriefing(session, text, data.Value.Color, data.Value.Sound);
    }

    /// <summary>
    /// Helper method to send the briefing text and sound to a session
    /// </summary>
    /// <param name="session">The player chosen to be an antag</param>
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(
        ICommonSession? session,
        string briefing,
        Color? briefingColor,
        SoundSpecifier? briefingSound)
    {
        if (session == null)
            return;

        _audio.PlayGlobal(briefingSound, session);
        if (!string.IsNullOrEmpty(briefing))
        {
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
            _chat.ChatMessageToOne(ChatChannel.Server, briefing, wrappedMessage, default, false, session.Channel,
                briefingColor);
        }
    }

    /// <summary>
    /// This technically is a gamerule-ent-less way to make an entity an antag.
    /// You should almost never be using this.
    /// </summary>
    public void ForceMakeAntag<T>(ICommonSession? player, string defaultRule) where T : Component
    {
        var rule = ForceGetGameRuleEnt<T>(defaultRule);

        if (!TryGetNextAvailableDefinition(rule, out var def))
            def = rule.Comp.Definitions.Last();
        MakeAntag(rule, player, def.Value);
    }

    /// <summary>
    /// Tries to grab one of the weird specific antag gamerule ents or starts a new one.
    /// This is gross code but also most of this is pretty gross to begin with.
    /// </summary>
    public Entity<AntagSelectionComponent> ForceGetGameRuleEnt<T>(string id) where T : Component
    {
        var query = EntityQueryEnumerator<T, AntagSelectionComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            return (uid, comp);
        }
        var ruleEnt = GameTicker.AddGameRule(id);
        RemComp<LoadMapRuleComponent>(ruleEnt);
        var antag = Comp<AntagSelectionComponent>(ruleEnt);
        antag.SelectionsComplete = true; // don't do normal selection.
        GameTicker.StartGameRule(ruleEnt);
        return (ruleEnt, antag);
    }
}
