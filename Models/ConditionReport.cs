namespace Reboot.Models;

public class ConditionCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; } = true;
    public string? Notes { get; set; }
}

public class ConditionReport
{
    public List<ConditionCheck> Checks { get; set; } = new();
    public string? KnownDefects { get; set; }

    public int PassedCount => Checks.Count(c => c.Passed);
    public int TotalCount => Checks.Count;
    public bool AllPassed => Checks.All(c => c.Passed);
}
