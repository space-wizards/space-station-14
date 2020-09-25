using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using static Content.Shared.GameObjects.Components.SharedCrayonComponent;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class CrayonComponent : Component, IAfterInteract, IUse
    {
        public override string Name => "Crayon";

        //TODO: useSound & Color
        private string _useSound;
        private string _color;
        public Color Color { get; private set; }
        public string SelectedState { get; set; } = "corgi";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "useSound", "");
            serializer.DataField(ref _color, "color", "white");
            Color = Color.FromName(_color);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            SelectedState = SelectedState == "corgi" ? "body" : "corgi";
            eventArgs.User.PopupMessage($"Now drawing {SelectedState}");
            return true;
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var entityManager = IoCManager.Resolve<IServerEntityManager>();
            var entity = entityManager.SpawnEntity("CrayonDecal", eventArgs.ClickLocation);
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(CrayonVisuals.State, SelectedState);
                appearance.SetData(CrayonVisuals.Color, Color);
            }
        }
    }
}
