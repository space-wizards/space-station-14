using System.Linq;
using Content.Server.Antag.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    public List<(EntityUid, SessionData, string)> GetAntagNameData(Entity<AntagSelectionComponent?> ent)
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

    public List<EntityUid> GetAntagMindUids(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new();

        return ent.Comp.SelectedMinds.Select(p => p.Item1).ToList();
    }

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

    public int GetAliveAntagCount(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        return GetAliveAntags(ent).Count();
    }

    public bool AnyAliveAntags(Entity<AntagSelectionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return GetAliveAntags(ent).Any();
    }

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
    /// <param name="sessions"></param>
    /// <param name="briefing"></param>
    /// <param name="briefingColor"></param>
    /// <param name="briefingSound"></param>
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
    /// <param name="briefing">The briefing text to send</param>
    /// <param name="briefingColor">The color the briefing should be, null for default</param>
    /// <param name="briefingSound">The sound to briefing/greeting sound to play</param>
    public void SendBriefing(ICommonSession? session, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {
        if (session == null)
            return;

        _audio.PlayGlobal(briefingSound, session);
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", briefing));
        _chat.ChatMessageToOne(ChatChannel.Server, briefing, wrappedMessage, default, false, session.Channel, briefingColor);
    }
}
