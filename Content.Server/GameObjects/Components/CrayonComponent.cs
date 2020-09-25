using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class CrayonComponent : SharedCrayonComponent, IAfterInteract, IUse, IDropped
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        //TODO: useSound
        private string _useSound;
        public Color Color { get; set; }

        //TODO: charges?

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CrayonUiKey.Key);

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "useSound", "");
            serializer.DataField(ref _color, "color", "white");
            Color = Color.FromName(_color);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
            SelectedState = "corgi"; //TODO: set to the first one in the list?
            Dirty();
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            switch (serverMsg.Message)
            {
                case CrayonSelectMessage msg:
                    // Check if the selected state is valid
                    var crayonDecals = _prototypeManager.EnumeratePrototypes<CrayonDecalPrototype>().FirstOrDefault();
                    if (crayonDecals != null)
                    {
                        if (crayonDecals.Decals.Contains(msg.State))
                        {
                            SelectedState = msg.State;
                            Dirty();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new CrayonComponentState(_color, SelectedState);
        }

        // Opens the selection window
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                UserInterface?.Open(actor.playerSession);
                return true;
            }
            return false;
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var entityManager = IoCManager.Resolve<IServerEntityManager>();
            //TODO: rotation?
            //TODO: check if the place is free
            var entity = entityManager.SpawnEntity("CrayonDecal", eventArgs.ClickLocation);
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(CrayonVisuals.State, SelectedState);
                appearance.SetData(CrayonVisuals.Color, Color);
            }
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent actor))
                UserInterface?.Close(actor.playerSession);
        }
    }
}
