using FacebookService.Data;
using FacebookService.Models;
using System.Text.Json;

namespace FacebookService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(IMessageRepository messageRepository, ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }
       
        public async Task OnTimedEvent()
        {
            var messages = await _messageRepository.FetchMessagesAsync();

            // Sérialisation en JSON
            var jsonMessages = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });

            // Affichage des messages
            _logger.LogInformation("Messages récupérés:");
            _logger.LogInformation(jsonMessages);
        }
        public async Task<List<Message>> FetchMessagesAsync()
        {
            try
            {
                var messages = await _messageRepository.FetchMessagesAsync();
                messages.Reverse();
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Une erreur est survenue : {ex.Message}");
                throw;
            }
        }

    }
}
