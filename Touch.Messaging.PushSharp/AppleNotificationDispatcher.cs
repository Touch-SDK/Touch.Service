using PushSharp.Apple;
using Touch.Notification;

namespace Touch.Messaging
{
    /// <summary>
    /// Apple push notification dispatcher.
    /// </summary>
    sealed public class AppleNotificationDispatcher : INotificationDispatcher
    {
        #region .ctor
        public AppleNotificationDispatcher(PushBrokerProvider provider, ApplePushChannelSettings settings)
        {
            _provider = provider;
            _provider.Broker.RegisterService<AppleNotification>(new ApplePushService(settings));
        }
        #endregion

        #region Data
        private readonly PushBrokerProvider _provider;
        #endregion

        #region INotificationDispatcher members
        public void Dispatch(string deviceToken, string message, int count = 0, string data = null)
        {
            _provider.Broker.QueueNotification(new AppleNotification(deviceToken) { Payload = new AppleNotificationPayload(message, count, "default") });
        }
        #endregion
    }
}
