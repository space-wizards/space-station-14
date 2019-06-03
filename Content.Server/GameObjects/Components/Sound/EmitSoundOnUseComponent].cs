using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Audio;

namespace Content.Server.GameObjects.Components.Sound
{
    /// <summary>
    /// Simple sound emitter that emits sound on use in hand
    /// </summary>
    public class EmitSoundOnUseComponent : Component, IUse
    {
        /// <inheritdoc />
        /// 
        public override string Name => "EmitSoundOnUse";

        public string _soundName;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundName, "sound", "");
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(_soundName))
            {
                Owner.GetComponent<SoundComponent>().Play(_soundName, AudioParams.Default.WithVolume(-2f));
                return true;
            }
            return false;
        }
    }
}
