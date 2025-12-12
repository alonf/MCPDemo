namespace WinDiagMcpServer.Services;

public class RegistryRootsService
{
    private readonly HashSet<string> _allowedRoots = new(StringComparer.OrdinalIgnoreCase);

    public RegistryRootsService()
    {
        // Default safe roots
        _allowedRoots.Add(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall");
        _allowedRoots.Add(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
    }

    public void SetAllowedRoots(IEnumerable<string> roots)
    {
        _allowedRoots.Clear();
        foreach (var root in roots)
        {
            _allowedRoots.Add(root);
        }
    }

    // ReSharper disable once UnusedMember.Global
    public void AddAllowedRoot(string root)
    {
        _allowedRoots.Add(root);
    }

    public bool IsPathAllowed(string hive, string key)
    {
        var fullPath = $"{hive}\\{key}";

        // Check if fullPath starts with any allowed root
        // Also handle hive aliases if needed, but for now assume standard format
        return _allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> GetAllowedRoots() => _allowedRoots.ToArray();
}
