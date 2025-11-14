using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class LizardAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");
    private static readonly Regex RegexLowerCyrS = new("с+");
    private static readonly Regex RegexUpperCyrS = new("С+");
    private static readonly Regex RegexLowerZ = new("з+");
    private static readonly Regex RegexUpperZ = new("З+");
    private static readonly Regex RegexLowerSh = new("ш+");
    private static readonly Regex RegexUpperSh = new("Ш+");
    private static readonly Regex RegexLowerCh = new("ч+");
    private static readonly Regex RegexUpperCh = new("Ч+");
    private static readonly Regex RegexInternalKha = new(@"(\w)х");
    private static readonly Regex RegexInternalUpperKha = new(@"(\w)Х");
    private static readonly Regex RegexLowerEndKha = new(@"\bх([\-|р|Р]|\b)");
    private static readonly Regex RegexUpperEndKha = new(@"\bХ([\-|р|Р]|\b)");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, LizardAccentComponent component, AccentGetEvent args)
    {
        var message = ApplyLatinTransformations(args.Message);
        message = ApplyCyrillicTransformations(message);
        args.Message = message;
    }

    private static string ApplyLatinTransformations(string message)
    {
        message = RegexLowerS.Replace(message, "sss");
        message = RegexUpperS.Replace(message, "SSS");
        message = RegexInternalX.Replace(message, "$1kss");
        message = RegexLowerEndX.Replace(message, "ecks$1");
        message = RegexUpperEndX.Replace(message, "ECKS$1");
        return message;
    }

    private static string ApplyCyrillicTransformations(string message)
    {
        message = RegexInternalKha.Replace(message, "$1кхс");
        message = RegexInternalUpperKha.Replace(message, "$1КХС");
        message = RegexLowerEndKha.Replace(message, "экс$1");
        message = RegexUpperEndKha.Replace(message, "ЭКС$1");
        message = RegexLowerCyrS.Replace(message, "ссс");
        message = RegexUpperCyrS.Replace(message, "ССС");
        message = RegexLowerZ.Replace(message, "ссс");
        message = RegexUpperZ.Replace(message, "ССС");
        message = RegexLowerSh.Replace(message, "шшш");
        message = RegexUpperSh.Replace(message, "ШШШ");
        message = RegexLowerCh.Replace(message, "щщщ");
        message = RegexUpperCh.Replace(message, "ЩЩЩ");
        return message;
    }
}
