using System.Collections.Generic;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OrdersApi.Commands;
using OrdersApi.Events;
using OrdersApi.Models;
using OrdersApi.Persistence;
using SixLabors.ImageSharp;

namespace OrdersApi.Controllers
{
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly IOrderRepository _orderRepo;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ILogger<OrdersController> logger, IOrderRepository orderRepo, DaprClient daprClient)
        {
            _logger = logger;
            _orderRepo = orderRepo;
            _daprClient = daprClient;
        }

        [HttpPost]
        [Route("OrderReceived")]
        [Topic("eventbus", // Name specified in "components/pubsub.yaml" file.
            "OrderReceivedEvent") // Same Topic name as the event class.
        ]
        public async Task<IActionResult> OrderReceived(OrderReceivedCommand command)
        {
            if (command?.OrderId != null && 
                command.PhotoUrl != null && 
                command.UserEmail != null &&
                command.ImageData != null)
            {
                _logger.LogInformation($"Cloud event {command.OrderId} {command.UserEmail} received");

                // Convert image data into an image and save it temporarily.
                var img = Image.Load(command.ImageData);
                await img.SaveAsync("dummy.jpg");

                // Create the order entity to be saved to database.
                var order = new Order
                {
                    OrderId = command.OrderId,
                    ImageData = command.ImageData,
                    UserEmail = command.UserEmail,
                    PhotoUrl = command.PhotoUrl,
                    Status = Status.Registered,
                    OrderDetails = new List<OrderDetail>()
                };

                var orderExists = await _orderRepo.GetOrderAsync(order.OrderId);
                if (orderExists == null)
                {
                    // Order is not in database already so, register it.
                    await _orderRepo.RegisterOrder(order);

                    // Publish the order registered event to Dapr sidecar.
                    await _daprClient.PublishEventAsync(
                        "eventbus", // Name specified in "components/pubsub.yaml" file.
                        "OrderRegisteredEvent", // Same Topic name as the event class.
                        new OrderRegisteredEvent
                        {
                            OrderId = order.OrderId,
                            UserEmail = order.UserEmail,
                            ImageData = order.ImageData
                        });

                    _logger.LogInformation($"For {order.OrderId}, OrderRegisteredEvent published");
                }

                return Ok();
            }

            return BadRequest();
        }
    }
}
