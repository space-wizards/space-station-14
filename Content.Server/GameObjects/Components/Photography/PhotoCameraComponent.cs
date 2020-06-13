using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Audio;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Photography;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoCameraComponent : SharedPhotoCameraComponent, IExamine, IUse, IInteractUsing, ISuicideAct
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default;
        [Dependency] private readonly IRobustRandom _robustRandom = default;
#pragma warning restore 649

        private AudioSystem _audioSystem;
        private PhotoSystem _photoSystem;

        private int _radius = 1;
        private int _film = 10;
        private int _filmMax = 10;
        private bool _cameraOn = false;
        private SpriteComponent _spriteComponent;

        [ViewVariables(VVAccess.ReadWrite)]
        public int Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Film
        {
            get => _film;
            set
            {
                _film = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public int FilmMax
        {
            get => _filmMax;
            set
            {
                _filmMax = value;
                Dirty();
            }
        }

        /// <summary>
        /// Status of camera, whether it is on and ready to take a photo
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CameraOn
        {
            get => _cameraOn;
            set
            {
                _cameraOn = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out _spriteComponent);
            _audioSystem = EntitySystem.Get<AudioSystem>();
            _photoSystem = EntitySystem.Get<PhotoSystem>();

            Dirty();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _film, "film", 10);
            serializer.DataField(ref _filmMax, "maxfilm", 10);
            serializer.DataField(ref _radius, "radius", 1);
        }

        public override ComponentState GetComponentState()
        {
            return new PhotoCameraComponentState(CameraOn, Radius, Film, FilmMax);
        }

        /// <summary>
        /// Whether the camera has film or not
        /// </summary>
        private bool HasFilm => Film > 0;

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case TakingPhotoMessage _:
                    Film--;
                    PlaySoundCollection("TakePhoto", -5);
                    break;
                case TookPhotoMessage photoTaken:
                    var author = _entityManager.GetEntity(photoTaken.Author);
                    var photoEnt = _entityManager.SpawnEntity("Photo", author.Transform.GridPosition);
                    var photo = photoEnt.GetComponent<PhotoComponent>();

                    Logger.InfoS("photo", $"{author.Name} took a photo at {author.Transform.GridPosition}");

                    if (photoTaken.Suicide)
                    {
                        photoEnt.Description = Loc.GetString("The last photo {0} ever took before they went missing...", author.Name);

                        //Drop their items
                        if (author.TryGetComponent(out IHandsComponent hands))
                        {
                            foreach (var heldItem in hands.GetAllHeldItems())
                            {
                                hands.Drop(heldItem.Owner, false);
                            }
                        }

                        //Drop their gear
                        if (author.TryGetComponent(out InventoryComponent inv))
                        {
                            inv.UnequipAll(true);
                        }

                        //Vanish without a trace
                        author.Delete();
                    } else
                    {
                        if(author.TryGetComponent(out HandsComponent hands)){
                            hands.PutInHand(photoEnt.GetComponent<ItemComponent>());
                        }
                    }

                    //Store photo to disk, assign path to photo entity
                    _photoSystem.StorePhoto(photoTaken.Data, photo);

                    break;
            }
        }

        private void PlaySoundCollection(string name, float volume)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(name);
            var file = _robustRandom.Pick(soundCollection.PickFiles);
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity(file, Owner, AudioHelpers.WithVariation(0.15f).WithVolume(volume));
        }

        /// <summary>
        /// Toggles camera's on/off state
        /// </summary>
        private bool ToggleOnOff()
        {
            if (CameraOn)
            {
                CameraOn = false;
                _spriteComponent.LayerSetState(1, "camera_off");
                _audioSystem.PlayFromEntity("/Audio/machines/machine_switch.ogg", Owner, AudioHelpers.WithVariation(0.15f).WithVolume(-5));
                return true;
            }

            CameraOn = true;
            _spriteComponent.LayerSetState(1, "camera_on");
            _audioSystem.PlayFromEntity("/Audio/machines/machine_switch.ogg", Owner, AudioHelpers.WithVariation(0.15f).WithVolume(-5));
            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleOnOff();
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (CameraOn)
            {
                message.AddMarkup(Loc.GetString("The {0} is [color=green]On[/color]", Owner.Name));
            }
            else
            {
                message.AddMarkup(Loc.GetString("The {0} is [color=red]Off[/color]", Owner.Name));
            }

            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetString("\nFilm: [color={0}]{1}/{2}[/color], ",
                    Film<=0 ? "red" : "white", Film, FilmMax));
                message.AddMarkup(Loc.GetString("Radius: [color=white]{0}x{0}[/color]", Radius*2));
            }
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<PhotoFilmComponent>(out var film))
                return false;

            var empty = film.TakeFilm(FilmMax - Film, out var took);
            Film += took;
            if (empty)
            {
                film.Owner.Delete();
            }
            if (took > 0)
            { 
                _audioSystem.PlayFromEntity("/Audio/machines/machine_switch.ogg", Owner, AudioHelpers.WithVariation(0.15f).WithVolume(-5));
            }
            return true;
        }

        // TODO: trap user's soul in a photo (take a "fullsize" photo of the user)
        public SuicideKind Suicide(IEntity victim, IChatManager chat)
        {
            if (HasFilm && CameraOn) 
            {
                //You could say it was a... Fatal Frame™
                PlaySoundCollection("TakePhoto", -5);
                chat.EntityMe(victim, Loc.GetString("takes {0:their} last photo! It looks like {0:theyre} trying to commit suicide!", victim));
                SendNetworkMessage(new SuicideSelfieMessage(victim.Uid)); //will eventually delete the victim
                return SuicideKind.Special;
            }
            chat.EntityMe(victim, Loc.GetString("beats {0:themselves} to death with the {1}!", victim, Owner.Name));
            return SuicideKind.Brute;
        }

        [Verb]
        public sealed class PhotoRadius2x2 : Verb<PhotoCameraComponent>
        {
            protected override void GetData(IEntity user, PhotoCameraComponent component, VerbData data)
            {
                data.CategoryData = VerbCategories.PhotoRadius;
                data.Text = "2x2";
            }

            protected override void Activate(IEntity user, PhotoCameraComponent component)
            {
                component.Radius = 1;
            }
        }

        [Verb]
        public sealed class PhotoRadius4x4 : Verb<PhotoCameraComponent>
        {
            protected override void GetData(IEntity user, PhotoCameraComponent component, VerbData data)
            {
                data.CategoryData = VerbCategories.PhotoRadius;
                data.Text = "4x4";
            }

            protected override void Activate(IEntity user, PhotoCameraComponent component)
            {
                component.Radius = 2;
            }
        }

        [Verb]
        public sealed class PhotoRadius6x6 : Verb<PhotoCameraComponent>
        {
            protected override void GetData(IEntity user, PhotoCameraComponent component, VerbData data)
            {
                data.CategoryData = VerbCategories.PhotoRadius;
                data.Text = "6x6";
            }

            protected override void Activate(IEntity user, PhotoCameraComponent component)
            {
                component.Radius = 3;
            }
        }

        [Verb]
        public sealed class PhotoRadius8x8 : Verb<PhotoCameraComponent>
        {
            protected override void GetData(IEntity user, PhotoCameraComponent component, VerbData data)
            {
                data.CategoryData = VerbCategories.PhotoRadius;
                data.Text = "8x8";
            }

            protected override void Activate(IEntity user, PhotoCameraComponent component)
            {
                component.Radius = 4;
            }
        }

        [Verb]
        public sealed class PhotoRadius10x10 : Verb<PhotoCameraComponent>
        {
            protected override void GetData(IEntity user, PhotoCameraComponent component, VerbData data)
            {
                data.CategoryData = VerbCategories.PhotoRadius;
                data.Text = "10x10";
            }

            protected override void Activate(IEntity user, PhotoCameraComponent component)
            {
                component.Radius = 5;
            }
        }
    }
}
