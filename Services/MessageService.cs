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

            // Политика повторителя.
            _retryPolicy = Policy
                .Handle<Exception>()
                // retryCount - указывает, сколько раз вы хотите повторить попытку.
                // sleepDurationProvider - делегат, который определяет, как долго ждать перед повторной попыткой. 
                .WaitAndRetryAsync(2, retryAttempt =>
                {
                    // Экспоненциальное время ожидания.
                    var timeToWait = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

                    Trace.WriteLine($"Waiting {timeToWait.TotalSeconds} seconds");
                    return timeToWait;
                });

            // Политика выключателя.
            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                // exceptionsAllowedBeforeBreaking - указывает, сколько исключений подряд вызовет разрыв цепи.
                // durationOfBreak - указывает, как долго цепь будет оставаться разорванной.
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30),
                    // onBreak - является делегатом, позволяет выполнить какое-то действие, когда цепь разорвана.
                    (exception, timeSpan) =>
                    {
                        Trace.WriteLine("Circuit broken!");
                    },
                    // onReset - является делегатом, позволяет выполнить какое-либо действие, когда канал сброшен
                    () =>
                    {
                        Trace.WriteLine("Circuit Reset!");
                    });
        }

        public async Task<string> GetHelloMessage()
        {
            // Обращаемся к репозиторию, через политику повторителя.
            return await _retryPolicy.ExecuteAsync(async () => await _messageRepository.GetHelloMessage());
        }

        public async Task<string> GetGoodbyeMessage()
        {
            Trace.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");

            // Если выключатель открыт, обходим запрос в репозиторий и выводим статические данные.
            if (_circuitBreakerPolicy.CircuitState != CircuitState.Open)
            {
                try
                {
                    // Обращаемся к репозиторию, через политику выключателя.
                    return await _circuitBreakerPolicy.ExecuteAsync(async () => await _messageRepository.GetGoodbyeMessage());
                }
                catch (Exception e)
                {
                    // В случае ошибки, выключатель перейдет в открытое состояние, на указанное время.
                    // Игнорируем ошибку и выводим статические данные.
                    Trace.WriteLine(e.Message);
                }
            }

            return await new ValueTask<string>("Bypassing a request to the repository.");
        }
    }
}