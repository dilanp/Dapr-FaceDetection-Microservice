using System.Collections.Generic;
using System.Threading.Tasks;
using MvcFront.Models;

namespace MvcFront.Services
{
    public interface IOrderClient
    {
        Task<IEnumerable<Order>> GetOrders();
    }
}
