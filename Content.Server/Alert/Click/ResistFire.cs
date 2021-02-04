﻿using Content.Server.GameObjects.Components.Atmos;
 using Content.Shared.Alert;
 using JetBrains.Annotations;
 using Robust.Shared.Interfaces.Serialization;
 using Robust.Shared.Serialization;

namespace Content.Server.Alert.Click
{
    /// <summary>
    /// Resist fire
    /// </summary>
    [UsedImplicitly]
    public class ResistFire : IAlertClick
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }

        public void AlertClicked(ClickAlertEventArgs args)
        {
            if (args.Player.TryGetComponent(out FlammableComponent flammable))
            {
                flammable.Resist();
            }
        }
    }
}
