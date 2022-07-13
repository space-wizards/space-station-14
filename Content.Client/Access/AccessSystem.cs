using System.Text;
using Content.Client.Stylesheets;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Access;

public sealed class AccessSystem : SharedAccessSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IUserInterfaceManager _interface = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public bool ReaderOverlay
    {
        get => (_overlay & AccessOverlay.Readers) != 0x0;
        set
        {
            var enabled = (_overlay & AccessOverlay.Readers) != 0x0;

            if (enabled == value) return;

            if (value)
            {
                _overlay |= AccessOverlay.Readers;
            }
            else
            {
                _overlay &= ~AccessOverlay.Readers;
            }
        }
    }

    private AccessOverlay _overlay = AccessOverlay.None;

    private readonly Dictionary<EntityUid, Control> _controls = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(EyeUpdateSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (ReaderOverlay)
        {
            var mapId = _eyeManager.CurrentMap;
            var worldAABB = _eyeManager.GetWorldViewport();

            var readerQuery = GetEntityQuery<AccessReaderComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var found = new HashSet<EntityUid>();
            var hovered = _interface.CurrentlyHovered;

            foreach (var ent in _lookup.GetEntitiesIntersecting(mapId, worldAABB,
                         LookupFlags.Anchored | LookupFlags.Approximate))
            {
                if (!readerQuery.TryGetComponent(ent, out var reader)) continue;

                if (!_controls.TryGetValue(ent, out var control))
                {
                    control = new PanelContainer()
                    {
                        StyleClasses = { StyleNano.StyleClassTooltipPanel },
                        MouseFilter = Control.MouseFilterMode.Stop,
                        Children =
                        {
                            new Label()
                            {
                                Text = $"{ent}",
                            }
                        }
                    };

                    _controls[ent] = control;
                    _interface.StateRoot.AddChild(control);
                }

                var text = new StringBuilder();
                var index = 0;
                var a = "";

                foreach (var list in reader.AccessLists)
                {
                    a = $"Tag {index}";
                    text.AppendLine(a);

                    foreach (var entry in list)
                    {
                        a = $"- {entry}";
                        text.AppendLine(a);
                    }

                    index++;
                }

                found.Add(ent);
                var label = (Label) control.GetChild(0);

                string textStr;

                if (text.Length >= 2)
                {
                    textStr = text.ToString();
                    textStr = textStr[..^2];
                }
                else
                {
                    textStr = "";
                }

                label.Text = textStr;
            }

            var toRemove = new ValueList<EntityUid>();

            foreach (var (ent, control) in _controls)
            {
                if (found.Contains(ent) && xformQuery.TryGetComponent(ent, out var xform))
                {
                    LayoutContainer.SetPosition(control, _interface.ScreenToUIPosition(_eyeManager.MapToScreen(xform.MapPosition)).Position);
                    continue;
                }

                control.Dispose();
                toRemove.Add(ent);
            }

            foreach (var ent in toRemove)
            {
                _controls.Remove(ent);
            }
        }
        else
        {
            foreach (var (ent, control) in _controls.ToArray())
            {
                control.Dispose();
                _controls.Remove(ent);
            }
        }
    }
}

[Flags]
public enum AccessOverlay : byte
{
    None = 0,
    Readers = 1 << 0,
}
