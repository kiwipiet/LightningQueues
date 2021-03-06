﻿using System;
using System.Reactive.Linq;
using LightningQueues.Storage;

namespace LightningQueues.Net
{
    public class SendingErrorPolicy
    {
        private readonly IObservable<OutgoingMessage> _retryStream;

        public SendingErrorPolicy(IMessageStore store, IObservable<OutgoingMessageFailure> failedToConnect)
        {
            _retryStream = failedToConnect.SelectMany(x => x.Batch.Messages)
                .Select(x => new { Message = x, AttemptCount = store.FailedToSend(x) })
                .Where(x => ShouldRetry(x.Message, x.AttemptCount))
                .SelectMany(x => Observable.Return(x.Message).Delay(TimeSpan.FromSeconds(x.AttemptCount * x.AttemptCount)));
        }

        public IObservable<OutgoingMessage> RetryStream => _retryStream;

        public bool ShouldRetry(OutgoingMessage message, int attemptCount)
        {
            return (attemptCount < (message.MaxAttempts ?? 100))
                &&
                (!message.DeliverBy.HasValue || DateTime.Now < message.DeliverBy);
        }
    }
}