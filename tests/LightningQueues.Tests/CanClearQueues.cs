﻿using System;
using Should;
using Xunit;

namespace LightningQueues.Tests
{
    public class CanClearQueues : IDisposable
    {
        private QueueManager _sender;
        private QueueManager _receiver;

        public CanClearQueues()
        {
            _sender = ObjectMother.QueueManager();
            _receiver = ObjectMother.QueueManager("test2", 23457);
        }

        public void Dispose()
        {
            _sender.Dispose();
            _receiver.Dispose();
        }

        [Fact(Skip="Not on mono")]
        public void ClearsOutgoingMessages()
        {
            _sender.Start();
            sendMessages();
            _sender.GetMessagesCurrentlySending().Length.ShouldEqual(50);
            _sender.ClearAllMessages();
            _sender.GetMessagesCurrentlySending().Length.ShouldEqual(0);
        }

        [Fact(Skip="Not on mono")]
        public void ClearsQueueMessages()
        {
            _sender.Start();
            _receiver.Start();
            sendMessages();
            Wait.Until(() => _receiver.GetAllMessages("h", null).Length == 50).ShouldBeTrue();

            var scope = _receiver.BeginTransactionalScope();
            scope.Receive("h");
            scope.Commit();
            _receiver.GetAllProcessedMessages("h").Length.ShouldEqual(1);
            _receiver.ClearAllMessages();
            _receiver.GetAllMessages("h", null).ShouldBeEmpty();
            _receiver.GetAllProcessedMessages("h").ShouldBeEmpty();
        }

        private void sendMessages()
        {
            var scope = _sender.BeginTransactionalScope();
            for (int i = 0; i < 50; ++i)
            {
                scope.Send(new Uri("rhino.queues://localhost:23457/h"),
                    new MessagePayload
                    {
                        Data = new byte[] {1, 2, 4, 5}
                    });
            }
            scope.Commit();
        }
    }
}