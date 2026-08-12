using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Light.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Light.Controls;

/// <summary>
/// Handles the label on the light replacer
/// </summary>
public sealed partial class LightReplacerStatusControl : Control
{
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly Entity<LightReplacerComponent> _parent;
    private readonly RichTextLabel _label;

    private EntProtoId? _prevActiveLightTube;
    private EntProtoId? _prevActiveLightBulb;
    private string? _labelTube;
    private string? _labelBulb;

    public LightReplacerStatusControl(Entity<LightReplacerComponent> parent)
    {
        IoCManager.InjectDependencies(this);

        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // only updates the UI if any of the details are different than they previously were
        if (_prevActiveLightTube == _parent.Comp.ActiveLightTube
            && _prevActiveLightBulb == _parent.Comp.ActiveLightBulb)
            return;

        _prevActiveLightTube = _parent.Comp.ActiveLightTube;
        _prevActiveLightBulb = _parent.Comp.ActiveLightBulb;

        if (!_prototype.Resolve(_prevActiveLightTube, out var tube)
            || !_prototype.Resolve(_prevActiveLightBulb, out var bulb))
            return;

        _labelBulb = bulb.Name;
        _labelTube = tube.Name;

        // Remove " light tube" at the end to save precious label space.
        if (_labelTube.EndsWith(" light tube"))
        {
            _labelTube = _labelTube.Remove(_labelTube.Length - 11);
            // Remove " crystal" in case of colored lights
            if (_labelTube.EndsWith(" crystal"))
                _labelTube = _labelTube.Remove(_labelTube.Length - 8);
        }
        // Same with bulbs.
        if (_labelBulb.EndsWith(" light bulb"))
        {
            _labelBulb = _labelBulb.Remove(_labelBulb.Length - 11);
            if (_labelBulb.EndsWith(" crystal"))
                _labelBulb = _labelBulb.Remove(_labelBulb.Length - 8);
        }

        // Update current active lights
        _label.SetMarkup(Loc.GetString("comp-light-replacer-label",
            ("tube", _labelTube),
            ("bulb", _labelBulb)));
    }
}
