using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalendarApp.Models;

/// <summary>The fixed set of colours an event can be tagged with.</summary>
public static class EventPalette
{
    public const string DefaultKey = "Blue";

    private static readonly Dictionary<string, string> Swatches = new()
    {
        ["Blue"] = "#2F6FED",
        ["Teal"] = "#0E9E96",
        ["Green"] = "#2E9E52",
        ["Amber"] = "#D98324",
        ["Red"] = "#D9455F",
        ["Purple"] = "#8558D6",
        ["Slate"] = "#64748B",
    };

    public static IReadOnlyList<string> Keys { get; } = new ReadOnlyCollection<string>(Swatches.Keys.ToList());

    public static string HexFor(string? key) =>
        key is not null && Swatches.TryGetValue(key, out string? hex) ? hex : Swatches[DefaultKey];
}
