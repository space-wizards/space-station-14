namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for a rule that announces kills globally.
/// </summary>
[RegisterComponent, Access(typeof(KillCalloutRuleSystem))]
public sealed partial class KillCalloutRuleComponent : Component
{
    /// <summary>
    /// Callouts used when one player kills another.
    /// </summary>
    [DataField("killCallouts")]
    public List<string> KillCallouts = new()
    {
        "death-match-kill-callout-1",
        "death-match-kill-callout-2",
        "death-match-kill-callout-3",
        "death-match-kill-callout-4",
        "death-match-kill-callout-5",
        "death-match-kill-callout-6",
        "death-match-kill-callout-7",
        "death-match-kill-callout-8",
        "death-match-kill-callout-9",
        "death-match-kill-callout-10",
        "death-match-kill-callout-11",
        "death-match-kill-callout-12",
        "death-match-kill-callout-13",
        "death-match-kill-callout-14",
        "death-match-kill-callout-15",
        "death-match-kill-callout-16",
        "death-match-kill-callout-17",
        "death-match-kill-callout-18",
        "death-match-kill-callout-19",
        "death-match-kill-callout-20",
        "death-match-kill-callout-21",
        "death-match-kill-callout-22",
        "death-match-kill-callout-23",
        "death-match-kill-callout-24",
        "death-match-kill-callout-25",
        "death-match-kill-callout-26",
        "death-match-kill-callout-27",
        "death-match-kill-callout-28",
        "death-match-kill-callout-29",
        "death-match-kill-callout-30",
        "death-match-kill-callout-31",
        "death-match-kill-callout-32",
        "death-match-kill-callout-33",
        "death-match-kill-callout-34",
        "death-match-kill-callout-35",
        "death-match-kill-callout-36",
        "death-match-kill-callout-37",
        "death-match-kill-callout-38",
        "death-match-kill-callout-39",
        "death-match-kill-callout-40",
        "death-match-kill-callout-41",
        "death-match-kill-callout-42",
        "death-match-kill-callout-43",
        "death-match-kill-callout-44",
        "death-match-kill-callout-45",
        "death-match-kill-callout-46",
        "death-match-kill-callout-47",
        "death-match-kill-callout-48",
        "death-match-kill-callout-49",
        "death-match-kill-callout-50",
        "death-match-kill-callout-51",
        "death-match-kill-callout-52",
        "death-match-kill-callout-53",
        "death-match-kill-callout-54",
        "death-match-kill-callout-55",
        "death-match-kill-callout-56",
        "death-match-kill-callout-57",
        "death-match-kill-callout-58",
        "death-match-kill-callout-59",
        "death-match-kill-callout-60"
    };

    /// <summary>
    /// Callouts used when a player is killed by the environment
    /// </summary>
    [DataField("environmentKillCallouts")]
    public List<string> SelfKillCallouts = new()
    {
        "death-match-kill-callout-env-1",
        "death-match-kill-callout-env-2",
        "death-match-kill-callout-env-3",
        "death-match-kill-callout-env-4",
        "death-match-kill-callout-env-5",
        "death-match-kill-callout-env-6",
        "death-match-kill-callout-env-7",
        "death-match-kill-callout-env-8",
        "death-match-kill-callout-env-9",
        "death-match-kill-callout-env-10"
    };
}
