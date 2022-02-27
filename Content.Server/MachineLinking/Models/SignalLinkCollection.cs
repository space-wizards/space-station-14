using System.Collections.Generic;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Exceptions;

namespace Content.Server.MachineLinking.Models
{
    public sealed class SignalLinkCollection
    {
        private Dictionary<SignalTransmitterComponent, List<SignalLink>> _transmitterDict = new();
        private Dictionary<SignalReceiverComponent, List<SignalLink>> _receiverDict = new();

        public SignalLink AddLink(SignalTransmitterComponent transmitterComponent, string transmitterPort,
            SignalReceiverComponent receiverComponent, string receiverPort)
        {
            if (LinkExists(transmitterComponent, transmitterPort, receiverComponent, receiverPort))
            {
                throw new LinkAlreadyRegisteredException();
            }

            if (!_transmitterDict.ContainsKey(transmitterComponent))
            {
                _transmitterDict[transmitterComponent] = new();
            }

            if (!_receiverDict.ContainsKey(receiverComponent))
            {
                _receiverDict[receiverComponent] = new();
            }

            var link = new SignalLink(transmitterComponent, transmitterPort, receiverComponent, receiverPort);
            _transmitterDict[transmitterComponent].Add(link);
            _receiverDict[receiverComponent].Add(link);

            return link;
        }

        public bool LinkExists(SignalTransmitterComponent transmitterComponent, string transmitterPort,
            SignalReceiverComponent receiverComponent, string receiverPort)
        {
            if (!_transmitterDict.ContainsKey(transmitterComponent) || !_receiverDict.ContainsKey(receiverComponent))
            {
                return false;
            }

            foreach (var link in _transmitterDict[transmitterComponent])
            {
                if (link.Transmitterport.Name == transmitterPort && link.Receiverport.Name == receiverPort &&
                    link.ReceiverComponent == receiverComponent)
                    return true;
            }

            return false;
        }

        public bool RemoveLink(SignalTransmitterComponent transmitterComponent, string transmitterPort,
            SignalReceiverComponent receiverComponent, string receiverPort)
        {
            if (!_transmitterDict.ContainsKey(transmitterComponent) || !_receiverDict.ContainsKey(receiverComponent))
            {
                return false;
            }

            SignalLink? theLink = null;
            foreach (var link in _transmitterDict[transmitterComponent])
            {
                if (link.Transmitterport.Name == transmitterPort && link.Receiverport.Name == receiverPort &&
                    link.ReceiverComponent == receiverComponent)
                {
                    theLink = link;
                    break;
                }
            }

            if (theLink == null) return false;

            _transmitterDict[transmitterComponent].Remove(theLink);
            if (_transmitterDict[transmitterComponent].Count == 0) _transmitterDict.Remove(transmitterComponent);
            _receiverDict[receiverComponent].Remove(theLink);
            if (_receiverDict[receiverComponent].Count == 0) _receiverDict.Remove(receiverComponent);
            return true;
        }

        public int LinkCount(SignalTransmitterComponent comp) =>
            _transmitterDict.ContainsKey(comp) ? _transmitterDict[comp].Count : 0;

        public int LinkCount(SignalReceiverComponent comp) =>
            _receiverDict.ContainsKey(comp) ? _receiverDict[comp].Count : 0;

        public void RemoveLinks(SignalTransmitterComponent component)
        {
            if (!_transmitterDict.ContainsKey(component))
            {
                return;
            }

            foreach (var link in _transmitterDict[component])
            {
                _receiverDict[link.ReceiverComponent].Remove(link);
                if (_receiverDict[link.ReceiverComponent].Count == 0) _receiverDict.Remove(link.ReceiverComponent);
            }

            _transmitterDict.Remove(component);
        }

        public void RemoveLinks(SignalReceiverComponent component)
        {
            if (!_receiverDict.ContainsKey(component))
            {
                return;
            }

            foreach (var link in _receiverDict[component])
            {
                _transmitterDict[link.TransmitterComponent].Remove(link);
                if (_transmitterDict[link.TransmitterComponent].Count == 0)
                    _transmitterDict.Remove(link.TransmitterComponent);
            }
        }

        public IEnumerable<SignalLink> GetLinks(SignalTransmitterComponent component, string port)
        {
            if (!_transmitterDict.ContainsKey(component)) yield break;

            foreach (var link in _transmitterDict[component])
            {
                if (link.Transmitterport.Name != port) continue;
                yield return link;
            }
        }
    }
}
