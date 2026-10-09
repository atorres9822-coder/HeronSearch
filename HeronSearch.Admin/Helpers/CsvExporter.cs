using System.Globalization;
using System.Text;

namespace HeronSearch.Admin.Helpers;

// Builds CSV files for the Reports / History exports. Pure and clock-free: the caller passes the header, the
// rows, and the dates. Output is UTF-8 with a BOM (so Excel reads accents), RFC 4180 quoting, and CRLF line ends.
public static class CsvExporter
{
    // Characters that, when a field starts with one, could be read as a formula by a spreadsheet. A guard apostrophe
    // is added in front so the value is shown as text instead of being evaluated.
    private static readonly char[] FormulaLeadingCharacters = { '=', '+', '-', '@', '\t', '\r' };

    // Returns the field with a leading apostrophe when it starts with a formula character, so a spreadsheet treats
    // it as text and never runs it. A null or empty field is left unchanged.
    public static string Guard(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return field ?? string.Empty;
        }

        return Array.IndexOf(FormulaLeadingCharacters, field[0]) >= 0 ? "'" + field : field;
    }

    // Returns the field quoted per RFC 4180 when it contains a comma, quote, or line break; a quote inside is
    // doubled. Other fields are returned unchanged. Guard runs first, so a formula field is already prefixed.
    public static string Escape(string? field)
    {
        var guarded = Guard(field);
        if (guarded.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
        {
            return guarded;
        }

        return "\"" + guarded.Replace("\"", "\"\"") + "\"";
    }

    // Builds the CSV bytes: a UTF-8 BOM, the header row, then one row per record, each field escaped, fields
    // joined by commas, and rows ended with CRLF.
    public static byte[] Build(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, header);
        foreach (var row in rows)
        {
            AppendRow(builder, row);
        }

        // Encoding with a BOM so spreadsheets detect UTF-8.
        var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return utf8WithBom.GetBytes(builder.ToString());
    }

    // Appends one CSV line: each field escaped, joined by commas, ended with CRLF.
    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> fields)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(Escape(fields[i]));
        }

        builder.Append("\r\n");
    }

    // Returns the download file name "heronsearch-{prefix}-YYYYMMDD-YYYYMMDD.csv" for the given range,
    // for example "heronsearch-items-20260501-20261006.csv". The dates are formatted as whole days.
    public static string FileName(string prefix, DateTime from, DateTime to)
    {
        var fromText = from.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var toText = to.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return $"heronsearch-{prefix}-{fromText}-{toText}.csv";
    }
}
