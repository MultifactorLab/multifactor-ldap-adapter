using System;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Configuration;

public class PrivacyModeDescriptor
{
    private readonly string[] _fields;
    public PrivacyMode Mode { get; }

    public static PrivacyModeDescriptor Default => new(PrivacyMode.None);

    public bool HasField(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return false;
        }

        return _fields.Any(x => x.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    private PrivacyModeDescriptor(PrivacyMode mode, params string[] fields)
    {
        Mode = mode;
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    public static PrivacyModeDescriptor Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new PrivacyModeDescriptor(PrivacyMode.None);

        var mode = GetMode(value);
        if (mode != PrivacyMode.Partial) return new PrivacyModeDescriptor(mode);

        var fields = GetFields(value);
        return new PrivacyModeDescriptor(mode, fields);
    }

    private static PrivacyMode GetMode(string value)
    {
        var index = value.IndexOf(':');
        if (index == -1)
        {
            if (!Enum.TryParse<PrivacyMode>(value, true, out var parsed1)) throw new Exception("Unexpected privacy-mode value");
            return parsed1;
        }

        var sub = value[..index];
        if (!Enum.TryParse<PrivacyMode>(sub, true, out var parsed2)) throw new Exception("Unexpected privacy-mode value");

        return parsed2;
    }

    private static string[] GetFields(string value)
    {
        var index = value.IndexOf(':');
        if (index == -1 || value.Length <= index + 1)
        {
            return Array.Empty<string>();
        }

        var sub = value[(index + 1)..];
        return sub.Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
    }
}