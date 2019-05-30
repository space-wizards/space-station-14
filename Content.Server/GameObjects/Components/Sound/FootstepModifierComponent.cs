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
using Content.Server.GameObjects.Components.Sound;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components
{
    /// <summary>
    /// Changes footstep sound
    /// </summary>
    public class FootstepModifierComponent : Component, IOnMove
    {
        #pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        #pragma warning restore 649
        /// <inheritdoc />
        /// 
        private Random _footstepRandom;

        public override string Name => "FootstepModifier";

        public string _soundCollectionName;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundCollectionName, "footstepSoundCollection", "");
        }

        public override void Initialize()
        {
            base.Initialize();
            _footstepRandom = new Random();
        }

        bool IOnMove.OnMove()
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollectionName);
            var file = _footstepRandom.Pick(soundCollection.PickFiles);
            Owner.GetComponent<SoundComponent>().Play(file);
            return true;
        }
    }
}
