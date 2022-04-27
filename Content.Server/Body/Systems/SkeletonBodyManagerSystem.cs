using Content.Server.Body.Components;
using Content.Server.Cloning;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
using Content.Shared.Species;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems
{
    public sealed class SkeletonBodyManagerSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SkeletonBodyManagerComponent, GetVerbsEvent<AlternativeVerb>>(AddReassembleVerbs);
        }

        /// <summary>
        /// Adds the custom verb for reassembling skeleton parts
        /// into a full skeleton
        /// </summary>
        private void AddReassembleVerbs(EntityUid uid, SkeletonBodyManagerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ActorComponent?>(args.User, out var actor))
                return;

            if (!TryComp<MindComponent>(uid, out var mind) || !mind.HasMind)
                return;

            // Custom verb
            AlternativeVerb custom = new();
            custom.Text = "Reassemble";
            custom.Act = () => Reassemble(uid, component, args);
            custom.IconTexture = "/Textures/Mobs/Species/Skeleton/parts.rsi/full.png";
            custom.Priority = 1;
            args.Verbs.Add(custom);
        }

        private void Reassemble(EntityUid uid, SkeletonBodyManagerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (component.DNA == null)
                return;

            // Creates the new entity and transfers the mind component
            var speciesProto = _prototype.Index<SpeciesPrototype>(component.DNA.Value.Profile.Species).Prototype;
            var mob = _entities.SpawnEntity(speciesProto, _entities.GetComponent<TransformComponent>(component.Owner).MapPosition);

            Get<SharedHumanoidAppearanceSystem>().UpdateFromProfile(mob, component.DNA.Value.Profile);
            _entities.GetComponent<MetaDataComponent>(mob).EntityName = component.DNA.Value.Profile.Name;

            if (TryComp<MindComponent>(uid, out var mindcomp) && mindcomp.Mind != null)
                mindcomp.Mind.TransferTo(mob);
        }

        /// <summary>
        /// Called before the skeleton entity is gibbed in order to save
        /// the dna for reassembly later
        /// </summary>
        /// <param name="uid"></param> the entity the mind is going to be transfered which also stores the DNA
        /// <param name="body"></param> the entity whose DNA is being saved
        public void UpdateDNAEntry(EntityUid uid, EntityUid body)
        {
            if (!TryComp<SkeletonBodyManagerComponent>(uid, out var skelBodyComp) ||
                !TryComp<MindComponent>(body, out var mindcomp))
                return;

            if (mindcomp.Mind == null)
                return;

            if (mindcomp.Mind.UserId == null)
                return;

            var profile = (HumanoidCharacterProfile) _prefsManager.GetPreferences(mindcomp.Mind.UserId.Value).SelectedCharacter;
            skelBodyComp.DNA = new ClonerDNAEntry(mindcomp.Mind, profile);
        }
    }
}
