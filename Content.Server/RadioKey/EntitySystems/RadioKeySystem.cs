using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.RadioKey.Components;
using Content.Server.Tools;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using RadioKeyPrototype = Content.Server.RadioKey.Components.RadioKeyPrototype;

namespace Content.Server.RadioKey.EntitySystems
{
    public sealed class RadioKeySystem : EntitySystem
    {
        [Dependency] private readonly SharedRadioSystem _sharedRadioSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RadioKeyHolderComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<RadioKeyHolderComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<RadioKeyComponent, InteractUsingEvent>(OnAfterRadioKeyInteract);
            //SubscribeLocalEvent<RadioKeyHolderComponent, InteractUsingEvent>(OnAfterInteract);
            SubscribeLocalEvent<RadioKeyComponent, RadioToggleFrequencyFilter>(OnRadioToggleFrequencyFilter);
        }

        private void OnAfterRadioKeyInteract(EntityUid uid, RadioKeyComponent component, InteractUsingEvent args)
        {
            if (args.Handled || args.Used is not { Valid: true } target || !_toolSystem.HasQuality(target, "Screwing"))
                return;

            if (component.RadioKeyPrototype.Count < 1)
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("radio-key-component-remove-no-radiokey"));
                return;
            }

            var prototypeList = component.RadioKeyPrototype;
            foreach (var ids in prototypeList)
            {
                // you MUST NAME the prototype equal to the entities!!!
                EntityManager.SpawnEntity(ids, Comp<TransformComponent>(component.Owner).Coordinates);
            }
            prototypeList.Clear();
            component.UpdateFrequencies();
            component.Owner.PopupMessage(args.User, Loc.GetString("radio-key-component-remove-radiokey"));
                // TODO update ui here

            args.Handled = true;
        }

        private void OnExamine(EntityUid uid, RadioKeyHolderComponent component, ExaminedEvent args)
        {
            // An encryption key for a radio headset.
            // It can access the following channels; (list)
            if (_prototypeManager.TryIndex<RadioKeyPrototype>(component.RadioKeyPrototype, out var prototype))
            {
                args.PushText(Loc.GetString("radio-key-holder-component-on-examine"));
                foreach (var freq in prototype.Frequency)
                {
                    var chan = _sharedRadioSystem.GetChannel(freq);
                    if (chan == null) continue;

                    args.PushMarkup(Loc.GetString("examine-headset-channel",
                        ("color", chan.Color),
                        ("key", chan.KeyCode),
                        ("id", chan.Name)));
                }
                return;
            }
            args.PushText(Loc.GetString("radio-key-holder-component-on-examine-err"));
        }

        private void OnAfterInteract(EntityUid uid, RadioKeyHolderComponent component, AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (args.Target is not { Valid: true } target ||
                !EntityManager.TryGetComponent(target, out RadioKeyComponent? radioKeyComponent))
                return;

            // headsets hold 2
            if (radioKeyComponent.RadioKeyPrototype.Count > 1)
            {
                radioKeyComponent.Owner.PopupMessage(args.User, Loc.GetString("radio-key-holder-component-after-interact-full"));
                args.Handled = true;
                return;
            }
            radioKeyComponent.RadioKeyPrototype.Add(component.RadioKeyPrototype);
            radioKeyComponent.UpdateFrequencies();
            if (EntityManager.TryGetComponent(target, out IRadio? radioComponent)) {
                _radioSystem.UpdateUIState(target, radioComponent, radioKeyComponent); // update if user adds keys while updating
            }

            EntityManager.QueueDeleteEntity(uid);

            args.Handled = true;
        }

        private void OnRadioToggleFrequencyFilter(EntityUid uid, RadioKeyComponent component,
            RadioToggleFrequencyFilter args)
        {
            if (component.BlockedFrequency.Contains(args.Frequency))
            {
                component.BlockedFrequency.Remove(args.Frequency);
            }
            else
            {
                component.BlockedFrequency.Add(args.Frequency);
            }
            _radioSystem.UpdateUIState(uid, null, component);
        }
    }
}
