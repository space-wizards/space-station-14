using Content.Client.Interfaces;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components.Photography;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Content.Client.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoCameraComponent : SharedPhotoCameraComponent, IItemStatus
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNotifyManager _notifyManager = default;
        [Dependency] private readonly IClyde _clyde = default;
        [Dependency] private readonly IEyeManager _eyeManager = default;
        [Dependency] private readonly IPlayerManager _playerManager = default;
        [Dependency] private readonly IResourceManager _resourceManager = default;
#pragma warning restore 649


        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;
        [ViewVariables] public bool CameraOn { get; private set; } = false;
        [ViewVariables] public int Radius { get; private set; } = 0;
        [ViewVariables] public int Film { get; private set; } = 0;
        [ViewVariables] public int FilmMax { get; private set; } = 0;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is PhotoCameraComponentState camera))
                return;

            CameraOn = camera.On;
            Radius = camera.Radius;
            Film = camera.Film;
            FilmMax = camera.FilmMax;
            _uiUpdateNeeded = true;
        }

        public Control MakeControl() => new StatusControl(this);

        private sealed class StatusControl : Control
        {
            private readonly PhotoCameraComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(PhotoCameraComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                var message = new FormattedMessage();

                if (_parent.CameraOn)
                {
                    message.AddMarkup(Loc.GetString("[color=green]On[/color]\n"));
                }
                else
                {
                    message.AddMarkup(Loc.GetString("[color=red]Off[/color]\n"));
                }

                message.AddMarkup(Loc.GetString("Film: [color={0}]{1}/{2}[/color], ",
                    _parent.Film <= 0 ? "red" : "white", _parent.Film, _parent.FilmMax));
                message.AddMarkup(Loc.GetString("Radius: [color=white]{0}x{0}[/color]", _parent.Radius * 2));

                _label.SetMessage(message);
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case SuicideSelfieMessage selfie:
                    var viewport = _eyeManager.GetWorldViewport();
                    var center = _eyeManager.WorldToScreen(_playerManager.LocalPlayer.ControlledEntity.Transform.GridPosition);
                    TryTakePhoto(selfie.Who, new Vector2(center.X, center.Y), true);
                    break;
            }
        }

        public async void TryTakePhoto(EntityUid author, Vector2 photoCenter, bool suicide = false)
        {
            if (!CameraOn)
            {
                _notifyManager.PopupMessageCursor(_playerManager.LocalPlayer.ControlledEntity, Loc.GetString("Turn the {0} on first!", Owner.Name));
                return;
            }

            if(Film <= 0)
            {
                _notifyManager.PopupMessageCursor(_playerManager.LocalPlayer.ControlledEntity, Loc.GetString("No film!"));
                return;
            }

            //Play sounds
            SendNetworkMessage(new TakingPhotoMessage());

            //Take a screenshot before the UI, and then crop it to the photo radius
            var screenshot = await _clyde.ScreenshotAsync(ScreenshotType.BeforeUI);
            var cropDimensions = EyeManager.PixelsPerMeter * (Radius * 4);

            //We'll try and center, but otherwise we'll shift the box so it doesn't go outside the viewport
            var cropX = (int)Math.Clamp(Math.Floor(photoCenter.X - cropDimensions / 2), 0, screenshot.Width - cropDimensions);
            var cropY = (int)Math.Clamp(Math.Floor(photoCenter.Y - cropDimensions / 2), 0, screenshot.Height - cropDimensions);

            Logger.InfoS("photo", $"cropX:{cropX}, cropY:{cropY}, w:{screenshot.Width}, h:{screenshot.Height}");

            var success = false;
            var filename = $"MY_PHOTO";
            using (screenshot)
            {
                screenshot.Mutate(
                    i => i.Crop(new Rectangle(cropX, cropY, cropDimensions, cropDimensions))
                );

                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        if (i != 0)
                        {
                            filename = $"MY_PHOTO-{i}";
                        }

                        await using var file =
                            _resourceManager.UserData.Open(new ResourcePath("/Screenshots") / $"{filename}.png", FileMode.OpenOrCreate);

                        await Task.Run(() =>
                        {
                            screenshot.SaveAsPng(file);
                        });

                        success = true;
                        Logger.InfoS("photo", "Photo taken as {0}.png", filename);
                        break;
                    }
                    catch (IOException e)
                    {
                        Logger.WarningS("photo", "Failed to save photo, retrying?:\n{0}", e);
                    }
                }
                if (!success)
                {
                    Logger.ErrorS("photo", "Unable to save photo.");
                }
            }

            //Photo's made, we can now upload it so everyone can see it on the item
            if (success)
            {

                await using var file =
                            _resourceManager.UserData.Open(new ResourcePath("/Screenshots") / $"{filename}.png", FileMode.Open);

                SendNetworkMessage(new TookPhotoMessage(author, file.CopyToArray(), suicide));
            }
        }
    }
}
