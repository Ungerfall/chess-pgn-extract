using CliWrap;
using CliWrap.Buffered;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace PgnExtract.AzureFunction
{
    public class PgnToHalg
    {
        private readonly ILogger _logger;

        public PgnToHalg(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PgnToHalg>();
        }

        [Function("PgnExtractFunction")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var body = await req.ReadAsStringAsync(Encoding.UTF8);
            if (body == null)
                return req.CreateResponse(HttpStatusCode.BadRequest);

            const string pgnExtractPath = "pgn-extract";
            await SetPermissionsAsync(pgnExtractPath, "644");
            var stdOutBuffer = new StringBuilder();
            await Cli.Wrap(pgnExtractPath)
                .WithArguments("-WxolalgPNBRQK") // enhanced long algebraic
                .WithStandardInputPipe(PipeSource.FromString(body))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                .ExecuteBufferedAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString(stdOutBuffer.ToString());

            return response;
        }

        public static async ValueTask SetPermissionsAsync(string filePath, string permissions) =>
            await Cli.Wrap("/bin/bash")
            .WithArguments(new[] { "-c", $"chmod {permissions} {filePath}" })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }
}
