using System.Text.RegularExpressions;
using Content.Server._MACRO.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server._MACRO.Speech.EntitySystems;

public sealed class AllulaloAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerslurR = new Regex("r{1,3}");
    private static readonly Regex RegexUpperslurR = new Regex("R{1,3}");
    private static readonly Regex RegexLowerdoubleCK = new Regex("ck{1,3}");
    private static readonly Regex RegexUpperdoubleCK = new Regex("CK{1,3}");
    private static readonly Regex RegexCasedoubleCK = new Regex("Ck{1,3}");
    private static readonly Regex RegexCameldoubleCK = new Regex("cK{1,3}");
    private static readonly Regex RegexLowerdoubleCKT = new Regex("ct{1,3}");
    private static readonly Regex RegexUpperdoubleCKT = new Regex("CT{1,3}");
    private static readonly Regex RegexCasedoubleCKT = new Regex("Ct{1,3}");
    private static readonly Regex RegexCameldoubleCKT = new Regex("cT{1,3}");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AllulaloAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, AllulaloAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // Im not writing music in this one
        message = RegexLowerslurR.Replace(message, "rr");
        // Sorry to the snalienaccentsystem fans
        message = RegexUpperslurR.Replace(message, "RR");
        //
        message = RegexLowerdoubleCK.Replace(message, "ck-ck");
        //
        message = RegexUpperdoubleCK.Replace(message, "CK-CK");
        //
        message = RegexCasedoubleCK.Replace(message, "Ck-ck");
        //
        message = RegexCameldoubleCK.Replace(message, "cK-CK");
        //
        message = RegexLowerdoubleCKT.Replace(message, "ck-ct");
        //
        message = RegexUpperdoubleCKT.Replace(message, "CK-CT");
        //
        message = RegexCasedoubleCKT.Replace(message, "Ck-ct");
        //
        message = RegexCameldoubleCKT.Replace(message, "cK-CT");
        args.Message = message;
    }
}
