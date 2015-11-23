using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Touch.ServiceModel
{
    /// <summary>
    /// Unit of work HTTP module.
    /// </summary>
    public class UnitOfWorkHttpModule : IHttpModule
    {
        #region .ctor
        protected UnitOfWorkHttpModule(IEnumerable<Func<IUnitOfWork>> factories)
        {
            if (factories == null) throw new ArgumentNullException("factories");
            _factories = factories;
        }
        #endregion

        #region Data
        /// <summary>
        /// HTTP context items dictinary key.
        /// </summary>
        private const string RequestUnitsKey = "UnitsOfWork";

        /// <summary>
        /// IUnitOfWork factory methods.
        /// </summary>
        private readonly IEnumerable<Func<IUnitOfWork>> _factories;

        /// <summary>
        /// HTTP context.
        /// </summary>
        private HttpApplication _context;
        #endregion

        #region IHttpModule implementation
        public void Init(HttpApplication context)
        {
            _context = context;

            context.BeginRequest += ContextBeginRequest;
            context.EndRequest += ContextEndRequest;
        }

        public void Dispose()
        {
            _context.BeginRequest -= ContextBeginRequest;
            _context.EndRequest -= ContextEndRequest;
        }
        #endregion

        #region Event handlers
        private void ContextBeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;

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

            context.Items[RequestUnitsKey] = units;
        }

        private void ContextEndRequest(object sender, EventArgs args)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var units = (IEnumerable<IUnitOfWork>) context.Items[RequestUnitsKey];

            if (units == null || units.Count() == 0) return;

            Exception error = null;

            foreach (var unit in units.Reverse())
            {
                try
                {
                    if (error == null && context.Error == null)
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

            if (error != null)
            {
                throw new OperationCanceledException("Unable to finish one or more units of work.", error);
            }
        }
        #endregion
    }
}
