namespace SQ7MRU.Utils.Infrastructure;

/// <summary>Configures the shared technical directory for download metadata and ADIF exports.</summary>
public sealed class ArtifactStorageOptions
{
    /// <summary>The default technical directory name used when no override is configured.</summary>
    public const string DefaultTechnicalDirectoryName = ".sq7mru";

    /// <summary>Gets or sets one directory name directly beneath the selected output root.</summary>
    /// <value>Defaults to .sq7mru. Must be a portable filename component of at most 100 characters, without separators, traversal, trailing dots/spaces or Windows device names.</value>
    /// <remarks>SQLite, its lock and downloaded ADIF exports share this directory. Configure once before resolving services; changing it selects a different index without moving or deleting earlier data.</remarks>
    public string TechnicalDirectoryName { get; set; } = DefaultTechnicalDirectoryName;

    internal static bool IsValidDirectoryName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name != name.Trim().TrimEnd('.') ||
            name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c))) return false;
        var stem = name.Split('.')[0].ToUpperInvariant();
        return stem is not ("CON" or "PRN" or "AUX" or "NUL") &&
            !(stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
              "123456789¹²³".Contains(stem[3]));
    }

    internal static string ValidateDirectoryName(string name) => IsValidDirectoryName(name)
        ? name : throw new ArgumentException("The technical directory must be a safe single directory name.", nameof(name));
}
