using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

namespace BlazorAppJobManager.Services;

public class JobService : IAsyncDisposable
{
    private readonly INatsClient _natsClient;
    private readonly INatsJSContext _jsContext;

    private const string LimitsStreamName = "LIMITS_STREAM";
    private const string LimitsSubjectName = "limits.>";
    private const string WorkqueueStreamName = "WORKQUEUE_STREAM";
    private const string WorkqueueSubjectName = "workqueue.>";

    public JobService()
    {
        var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
        _natsClient = new NatsClient(url);
        _jsContext = _natsClient.CreateJetStreamContext();

        // Connect to NATS server and initialize streams
        Task.Run(async () =>
        {
            await _natsClient.ConnectAsync();
            await InitializeStreamsAsync();
        });
    }

    private async Task InitializeStreamsAsync()
    {
        // Create LIMITS_STREAM
        var limitsStreamConfig = new StreamConfig(name: LimitsStreamName, subjects: new[] { LimitsSubjectName })
        {
            Retention = StreamConfigRetention.Limits,
            MaxMsgs = 1000,
            MaxBytes = 1024 * 1024,
            MaxAge = TimeSpan.FromHours(24)
        };
        await _jsContext.CreateStreamAsync(limitsStreamConfig);

        // Create WORKQUEUE_STREAM
        var workqueueStreamConfig = new StreamConfig(name: WorkqueueStreamName, subjects: new[] { WorkqueueSubjectName })
        {
            Retention = StreamConfigRetention.Workqueue,
            MaxMsgs = 500,
            MaxBytes = 512 * 1024,
            MaxAge = TimeSpan.FromHours(12)
        };
        await _jsContext.CreateStreamAsync(workqueueStreamConfig);
    }

    public async Task PublishToLimitsStreamAsync(TaskMessage message)
    {
        await _jsContext.PublishAsync(subject: "limits." + message.TaskId, data: message);
    }

    public async Task PublishToWorkqueueStreamAsync(TaskMessage message)
    {
        await _jsContext.PublishAsync(subject: "workqueue." + message.TaskId, data: message);
    }

    public async ValueTask DisposeAsync()
    {
        await _natsClient.DisposeAsync();
    }
}

public record TaskMessage
{
    [JsonPropertyName("task_id")]
    public int TaskId { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}