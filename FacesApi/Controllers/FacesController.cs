using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using FacesApi.Commands;
using FacesApi.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FacesApi.Controllers
{
    /// <summary>
    /// The "Inbox" micro-service Pattern is implemented here.
    /// Step 1 - ProcessOrder() de-queues the event and adds it to the end of (FIFO) state store.
    /// Step 2 - Cron() periodically fetches the first item from state store and processes it.
    /// </summary>

    [ApiController]
    public class FacesController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<FacesController> _logger;
        private readonly AzureFaceConfiguration _azureFaceConfiguration;

        public FacesController(ILogger<FacesController> logger, DaprClient daprClient, AzureFaceConfiguration azureFaceConfiguration)
        {
            _logger = logger;
            _daprClient = daprClient;
            _azureFaceConfiguration = azureFaceConfiguration;
        }

        [HttpPost]
        [Route("processorder")]
        [Topic("eventbus", // Name specified in "components/pubsub.yaml" file.
            "OrderRegisteredEvent")] // Same Topic name as the event class.
        public async Task<IActionResult> ProcessOrder([FromBody] ProcessOrderCommand command) // [FromBody] is optional.
        {
            _logger.LogInformation("ProcessOrder method entered....");
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Command params: {command.OrderId}");

                // Convert image data into an image and save it temporarily.
                var img = Image.Load(command.ImageData);
                await img.SaveAsync("dummy.jpg");

                // Get ordered command list from state.
                var orderState = await _daprClient.
                    GetStateEntryAsync<List<ProcessOrderCommand>>(
                        "redisstore", // Name specified in "components/statestore.yaml" file.
                        "orderList"); // Hard-coded key that contains the list of command objects.
                
                List<ProcessOrderCommand> orderList = new();

                // If there's an ordered list available in state then add it to the end of the list.
                // Otherwise add it to the empty list. This is how we ensure FIFO nature of processing.
                if (orderState.Value == null)
                {
                    _logger.LogInformation("OrderState   Case 1 ");
                    orderList.Add(command);
                    await _daprClient.SaveStateAsync("redisstore", "orderList", orderList);
                }
                else
                {
                    _logger.LogInformation("OrderState  Case 2 ");
                    orderList = orderState.Value;
                    orderList.Add(command);
                    await _daprClient.SaveStateAsync("redisstore", "orderList", orderList);
                }
            }

            return Ok();
        }

        [HttpPost("cron")] // Name specified in "components/binding-cron.yaml" file.
        public async Task<IActionResult> Cron()
        {
            _logger.LogInformation("Cron method entered");
            var orderState = await _daprClient.
                GetStateEntryAsync<List<ProcessOrderCommand>>("redisstore", "orderList");

            if (orderState?.Value?.Count > 0)
            {
                _logger.LogInformation($"Count value of the orders in the store {orderState.Value.Count}");
                var orderList = orderState.Value;

                // Only process the first item in the FIFO state store.
                var firstInTheList = orderList[0];
                if (firstInTheList != null)
                {
                    _logger.LogInformation($"First order's OrderId : {firstInTheList.OrderId}");

                    // Convert image data into an image and save it temporarily.
                    var imageBytes = firstInTheList.ImageData.ToArray();
                    var img = Image.Load(imageBytes);
                    await img.SaveAsync("dummy1.jpg");

                    // Use Azure Faces to get faces identified and cropped.
                    var facesCropped = await UploadPhotoAndDetectFaces(img, new MemoryStream(imageBytes));

                    //Publish the order processed event to its own topic.
                    var ope = new OrderProcessedEvent
                    {
                        OrderId = firstInTheList.OrderId,
                        UserEmail = firstInTheList.UserEmail,
                        ImageData = firstInTheList.ImageData,
                        Faces = facesCropped
                    };
                    await _daprClient.PublishEventAsync("eventbus", "OrderProcessedEvent", ope);

                    // Remove the processed command from the state store.
                    orderList.Remove(firstInTheList);
                    await _daprClient.SaveStateAsync("redisstore", "orderList", orderList);

                    _logger.LogInformation($"Order List count after processing  {orderList.Count}");
                    return new OkResult();
                }
            }
            return NoContent();
        }

        private async Task<List<byte[]>> UploadPhotoAndDetectFaces(Image img, MemoryStream imageStream)
        {
            var subKey = _azureFaceConfiguration.AzureSubscriptionKey;
            var endPoint = _azureFaceConfiguration.AzureEndPoint;
            var client = Authenticate(endPoint, subKey);
            var faceList = new List<byte[]>();
            try
            {
                var faces = await client.Face.DetectWithStreamAsync(imageStream);
                for (var j = 0; j < faces.Count; j++)
                {
                    var h = faces[j].FaceRectangle.Height;
                    var w = faces[j].FaceRectangle.Width;
                    var x = faces[j].FaceRectangle.Left;
                    var y = faces[j].FaceRectangle.Top;
                    img.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h))).Save("face" + j + ".jpg");
                    var s = new MemoryStream();
                    img.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h))).SaveAsJpeg(s);
                    faceList.Add(s.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return faceList;
        }

        private static IFaceClient Authenticate(string endPoint, string subKey)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(subKey))
            {
                Endpoint = endPoint
            };
        }
    }
}
