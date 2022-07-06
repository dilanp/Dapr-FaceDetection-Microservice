using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MvcFront.Models;

namespace MvcFront.Services
{
    public class OrderClient : IOrderClient
    {
        private readonly HttpClient _httpClient;

        public OrderClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Order>> GetOrders()
        {
            return await _httpClient
                .GetFromJsonAsync<IEnumerable<Order>>(
                    "/allorders"); // Method name of the remote service controller action
        }
    }
}
