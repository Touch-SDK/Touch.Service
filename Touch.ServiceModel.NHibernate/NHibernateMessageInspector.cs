using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using NHibernate;
using NHibernate.Context;

namespace Touch.ServiceModel.Dispatcher
{
    sealed public class NHibernateMessageInspector : HttpDispatchMessageInspector
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="factory">Session factory.</param>
        /// <param name="commitOnly">If <c>false</c>, automatically opens a new session and starts a transaction.
        /// If <c>true</c>, doesn't start any sessions.</param>
        public NHibernateMessageInspector(ISessionFactory factory, bool commitOnly = true)
        {
            _factory = factory;
            _commitOnly = commitOnly;
        }

        private readonly ISessionFactory _factory;
        private readonly bool _commitOnly;

        #region HttpDispatchMessageInspector
        public override object AfterReceiveRequest(ref Message request, HttpRequestMessageProperty httpRequest, IClientChannel channel, InstanceContext instanceContext)
        {
            if (!_commitOnly)
            {
                var session = _factory.OpenSession();
                session.BeginTransaction();

                CurrentSessionContext.Bind(session);
            }

            return null;
        }

        public override void BeforeSendReply(ref Message reply, HttpResponseMessageProperty httpResponse, object correlationState)
        {
            if (!CurrentSessionContext.HasBind(_factory)) return;

            var success = httpResponse.StatusCode >= HttpStatusCode.Continue &&
                          httpResponse.StatusCode < HttpStatusCode.BadRequest;

            try
            {
                var session = _factory.GetCurrentSession();

                if (session.Transaction.IsActive)
                {
                    try
                    {
                        if (success)
                            session.Transaction.Commit();
                        else
                            session.Transaction.Rollback();
                    }
                    finally
                    {
                        session.Transaction.Dispose();
                    }
                }
            }
            finally
            {
                var session = CurrentSessionContext.Unbind(_factory);
                session.Dispose();
            }
        }
        #endregion
    }
}
