namespace APME.Payments;

/// <summary>
/// Configuration options for Stripe payment integration
/// </summary>
public class StripeOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Stripe";

    /// <summary>
    /// Stripe Secret Key (sk_test_... or sk_live_...)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Publishable Key (pk_test_... or pk_live_...)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Webhook Secret (whsec_...)
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use test mode
    /// </summary>
    public bool TestMode { get; set; } = true;

    /// <summary>
    /// Default currency code
    /// </summary>
    public string Currency { get; set; } = "usd";
}

