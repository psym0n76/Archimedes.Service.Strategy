using System.Threading;

namespace Archimedes.Service.Strategy
{
    public interface IStrategySubscriber
    {
        void Consume(CancellationToken cancellationToken);
    }
}