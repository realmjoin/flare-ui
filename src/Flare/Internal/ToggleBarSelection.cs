namespace Flare.Internal;

/// <summary>Selection logic for <see cref="FlareToggleBar{TItem}"/> in multi-select mode.</summary>
internal static class ToggleBarSelection
{
    /// <summary>
    /// Returns the new selection after switching <paramref name="item"/> on or off.
    /// The result follows the order of <paramref name="items"/>, so it does not depend on
    /// the order the user clicked in. Selected values that are not part of
    /// <paramref name="items"/> are kept at the end.
    /// </summary>
    public static List<TItem> Toggle<TItem>(
        IEnumerable<TItem> items,
        IReadOnlyList<TItem> current,
        TItem item,
        bool selected,
        IEqualityComparer<TItem> comparer)
    {
        var next = current.Where(v => !comparer.Equals(v, item)).ToList();
        if (selected)
            next.Add(item);

        var result = new List<TItem>(next.Count);
        foreach (var candidate in items)
        {
            var idx = next.FindIndex(v => comparer.Equals(v, candidate));
            if (idx < 0) continue;
            result.Add(next[idx]);
            next.RemoveAll(v => comparer.Equals(v, candidate));
        }

        foreach (var orphan in next)
        {
            if (!result.Any(v => comparer.Equals(v, orphan)))
                result.Add(orphan);
        }

        return result;
    }
}
