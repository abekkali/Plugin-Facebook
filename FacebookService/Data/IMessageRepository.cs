using FacebookService.Models;


namespace FacebookService.Data
{
    public interface IMessageRepository
    {
        Task<List<Message>> FetchMessagesAsync();

    }
}
