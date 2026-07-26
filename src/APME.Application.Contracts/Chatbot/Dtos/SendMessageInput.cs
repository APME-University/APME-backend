using System.ComponentModel.DataAnnotations;

namespace APME.Chatbot.Dtos;

public class SendMessageInput
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}
