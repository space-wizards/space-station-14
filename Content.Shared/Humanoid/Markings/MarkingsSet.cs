using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Markings;

// the better version of MarkingsSet
// This one should ensure that a set is valid. Dependency retrieval is
// probably not a good idea, and any dependency references should last
// only for the length of a call, and not the lifetime of the set itself.
//
// Compared to MarkingsSet, this should allow for server-side authority.
// Instead of sending the set over, we can instead just send the dictionary
// and build the set from there. We can also just send a list and rebuild
// the set without validating points (we're assuming that the server

public sealed class MarkingSet
{
    /// <summary>
    ///     Every single marking in this set.
    /// </summary>
    /// <remarks>
    ///     The original version of MarkingSet preserved ordering across all
    ///     markings - this one should instead preserve ordering across all
    ///     categories, but not marking categories themselves. This is because
    ///     the layers that markings appear in are guaranteed to be in the correct
    ///     order. This is here to make lookups slightly faster, even if the n of
    ///     a marking set is relatively small, and to encapsulate another important
    ///     feature of markings, which is the limit of markings you can put on a
    ///     humanoid.
    /// </remarks>
    private Dictionary<MarkingCategories, List<Marking>> _markings = new();

    // why i didn't encapsulate this in the first place, i won't know

    /// <summary>
    ///     Marking points for each category.
    /// </summary>
    private Dictionary<MarkingCategories, MarkingPoints> _points = new();

    public IReadOnlyList<Marking> this[MarkingCategories category] => _markings[category];

    public MarkingSet()
    {}

    /// <summary>
    ///     Construct a MarkingSet using a list of markings, and a points
    ///     dictionary. This will set up the points dictionary, and
    ///     process the list, truncating if necessary. Markings that
    ///     do not exist as a prototype will be removed.
    /// </summary>
    /// <param name="markings"></param>
    /// <param name="pointsPrototype">The ID of the points dictionary prototype.</param>
    public MarkingSet(List<Marking> markings, string pointsPrototype, MarkingManager? markingManager = null, IPrototypeManager? prototypeManager = null)
    {
        IoCManager.Resolve(ref markingManager, ref prototypeManager);

        if (!prototypeManager.TryIndex(pointsPrototype, out MarkingPointsPrototype? points))
        {
            return;
        }

        _points = MarkingPoints.CloneMarkingPointDictionary(points.Points);

        foreach (var marking in markings)
        {
            if (!markingManager.TryGetMarking(marking, out var prototype))
            {
                continue;
            }

            AddBack(prototype.MarkingCategory, marking);
        }
    }

    /// <summary>
    ///     Construct a MarkingSet using a dictionary of markings,
    ///     without point validation. This will still validate every
    ///     marking, to ensure that it can be placed into the set.
    /// </summary>
    /// <param name="markings"></param>
    public MarkingSet(List<Marking> markings, MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        foreach (var marking in markings)
        {
            if (!markingManager.TryGetMarking(marking, out var prototype))
            {
                continue;
            }

            AddBack(prototype.MarkingCategory, marking);
        }
    }

    /// <summary>
    ///     Construct a MarkingSet by cloning another set.
    /// </summary>
    /// <param name="other"></param>
    public MarkingSet(MarkingSet other)
    {
        foreach (var (key, list) in other._markings)
        {
            foreach (var marking in list)
            {
                AddBack(key, new(marking));
            }
        }

        _points = MarkingPoints.CloneMarkingPointDictionary(other._points);
    }

    public void FilterSpecies(string species, MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        var toRemove = new List<(MarkingCategories category, string id)>();

        foreach (var (category, list) in _markings)
        {
            foreach (var marking in list)
            {
                if (!markingManager.TryGetMarking(marking, out var prototype)
                    || prototype.SpeciesRestrictions != null
                    && !prototype.SpeciesRestrictions.Contains(species))
                {
                    toRemove.Add((category, marking.MarkingId));
                }
            }
        }

        foreach (var remove in toRemove)
        {
            Remove(remove.category, remove.id);
        }
    }

    public void EnsureValid(MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        var toRemove = new List<int>();
        foreach (var (category, list) in _markings)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (!markingManager.TryGetMarking(list[i], out var marking))
                {
                    toRemove.Add(i);
                    continue;
                }

                if (marking.Sprites.Count != list[i].MarkingColors.Count)
                {
                    list[i] = new Marking(marking.ID, marking.Sprites.Count);
                }
            }

            foreach (var i in toRemove)
            {
                Remove(category, i);
            }
        }
    }

    public void EnsureDefault(Color? skinColor = null, MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        foreach (var (category, points) in _points)
        {
            if (points.Points <= 0 || points.DefaultMarkings.Count <= 0)
            {
                continue;
            }

            var index = 0;
            while (points.Points > 0 || index < points.DefaultMarkings.Count)
            {
                if (markingManager.Markings.TryGetValue(points.DefaultMarkings[index], out var prototype))
                {
                    Marking marking;
                    if (skinColor == null)
                    {
                        marking = new Marking(points.DefaultMarkings[index], prototype.Sprites.Count);
                    }
                    else
                    {
                        var colors = new List<Color>();

                        for (var i = 0; i < prototype.Sprites.Count; i++)
                        {
                            colors.Add(skinColor.Value);
                        }

                        marking = new Marking(points.DefaultMarkings[index], colors);
                    }

                    AddBack(category, marking);
                }

                index++;
            }
        }
    }

    public int PointsLeft(MarkingCategories category)
    {
        if (!_points.TryGetValue(category, out var points))
        {
            return -1;
        }

        return points.Points;
    }

    /// <summary>
    ///     Add a marking to the front of the category's list of markings.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="marking"></param>
    public void AddFront(MarkingCategories category, Marking marking)
    {
        if (_points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                return;
            }

            points.Points--;
        }

        if (!_markings.TryGetValue(category, out var markings))
        {
            markings = new();
            _markings[category] = markings;
        }

        markings.Insert(0, marking);
    }

    /// <summary>
    ///     Add a marking to the back of the category's list of markings.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="marking"></param>
    public void AddBack(MarkingCategories category, Marking marking)
    {
        if (_points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                return;
            }

            points.Points--;
        }

        if (!_markings.TryGetValue(category, out var markings))
        {
            markings = new();
            _markings[category] = markings;
        }


        markings.Add(marking);
    }

    public List<Marking> AddCategory(MarkingCategories category)
    {
        var markings = new List<Marking>();
        _markings.Add(category, markings);
        return markings;
    }

    public bool Remove(MarkingCategories category, string id)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        for (var i = 0; i < markings.Count; i++)
        {
            if (markings[i].MarkingId != id)
            {
                continue;
            }

            if (!markings[i].Forced && _points.TryGetValue(category, out var points))
            {
                points.Points++;
            }

            markings.RemoveAt(i);
            return true;
        }

        return false;
    }

    public void Remove(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count)
        {
            return;
        }

        if (!markings[idx].Forced && _points.TryGetValue(category, out var points))
        {
            points.Points++;
        }

        markings.RemoveAt(idx);
    }

    public bool RemoveCategory(MarkingCategories category)
    {
        // TODO: This should re-add points.
        if (!_markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        if (_points.TryGetValue(category, out var points))
        {
            foreach (var marking in markings)
            {
                if (marking.Forced)
                {
                    continue;
                }

                points.Points++;
            }
        }

        _markings.Remove(category);
        return true;
    }

    public void Clear()
    {
        foreach (var category in Enum.GetValues<MarkingCategories>())
        {
            RemoveCategory(category);
        }
    }

    public int FindIndexOf(MarkingCategories category, string id)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return -1;
        }

        return markings.FindIndex(m => m.MarkingId == id);
    }

    public bool TryGetCategory(MarkingCategories category, [NotNullWhen(true)] out IReadOnlyList<Marking>? markings)
    {
        markings = null;

        if (_markings.TryGetValue(category, out var list))
        {
            markings = list;
            return true;
        }

        return false;
    }

    public bool TryGetMarking(MarkingCategories category, string id, [NotNullWhen(true)] out Marking? marking)
    {
        marking = null;

        if (!_markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        foreach (var m in markings)
        {
            if (m.MarkingId == id)
            {
                marking = m;
                return true;
            }
        }

        return false;
    }

    public void ShiftRankUp(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count || idx - 1 < 0)
        {
            return;
        }

        (markings[idx - 1], markings[idx]) = (markings[idx], markings[idx - 1]);
    }

    // Shifts up from the back (i.e., 2nd position from end)
    public void ShiftRankUpFromEnd(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        ShiftRankUp(category, markings.Count - idx - 1);
    }

    // Ditto, but the opposite direction.
    public void ShiftRankDown(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count || idx + 1 >= markings.Count)
        {
            return;
        }

        (markings[idx + 1], markings[idx]) = (markings[idx], markings[idx + 1]);
    }

    // Ditto as above.
    public void ShiftRankDownFromEnd(MarkingCategories category, int idx)
    {
        if (!_markings.TryGetValue(category, out var markings))
        {
            return;
        }

        ShiftRankDown(category, markings.Count - idx - 1);
    }

    public ForwardMarkingEnumerator GetForwardEnumerator()
    {
        var markings = new List<Marking>();
        foreach (var (_, list) in _markings)
        {
            markings.AddRange(list);
        }

        return new ForwardMarkingEnumerator(markings);
    }

    public ForwardMarkingEnumerator GetForwardEnumerator(MarkingCategories category)
    {
        var markings = new List<Marking>();
        if (_markings.TryGetValue(category, out var listing))
        {
            markings = new(listing);
        }

        return new ForwardMarkingEnumerator(markings);
    }

    public ReverseMarkingEnumerator GetReverseEnumerator()
    {
        var markings = new List<Marking>();
        foreach (var (_, list) in _markings)
        {
            markings.AddRange(list);
        }

        return new ReverseMarkingEnumerator(markings);
    }

    public ReverseMarkingEnumerator GetReverseEnumerator(MarkingCategories category)
    {
        var markings = new List<Marking>();
        if (_markings.TryGetValue(category, out var listing))
        {
            markings = new(listing);
        }

        return new ReverseMarkingEnumerator(markings);
    }

    public bool CategoryEquals(MarkingCategories category, MarkingSet other)
    {
        if (!_markings.TryGetValue(category, out var markings)
            || !other._markings.TryGetValue(category, out var markingsOther))
        {
            return false;
        }

        return markings.SequenceEqual(markingsOther);
    }

    public bool Equals(MarkingSet other)
    {
        foreach (var (category, _) in _markings)
        {
            if (!CategoryEquals(category, other))
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerable<MarkingCategories> CategoryDifference(MarkingSet other)
    {
        foreach (var (category, _) in _markings)
        {
            if (!CategoryEquals(category, other))
            {
                yield return category;
            }
        }
    }
}

public sealed class ForwardMarkingEnumerator : IEnumerable<Marking>
{
    private List<Marking> _markings;

    public ForwardMarkingEnumerator(List<Marking> markings)
    {
        _markings = markings;
    }

    public IEnumerator<Marking> GetEnumerator()
    {
        return new MarkingsEnumerator(_markings, false);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class ReverseMarkingEnumerator : IEnumerable<Marking>
{
    private List<Marking> _markings;

    public ReverseMarkingEnumerator(List<Marking> markings)
    {
        _markings = markings;
    }

    public IEnumerator<Marking> GetEnumerator()
    {
        return new MarkingsEnumerator(_markings, true);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class MarkingsEnumerator : IEnumerator<Marking>
{
    private List<Marking> _markings;
    private bool _reverse;

    int position;

    public MarkingsEnumerator(List<Marking> markings, bool reverse)
    {
        _markings = markings;
        _reverse = reverse;

        if (_reverse)
        {
            position = _markings.Count;
        }
        else
        {
            position = -1;
        }
    }

    public bool MoveNext()
    {
        if (_reverse)
        {
            position--;
            return (position >= 0);
        }
        else
        {
            position++;
            return (position < _markings.Count);
        }
    }

    public void Reset()
    {
        if (_reverse)
        {
            position = _markings.Count;
        }
        else
        {
            position = -1;
        }
    }

    public void Dispose()
    {}

    object IEnumerator.Current
    {
        get => _markings[position];
    }

    public Marking Current
    {
        get => _markings[position];
    }
}
