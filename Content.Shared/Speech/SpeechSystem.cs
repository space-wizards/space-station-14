//#region starlight
using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
//#endregion

namespace Content.Shared.Speech
{
    public sealed class SpeechSystem : EntitySystem
    {

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!; //#starlight

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SpeechComponent, ComponentInit>(OnSpeechComponentInit);//#starlight
        }

        public void SetSpeech(EntityUid uid, bool value, SpeechComponent? component = null)
        {
            if (value && !Resolve(uid, ref component))
                return;

            component = EnsureComp<SpeechComponent>(uid);

            if (component.Enabled == value)
                return;

            component.Enabled = value;

            Dirty(uid, component);
        }

        private void OnSpeakAttempt(SpeakAttemptEvent args)
        {
            if (!TryComp(args.Uid, out SpeechComponent? speech) || !speech.Enabled)
                args.Cancel();
        }

        //#region starlight
        public void AddMarkingEmotes(EntityUid uid, SpeechComponent component)
        {
            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearance))
                return; // no humanoid appearance so no emotes to add.
            List<MarkingPrototype> markings = [];
            foreach (var protoId in humanoidAppearance.MarkingSet.Markings.SelectMany(markingSet => markingSet.Value.Select(marking => marking.MarkingId)))
            {
                if (_prototypeManager.TryIndex<MarkingPrototype>((ProtoId<MarkingPrototype>)protoId, out MarkingPrototype? markingPrototype))
                    markings.Add(markingPrototype!);
            }
            HashSet<string> AttachedIds = new();
            foreach (var marking in markings)
            {
                AttachedIds.Add(marking.ID);
            }

            foreach (var marking in markings)
            {
                if (marking.Emotes == null)
                    continue;
                foreach (var emote in marking.Emotes)
                {
                    if (emote.RequiredMarkings == null ||
                        AttachedIds.IsSupersetOf(emote.RequiredMarkings.Select(i => i.Id)))
                    {
                        component.AllowedEmotes.Add(emote.EmotePrototype.Id);
                    }

                    if (emote.RequiredMarkingsAny != null)
                    {
                        foreach (var required in emote.RequiredMarkingsAny)
                        {
                            if (AttachedIds.Contains(required.Id))
                            {
                                component.AllowedEmotes.Add(emote.EmotePrototype.Id);
                            }
                        }
                    }
                }
            }
        }

        private void OnSpeechComponentInit(EntityUid uid, SpeechComponent component, ComponentInit args)
        {
            AddMarkingEmotes(uid, component);
        }
        //#endregion starlight
    }
}
