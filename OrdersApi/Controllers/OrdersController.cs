using System.Collections.Generic;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
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
            "OrderReceivedEvent")] // Same Topic name as the event class.
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

        [HttpPost]
        [Route("OrderProcessed")]
        [Topic("eventbus", // Name specified in "components/pubsub.yaml" file.
            "OrderProcessedEvent")] // Same Topic name as the event class.
        public async Task<IActionResult> OrderProcessed(OrderStatusChangedToProcessedCommand command)
        {
            _logger.LogInformation("OrderProcessed method entered");
            if (ModelState.IsValid)
            {
                var order = await _orderRepo.GetOrderAsync(command.OrderId);
                if (order != null)
                {
                    // Set order status to Processed.
                    order.Status = Status.Processed;

                    // Populate order details with processed faces.
                    for (var j = 0; j < command.Faces.Count; j++)
                    {
                        var face = command.Faces[j];
                        var img = Image.Load(face);
                        await img.SaveAsync("face" + j + ".jpg");
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            FaceData = face
                        };
                        order.OrderDetails.Add(orderDetail);
                    }

                    //Update the order.
                    await _orderRepo.UpdateOrder(order);
                }
            }
            return Ok();
        }

        [HttpPost]
        [Route("orderdispatched")]
        [Topic("eventbus", // Name specified in "components/pubsub.yaml" file.
            "OrderDispatchedEvent")] // Same Topic name as the event class.
        public async Task<IActionResult> OrderDispatched(OrderStatusChangedToDispatchedCommand command)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Order dispatched message received : " + command.OrderId);
                Order order = await _orderRepo.GetOrderAsync(command.OrderId);
                if (order != null)
                {
                    order.Status = Status.Dispatched;
                    await _orderRepo.UpdateOrder(order);
                    _logger.LogInformation("Order status changed to dispatched: " + command.OrderId);
                }
                return Ok();
            }
            return BadRequest();
        }

        [HttpGet("allorders")] // Method name to call from remote clients.
        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _orderRepo.GetAllOrdersAsync();
        }

    }
}
