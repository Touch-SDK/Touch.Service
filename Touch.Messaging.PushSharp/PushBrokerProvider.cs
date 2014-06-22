using PushSharp;
using PushSharp.Core;
using System;
using Touch.Logging;

namespace Touch.Messaging
{
    public sealed class PushBrokerProvider : IDisposable
    {
        public PushBrokerProvider(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.Get<PushBrokerProvider>();

            _broker = new PushBroker();
            _broker.OnNotificationSent += NotificationSent;
            _broker.OnChannelException += ChannelException;
            _broker.OnServiceException += ServiceException;
            _broker.OnNotificationFailed += NotificationFailed;
            _broker.OnDeviceSubscriptionExpired += DeviceSubscriptionExpired;
            _broker.OnChannelCreated += ChannelCreated;
            _broker.OnChannelDestroyed += ChannelDestroyed;
        }

        private readonly ILogger _logger;
        private readonly PushBroker _broker;

        public PushBroker Broker { get { return _broker; } }

        #region Event handlers
        private void NotificationSent(object sender, INotification notification)
        {
            _logger.Debug("Sent: " + sender + " -> " + notification);
        }

        private void NotificationFailed(object sender, INotification notification, Exception notificationFailureException)
        {
            _logger.Error("Failure: " + sender + " -> " + notificationFailureException.Message + " -> " + notification);
        }

        private void ChannelException(object sender, IPushChannel channel, Exception exception)
        {
            _logger.Error("Channel Exception: " + sender + " -> " + exception);
        }

        private void ServiceException(object sender, Exception exception)
        {
            _logger.Error("Channel Exception: " + sender + " -> " + exception);
        }

        private void DeviceSubscriptionExpired(object sender, string expiredDeviceSubscriptionId, DateTime timestamp, INotification notification)
        {
            _logger.Debug("Device Subscription Expired: " + sender + " -> " + expiredDeviceSubscriptionId);
        }

        private void ChannelDestroyed(object sender)
        {
            _logger.Debug("Channel Destroyed for: " + sender);
        }

        private void ChannelCreated(object sender, IPushChannel pushChannel)
        {
            _logger.Debug("Channel Created for: " + sender);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _broker.OnNotificationSent -= NotificationSent;
            _broker.OnChannelException -= ChannelException;
            _broker.OnServiceException -= ServiceException;
            _broker.OnNotificationFailed -= NotificationFailed;
            _broker.OnDeviceSubscriptionExpired -= DeviceSubscriptionExpired;
            _broker.OnChannelCreated -= ChannelCreated;
            _broker.OnChannelDestroyed -= ChannelDestroyed;

            _broker.StopAllServices(true);
        }
        #endregion
    }
}
