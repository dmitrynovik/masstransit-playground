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
        const string ExpectedMessage = "Testing MassTransit in-memory bus";

        [Fact(Skip = "Send requires convention")]
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
                cfg.ReceiveEndpoint("queue_name", ep =>
                {
                    //configure the endpoint
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

        private static async Task SendReceiveStop(IBusControl busControl, WaitHandle waitHandle) => await DispatchReceiveStop(busControl, waitHandle, m => busControl.Send(m));

        private static async Task PublishReceiveStop(IBusControl busControl, WaitHandle waitHandle) => await DispatchReceiveStop(busControl, waitHandle, m => busControl.Publish(m));

        private static async Task DispatchReceiveStop(IBusControl busControl, WaitHandle waitHandle, Func<Message, Task> dispatcher)
        {
            await busControl.StartAsync();

            var message = new Message { Payload = ExpectedMessage };
            await dispatcher(message);

            waitHandle.WaitOne();

            waitHandle.Dispose();
            await busControl.StopAsync();
        }
    }
}
