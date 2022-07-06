using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NotificationApi.Commands;
using NotificationApi.Events;
using NotificationApi.Helpers;
using SixLabors.ImageSharp;

namespace NotificationApi.Controllers
{
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ILogger<NotificationController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }
        
        [HttpPost]
        [Route("sendemail")]
        [Topic("eventbus",  // Name specified in "components/pubsub.yaml" file.
            "OrderProcessedEvent")] // Same Topic name as the event class.
        public async Task<IActionResult> SendEmail(DispatchOrderCommand command)
        {
            _logger.LogInformation("SendEmail method entered");
            _logger.LogInformation("Order received for dispatch: " + command.OrderId);
            var metaData = new Dictionary<string, string>
            {
                ["emailFrom"] = "faceplc@abc.com",
                ["emailTo"] = command.UserEmail,
                ["subject"] = $"your order {command.OrderId}"
            };
            var rootFolder = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin"));
            var facesData = command.Faces;

            if (facesData.Count < 1)
            {
                _logger.LogInformation("Mo faces detected");

            }
            else
            {
                // Save detected images.
                int j = 0;
                foreach (var face in facesData)
                {
                    Image img = Image.Load(face);
                    img.Save(rootFolder + "/Images/face" + j + ".jpg");
                    j++;

                }
            }

            // Use utility class to generate email body using saved images.
            var body = EmailUtils.CreateEmailBody(command);

            // Send email notification using output binding.
            await _daprClient.InvokeBindingAsync(
                "sendmail", // Name specified in "components/binding-email.yaml" file.
                "create", 
                body, 
                metaData);

            // Queue the event.
            var eventMsg = new OrderDispatchedEvent
            {
                OrderId = command.OrderId,
                DispatchDateTime = DateTime.UtcNow
            };
            await _daprClient.PublishEventAsync("eventbus", "OrderDispatchedEvent", eventMsg);

            _logger.LogInformation($"Order dispatched OrderId {eventMsg.OrderId} and dispatch date {eventMsg.DispatchDateTime}");
            return Ok();
        }
    }
}
