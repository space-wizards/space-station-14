// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.CameraMap;

public sealed class CameraMap : PictureViewer.PictureViewer
{
    private const int InitialCameraButtonPoolSize = 40;
    private Queue<CameraButton> _cameraButtonPool;
    private Dictionary<string, CameraButton> _cameraButtons;
    public float MapScale = 1;
    public string? SelectedAddress;
    public event Action<string>? Selected;

    public CameraMap()
    {
        _cameraButtonPool = new();
        _cameraButtons = new();

        for (var i = 0; i < InitialCameraButtonPoolSize; i++)
        {
            var button = CreateButton();
            _cameraButtonPool.Enqueue(button);
        }
    }

    private CameraButton CreateButton()
    {
        var button = new CameraButton();
        button.Pressed += OnButtonPressed;
        return button;
    }

    private void OnButtonPressed(GUIBoundKeyEventArgs args, string? address)
    {
        if (address != null)
            Selected?.Invoke(address);
    }

    public void SetSelectedAddress(string? selected)
    {
        if (selected == SelectedAddress)
            return;

        if (SelectedAddress != null && _cameraButtons.TryGetValue(SelectedAddress, out var previousButton))
        {
            previousButton.Selected = false;
            previousButton.UpdateVisuals();
        }

        if (string.IsNullOrEmpty(selected))
        {
            SelectedAddress = null;
        }
        else
        {
            SelectedAddress = selected;
            if (_cameraButtons.TryGetValue(SelectedAddress, out var button))
            {
                button.Selected = true;
                button.UpdateVisuals();
            }
        }
    }

    public void Populate(Dictionary<string, Dictionary<string, (string, Vector2)>> cameras)
    {
        List<string> toRemove = new();
        foreach (var (address, button) in _cameraButtons)
        {
            if (!cameras.ContainsKey(address))
            {
                _cameraButtonPool.Enqueue(button);
                RemoveChild(button);
                toRemove.Add(address);
            }
        }

        foreach (var address in toRemove)
        {
            _cameraButtons.Remove(address);
        }

        foreach (var (_, subnetCameras) in cameras)
        {
            foreach (var (address, (name, position)) in subnetCameras)
            {
                if (!_cameraButtons.TryGetValue(address, out var button))
                {
                    if (!_cameraButtonPool.TryDequeue(out button))
                        button = CreateButton();

                    _cameraButtons.Add(address, button);
                    button.Address = address;
                    button.Selected = button.Address == SelectedAddress;
                    button.UpdateVisuals();
                    AddChild(button);
                }

                TrackControl(button, new Vector2(position.X, -position.Y) * MapScale);
            }
        }

        UpdateTrackedControls();
    }

    private sealed class CameraButton : PanelContainer
    {
        public static readonly Color NormalColor = Color.FromHex("#1f53c2");
        public static readonly Color HoverColor = Color.LightBlue;
        public static readonly Color SelectedColor = Color.Green;

        private bool _mouseIsHovering = false;
        public bool Selected = false;
        public string? Address;

        public event Action<GUIBoundKeyEventArgs, string?>? Pressed;

        public CameraButton()
        {
            PanelOverride = new StyleBoxFlat()
            {
                BackgroundColor = NormalColor,
                BorderThickness = new Thickness(2f),
                BorderColor = Color.Black
            };

            SetSize = new Vector2(12, 12);
            MouseFilter = MouseFilterMode.Stop;

            SetupInputs();
            UpdateVisuals();
        }

        private void SetupInputs()
        {
            OnKeyBindDown += OnButtonPressed;

            OnMouseEntered += _ =>
            {
                _mouseIsHovering = true;
                UpdateVisuals();
            };

            OnMouseExited += _ =>
            {
                _mouseIsHovering = false;
                UpdateVisuals();
            };
        }

        public void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            Pressed?.Invoke(args, Address);
        }

        public void UpdateVisuals()
        {
            if (PanelOverride is StyleBoxFlat styleBox)
            {
                if (Selected)
                    styleBox.BackgroundColor = SelectedColor;
                else if (_mouseIsHovering)
                    styleBox.BackgroundColor = HoverColor;
                else
                    styleBox.BackgroundColor = NormalColor;
            }
        }
    }
}
