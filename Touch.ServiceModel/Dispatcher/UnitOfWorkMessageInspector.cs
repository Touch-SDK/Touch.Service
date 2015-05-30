using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Touch.ServiceModel.Dispatcher
{
    /// <summary>
    /// Unit of work WCF service message inspector.
    /// </summary>
    sealed public class UnitOfWorkMessageInspector : IDispatchMessageInspector
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

        #region IDispatchMessageInspector
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
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
            {
                _activeUnits[state] = units;
            }

            return state;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if (correlationState == null) return;

            lock (correlationState)
            {
                if (!_activeUnits.ContainsKey(correlationState)) return;
            }

            Exception error = null;

            foreach (var unit in _activeUnits[correlationState].Reverse())
            {
                try
                {
                    if (error == null && !reply.IsFault)
                    {
                        unit.Commit();
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Error commiting a unit of work: " + e.Message, "UnitOfWork");
                    error = e;
                }
                finally
                {
                    try
                    {
                        unit.Dispose();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Error disposing a unit of work: " + e.Message, "UnitOfWork");
                    }
                }
            }

            lock (correlationState)
            {
                _activeUnits.Remove(correlationState);
            }

            if (error != null)
            {
                throw new OperationCanceledException("Unable to finish one or more units of work.", error);
            }
        }
        #endregion
    }
}
