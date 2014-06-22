using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Touch.ServiceModel
{
    /// <summary>
    /// Unit of work WCF service behavior.
    /// </summary>
    sealed public class UnitOfWorkServiceBehavior : BehaviorExtensionElement, IServiceBehavior 
    {
        #region .ctor
        public UnitOfWorkServiceBehavior(IEnumerable<Func<IUnitOfWork>> factories)
        {
            if (factories == null) throw new ArgumentNullException("factories");
            _factories = factories;
        }
        #endregion

        #region Data
        /// <summary>
        /// IUnitOfWork factory methods.
        /// </summary>
        private readonly IEnumerable<Func<IUnitOfWork>> _factories;
        #endregion

        #region IServiceBehavior implementation
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (var endpoint in channelDispatcher.Endpoints)
                {
                    endpoint.DispatchRuntime.MessageInspectors.Add(new UnitOfWorkMessageInspector(_factories));
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
        #endregion

        #region BehaviorExtensionElement implementation
        public override Type BehaviorType
        {
            get { return typeof(UnitOfWorkServiceBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new UnitOfWorkServiceBehavior(_factories);
        }
        #endregion
    }
}
