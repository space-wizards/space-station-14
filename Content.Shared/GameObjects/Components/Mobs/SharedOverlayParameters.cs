using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;



namespace Content.Shared.GameObjects.Components.Mobs
{

    [Serializable, NetSerializable]
    public abstract class OverlayParameter
    {
    }


    [Serializable, NetSerializable]
    public class TimedOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public int Length { get; set; }

        public double StartedAt { get; private set; }

        public TimedOverlayParameter(int length)
        {
            Length = length;
            StartedAt = IoCManager.Resolve<IGameTiming>().CurTime.TotalMilliseconds;
        }
    }

    [Serializable, NetSerializable]
    public class TextureOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]

        public string[] RSIPaths { get; set; }
        public string[] States { get; set; }

        public TextureOverlayParameter(string rsiPath, string state)
        {
            RSIPaths = new string[] { rsiPath };
            States = new string[] { state };
        }

        public TextureOverlayParameter(string[] rsiPaths, string[] states)
        {
            RSIPaths = rsiPaths;
            States = states;
        }
    }


    //Note: unfortunately these keyed parameters cannot use generics for these since I think NetSerializable classes are not allowed to use generics (it threw some kind of error)

    [Serializable, NetSerializable]
    public class KeyedFloatOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private string[] _keys;

        [ViewVariables(VVAccess.ReadOnly)]
        private float[] _values;

        public Dictionary<string, float> Dict
        {
            get
            {
                return _keys.Zip(_values, (first, second) => new { first, second }).ToDictionary(val => val.first, val => val.second);
            }
        }

        public KeyedFloatOverlayParameter(Dictionary<string, float> dict)
        {
            SetValues(dict);
        }

        public void SetValues(Dictionary<string, float> dict)
        {
            _keys = dict.Keys.ToArray();
            _values = dict.Values.ToArray();
        }
    }

    [Serializable, NetSerializable]
    public class KeyedBoolOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private string[] _keys;

        [ViewVariables(VVAccess.ReadOnly)]
        private bool[] _values;

        public Dictionary<string, bool> Dict
        {
            get
            {
                return _keys.Zip(_values, (first, second) => new { first, second }).ToDictionary(val => val.first, val => val.second);
            }
        }

        public KeyedBoolOverlayParameter(Dictionary<string, bool> dict)
        {
            SetValues(dict);
        }

        public void SetValues(Dictionary<string, bool> dict)
        {
            _keys = dict.Keys.ToArray();
            _values = dict.Values.ToArray();
        }
    }

    [Serializable, NetSerializable]
    public class KeyedStringOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private string[] _keys;

        [ViewVariables(VVAccess.ReadOnly)]
        private string[] _values;

        public Dictionary<string, string> Dict
        {
            get
            {
                return _keys.Zip(_values, (first, second) => new { first, second }).ToDictionary(val => val.first, val => val.second);
            }
        }

        public KeyedStringOverlayParameter(Dictionary<string, string> dict)
        {
            SetValues(dict);
        }

        public void SetValues(Dictionary<string, string> dict)
        {
            _keys = dict.Keys.ToArray();
            _values = dict.Values.ToArray();
        }
    }

    [Serializable, NetSerializable]
    public class KeyedVector2OverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private string[] _keys;

        [ViewVariables(VVAccess.ReadOnly)]
        private Vector2[] _values;

        public Dictionary<string, Vector2> Dict
        {
            get
            {
                return _keys.Zip(_values, (first, second) => new { first, second }).ToDictionary(val => val.first, val => val.second);
            }
        }

        public KeyedVector2OverlayParameter(Dictionary<string, Vector2> dict)
        {
            SetValues(dict);
        }

        public void SetValues(Dictionary<string, Vector2> dict)
        {
            _keys = dict.Keys.ToArray();
            _values = dict.Values.ToArray();
        }
    }

    [Serializable, NetSerializable]
    public class KeyedOverlaySpaceOverlayParameter : OverlayParameter
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private string[] _keys;

        [ViewVariables(VVAccess.ReadOnly)]
        private OverlaySpace[] _values;

        public Dictionary<string, OverlaySpace> Dict
        {
            get
            {
                return _keys.Zip(_values, (first, second) => new { first, second }).ToDictionary(val => val.first, val => val.second);
            }
        }

        public KeyedOverlaySpaceOverlayParameter(Dictionary<string, OverlaySpace> dict)
        {
            SetValues(dict);
        }

        public void SetValues(Dictionary<string, OverlaySpace> dict)
        {
            _keys = dict.Keys.ToArray();
            _values = dict.Values.ToArray();
        }
    }
}
