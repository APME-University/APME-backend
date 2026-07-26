using System.Threading.Tasks;
using APME.Chatbot.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Chatbot;

public interface IChatAppService : IApplicationService
{
    Task<ChatResponseDto> SendMessageAsync(SendMessageInput input);
}
