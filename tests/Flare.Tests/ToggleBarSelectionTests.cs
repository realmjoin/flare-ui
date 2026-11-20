using Flare.Internal;
using Xunit;

namespace Flare.Tests;

public class ToggleBarSelectionTests
{
    private static readonly string[] Items = ["mon", "tue", "wed", "thu", "fri"];

    private static List<string> Toggle(string[] current, string item, bool selected) =>
        ToggleBarSelection.Toggle(Items, current, item, selected, StringComparer.Ordinal);

    [Fact]
    public void Selecting_adds_the_item() =>
        Assert.Equal(["mon"], Toggle([], "mon", selected: true));

    [Fact]
    public void Deselecting_removes_the_item() =>
        Assert.Equal(["mon", "fri"], Toggle(["mon", "wed", "fri"], "wed", selected: false));

    [Fact]
    public void Result_follows_item_order_not_click_order() =>
        Assert.Equal(["mon", "wed", "fri"], Toggle(["fri", "wed"], "mon", selected: true));

    [Fact]
    public void Selecting_an_already_selected_item_does_not_duplicate_it() =>
        Assert.Equal(["tue"], Toggle(["tue"], "tue", selected: true));

    [Fact]
    public void Deselecting_an_unselected_item_changes_nothing() =>
        Assert.Equal(["tue"], Toggle(["tue"], "thu", selected: false));

    [Fact]
    public void All_items_can_be_selected() =>
        Assert.Equal(Items, Toggle(["mon", "tue", "wed", "thu"], "fri", selected: true));

    [Fact]
    public void Values_outside_the_item_list_are_kept_at_the_end() =>
        Assert.Equal(["tue", "sat"], Toggle(["sat"], "tue", selected: true));

    [Fact]
    public void Comparer_decides_what_counts_as_the_same_item()
    {
        var result = ToggleBarSelection.Toggle(
            Items, ["MON", "wed"], "mon", selected: false, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(["wed"], result);
    }

    [Fact]
    public void Input_list_is_not_modified()
    {
        string[] current = ["mon"];
        Toggle(current, "tue", selected: true);
        Assert.Equal(["mon"], current);
    }
}
