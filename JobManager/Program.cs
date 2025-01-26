// Publisher.csproj
// Install NuGet package `NATS.Net`
using System.Text.Json.Serialization;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
await using var nc = new NatsClient(url);
await nc.ConnectAsync();

var js = nc.CreateJetStreamContext();

// -----------------------------------------
// Stream 1: LIMITS_STREAM
// -----------------------------------------
var limitsStreamName = "LIMITS_STREAM";
var limitsSubjectName = "limits.>";
var limitsStreamConfig = new StreamConfig(name: limitsStreamName, subjects: new[] { limitsSubjectName })
{
    Retention = StreamConfigRetention.Limits,
    MaxMsgs = 1000,
    MaxBytes = 1024 * 1024,
    MaxAge = TimeSpan.FromHours(24)
};
await js.CreateStreamAsync(limitsStreamConfig);
Console.WriteLine($"Stream '{limitsStreamName}' created.");

// LIMITS_STREAM メッセージ送信
await js.PublishAsync(subject: "limits.task1", data: new TaskMessage { TaskId = 1, Description = "Task 1 for Limits" });
await js.PublishAsync(subject: "limits.task2", data: new TaskMessage { TaskId = 2, Description = "Task 2 for Limits" });
await js.PublishAsync(subject: "limits.task3", data: new TaskMessage { TaskId = 3, Description = "Task 3 for Limits" });
Console.WriteLine("Messages published to LIMITS_STREAM.");

// -----------------------------------------
// Stream 2: WORKQUEUE_STREAM
// -----------------------------------------
var workqueueStreamName = "WORKQUEUE_STREAM";
var workqueueSubjectName = "workqueue.>";
var workqueueStreamConfig = new StreamConfig(name: workqueueStreamName, subjects: new[] { workqueueSubjectName })
{
    Retention = StreamConfigRetention.Workqueue,
    MaxMsgs = 500,
    MaxBytes = 512 * 1024,
    MaxAge = TimeSpan.FromHours(12)
};
await js.CreateStreamAsync(workqueueStreamConfig);
Console.WriteLine($"Stream '{workqueueStreamName}' created.");

// WORKQUEUE_STREAM メッセージ送信
await js.PublishAsync(subject: "workqueue.task1", data: new TaskMessage { TaskId = 1, Description = "Task 1 for Workqueue" });
await js.PublishAsync(subject: "workqueue.task2", data: new TaskMessage { TaskId = 2, Description = "Task 2 for Workqueue" });
await js.PublishAsync(subject: "workqueue.task3", data: new TaskMessage { TaskId = 3, Description = "Task 3 for Workqueue" });
Console.WriteLine("Messages published to WORKQUEUE_STREAM.");

Console.WriteLine("Publisher finished.");

// -----------------------------------------
// モデルクラス
// -----------------------------------------
public record TaskMessage
{
    [JsonPropertyName("task_id")]
    public int TaskId { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
