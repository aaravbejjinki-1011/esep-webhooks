
using System;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(
    Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient Client = new HttpClient();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        var json = JObject.Parse(input.ToString() ?? "{}");

        // Ignore requests without an issue.
        if (json["issue"] == null)
        {
            return "ignored";
        }

        // Only notify Slack when an issue is opened.
        var action = json["action"]?.ToString();
        if (action != null && action != "opened")
        {
            return "ignored";
        }

        string? issueUrl = json["issue"]?["html_url"]?.ToString();

        if (string.IsNullOrWhiteSpace(issueUrl))
        {
            return "missing issue URL";
        }

        string? slackUrl =
            Environment.GetEnvironmentVariable("SLACK_URL");

        if (string.IsNullOrWhiteSpace(slackUrl))
        {
            throw new Exception("SLACK_URL is not configured");
        }

        var payload = JsonConvert.SerializeObject(new
        {
            text = $"Issue Created: {issueUrl}"
        });

        using var content = new StringContent(
            payload,
            Encoding.UTF8,
            "application/json"
        );

        using var response = Client.PostAsync(
            slackUrl,
            content
        ).GetAwaiter().GetResult();

        response.EnsureSuccessStatusCode();

        return response.Content.ReadAsStringAsync()
            .GetAwaiter().GetResult();
    }
}
