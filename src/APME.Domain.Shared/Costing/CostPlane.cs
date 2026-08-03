namespace APME.Costing;

/// <summary>
/// Which financial plane a cost sits on. Inventory costs capitalize into COGS (landed cost);
/// Operating costs are variable selling costs that drive contribution margin.
/// </summary>
public enum CostPlane
{
    Inventory = 0,
    Operating = 1
}
