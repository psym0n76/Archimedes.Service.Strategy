using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Microsoft.AspNetCore.SignalR;

namespace Archimedes.Service.Strategy.Hubs
{
    public class StrategyHub : Hub<IStrategyHub>
    {
        public async Task Add(StrategyDto value)
        {
            await Clients.All.Add(value);
        }

        public async Task Delete(StrategyDto value)
        {
            await Clients.All.Delete(value);
        }

        public async Task Update(StrategyDto value)
        {
            await Clients.All.Update(value);
        }
    }
}