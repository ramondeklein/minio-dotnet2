using System.Text;

namespace Minio.Helpers;

internal class QueryParams
{
    private Dictionary<string, List<string>>? _params;

    public QueryParams Add(string name, string value)
    {
        _params ??= new();
        if (!_params.TryGetValue(name, out var values))
        {
            values = new List<string>(1);
            _params.Add(name, values);
        }
        values.Add(value);
        return this;
    }

    public IReadOnlyList<string> Get(string name)
    {
        if (_params != null && _params.TryGetValue(name, out var values))
            return values;
        return Array.Empty<string>();
    }

    public override string ToString()
    {
        if (_params == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var (name, values) in _params)
        {
            sb.Append(sb.Length == 0 ? "?" : "&");
            var encodedName = AwsUriEncode(name);
            foreach (var value in values)
            {
                sb.Append(encodedName);
                sb.Append('=');
                if (!string.IsNullOrEmpty(value))
                    sb.Append(AwsUriEncode(value));
            }
        }

        return sb.ToString();
    }

    private string AwsUriEncode(string text)
    {
        // AWS requires URL encoding where HEX values are in uppercase
        return System.Net.WebUtility.UrlEncode(text);
    }
}