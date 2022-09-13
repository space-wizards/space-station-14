namespace Content.Server.CharacterAppearance.Components;

[RegisterComponent]
public sealed class RandomHumanoidAppearanceComponent : Component
{
    [DataField("randomizeName")] public bool RandomizeName = true;

    [DataField("ignoredSpecies")] public readonly HashSet<string> IgnoredSpecies = new();
}
