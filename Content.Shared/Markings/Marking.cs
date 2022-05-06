using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Markings
{
    [Serializable, NetSerializable]
    public sealed class Marking : IEquatable<Marking>, IComparable<Marking>, IComparable<string>
    {
        private List<Color> _markingColors = new();

        private Marking(string markingId,
            List<Color> markingColors)
        {
            MarkingId = markingId;
            _markingColors = markingColors;
        }

        public Marking(string markingId,
            IReadOnlyList<Color> markingColors)
            : this(markingId, new List<Color>(markingColors))
        {
        }

        /*
        public Marking(string markingId)
            : this(markingId, new List<Color>())
        {
        }
        */

        public Marking(string markingId, int colorCount)
        {
            MarkingId = markingId;
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
                colors.Add(Color.White);
            _markingColors = colors;
        }

        [DataField("markingId")]
        [ViewVariables]
        public string MarkingId { get; } = default!;

        [DataField("markingColor")]
        [ViewVariables]
        public IReadOnlyList<Color> MarkingColors => _markingColors;

        public void SetColor(int colorIndex, Color color) =>
            _markingColors[colorIndex] = color;

        public int CompareTo(Marking? marking)
        {
            if (marking == null) return 1;
            else return this.MarkingId.CompareTo(marking.MarkingId);
        }

        public int CompareTo(string? markingId)
        {
            if (markingId == null) return 1;
            return this.MarkingId.CompareTo(markingId);
        }

        public bool Equals(Marking? other)
        {
            if (other == null) return false;
            return (this.MarkingId.Equals(other.MarkingId));
        }


        // look this could be better but I don't think serializing
        // colors is the correct thing to do
        //
        // this is still janky imo but serializing a color and feeding
        // it into the default JSON serializer (which is just *fine*)
        // doesn't seem to have compatible interfaces? this 'works'
        // for now but should eventually be improved so that this can,
        // in fact just be serialized through a convenient interface
        new public string ToString()
        {
            // reserved character
            string sanitizedName = this.MarkingId.Replace('@', '_');
            List<string> colorStringList = new();
            foreach (Color color in _markingColors)
                colorStringList.Add(color.ToHex());

            return $"{sanitizedName}@{String.Join(',', colorStringList)}";
        }

        public static Marking? ParseFromDbString(string input)
        {
            if (input.Length == 0) return null;
            var split = input.Split('@');
            if (split.Length != 2) return null;
            List<Color> colorList = new();
            foreach (string color in split[1].Split(','))
                colorList.Add(Color.FromHex(color));

            return new Marking(split[0], colorList);
        }
    }
}
