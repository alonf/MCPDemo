using System;
using System.Collections.Generic;
using System.Linq;

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

    public bool IsPathAllowed(string hive, string key)
    {
        var fullPath = $"{hive}\\{key}";
        Console.WriteLine($"Checking: '{fullPath}' against roots:");
        foreach(var r in _allowedRoots) Console.WriteLine($"  - '{r}'");
        
        return _allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }
}

public class Program
{
    public static void Main()
    {
        var service = new RegistryRootsService();
        
        Console.WriteLine("Test 1: Default Roots, Requesting HKCU\\Software");
        bool allowed1 = service.IsPathAllowed("HKCU", "Software");
        Console.WriteLine($"Allowed? {allowed1}"); // Expect False

        Console.WriteLine("\nTest 2: Default Roots, Requesting HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run");
        bool allowed2 = service.IsPathAllowed("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Run");
        Console.WriteLine($"Allowed? {allowed2}"); // Expect True

        Console.WriteLine("\nTest 3: Default Roots, Requesting HKEY_CURRENT_USER\\Software");
        bool allowed3 = service.IsPathAllowed("HKEY_CURRENT_USER", "Software");
        Console.WriteLine($"Allowed? {allowed3}"); // Expect False

        // Simulate what the user might have done if they configured roots
        service.SetAllowedRoots(new[] { @"HKEY_CURRENT_USER\Software" });
        Console.WriteLine("\nTest 4: Configured Root HKCU\\Software, Requesting HKCU\\Software");
        bool allowed4 = service.IsPathAllowed("HKEY_CURRENT_USER", "Software");
        Console.WriteLine($"Allowed? {allowed4}"); // Expect True
    }
}
