using System.IO;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Server.GuideGenerator;

public static class RulesGenerator
{
    public static void Publish(StreamWriter file)
    {
        var cfgManager = IoCManager.Resolve<IConfigurationManager>();
        var resManager = IoCManager.Resolve<IResourceManager>();
        var rulesFile = cfgManager.GetCVar(CCVars.RulesFile);

        var rulesMarkup = resManager.ContentFileReadAllText($"/ServerInfo/{rulesFile}");

        // Lort forgive me I just want an easy way to dump text on the wiki
        // and DocumentParsingManager doesn't seem sufficient
        var markupParse = new Regex(@"(\[color=)(#.+)(\])(.*)(\[\/color\])", RegexOptions.Compiled);
        rulesMarkup = markupParse.Replace(rulesMarkup, "<span style=\"color=$2\">$4</span>");

        file.Write(rulesMarkup);
    }
}
