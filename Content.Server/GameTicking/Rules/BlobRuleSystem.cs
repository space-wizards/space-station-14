using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;

namespace Content.Server.GameTicking.Rules;

public sealed class BlobRuleSystem : GameRuleSystem<BlobRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("preset");

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<BlobRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ninja, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (ninja.Blobs.Count < 1)
                return;

            var result = Loc.GetString("blob-round-end-result", ("blobCount", ninja.Blobs.Count));

            // yeah this is duplicated from traitor rules lol, there needs to be a generic rewrite where it just goes through all minds with objectives
            foreach (var t in ninja.Blobs)
            {
                var name = t.Mind.CharacterName;
                _mindSystem.TryGetSession(t.Mind, out var session);
                var username = session?.Name;

                var objectives = t.Mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result += "\n" + Loc.GetString("blob-user-was-a-blob", ("user", username));
                        else
                        {
                            result += "\n" + Loc.GetString("blob-user-was-a-blob-named", ("user", username),
                                ("name", name));
                        }
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString("blob-was-a-blob-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                    {
                        result += "\n" + Loc.GetString("blob-user-was-a-blob-with-objectives",
                            ("user", username));
                    }
                    else
                    {
                        result += "\n" + Loc.GetString("blob-user-was-a-blob-with-objectives-named",
                            ("user", username), ("name", name));
                    }
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("blob-was-a-blob-with-objectives-named", ("name", name));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += "\n" + Loc.GetString($"preset-blob-objective-issuer-{objectiveGroup.Key}");

                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }

            ev.AddLine(result);
        }
    }
}
