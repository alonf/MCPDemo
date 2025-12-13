namespace WinDiagMcpServer.Services;

    /// <summary>
    /// Provides allow-list management for registry paths to ensure access is restricted to predefined roots.
    /// </summary>
    public class RegistryRootsService
{
    private readonly HashSet<string> _allowedRoots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryRootsService"/> class with a default set of safe registry roots.
    /// </summary>
    public RegistryRootsService()
    {
        // Default safe roots
        _allowedRoots.Add(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall");
        _allowedRoots.Add(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
    }

    /// <summary>
    /// Replaces the current allowed registry roots with the provided collection.
    /// </summary>
    /// <param name="roots">The registry roots that should be allowed.</param>
    // ReSharper disable UnusedMember.Global
    public void SetAllowedRoots(IEnumerable<string> roots)
    {
        _allowedRoots.Clear();
        foreach (var root in roots)
        {
            _allowedRoots.Add(root);
        }
    }

    /// <summary>
    /// Adds a single registry root to the allow list.
    /// </summary>
    /// <param name="root">The registry root to allow.</param>
    // ReSharper disable once UnusedMember.Global
    public void AddAllowedRoot(string root)
    {
        _allowedRoots.Add(root);
    }

    /// <summary>
    /// Determines whether the specified registry hive and key combination is allowed.
    /// </summary>
    /// <param name="hive">The registry hive (e.g., HKLM, HKCU).</param>
    /// <param name="key">The registry key path within the hive.</param>
    /// <returns><c>true</c> if the full path starts with an allowed root; otherwise, <c>false</c>.</returns>
    public bool IsPathAllowed(string hive, string key)
    {
        var fullPath = $"{hive}\\{key}";

        // Check if fullPath starts with any allowed root
        // Also handle hive aliases if needed, but for now assume standard format
        return _allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> GetAllowedRoots() => _allowedRoots.ToArray();
}
