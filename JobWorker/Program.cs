// Subscriber.csproj
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
// Stream 1: LIMITS_STREAM の受信
// -----------------------------------------
var limitsStreamName = "LIMITS_STREAM";
//var limitsConsumerName = "limits_consumer";
var limitsConsumerName = $"limits_consumer-{SanitizeName(Environment.MachineName)}";
var limitsSubjectName = "limits.>";

var limitsConsumerConfig = new ConsumerConfig
{
    Name = limitsConsumerName,
    DurableName = limitsConsumerName,
    AckPolicy = ConsumerConfigAckPolicy.Explicit,
    FilterSubject = limitsSubjectName
};
var limitsConsumer = await js.CreateOrUpdateConsumerAsync(limitsStreamName, limitsConsumerConfig);
Console.WriteLine($"Durable Consumer for {limitsStreamName} created.");

// 両方のストリームを並行して処理
var limitsProcessingTask = Task.Run(async () =>
{
    await foreach (var msg in limitsConsumer.ConsumeAsync<TaskMessage>())
    {
        Console.WriteLine($"[LIMITS_STREAM] Received: TaskId={msg.Data.TaskId}, Description={msg.Data.Description}");
        await msg.AckAsync();
    }
});

// -----------------------------------------
// Stream 2: WORKQUEUE_STREAM の受信
// -----------------------------------------
var workqueueStreamName = "WORKQUEUE_STREAM";
var workqueueConsumerName = "workqueue_consumer";
var workqueueSubjectName = "workqueue.>";

var workqueueConsumerConfig = new ConsumerConfig
{
    Name = workqueueConsumerName,
    DurableName = workqueueConsumerName,
    AckPolicy = ConsumerConfigAckPolicy.Explicit,
    FilterSubject = workqueueSubjectName
};
var workqueueConsumer = await js.CreateOrUpdateConsumerAsync(workqueueStreamName, workqueueConsumerConfig);
Console.WriteLine($"Durable Consumer for {workqueueStreamName} created.");

var workqueueProcessingTask = Task.Run(async () =>
{
    await foreach (var msg in workqueueConsumer.ConsumeAsync<TaskMessage>())
    {
        Console.WriteLine($"[WORKQUEUE_STREAM] Received: TaskId={msg.Data.TaskId}, Description={msg.Data.Description}");
        await msg.AckAsync();
    }
});

await Task.WhenAll(limitsProcessingTask, workqueueProcessingTask);
Console.WriteLine("Subscriber finished.");

string SanitizeName(string name)
{
    var invalidChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', 'ー' };
    foreach (var ch in invalidChars)
    {
        name = name.Replace(ch, '_'); // 無効な文字を '_' に置換
    }
    return name;
}

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
