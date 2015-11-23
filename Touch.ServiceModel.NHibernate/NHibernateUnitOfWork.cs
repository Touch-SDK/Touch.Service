using NHibernate.Context;
using Touch.Persistence;

namespace Touch.ServiceModel
{
    public sealed class NHibernateUnitOfWork : IUnitOfWork
    {
        public NHibernateUnitOfWork(NHibernateSessionFactory factory)
        {
            _factory = factory;
        }

        private readonly NHibernateSessionFactory _factory;

        public void Commit()
        {
            if (!CurrentSessionContext.HasBind(_factory.Factory)) return;

            var session = _factory.GetCurrentSession();

            if (session.Transaction.IsActive)
            {
                try
                {
                    session.Transaction.Commit();
                }
                finally
                {
                    session.Transaction.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (!CurrentSessionContext.HasBind(_factory.Factory)) return;

            try
            {
                var session = _factory.GetCurrentSession();

                if (session.Transaction != null && session.Transaction.IsActive)
                {
                    session.Transaction.Rollback();
                }
            }
            finally
            {
                var session = CurrentSessionContext.Unbind(_factory.Factory);
                session.Dispose();
            }
        }
    }
}
