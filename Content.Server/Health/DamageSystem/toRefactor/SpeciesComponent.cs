using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Observer;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSpeciesComponent))]
    public class SpeciesComponent : SharedSpeciesComponent, IActionBlocker, IOnDamageBehavior, IExAct, IRelayMoveInput
    {


        /// <summary>
        /// Variable for serialization
        /// </summary>
        private string templatename;

        private int _heatResistance;
        public int HeatResistance => _heatResistance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref templatename, "Template", "Human");

            var type = typeof(SpeciesComponent).Assembly.GetType("Content.Server.GameObjects." + templatename);
            DamageTemplate = (DamageTemplates) Activator.CreateInstance(type);
            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }




        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (CurrentDamageState is DeadState)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }
    }

    /// <summary>
    ///     Fired when <see cref="SpeciesComponent.CurrentDamageState"/> changes.
    /// </summary>
    public sealed class MobDamageStateChangedMessage : EntitySystemMessage
    {
        public MobDamageStateChangedMessage(SpeciesComponent species)
        {
            Species = species;
        }

        /// <summary>
        ///     The species component that was changed.
        /// </summary>
        public SpeciesComponent Species { get; }
    }
}
