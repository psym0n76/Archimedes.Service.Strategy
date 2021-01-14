using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy
{
    public interface IStrategyPublisher
    {
        Task AddTableAndPublishToQueue(PriceLevelDto level, StrategyDto strategy);
    }
}