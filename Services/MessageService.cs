using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ExampleAdvancedCircuitBreakerWithPolly.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

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
        private readonly AsyncCircuitBreakerPolicy<string> _circuitBreakerPolicy;
        private readonly AsyncPolicyWrap<string> _policyWrap;

        public MessageService(IMessageRepository messageRepository)
        {
            Trace.WriteLine("MessageService running");
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
            // Обращаемся к репозиторию, через объединение политик.
            // В случае ошибки, выключатель перейдет в открытое состояние, на указанное время.
            _circuitBreakerPolicy = Policy<string>
                .Handle<Exception>()
                // exceptionsAllowedBeforeBreaking - указывает, сколько исключений подряд вызовет разрыв цепи.
                // durationOfBreak - указывает, как долго цепь будет оставаться разорванной.
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30),
                    // onBreak - является делегатом, позволяет выполнить какое-то действие, когда цепь разорвана.
                    (exception, timeSpan) =>
                    {
                        Trace.WriteLine($"Circuit broken! {timeSpan}");
                    },
                    // onReset - является делегатом, позволяет выполнить какое-либо действие, когда канал сброшен
                    () =>
                    {
                        Trace.WriteLine("Circuit Reset!");
                    });


            // Политика подмены исключения на ответ.
            // Игнорируем ошибку и выводим статические данные.
            var fallbackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync(async token =>
                {
                    Trace.WriteLine("Return Fallback");

                    return await new ValueTask<string>("Bypassing a request to the repository.");
                });

            // Объединение политик в одну.
            _policyWrap = Policy.WrapAsync(fallbackPolicy, _circuitBreakerPolicy);
        }

        public async Task<string> GetHelloMessage()
        {
            // Обращаемся к репозиторию, через политику повторителя.
            return await _retryPolicy.ExecuteAsync(async () => await _messageRepository.GetHelloMessage());
        }

        public async Task<string> GetGoodbyeMessage()
        {
            Trace.WriteLine($"Circuit State: {_circuitBreakerPolicy.CircuitState}");

            return await _policyWrap.ExecuteAsync(async () => await _messageRepository.GetGoodbyeMessage());
        }
    }
}