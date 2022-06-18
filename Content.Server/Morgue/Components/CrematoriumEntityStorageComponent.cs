
using System.Threading;
using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.Morgue;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(MorgueEntityStorageComponent))]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
#pragma warning disable 618
    public sealed class CrematoriumEntityStorageComponent : MorgueEntityStorageComponent
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [DataField("cremateStartSound")] private SoundSpecifier _cremateStartSound = new SoundPathSpecifier("/Audio/Items/lighter1.ogg");
        [DataField("crematingSound")] private SoundSpecifier _crematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");
        [DataField("cremateFinishSound")] private SoundSpecifier _cremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");

        [ViewVariables]
        public bool Cooking { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        private int _burnMilis = 5000;

        private CancellationTokenSource? _cremateCancelToken;

        public override bool CanOpen(EntityUid user, bool silent = false)
        {
            if (Cooking)
            {
                if (!silent)
                    Owner.PopupMessage(user, Loc.GetString("crematorium-entity-storage-component-is-cooking-safety-message"));
                return false;
            }
            return base.CanOpen(user, silent);
        }

        public void TryCremate()
        {
            if (Cooking) return;
            if (Open) return;

            SoundSystem.Play(_cremateStartSound.GetSound(), Filter.Pvs(Owner), Owner);

            Cremate();
        }

        public void Cremate()
        {
            if (Open)
                CloseStorage();

            if(_entities.TryGetComponent(Owner, out AppearanceComponent? appearanceComponent))
                appearanceComponent.SetData(CrematoriumVisuals.Burning, true);
            Cooking = true;

            SoundSystem.Play(_crematingSound.GetSound(), Filter.Pvs(Owner), Owner);

            _cremateCancelToken?.Cancel();

            _cremateCancelToken = new CancellationTokenSource();
            Owner.SpawnTimer(_burnMilis, () =>
            {
                if (_entities.Deleted(Owner))
                    return;
                if(_entities.TryGetComponent(Owner, out appearanceComponent))
                    appearanceComponent.SetData(CrematoriumVisuals.Burning, false);
                Cooking = false;

                if (Contents.ContainedEntities.Count > 0)
                {
                    for (var i = Contents.ContainedEntities.Count - 1; i >= 0; i--)
                    {
                        var item = Contents.ContainedEntities[i];
                        Contents.Remove(item);
                        _entities.DeleteEntity(item);
                    }

                    var ash = _entities.SpawnEntity("Ash", _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                    Contents.Insert(ash);
                }

                TryOpenStorage(Owner);

                SoundSystem.Play(_cremateFinishSound.GetSound(), Filter.Pvs(Owner), Owner);

            }, _cremateCancelToken.Token);
        }
    }
}
