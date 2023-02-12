using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings;

// the better version of MarkingsSet
// This one should ensure that a set is valid. Dependency retrieval is
// probably not a good idea, and any dependency references should last
// only for the length of a call, and not the lifetime of the set itself.
//
// Compared to MarkingsSet, this should allow for server-side authority.
// Instead of sending the set over, we can instead just send the dictionary
// and build the set from there. We can also just send a list and rebuild
// the set without validating points (we're assuming that the server

/// <summary>
///     Marking set. For humanoid markings.
/// </summary>
/// <remarks>
///     This is serializable for the admin panel that sets markings on demand for a player.
///     Most APIs that accept a set of markings usually use a List of type Marking instead.
/// </remarks>
[DataDefinition]
[Serializable, NetSerializable]
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
    [DataField("markings")]
    public Dictionary<MarkingCategories, List<Marking>> Markings = new();

    /// <summary>
    ///     Marking points for each category.
    /// </summary>
    [DataField("points")]
    public Dictionary<MarkingCategories, MarkingPoints> Points = new();

    public MarkingSet()
    {}

    /// <summary>
    ///     Construct a MarkingSet using a list of markings, and a points
    ///     dictionary. This will set up the points dictionary, and
    ///     process the list, truncating if necessary. Markings that
    ///     do not exist as a prototype will be removed.
    /// </summary>
    /// <param name="markings">The lists of markings to use.</param>
    /// <param name="pointsPrototype">The ID of the points dictionary prototype.</param>
    public MarkingSet(List<Marking> markings, string pointsPrototype, MarkingManager? markingManager = null, IPrototypeManager? prototypeManager = null)
    {
        IoCManager.Resolve(ref markingManager, ref prototypeManager);

        if (!prototypeManager.TryIndex(pointsPrototype, out MarkingPointsPrototype? points))
        {
            return;
        }

        Points = MarkingPoints.CloneMarkingPointDictionary(points.Points);

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
    /// <param name="markings">The list of markings to use.</param>
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
    ///     Construct a MarkingSet by deep cloning another set.
    /// </summary>
    /// <param name="other">The other marking set.</param>
    public MarkingSet(MarkingSet other)
    {
        foreach (var (key, list) in other.Markings)
        {
            foreach (var marking in list)
            {
                AddBack(key, new(marking));
            }
        }

        Points = MarkingPoints.CloneMarkingPointDictionary(other.Points);
    }

    /// <summary>
    ///     Filters markings based on species restrictions in the marking's prototype from this marking set.
    /// </summary>
    /// <param name="species">The species to filter.</param>
    /// <param name="markingManager">Marking manager.</param>
    /// <param name="prototypeManager">Prototype manager.</param>
    public void FilterSpecies(string species, MarkingManager? markingManager = null, IPrototypeManager? prototypeManager = null)
    {
        IoCManager.Resolve(ref markingManager);
        IoCManager.Resolve(ref prototypeManager);

        var toRemove = new List<(MarkingCategories category, string id)>();
        var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
        var onlyWhitelisted = prototypeManager.Index<MarkingPointsPrototype>(speciesProto.MarkingPoints).OnlyWhitelisted;

        foreach (var (category, list) in Markings)
        {
            foreach (var marking in list)
            {
                if (!markingManager.TryGetMarking(marking, out var prototype))
                {
                    toRemove.Add((category, marking.MarkingId));
                    continue;
                }

                if (onlyWhitelisted && prototype.SpeciesRestrictions == null)
                {
                    toRemove.Add((category, marking.MarkingId));
                }

                if (prototype.SpeciesRestrictions != null
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

    /// <summary>
    ///     Ensures that all markings in this set are valid.
    /// </summary>
    /// <param name="markingManager">Marking manager.</param>
    public void EnsureValid(MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        var toRemove = new List<int>();
        foreach (var (category, list) in Markings)
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

    /// <summary>
    ///     Ensures that the default markings as defined by the marking point set in this marking set are applied.
    /// </summary>
    /// <param name="skinColor">Color to apply.</param>
    /// <param name="markingManager">Marking manager.</param>
    public void EnsureDefault(Color? skinColor = null, MarkingManager? markingManager = null)
    {
        IoCManager.Resolve(ref markingManager);

        foreach (var (category, points) in Points)
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

    /// <summary>
    ///     How many points are left in this marking set's category
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>A number equal or greater than zero if the category exists, -1 otherwise.</returns>
    public int PointsLeft(MarkingCategories category)
    {
        if (!Points.TryGetValue(category, out var points))
        {
            return -1;
        }

        return points.Points;
    }

    /// <summary>
    ///     Add a marking to the front of the category's list of markings.
    /// </summary>
    /// <param name="category">Category to add the marking to.</param>
    /// <param name="marking">The marking instance in question.</param>
    public void AddFront(MarkingCategories category, Marking marking)
    {
        if (!marking.Forced && Points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                return;
            }

            points.Points--;
        }

        if (!Markings.TryGetValue(category, out var markings))
        {
            markings = new();
            Markings[category] = markings;
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
        if (!marking.Forced && Points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                return;
            }

            points.Points--;
        }

        if (!Markings.TryGetValue(category, out var markings))
        {
            markings = new();
            Markings[category] = markings;
        }


        markings.Add(marking);
    }

    /// <summary>
    ///     Adds a category to this marking set.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public List<Marking> AddCategory(MarkingCategories category)
    {
        var markings = new List<Marking>();
        Markings.Add(category, markings);
        return markings;
    }

    /// <summary>
    ///     Replace a marking at a given index in a marking category with another marking.
    /// </summary>
    /// <param name="category">The category to replace the marking in.</param>
    /// <param name="index">The index of the marking.</param>
    /// <param name="marking">The marking to insert.</param>
    public void Replace(MarkingCategories category, int index, Marking marking)
    {
        if (index < 0 || !Markings.TryGetValue(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        markings[index] = marking;
    }

    /// <summary>
    ///     Remove a marking by category and ID.
    /// </summary>
    /// <param name="category">The category that contains the marking.</param>
    /// <param name="id">The marking's ID.</param>
    /// <returns>True if removed, false otherwise.</returns>
    public bool Remove(MarkingCategories category, string id)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        for (var i = 0; i < markings.Count; i++)
        {
            if (markings[i].MarkingId != id)
            {
                continue;
            }

            if (!markings[i].Forced && Points.TryGetValue(category, out var points))
            {
                points.Points++;
            }

            markings.RemoveAt(i);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Remove a marking by category and index.
    /// </summary>
    /// <param name="category">The category that contains the marking.</param>
    /// <param name="idx">The marking's index.</param>
    /// <returns>True if removed, false otherwise.</returns>
    public void Remove(MarkingCategories category, int idx)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count)
        {
            return;
        }

        if (!markings[idx].Forced && Points.TryGetValue(category, out var points))
        {
            points.Points++;
        }

        markings.RemoveAt(idx);
    }

    /// <summary>
    ///     Remove an entire category from this marking set.
    /// </summary>
    /// <param name="category">The category to remove.</param>
    /// <returns>True if removed, false otherwise.</returns>
    public bool RemoveCategory(MarkingCategories category)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return false;
        }

        if (Points.TryGetValue(category, out var points))
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

        Markings.Remove(category);
        return true;
    }

    /// <summary>
    ///     Clears all markings from this marking set.
    /// </summary>
    public void Clear()
    {
        foreach (var category in Enum.GetValues<MarkingCategories>())
        {
            RemoveCategory(category);
        }
    }

    /// <summary>
    ///     Attempt to find the index of a marking in a category by ID.
    /// </summary>
    /// <param name="category">The category to search in.</param>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The index of the marking, otherwise a negative number.</returns>
    public int FindIndexOf(MarkingCategories category, string id)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return -1;
        }

        return markings.FindIndex(m => m.MarkingId == id);
    }

    /// <summary>
    ///     Tries to get an entire category from this marking set.
    /// </summary>
    /// <param name="category">The category to fetch.</param>
    /// <param name="markings">A read only list of the all markings in that category.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool TryGetCategory(MarkingCategories category, [NotNullWhen(true)] out IReadOnlyList<Marking>? markings)
    {
        markings = null;

        if (Markings.TryGetValue(category, out var list))
        {
            markings = list;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to get a marking from this marking set, by category.
    /// </summary>
    /// <param name="category">The category to search in.</param>
    /// <param name="id">The ID to search for.</param>
    /// <param name="marking">The marking, if it was retrieved.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool TryGetMarking(MarkingCategories category, string id, [NotNullWhen(true)] out Marking? marking)
    {
        marking = null;

        if (!Markings.TryGetValue(category, out var markings))
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

    /// <summary>
    ///     Shifts a marking's rank towards the front of the list
    /// </summary>
    /// <param name="category">The category to shift in.</param>
    /// <param name="idx">Index of the marking.</param>
    public void ShiftRankUp(MarkingCategories category, int idx)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count || idx - 1 < 0)
        {
            return;
        }

        (markings[idx - 1], markings[idx]) = (markings[idx], markings[idx - 1]);
    }

    /// <summary>
    ///     Shifts a marking's rank upwards from the end of the list
    /// </summary>
    /// <param name="category">The category to shift in.</param>
    /// <param name="idx">Index of the marking from the end</param>
    public void ShiftRankUpFromEnd(MarkingCategories category, int idx)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return;
        }

        ShiftRankUp(category, markings.Count - idx - 1);
    }

    /// <summary>
    ///     Shifts a marking's rank towards the end of the list
    /// </summary>
    /// <param name="category">The category to shift in.</param>
    /// <param name="idx">Index of the marking.</param>
    public void ShiftRankDown(MarkingCategories category, int idx)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return;
        }

        if (idx < 0 || idx >= markings.Count || idx + 1 >= markings.Count)
        {
            return;
        }

        (markings[idx + 1], markings[idx]) = (markings[idx], markings[idx + 1]);
    }

    /// <summary>
    ///     Shifts a marking's rank downwards from the end of the list
    /// </summary>
    /// <param name="category">The category to shift in.</param>
    /// <param name="idx">Index of the marking from the end</param>
    public void ShiftRankDownFromEnd(MarkingCategories category, int idx)
    {
        if (!Markings.TryGetValue(category, out var markings))
        {
            return;
        }

        ShiftRankDown(category, markings.Count - idx - 1);
    }

    /// <summary>
    ///     Gets all markings in this set as an enumerator. Lists will be organized, but categories may be in any order.
    /// </summary>
    /// <returns>An enumerator of <see cref="Marking"/>s.</returns>
    public ForwardMarkingEnumerator GetForwardEnumerator()
    {
        var markings = new List<Marking>();
        foreach (var (_, list) in Markings)
        {
            markings.AddRange(list);
        }

        return new ForwardMarkingEnumerator(markings);
    }

    /// <summary>
    ///     Gets an enumerator of markings in this set, but only for one category.
    /// </summary>
    /// <param name="category">The category to fetch.</param>
    /// <returns>An enumerator of <see cref="Marking"/>s in that category.</returns>
    public ForwardMarkingEnumerator GetForwardEnumerator(MarkingCategories category)
    {
        var markings = new List<Marking>();
        if (Markings.TryGetValue(category, out var listing))
        {
            markings = new(listing);
        }

        return new ForwardMarkingEnumerator(markings);
    }

    /// <summary>
    ///     Gets all markings in this set as an enumerator, but in reverse order. Lists will be in reverse order, but categories may be in any order.
    /// </summary>
    /// <returns>An enumerator of <see cref="Marking"/>s in reverse.</returns>
    public ReverseMarkingEnumerator GetReverseEnumerator()
    {
        var markings = new List<Marking>();
        foreach (var (_, list) in Markings)
        {
            markings.AddRange(list);
        }

        return new ReverseMarkingEnumerator(markings);
    }

    /// <summary>
    ///     Gets an enumerator of markings in this set in reverse order, but only for one category.
    /// </summary>
    /// <param name="category">The category to fetch.</param>
    /// <returns>An enumerator of <see cref="Marking"/>s in that category, in reverse order.</returns>
    public ReverseMarkingEnumerator GetReverseEnumerator(MarkingCategories category)
    {
        var markings = new List<Marking>();
        if (Markings.TryGetValue(category, out var listing))
        {
            markings = new(listing);
        }

        return new ReverseMarkingEnumerator(markings);
    }

    public bool CategoryEquals(MarkingCategories category, MarkingSet other)
    {
        if (!Markings.TryGetValue(category, out var markings)
            || !other.Markings.TryGetValue(category, out var markingsOther))
        {
            return false;
        }

        return markings.SequenceEqual(markingsOther);
    }

    public bool Equals(MarkingSet other)
    {
        foreach (var (category, _) in Markings)
        {
            if (!CategoryEquals(category, other))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Gets a difference of marking categories between two marking sets
    /// </summary>
    /// <param name="other">The other marking set.</param>
    /// <returns>Enumerator of marking categories that were different between the two.</returns>
    public IEnumerable<MarkingCategories> CategoryDifference(MarkingSet other)
    {
        foreach (var (category, _) in Markings)
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
