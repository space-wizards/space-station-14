using Content.Shared.Examine;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;
using Content.Server.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Headset
{
    public sealed class HeadsetSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HeadsetComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<HeadsetComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<HeadsetComponent, InteractUsingEvent>(OnInteractUsing);
        }
        private void OnInit(EntityUid uid, HeadsetComponent component, ComponentInit args)
        {
            component.ChipContainer = _container.EnsureContainer<Container>(uid, HeadsetComponent.ChipContainerName);
            if(component.ChipsPrototypes.Count > 0)
            {
                foreach(string chip in component.ChipsPrototypes)
                {
                    if (TryComp<TransformComponent>(uid, out var transform))
                    {
                        var C = EntityManager.SpawnEntity(chip, transform.Coordinates);
                        if(!InstallChip(component, C))
                        {
                            EntityManager.DeleteEntity(C);
                            break;
                        }
                    }
                }
            }
            RecalculateChannels(component);
        }
        private bool InstallChip(HeadsetComponent src, EntityUid Chip)
        {
            if(src.ChipContainer.Insert(Chip))
            {
                src.ChipsInstalled.Add(Chip);
                return true;
            }
            RecalculateChannels(src);
            return false;
        }
        private void RecalculateChannels(HeadsetComponent src)
        {
            // foreach(string i in component.Channels)
            //     component.Channels.Remove(i);
            src.Channels.Clear();
            foreach(EntityUid i in src.ChipsInstalled)
                if(TryComp<EncryptionChipComponent?>(i, out var chip))
                    foreach(var j in chip.Channels)
                        src.Channels.Add(j);
            return;
        }
        private void OnInteractUsing(EntityUid uid, HeadsetComponent component, InteractUsingEvent args)
        {
            if(!component.IsChipsExtractable || !TryComp<ContainerManagerComponent>(uid, out var Storage))
            {
                return;
            }
           if(TryComp<EncryptionChipComponent?>(args.Used, out var chip))
            {
                if(component.ChipSlotsAmount > component.ChipsInstalled.Count)
                    if(_container.TryRemoveFromContainer(args.Used) && component.ChipContainer.Insert(args.Used))
                    {
                        component.ChipsInstalled.Add(args.Used);
                        RecalculateChannels(component);

                        _popupSystem.PopupEntity(Loc.GetString("headset-encryption-chip-successfully-installed"), uid, Filter.Entities(args.User));
                        //("chipname", args.Used.GetComponent<MetaDataComponent>(speaker).EntityName), ("srcname", uid))
                        SoundSystem.Play(component.ChipInsertionSound.GetSound(), Filter.Pvs(args.Target), args.Target);
                    }
                else
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-chip-slots-already-full"), uid, Filter.Entities(args.User));
                return;
            } 
            if(TryComp<ToolComponent?>(args.Used, out var tool))
            {
                if(component.ChipsInstalled.Count > 0)
                {
                    if(_toolSystem.UseTool(
                        args.Used,                  args.User,          uid,
                        0f,                         0f,                 new String[]{"Screwing"},
                        doAfterCompleteEvent: null, toolComponent: tool)
                    )
                    {
                        foreach(var i in component.ChipsInstalled)
                        {
                            component.ChipContainer.Remove(i);
                        }
                        component.ChipsInstalled.Clear();
                        RecalculateChannels(component);
                        _popupSystem.PopupEntity(Loc.GetString("headset-encryption-chips-all-extrated"), uid, Filter.Entities(args.User));
                        SoundSystem.Play(component.ChipExtarctionSound.GetSound(), Filter.Pvs(args.Target), args.Target);
                    }
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-chips-no-chips"), uid, Filter.Entities(args.User));

                }
            }
        }
        private void OnExamined(EntityUid uid, HeadsetComponent component, ExaminedEvent args)
        {
            if(!args.IsInDetailsRange)
                return;
            // args.PushMarkup(Loc.GetString("examine-radio-frequency", ("frequency", component.BroadcastFrequency)));
            if(component.Channels.Count > 0)
            {
                args.PushMarkup("\n" + Loc.GetString("examine-headset") + "\n");
                foreach (var id in component.Channels)
                {
                    // if(id == "Common")
                    //     continue;
                    var proto = _protoManager.Index<RadioChannelPrototype>(id);
                    args.PushMarkup(Loc.GetString("examine-headset-channel",
                        ("color", proto.Color),
                        ("key", proto.KeyCode),
                        ("id", proto.LocalizedName),
                        ("freq", proto.Frequency)) + "\n");
                }
                args.PushMarkup(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
            }
        }
    }
}
