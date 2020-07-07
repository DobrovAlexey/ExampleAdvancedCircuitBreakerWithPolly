using System.Threading.Tasks;
using ExampleAdvancedCircuitBreakerWithPolly.Repositories;

namespace ExampleAdvancedCircuitBreakerWithPolly.Services
{
    public interface IMessageService
    {
        Task<string> GetHelloMessage();
        Task<string> GetGoodbyeMessage();
    }

    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;

        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<string> GetHelloMessage()
        {
            return await _messageRepository.GetHelloMessage();
        }

        public async Task<string> GetGoodbyeMessage()
        {
            return await _messageRepository.GetGoodbyeMessage();
        }
    }
}