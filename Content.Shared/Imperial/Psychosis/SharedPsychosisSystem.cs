using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Traits.Assorted
{
    public abstract class SharedPsychosisSystem : EntitySystem
    {
    }
    [Serializable, NetSerializable]
    public sealed class StageChange : EntityEventArgs
    {
        public int Stage = 1;

        public NetEntity Psychosis = default!;
        public StageChange(int stage, NetEntity component)
        {
            Stage = stage;
            Psychosis = component;
        }
    }
    [Serializable, NetSerializable]
    public sealed class PopUpTransfer : EntityEventArgs
    {
        public string Popup = string.Empty;

        public NetEntity Psychosis = default!;
        public PopUpTransfer(string popup, NetEntity component)
        {
            Popup = popup;
            Psychosis = component;
        }
    }
    [Serializable, NetSerializable]
    public sealed class GetPopup : EntityEventArgs
    {

        public NetEntity Psychosis = default!;
        public GetPopup(NetEntity component)
        {
            Psychosis = component;
        }
    }
}

