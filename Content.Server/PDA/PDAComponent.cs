using System;
using System.Collections.Generic;
using Content.Server.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.ActionBlocker;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA
{
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : Component, IAccess
    {
        public override string Name => "PDA";

        public const string IDSlotName = "pda_id_slot";
        public const string PenSlotName = "pda_pen_slot";

        [ViewVariables] [DataField("idCard")] public string? StartingIdCard;

        [ViewVariables] public IdCardComponent? ContainedID;
        [ViewVariables] public bool PenInserted;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables] public string? OwnerName;

        // TODO: Move me to ECS after Access refactoring
        #region Acces Logic
        [ViewVariables] private readonly PDAAccessSet _accessSet;

        public PDAComponent()
        {
            _accessSet = new PDAAccessSet(this);
        }

        public ISet<string>? GetContainedAccess()
        {
            return ContainedID?.Owner?.GetComponent<AccessComponent>()?.Tags;
        }

        ISet<string> IAccess.Tags => _accessSet;

        bool IAccess.IsReadOnly => true;

        void IAccess.SetTags(IEnumerable<string> newTags)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }
        #endregion

        // TODO: replace me with dynamic verbs for ItemSlotsSystem
        #region Verbs
        [Verb]
        public sealed class EjectPenVerb : Verb<PDAComponent>
        {
            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!component.Owner.TryGetComponent(out SharedItemSlotsComponent? slots))
                    return;

                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                    return;

                var item = EntitySystem.Get<SharedItemSlotsSystem>().PeekItemInSlot(slots, PenSlotName);
                if (item == null)
                    return;

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("eject-item-verb-text-default", ("item", item.Name));
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, PDAComponent pda)
            {
                var entityManager = pda.Owner.EntityManager;
                if (pda.Owner.TryGetComponent(out SharedItemSlotsComponent? itemSlots))
                {
                    entityManager.EntitySysManager.GetEntitySystem<SharedItemSlotsSystem>().
                        TryEjectContent(itemSlots, PenSlotName, user);
                }
            }
        }

        [Verb]
        public sealed class EjectIDVerb : Verb<PDAComponent>
        {
            public override bool AlternativeInteraction => true;

            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!component.Owner.TryGetComponent(out SharedItemSlotsComponent? slots))
                    return;

                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                    return;

                var item = EntitySystem.Get<SharedItemSlotsSystem>().PeekItemInSlot(slots, IDSlotName);
                if (item == null)
                    return;

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("eject-item-verb-text-default", ("item", item.Name));
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, PDAComponent pda)
            {
                var entityManager = pda.Owner.EntityManager;
                if (pda.Owner.TryGetComponent(out SharedItemSlotsComponent? itemSlots))
                {
                    entityManager.EntitySysManager.GetEntitySystem<SharedItemSlotsSystem>().
                        TryEjectContent(itemSlots, IDSlotName, user);
                }
            }
        }
        #endregion
    }
}
