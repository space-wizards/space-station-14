using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class CrayonComponent : SharedCrayonComponent, IAfterInteract, IUse
    {
        //TODO: useSound
        private string _useSound;
        public Color Color { get; set; }
        
        //TODO: charges?

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
            SelectedState = "corgi";
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new CrayonComponentState(_color, SelectedState);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var crayonDecals = prototypeManager.EnumeratePrototypes<CrayonDecalPrototype>().FirstOrDefault();
            if (crayonDecals == null)
                return false;

            var nextIndex = (crayonDecals.Decals.IndexOf(SelectedState) + 1) % crayonDecals.Decals.Count;
            SelectedState = crayonDecals.Decals[nextIndex];
            eventArgs.User.PopupMessage($"Now drawing {SelectedState}");
            Dirty();
            return true;
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var entityManager = IoCManager.Resolve<IServerEntityManager>();
            //TODO: rotation?
            var entity = entityManager.SpawnEntity("CrayonDecal", eventArgs.ClickLocation);
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(CrayonVisuals.State, SelectedState);
                appearance.SetData(CrayonVisuals.Color, Color);
            }
        }
    }
}
