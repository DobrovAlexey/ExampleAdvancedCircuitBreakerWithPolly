using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ExampleAdvancedCircuitBreakerWithPolly.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

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
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(2, retryAttempt =>
                {
                    var timeToWait = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

                    Trace.WriteLine($"Waiting {timeToWait.TotalSeconds} seconds");
                    return timeToWait;
                }); 

            _circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(1, TimeSpan.FromMinutes(1),
                    (exception, timeSpan) =>
                    {
                        Trace.WriteLine("Circuit broken!");
                    },
                    () =>
                    {
                        Trace.WriteLine("Circuit Reset!");
                    });
        }

        public async Task<string> GetHelloMessage()
        {
            //return await _messageRepository.GetHelloMessage();
            return await _retryPolicy.ExecuteAsync<string>(async () => await _messageRepository.GetHelloMessage());
        }

        public async Task<string> GetGoodbyeMessage()
        {
            //return await _messageRepository.GetGoodbyeMessage();

            try
            {
                Trace.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");
                return await _circuitBreakerPolicy.ExecuteAsync<string>(async () =>
                {
                    return await _messageRepository.GetGoodbyeMessage();
                });
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}