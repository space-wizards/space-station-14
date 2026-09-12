using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Antag;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ClientAntagSelectionSystem : AntagSelectionSystem
{
    public override void SendBriefing(ICommonSession? session, string briefing, Color? briefingColor, SoundSpecifier? briefingSound)
    {

    }

    public override IEnumerable<ProtoId<AntagPrototype>> GetValidAntagPreferences(ICommonSession session, List<ProtoId<AntagPrototype>>? filter = null)
    {
        yield break;
    }

    public override bool IsAntagBanned(ICommonSession session, AntagSpecifierPrototype definition)
    {
        return false;
    }

    protected override Entity<AntagSelectionComponent>? ForceGetGameRuleEnt<T>(string id)
    {
        return null;
    }
}
