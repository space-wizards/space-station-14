#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedMagazineComponent))]
    public sealed class ClientRangedMagazineComponent : SharedRangedMagazineComponent, IExamine
    {
        private Stack<bool> _spawnedAmmo = new Stack<bool>();

        public override int ShotsLeft => _spawnedAmmo.Count + UnspawnedCount;

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
                return;
            
            appearanceComponent.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            appearanceComponent.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is RangedMagazineComponentState cast))
                return;

            if (_spawnedAmmo == cast.SpawnedAmmo)
                return;
            
            _spawnedAmmo = cast.SpawnedAmmo;
            UpdateAppearance();
        }

        public bool TryPop(out bool entity)
        {
            if (_spawnedAmmo.TryPop(out entity))
            {
                UpdateAppearance();
                return true;
            }

            return false;
        }

        protected override bool TryInsertAmmo(IEntity user, IEntity ammo)
        {
            // TODO
            return true;
        }

        protected override bool Use(IEntity user)
        {
            // TODO
            return true;
        }
        
        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            var text = Loc.GetString("It's a [color=white]{0}[/color] magazine of [color=white]{1}[/color] caliber.", MagazineType, Caliber);
            message.AddMarkup(text);
        }
        
        [Verb]
        private sealed class DumpMagazineVerb : Verb<ClientRangedMagazineComponent>
        {
            protected override void GetData(IEntity user, ClientRangedMagazineComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var remaining = Math.Min(10, component.ShotsLeft);

                data.Text = Loc.GetString($"Dump {remaining}");
            }

            protected override void Activate(IEntity user, ClientRangedMagazineComponent component)
            {
                var remaining = (byte) Math.Min(10, component.ShotsLeft);
                
                component.SendNetworkMessage(new DumpRangedMagazineComponentMessage(remaining));
            }
        }
    }
}