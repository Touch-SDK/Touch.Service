using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Touch.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Description
{
    /// <summary>
    /// Unit of work service behavior.
    /// </summary>
    sealed public class UnitOfWorkServiceBehavior : IServiceBehavior
    {
        public UnitOfWorkServiceBehavior(IEnumerable<Func<IUnitOfWork>> factories)
        {
            _factories = factories;
        }

        private readonly IEnumerable<Func<IUnitOfWork>> _factories;

        #region IServiceBehavior members
        public void AddBindingParameters(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (var endpoint in channelDispatcher.Endpoints)
                {
                    endpoint.DispatchRuntime.MessageInspectors.Add(new UnitOfWorkMessageInspector(_factories));
                }
            }
        }

        public void Validate(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase) { } 
        #endregion
    }
}
