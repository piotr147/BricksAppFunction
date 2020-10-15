using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BricksAppFunction.Utilities;

namespace BricksAppFunction
{
    public static class ManagePage
    {
        [FunctionName("ManagePage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log) =>
            new ContentResult
            {
                Content = PlainTextContent.ManagePage,
                ContentType = "text/html"
            };
    }
}
