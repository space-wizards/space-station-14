using Content.Shared.Preferences;

namespace Content.Shared.CF.Preferences;

public static class CFHumanoidCharacterProfileExtensions
{
    public static HumanoidCharacterProfile WithCFBuild(this HumanoidCharacterProfile profile, CFCharacterBuildData build)
    {
        return new HumanoidCharacterProfile(profile)
        {
            CFBuild = build.Clone(),
        };
    }
}
