using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Strategy.Hubs
{
    public interface IStrategyHub
    {
        Task Add(StrategyDto value);
        Task Delete(StrategyDto value);
        Task Update(StrategyDto value);
    }
}