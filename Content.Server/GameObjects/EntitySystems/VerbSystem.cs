using System.Collections.Generic;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystemMessages;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using static Content.Shared.GameObjects.EntitySystemMessages.VerbSystemMessages;

namespace Content.Server.GameObjects.EntitySystems
{
    public class VerbSystem : EntitySystem
    {
        #pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        #pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<RequestVerbsMessage>();
            RegisterMessageType<UseVerbMessage>();
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case RequestVerbsMessage req:
                {
                    if (!_entityManager.TryGetEntity(req.EntityUid, out var entity))
                    {
                        return;
                    }

                    var session = _playerManager.GetSessionByChannel(channel);
                    var userEntity = session.AttachedEntity;

                    var data = new List<VerbsResponseMessage.VerbData>();
                    foreach (var provider in entity.GetAllComponents<IVerbProviderComponent>())
                    {
                        foreach (var verb in provider.GetVerbs(userEntity))
                        {
                            data.Add(new VerbsResponseMessage.VerbData(verb.GetName(userEntity, provider),
                                verb.GetType().AssemblyQualifiedName,
                                !verb.IsDisabled(userEntity, provider)));
                        }
                    }

                    var response = new VerbsResponseMessage(data, req.EntityUid);
                    RaiseNetworkEvent(response, channel);
                    break;
                }


                case UseVerbMessage use:
                {
                    if (!_entityManager.TryGetEntity(use.EntityUid, out var entity))
                    {
                        return;
                    }

                    var session = _playerManager.GetSessionByChannel(channel);
                    var userEntity = session.AttachedEntity;

                    foreach (var provider in entity.GetAllComponents<IVerbProviderComponent>())
                    {
                        foreach (var verb in provider.GetVerbs(userEntity))
                        {
                            if (verb.GetType().AssemblyQualifiedName == use.VerbKey)
                            {
                                verb.Activate(userEntity, provider);
                                break;
                            }
                        }
                    }

                    break;
                }
            }
        }
    }
}
