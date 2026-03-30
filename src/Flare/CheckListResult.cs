namespace Flare;

/// <summary>
/// Result returned by <see cref="FlareCheckList{TItem}.ProviderFunc"/> containing
/// a page of items and the total count for virtual scroll calculation.
/// </summary>
/// <typeparam name="TItem">The type of items.</typeparam>
public readonly record struct CheckListResult<TItem>(IEnumerable<TItem> Items, int TotalCount);
