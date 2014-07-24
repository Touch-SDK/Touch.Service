using System;

namespace Touch.ServiceModel
{
    /// <summary>
    /// Unit of work.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Commit operation.
        /// </summary>
        /// <exception cref="InvalidOperationException">Unit of work is finished.</exception>
        void Commit();
    }
}
