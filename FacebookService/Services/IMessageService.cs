using FacebookService.Models;

namespace FacebookService.Services
{
    public interface IMessageService
    {
        Task<List<Message>> FetchMessagesAsync();
    }
}
