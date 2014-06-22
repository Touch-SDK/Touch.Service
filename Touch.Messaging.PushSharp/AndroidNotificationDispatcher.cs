using PushSharp.Android;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Touch.Domain;
using Touch.Notification;

namespace Touch.Messaging
{
    /// <summary>
    /// Android push notification dispatcher.
    /// </summary>
    sealed public class AndroidNotificationDispatcher : INotificationDispatcher
    {
        #region .ctor
        public AndroidNotificationDispatcher(PushBrokerProvider provider, GcmPushChannelSettings settings)
        {
            _provider = provider;
            _provider.Broker.RegisterService<GcmNotification>(new GcmPushService(settings));
        }
        #endregion

        #region Data
        private readonly PushBrokerProvider _provider;
        #endregion

        #region INotificationDispatcher members
        public void Dispatch(string deviceToken, string message, int count = 0, string data = null)
        {
            var json = "{" + string.Format("\"request_token\":\"{0}\",\"text\":\"{1}\",\"badge\":\"{2}\"", data, message, count) + "}";

            _provider.Broker.QueueNotification(new GcmNotification { RegistrationIds = new List<string> { deviceToken }, JsonData = json });
        }
        #endregion
    }
}
