using Content.Client.Nodes.Overlays;
using Content.Shared.Nodes;

namespace Content.Client.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem
{
    /// <summary>Whether or not the node graph debugging overlay is currently enabled.</summary>
    private bool _visEnabled = false;


    private void EnableDebugOverlay()
    {
        if (_visEnabled)
            return;

        _visEnabled = true;

        _overlayMan.AddOverlay(new DebugNodeVisualsOverlay(
            EntityManager,
            _gameTiming,
            _inputMan,
            _uiMan,
            _rscCache
        ));
    }

    private void DisableDebugOverlay()
    {
        if (!_visEnabled)
            return;

        _visEnabled = false;

        _overlayMan.RemoveOverlay<DebugNodeVisualsOverlay>();
    }


    #region Event Handlers

    /// <summary>Starts or stops rendering the node graph debugging overlay.</summary>
    private void OnEnableNodeVisMsg(EnableNodeVisMsg msg)
    {
        if (msg.Enabled)
            EnableDebugOverlay();
        else
            DisableDebugOverlay();
    }

    #endregion Event Handlers
}
