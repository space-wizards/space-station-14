#nullable enable
using Content.Server.DeviceNetwork;
using Content.Server.GameObjects.Components.DeviceNetworkConnections;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Disposal.MailingUnit;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.GameObjects.Components.Disposal
{
    //TODO: Documenting
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public class MailingUnitComponent : DisposalUnitComponent
    {
        private const string HolderPrototypeId = "DisposalHolder";

        public const string TAGS_MAIL = "mail";

        public const string NET_TAG = "tag";
        public const string NET_SRC = "src";
        public const string NET_TARGET = "target";
        public const string NET_CMD_SENT = "mail_sent";
        public const string NET_CMD_REQUEST = "get_mailer_tag";
        public const string NET_CMD_RESPONSE = "mailer_tag";

        public override string Name => "MailingUnit";

        public static readonly Regex TagRegex = new("^[a-zA-Z0-9, ]*$", RegexOptions.Compiled);

        [ViewVariables]
        private readonly List<string> _targetList = new();

        [ViewVariables]
        private string _target = "";

        [ViewVariables(VVAccess.ReadWrite)]
        private string _tag = "";


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _tag, "Tag", "");
        }

        public override void Initialize()
        {
            base.Initialize();

            UserInterface = Owner.GetUIOrNull(MailingUnitUiKey.Key);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            UpdateInterface();
        }

        public override void OnRemove()
        {
            UserInterface?.CloseAll();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }
        }



        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case SharedConfigurationComponent.ConfigUpdatedComponentMessage msg:
                    OnConfigUpdate(msg.Config);
                    break;
                case BaseNetworkConnectionComponent.PacketReceivedComponentMessage msg:
                    OnPacketReceived(msg);
                    break;
                case DisposalInserterComponent.ManualFlushReadyMessage:
                    if (Inserter != null)
                    {
                        SendMessage(new DisposalInserterComponent.ManualInsertMessage(CreateTaggedHolder(Inserter.ContainedEntities, _tag)));
                    }
                    break;
                case DisposalInserterComponent.InserterFlushedMessage msg:
                    if (!msg.Failed)
                    {
                        var payload = NetworkPayload.Create(
                            ( NetworkUtils.COMMAND, NET_CMD_SENT ),
                            ( NET_SRC, _tag ),
                            ( NET_TARGET, _target )
                        );

                        SendMessage(new BaseNetworkConnectionComponent.BroadcastComponentMessage(payload));
                    }
                    break;
            }
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateTargetList();
        }

        protected override void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null || !PlayerCanUse(obj.Session.AttachedEntity))
            {
                return;
            }

            base.OnUiReceiveMessage(obj);

            if (obj.Message is UiTargetUpdateMessage tagMessage && TagRegex.IsMatch(tagMessage.Target))
            {
                _target = tagMessage.Target;
            }
        }

        protected override bool OpenUiForUser(ITargetedInteractEventArgs eventArgs)
        {
            if (base.OpenUiForUser(eventArgs))
            {
                UpdateTargetList();
                return true;
            }

            return false;
        }

        protected override void UpdateInterface()
        {
            UserInterface?.SetState(new MailingUnitBoundUserInterfaceState(Owner.Name, State, Powered, Engaged, _tag, _targetList, _target));
        }

        private IEntity CreateTaggedHolder(IReadOnlyCollection<IEntity> entities, string tag)
        {
            var holder = Owner.EntityManager.SpawnEntity(HolderPrototypeId, Owner.Transform.MapPosition);
            var holderComponent = holder.GetComponent<DisposalHolderComponent>();

            holderComponent.Tags.Add(tag);
            holderComponent.Tags.Add(TAGS_MAIL);

            foreach (var entity in entities.ToArray())
            {
                holderComponent.TryInsert(entity);
            }

            return holder;
        }

        private void UpdateTargetList()
        {
            _targetList.Clear();
            var payload = NetworkPayload.Create(
                ( NetworkUtils.COMMAND, NET_CMD_REQUEST )
            );

            SendMessage(new BaseNetworkConnectionComponent.BroadcastComponentMessage(payload));
        }

        private void OnConfigUpdate(Dictionary<string, string> config)
        {
            if (config.TryGetValue("Tag", out var tag))
                _tag = tag;
        }

        private void OnPacketReceived(BaseNetworkConnectionComponent.PacketReceivedComponentMessage msg)
        {
            if (msg.Payload.TryGetValue(NetworkUtils.COMMAND, out var command) && Powered)
            {
                if (command == NET_CMD_RESPONSE && msg.Payload.TryGetValue(NET_TAG, out var tag))
                {
                    _targetList.Add(tag);
                    UpdateInterface();
                }

                if (command == NET_CMD_REQUEST)
                {
                    if (_tag == "" || !Powered)
                        return;

                   var data = NetworkPayload.Create(
                        (NetworkUtils.COMMAND, NET_CMD_RESPONSE),
                        (NET_TAG, _tag)
                   );

                    SendMessage(new BaseNetworkConnectionComponent.SendComponentMessage(msg.Sender, data));
                }
            }
        }
    }
}
