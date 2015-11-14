using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Touch.Persistence;
using Touch.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Description
{
    sealed public class NHibernateServiceBehavior : IServiceBehavior
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="factory">Session factory.</param>
        /// <param name="commitOnly">If <c>false</c>, automatically opens a new session and starts a transaction.
        /// If <c>true</c>, doesn't start any sessions.</param>
        public NHibernateServiceBehavior(NHibernateSessionFactory factory, bool commitOnly = true)
        {
            _factory = factory;
            _commitOnly = commitOnly;
        }

        private readonly NHibernateSessionFactory _factory;
        private readonly bool _commitOnly;

        public void AddBindingParameters(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters)
        { }

        public void ApplyDispatchBehavior(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (var endpoint in channelDispatcher.Endpoints)
                {
                    endpoint.DispatchRuntime.MessageInspectors.Add(new NHibernateMessageInspector(_factory.Factory, _commitOnly));
                }
            }
        }

        public void Validate(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        { }
    }
}
