using System.Collections.Generic;

namespace APME.Chatbot.Dtos;

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;

    public string Intent { get; set; } = string.Empty;

    public float Confidence { get; set; }

    public List<ProductSummaryDto> Products { get; set; } = [];

    public string? ClarificationQuestion { get; set; }

    public string? SuggestedIntent { get; set; }
}
