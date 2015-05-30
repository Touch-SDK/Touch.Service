using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Touch.ServiceModel.Dispatcher
{
    /// <summary>
    /// Unit of work WCF service message inspector.
    /// </summary>
    sealed public class UnitOfWorkMessageInspector : HttpDispatchMessageInspector
    {
        #region .ctor
        public UnitOfWorkMessageInspector(IEnumerable<Func<IUnitOfWork>> factories)
        {
            if (factories == null) throw new ArgumentNullException("factories");
            _factories = factories;

            _activeUnits = new Dictionary<object, IEnumerable<IUnitOfWork>>();
        }
        #endregion

        #region Data
        /// <summary>
        /// IUnitOfWork factory methods.
        /// </summary>
        private readonly IEnumerable<Func<IUnitOfWork>> _factories;

        /// <summary>
        /// Active units of work.
        /// </summary>
        private readonly Dictionary<object, IEnumerable<IUnitOfWork>> _activeUnits;

        private readonly object _thisLock = new object();
        #endregion

        #region HttpDispatchMessageInspector
        public override object AfterReceiveRequest(ref Message request, HttpRequestMessageProperty httpRequest, IClientChannel channel, InstanceContext instanceContext)
        {
            var units = new List<IUnitOfWork>();

            foreach (var factory in _factories)
            {
                try
                {
                    var unit = factory.Invoke();
                    if (unit != null) units.Add(unit);
                }
                catch (Exception error)
                {
                    Trace.WriteLine("Error invoking a unit of work: " + error.Message, "UnitOfWork");
                    throw new OperationCanceledException("Error invoking a unit of work.", error);
                }
            }

            var state = new object();

            lock (_thisLock)
                _activeUnits[state] = units;

            return state;
        }

        public override void BeforeSendReply(ref Message reply, HttpResponseMessageProperty httpResponse, object correlationState)
        {
            if (correlationState == null) return;

            lock (_thisLock)
            {
                if (!_activeUnits.ContainsKey(correlationState)) 
                    return;
            }

            var errors = new List<Exception>();
            var success = httpResponse.StatusCode >= HttpStatusCode.Continue &&
                          httpResponse.StatusCode < HttpStatusCode.BadRequest;

            foreach (var unit in _activeUnits[correlationState])
            {
                try
                {
                    if (!reply.IsFault && success)
                        unit.Commit();
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Error commiting unit of work: " + e.Message, "UnitOfWork");
                    errors.Add(e);
                }
                finally
                {
                    try
                    {
                        unit.Dispose();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Error disposing unit of work: " + e.Message, "UnitOfWork");
                    }
                }
            }

            lock (_thisLock)
                _activeUnits.Remove(correlationState);

            if (errors.Count > 0)
                throw new AggregateException("Unable to finish one or more units of work.", errors);
        }
        #endregion
    }
}
