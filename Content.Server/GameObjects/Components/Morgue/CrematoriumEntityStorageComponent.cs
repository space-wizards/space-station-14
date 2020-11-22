#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components.Morgue;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Morgue
{
    [RegisterComponent]
    [ComponentReference(typeof(MorgueEntityStorageComponent))]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class CrematoriumEntityStorageComponent : MorgueEntityStorageComponent, IExamine
    {
        public override string Name => "CrematoriumEntityStorage";

        [ViewVariables]
        public bool Cooking { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        private int _burnMilis = 3000;

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Appearance == null) return;

            if (inDetailsRange)
            {
                if (Appearance.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
                {
                    message.AddMarkup(Loc.GetString("The {0:theName} is [color=red]active[/color]!\n", Owner));
                }

                if (Appearance.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
                {
                    message.AddMarkup(Loc.GetString("The content light is [color=green]on[/color], there's something in here."));
                }
                else
                {
                    message.AddText(Loc.GetString("The content light is off, there's nothing in here."));
                }
            }
        }

        public override bool CanOpen(IEntity user, bool silent = false)
        {
            if (Cooking)
            {
                if (!silent) Owner.PopupMessage(user, Loc.GetString("Safety first, not while it's active!"));
                return false;
            }
            return base.CanOpen(user, silent);
        }

        public void Cremate()
        {
            if (Cooking) return;
            if (Open) return;

            Appearance?.SetData(CrematoriumVisuals.Burning, true);
            Cooking = true;

            Timer.Spawn(_burnMilis, () =>
            {
                Appearance?.SetData(CrematoriumVisuals.Burning, false);
                Cooking = false;

                if (Contents.ContainedEntities.Count > 0)
                {
                    for (var i = Contents.ContainedEntities.Count - 1; i >= 0; i--)
                    {
                        var item = Contents.ContainedEntities[i];
                        Contents.Remove(item);
                        item.Delete();
                    }

                    var ash = Owner.EntityManager.SpawnEntity("Ash", Owner.Transform.Coordinates);
                    Contents.Insert(ash);
                }

                TryOpenStorage(Owner);

                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/ding.ogg", Owner);
            });
        }

        [Verb]
        private sealed class CremateVerb : Verb<CrematoriumEntityStorageComponent>
        {
            protected override void GetData(IEntity user, CrematoriumEntityStorageComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || component.Cooking || component.Open)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Cremate");
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, CrematoriumEntityStorageComponent component)
            {
                component.Cremate();
            }
        }
    }
}
