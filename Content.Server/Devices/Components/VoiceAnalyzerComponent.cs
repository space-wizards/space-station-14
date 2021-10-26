using Content.Server.Radio.Components;
using Content.Shared.Devices;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;

namespace Content.Server.Devices.Components
{
    public class VoiceAnalyzerComponent : SharedVoiceAnalyzerComponent, IListen
    {
        public int ListenRange { get; }
        public bool CanListen(string message, IEntity source)
        {
            return Owner.InRangeUnobstructed(source.Transform.Coordinates, range: ListenRange);
        }

        public void Listen(string message, IEntity speaker)
        {
            throw new System.NotImplementedException();
        }
    }
}
