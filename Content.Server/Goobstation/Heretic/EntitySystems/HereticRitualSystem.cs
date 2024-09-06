using Content.Server.Heretic.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticRitualSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HereticKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/Goobstation/Heretic/castsummon.ogg");

    public HereticRitualPrototype GetRitual(ProtoId<HereticRitualPrototype>? id)
    {
        if (id == null) throw new ArgumentNullException();
        return _proto.Index<HereticRitualPrototype>(id);
    }

    public bool TryDoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        if (!TryComp<HereticComponent>(performer, out var hereticComp))
            return false;

        var rit = (HereticRitualPrototype) GetRitual(ritualId).Clone();
        var lookup = _lookup.GetEntitiesInRange(platform, 0.5f);

        var missingList = new List<string>();
        var toDelete = new List<EntityUid>();

        // check for all conditions
        foreach (var behavior in rit.CustomBehaviors ?? new())
        {
            var ritData = new RitualData(performer, platform, ritualId, EntityManager);

            if (!behavior.Execute(ritData, out var missingStr))
            {
                if (missingStr != null)
                    _popup.PopupEntity(missingStr, platform, performer);
                return false;
            }
        }

        // check for matching entity names
        if (rit.RequiredEntityNames != null)
        {
            foreach (var name in rit.RequiredEntityNames)
            {
                foreach (var look in lookup)
                {
                    if (Name(look) == name.Key)
                    {
                        rit.RequiredEntityNames[name.Key] -= 1;
                        toDelete.Add(look);
                    }
                }
            }

            foreach (var name in rit.RequiredEntityNames)
                if (name.Value > 0)
                    missingList.Add(name.Key);
        }

        // check for matching tags
        if (rit.RequiredTags != null)
        {
            foreach (var tag in rit.RequiredTags)
            {
                foreach (var look in lookup)
                {
                    if (!TryComp<TagComponent>(look, out var tags))
                        continue;
                    var ltags = tags.Tags;

                    if (ltags.Contains(tag.Key))
                    {
                        rit.RequiredTags[tag.Key] -= 1;
                        toDelete.Add(look);
                    }
                }
            }

            foreach (var tag in rit.RequiredTags)
                if (tag.Value > 0)
                    missingList.Add(tag.Key);
        }

        // are we missing anything?
        if (missingList.Count > 0)
        {
            // we are! notify the performer about that!
            var sb = new StringBuilder();
            for (int i = 0; i < missingList.Count; i++)
            {
                // makes a nice, list, of, missing, items.
                if (i != missingList.Count - 1)
                    sb.Append($"{missingList[i]}, ");
                else sb.Append(missingList[i]);
            }

            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString())), platform, performer);
            return false;
        }

        // yay! ritual successfull!

        // finalize all of the custom ones
        foreach (var behavior in rit.CustomBehaviors ?? new())
        {
            var ritData = new RitualData(performer, platform, ritualId, EntityManager);
            behavior.Finalize(ritData);
        }

        // ya get some, ya lose some
        foreach (var ent in toDelete)
            QueueDel(ent);

        // add stuff
        if (rit.Output != null)
        {
            foreach (var ent in rit.Output.Keys)
                for (int i = 0; i < rit.Output[ent]; i++)
                    Spawn(ent, Transform(platform).Coordinates);
        }
        if (rit.OutputEvent != null)
            RaiseLocalEvent(performer, rit.OutputEvent, true);

        if (rit.OutputKnowledge != null)
            _knowledge.AddKnowledge(performer, hereticComp, (ProtoId<HereticKnowledgePrototype>) rit.OutputKnowledge);

        return true;
    }
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticRitualRuneComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        if (heretic.KnownRituals.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-norituals"), args.User, args.User);
            return;
        }

        if (heretic.ChosenRitual == null)
            heretic.ChosenRitual = heretic.KnownRituals[0];

        else if (heretic.ChosenRitual != null)
        {
            var index = heretic.KnownRituals.FindIndex(m => m == heretic.ChosenRitual) + 1;

            if (index >= heretic.KnownRituals.Count)
                index = 0;

            heretic.ChosenRitual = heretic.KnownRituals[index];
        }

        var ritualName = Loc.GetString(GetRitual(heretic.ChosenRitual).Name);
        _popup.PopupEntity(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), args.User, args.User);
    }
    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        if (!TryComp<MansusGraspComponent>(args.Used, out var grasp))
            return;

        if (heretic.ChosenRitual == null)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        if (!TryDoRitual(args.User, ent, (ProtoId<HereticRitualPrototype>) heretic.ChosenRitual))
            return;

        _audio.PlayPvs(RitualSuccessSound, ent, AudioParams.Default.WithVolume(-3f));
        Spawn("HereticRuneRitualAnimation", Transform(ent).Coordinates);
    }
}
