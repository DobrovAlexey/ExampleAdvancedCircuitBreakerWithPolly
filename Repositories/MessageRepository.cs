using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ExampleAdvancedCircuitBreakerWithPolly.Repositories
{
    public interface IMessageRepository
    {
        ValueTask<string> GetHelloMessage();
        ValueTask<string> GetGoodbyeMessage();
    }

    public class MessageRepository : IMessageRepository
    {
        public ValueTask<string> GetHelloMessage()
        {
            Trace.WriteLine("MessageRepository GetHelloMessage running");
            ThrowRandomException();
            return new ValueTask<string>("Hello World!");
        }

        public ValueTask<string> GetGoodbyeMessage()
        {
            Trace.WriteLine("MessageRepository GetGoodbyeMessage running");
            ThrowRandomException();
            return new ValueTask<string>("Goodbye World!");
        }
        private static void ThrowRandomException()
        {
            var diceRoll = new Random().Next(0, 10);

            if (diceRoll > 5)
            {
                Trace.WriteLine("ERROR! Throwing Exception");
                throw new Exception("Exception in MessageRepository");
            }
        }
    }
}