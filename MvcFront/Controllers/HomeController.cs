using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MvcFront.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Dapr.Client;
using MvcFront.Events;

namespace MvcFront.Controllers
{
    public class HomeController : Controller
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpGet]
        public IActionResult UploadData()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadData(UploadDataCommand model)
        {
            // Convert incoming image to a stream and then to a byte array.
            MemoryStream ms = new();
            await using (var uploadedFile = model.File.OpenReadStream())
            {
                await uploadedFile.CopyToAsync(ms);
            }

            // Setup order received event.
            var imageData = ms.ToArray();
            model.PhotoUrl = model.File.FileName;
            model.OrderId = Guid.NewGuid();
            var eventData = new OrderReceivedEvent(model.OrderId, model.UserEmail, model.PhotoUrl, imageData);

            try
            {
                // Publish the order received event to Dapr sidecar.
                await _daprClient.PublishEventAsync(
                    "eventbus", // Name specified in "components/pubsub.yaml" file.
                    "OrderReceivedEvent",  // Same Topic name as the event class.
                    eventData);

                _logger.LogInformation("Publishing event: OrderReceivedEvent, OrderId: {orderId}.", model.OrderId);
            }
            catch (Exception)
            {
                _logger.LogError("ERROR Publishing event: OrderReceivedEvent: OrderId: {orderId}.", model.OrderId);
                throw;
            }

            ViewData["OrderId"] = model.OrderId;
            return View("Thanks");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
