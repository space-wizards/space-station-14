using Content.Server.Heretic.EntitySystems;
using Content.Shared.Dataset;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualKnowledgeBehavior : RitualCustomBehavior
{
    // made static so that it doesn't regenerate itself each time
    private static Dictionary<ProtoId<TagPrototype>, int> requiredTags = new();
    private List<EntityUid> toDelete = new();

    private IPrototypeManager _prot = default!;
    private IRobustRandom _rand = default!;
    private EntityLookupSystem _lookup = default!;
    private HereticSystem _heretic = default!;

    // this is basically a ripoff from hereticritualsystem
    public override bool Execute(RitualData args, out string? outstr)
    {
        _prot = IoCManager.Resolve<IPrototypeManager>();
        _rand = IoCManager.Resolve<IRobustRandom>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();

        outstr = null;

        // generate new set of tags
        if (requiredTags.Count == 0)
            for (int i = 0; i < 4; i++)
                requiredTags.Add(_rand.Pick(_prot.Index<DatasetPrototype>("EligibleTags").Values), 1);

        var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);
        var missingList = new List<string>();

        foreach (var look in lookup)
        {
            foreach (var tag in requiredTags)
            {
                if (!args.EntityManager.TryGetComponent<TagComponent>(look, out var tags))
                    continue;
                var ltags = tags.Tags;

                if (ltags.Contains(tag.Key))
                {
                    requiredTags[tag.Key] -= 1;
                    toDelete.Add(look);
                }
            }
        }

        foreach (var tag in requiredTags)
            if (tag.Value > 0)
                missingList.Add(tag.Key);

        if (missingList.Count > 0)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < missingList.Count; i++)
            {
                if (i != missingList.Count - 1)
                    sb.Append($"{missingList[i]}, ");
                else sb.Append(missingList[i]);
            }

            outstr = Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString()));
            return false;
        }

        return true;
    }

    public override void Finalize(RitualData args)
    {
        // delete all and reset
        foreach (var ent in toDelete)
            args.EntityManager.QueueDeleteEntity(ent);
        toDelete = new();

        if (args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
            _heretic.UpdateKnowledge(args.Performer, hereticComp, 4);

        // reset tags
        requiredTags = new();
    }
}
