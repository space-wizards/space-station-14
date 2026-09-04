using Content.Server.Objectives;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.PDA;
using Content.Shared.GameTicking.Rules;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerTraitorRuleSystem : TraitorRuleSystem
{
    [Dependency] private UplinkSystem _uplink = default!;

    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Debug;
    }

    protected override (Note[]?, string) RequestUplink(EntityUid traitor, FixedPoint2 startingBalance, string briefing)
    {
        var pda = _uplink.FindUplinkTarget(traitor);

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink add");
        var uplinked = _uplink.AddUplink(traitor, startingBalance, out var code, pda, giveDiscounts: true, bindToPda: false);

        if (code != null && uplinked == AddUplinkResult.Pda)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is PDA");

            // If giveUplink is false the uplink code part is omitted
            briefing = string.Format("{0}\n{1}",
                briefing,
                Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#"))));
            return (code, briefing);
        }

        if (uplinked == AddUplinkResult.Implant)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is implant");
            briefing += "\n" + Loc.GetString("traitor-role-uplink-implant-short");
        }
        else
        {
            Log.Error($"MakeTraitor failed on {ToPrettyString(traitor)} - No uplink could be added");
        }


        return (null, briefing);
    }

    // TODO: AntagCodewordsComponent
    [SubscribeLocalEvent]
    private void OnObjectivesTextPrepend(EntityUid uid, Shared.GameTicking.Rules.Components.TraitorRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        if(comp.GiveCodewords)
            args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", Codeword.GetCodewords(comp.CodewordFactionPrototypeId))));
    }
}
