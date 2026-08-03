using System.Collections.Generic;

namespace APME.Costing;

public class CostImportResultDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Messages { get; set; } = new();
}
