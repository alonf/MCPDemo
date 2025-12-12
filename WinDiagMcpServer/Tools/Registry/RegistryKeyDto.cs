namespace WinDiagMcpServer.Tools.Registry;

public record RegistryKeyDto
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, string> Values { get; set; } = new();

    public List<RegistryKeyDto> SubKeys { get; set; } = new();
}
