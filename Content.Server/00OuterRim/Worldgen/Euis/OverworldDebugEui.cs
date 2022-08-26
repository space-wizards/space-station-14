using Content.Server._00OuterRim.Worldgen.Systems.Overworld;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Procedural;

namespace Content.Server._00OuterRim.Worldgen.Euis;

public class OverworldDebugEui : BaseEui
{
    public int Zoom = 8;

    public override OverworldDebugEuiState GetNewState()
    {
        return new OverworldDebugEuiState(EntitySystem.Get<WorldChunkSystem>().GetWorldDebugData(Zoom, Zoom, (-(Zoom/2), -(Zoom/2))));
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case OverworldDebugCloseMessage _:
                Closed();
                break;
            case OverworldDebugSettingsMessage s:
            {
                Zoom = s.Zoom;
                StateDirty();
                break;
            }
        }
    }

    public override void Closed()
    {
        base.Closed();

        EntitySystem.Get<WorldChunkSystem>().CloseEui(Player);
    }
}
