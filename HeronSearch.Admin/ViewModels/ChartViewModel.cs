using System.Text.Json;
using System.Text.Json.Serialization;
using HeronSearch.Admin.Helpers;

namespace HeronSearch.Admin.ViewModels;

// The chart kinds of the chart data contract. ToJson writes them as exactly "line", "bar", and "horizontalBar";
// charts.js draws horizontalBar as a Chart.js bar chart with indexAxis 'y'.
[JsonConverter(typeof(JsonStringEnumConverter<ChartKind>))]
public enum ChartKind
{
    [JsonStringEnumMemberName("line")]
    Line,

    [JsonStringEnumMemberName("bar")]
    Bar,

    [JsonStringEnumMemberName("horizontalBar")]
    HorizontalBar
}

// One named list of chart values, such as the Lost counts per month. Values[i] belongs to the chart's Labels[i].
public class ChartSeries
{
    // The series name, shown in the legend and in the text alternative.
    public string Name { get; set; } = "";

    // One whole-number value per chart label, in label order.
    public List<int> Values { get; set; } = new();
}

// One chart for the _Chart partial: a title, a kind, the category labels, and one or more series.
// ToJson() writes the data-chart JSON that wwwroot/js/charts.js reads.
public class ChartViewModel
{
    // ToJson writes camelCase property names: "kind", "labels", "series", "name", and "values".
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // The chart title, shown in the card header and at the start of the text alternative.
    public string Title { get; set; } = "";

    // How charts.js draws the chart: Line, Bar, or HorizontalBar.
    public ChartKind Kind { get; set; }

    // The category labels (months, locations, categories, or hours) in display order.
    public List<string> Labels { get; set; } = new();

    // The data series in order; charts.js colors them Maroon, Gold, Deep Green, then Sand.
    public List<ChartSeries> Series { get; set; } = new();

    // True when the chart has nothing to show: no labels, or every value of every series is 0.
    // The _Chart partial then shows the empty state instead of the canvas.
    public bool IsEmpty => Labels.Count == 0 || Series.All(series => series.Values.All(value => value == 0));

    // The text alternative for the canvas aria-label, built by ChartData.Describe,
    // for example "Top 5 locations: Library 8, Canteen 6".
    public string AltText => ChartData.Describe(this);

    // Returns the data-chart JSON, for example
    // {"kind":"line","labels":["May 2026"],"series":[{"name":"Lost","values":[3]},{"name":"Found","values":[2]}]}.
    // Only kind, labels, and series are written; Razor HTML-encodes the text when it goes into the attribute.
    public string ToJson()
    {
        var payload = new
        {
            Kind,
            Labels,
            Series = Series.Select(series => new { series.Name, series.Values })
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
