using Content.Server.Instruments;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.PAI;
using Robust.Server.GameObjects;

namespace Content.Server.PAI
{
    public sealed class PAISystem : SharedPAISystem
    {
        [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PAIComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PAIComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<PAIComponent, MindRemovedMessage>(OnMindRemoved);
        }

        private void OnUseInHand(EntityUid uid, PAIComponent component, UseInHandEvent args)
        {
            if (!TryComp<MindContainerComponent>(uid, out var mind) || !mind.HasMind)
                component.LastUser = args.User;
        }

        private void OnMindAdded(EntityUid uid, PAIComponent component, MindAddedMessage args)
        {
            if (component.LastUser == null)
                return;

            // Ownership tag
            var val = Loc.GetString("pai-system-pai-name", ("owner", component.LastUser));

            // TODO Identity? People shouldn't dox-themselves by carrying around a PAI.
            // But having the pda's name permanently be "old lady's PAI" is weird.
            // Changing the PAI's identity in a way that ties it to the owner's identity also seems weird.
            // Cause then you could remotely figure out information about the owner's equipped items.

            _metaData.SetEntityName(uid, val);
        }

        private void OnMindRemoved(EntityUid uid, PAIComponent component, MindRemovedMessage args)
        {
            // Mind was removed, shutdown the PAI.
            PAITurningOff(uid);
        }

        public void PAITurningOff(EntityUid uid)
        {
            //  Close the instrument interface if it was open
            //  before closing
            if (HasComp<ActiveInstrumentComponent>(uid) && TryComp<ActorComponent>(uid, out var actor))
            {
                _instrumentSystem.ToggleInstrumentUi(uid, actor.PlayerSession);
            }

            //  Stop instrument
            if (TryComp<InstrumentComponent>(uid, out var instrument)) _instrumentSystem.Clean(uid, instrument);
            if (TryComp<MetaDataComponent>(uid, out var metadata))
            {
                var proto = metadata.EntityPrototype;
                if (proto != null)
                    _metaData.SetEntityName(uid, proto.Name);
            }
        }
    }
}
