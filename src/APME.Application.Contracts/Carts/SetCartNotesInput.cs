using System.ComponentModel.DataAnnotations;

namespace APME.Carts;

/// <summary>
/// Input for setting cart notes
/// </summary>
public class SetCartNotesInput
{
    /// <summary>
    /// Notes for the order (max 1000 characters)
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
