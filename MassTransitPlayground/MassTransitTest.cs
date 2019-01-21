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

    public class MassTransitTest : IDisposable
    {
        private readonly EventWaitHandle _event = new ManualResetEvent(false);

        [Fact]
        public async Task MassTransit_InMemory_Test()
        {
            const string expectedMessage = "Testing MassTransit in-memory bus";

            var busControl = Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.ReceiveEndpoint("queue_name", ep =>
                {
                    //configure the endpoint
                    ep.Handler<Message>(m =>
                    {
                        Console.WriteLine(m.Message);
                        _event.Set();
                        m.Message.Should().Be(expectedMessage);
                        return Task.CompletedTask;
                    });
                });
            });

            await busControl.StartAsync();

            await busControl.Publish(new Message { Payload = expectedMessage });
            _event.WaitOne();

            await busControl.StopAsync();
        }

        public void Dispose()
        {
            _event?.Dispose();
        }
    }
}
