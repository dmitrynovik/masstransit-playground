using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using Xunit;

namespace MassTransitPlayground
{
    public class Message
    {
        public string Payload { get; set; }
        public override string ToString() => Payload;
    }

    public class MassTransitTest
    {
        private const string ExpectedMessage = "Testing MassTransit in-memory bus";
        private const string QueueName = "queue_name";
        private const int DefaultWaitTimeout = 5000;

        [Fact]
        public async Task MassTransit_InMemory_Send_Receive_Test()
        {
            var args = ConfigureInMemoryReceiveEndpoint();
            await SendReceiveStop(args.Item1, args.Item2);
        }

        [Fact]
        public async Task MassTransit_InMemory_Publish_Receive_Test()
        {
            var args = ConfigureInMemoryReceiveEndpoint();
            await PublishReceiveStop(args.Item1, args.Item2);
        }

        private static (IBusControl, ManualResetEvent) ConfigureInMemoryReceiveEndpoint()
        {
            var waitHandle = new ManualResetEvent(false);

            var busControl = Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.ReceiveEndpoint(QueueName, ep =>
                {
                    ep.Handler<Message>(m =>
                    {
                        Console.WriteLine(m.Message);
                        waitHandle.Set();
                        m.Message.Should().Be(ExpectedMessage);
                        return Task.CompletedTask;
                    });
                });
            });

            return (busControl, waitHandle);
        }

        private static async Task SendReceiveStop(IBusControl busControl, WaitHandle waitHandle)
        {
            await DispatchReceiveStop(busControl, waitHandle, async m =>
            {
                // This will work:
                var sendEndpoint = await busControl.GetSendEndpoint(new Uri($"loopback://localhost/{QueueName}"));
                // This will NOT work:
                //var sendEndpoint = await busControl.GetSendEndpoint(new Uri(busControl.Address));
                await sendEndpoint.Send(m);
            });
        }

        private static async Task PublishReceiveStop(IBusControl busControl, WaitHandle waitHandle)
        {
            await DispatchReceiveStop(busControl, waitHandle, m => busControl.Publish(m));
        }

        private static async Task DispatchReceiveStop(IBusControl busControl, WaitHandle waitHandle, Func<Message, Task> dispatcher, int timeout = DefaultWaitTimeout)
        {
            await busControl.StartAsync();

            var message = new Message { Payload = ExpectedMessage };
            await dispatcher(message);

            if (!waitHandle.WaitOne(timeout))
                throw new TimeoutException($"Timeout of {timeout}ms expired");

            waitHandle.Dispose();
            await busControl.StopAsync();
        }
    }
}
