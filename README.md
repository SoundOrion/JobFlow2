# Publisher & Subscriber

## 概要

このプロジェクトは、NATS JetStreamを使用してメッセージの送信および受信を行うシステムです。

- **Publisher**: メッセージを異なるストリームに送信する役割を担います。
- **Subscriber**: ストリームからメッセージを消費し、並行処理を行います。

## 特徴

- **NATS JetStream** を活用したストリームベースのメッセージング。
- 並列処理による複数ストリームの同時処理。
- メッセージ耐久性を確保するための明示的なACK。
- ストリームごとに異なる設定（例: Retentionポリシー、最大メッセージ数など）。

## 必要な環境

- .NET 6 以上
- [NATSサーバ](https://nats.io/) (JetStream対応)
- NuGet パッケージ: `NATS.Net`

## インストール方法

1. 必要なNuGetパッケージをインストール:

```bash
$ dotnet add package NATS.Net
```

2. 必要なプロジェクトを準備:
   - **Publisher**
   - **Subscriber**

## Publisher の概要

Publisherは以下の処理を行います:

1. **ストリーム作成**: JetStream上に2つのストリームを作成します。
    - **LIMITS_STREAM**: `limits.>` サブジェクト用。
    - **WORKQUEUE_STREAM**: `workqueue.>` サブジェクト用。
2. **メッセージ送信**: 各ストリームに関連するサブジェクトにメッセージを送信します。

### 使用方法

Publisherのコード:

```csharp
var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
await using var nc = new NatsClient(url);
await nc.ConnectAsync();

var js = nc.CreateJetStreamContext();

// LIMITS_STREAM 作成とメッセージ送信
var limitsStreamConfig = new StreamConfig(name: "LIMITS_STREAM", subjects: new[] { "limits.>" })
{
    Retention = StreamConfigRetention.Limits,
    MaxMsgs = 1000,
    MaxBytes = 1024 * 1024,
    MaxAge = TimeSpan.FromHours(24)
};
await js.CreateStreamAsync(limitsStreamConfig);
await js.PublishAsync(subject: "limits.task1", data: new TaskMessage { TaskId = 1, Description = "Task 1 for Limits" });

// WORKQUEUE_STREAM 作成とメッセージ送信
var workqueueStreamConfig = new StreamConfig(name: "WORKQUEUE_STREAM", subjects: new[] { "workqueue.>" })
{
    Retention = StreamConfigRetention.Workqueue,
    MaxMsgs = 500,
    MaxBytes = 512 * 1024,
    MaxAge = TimeSpan.FromHours(12)
};
await js.CreateStreamAsync(workqueueStreamConfig);
await js.PublishAsync(subject: "workqueue.task1", data: new TaskMessage { TaskId = 1, Description = "Task 1 for Workqueue" });

Console.WriteLine("Publisher finished.");
```

## Subscriber の概要

Subscriberは以下の処理を行います:

1. **ストリームの設定**: `LIMITS_STREAM`と`WORKQUEUE_STREAM`のそれぞれに対してDurable Consumerを設定します。
2. **メッセージ消費**: 両ストリームから並行してメッセージを受信し処理します。

### 使用方法

Subscriberのコード:

```csharp
var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://127.0.0.1:4222";
await using var nc = new NatsClient(url);
await nc.ConnectAsync();

var js = nc.CreateJetStreamContext();

// LIMITS_STREAM 設定と処理
var limitsConsumerConfig = new ConsumerConfig
{
    Name = "limits_consumer",
    DurableName = "limits_consumer",
    AckPolicy = ConsumerConfigAckPolicy.Explicit,
    FilterSubject = "limits.>"
};
var limitsConsumer = await js.CreateOrUpdateConsumerAsync("LIMITS_STREAM", limitsConsumerConfig);

var limitsProcessingTask = Task.Run(async () =>
{
    await foreach (var msg in limitsConsumer.ConsumeAsync<TaskMessage>())
    {
        Console.WriteLine($"[LIMITS_STREAM] Received: TaskId={msg.Data.TaskId}, Description={msg.Data.Description}");
        await msg.AckAsync();
    }
});

// WORKQUEUE_STREAM 設定と処理
var workqueueConsumerConfig = new ConsumerConfig
{
    Name = "workqueue_consumer",
    DurableName = "workqueue_consumer",
    AckPolicy = ConsumerConfigAckPolicy.Explicit,
    FilterSubject = "workqueue.>"
};
var workqueueConsumer = await js.CreateOrUpdateConsumerAsync("WORKQUEUE_STREAM", workqueueConsumerConfig);

var workqueueProcessingTask = Task.Run(async () =>
{
    await foreach (var msg in workqueueConsumer.ConsumeAsync<TaskMessage>())
    {
        Console.WriteLine($"[WORKQUEUE_STREAM] Received: TaskId={msg.Data.TaskId}, Description={msg.Data.Description}");
        await msg.AckAsync();
    }
});

await Task.WhenAll(limitsProcessingTask, workqueueProcessingTask);
```

## データモデル

### TaskMessage

```csharp
public record TaskMessage
{
    [JsonPropertyName("task_id")]
    public int TaskId { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; }
}
```

## 実行方法

1. NATSサーバを起動します。

```bash
$ nats-server --jetstream
```

2. Publisherを実行してメッセージを送信します。

```bash
$ dotnet run --project Publisher
```

3. Subscriberを実行してメッセージを受信します。

```bash
$ dotnet run --project Subscriber
```

## 貢献

バグ報告や機能リクエストは [Issues](https://github.com/your-repository/issues) で受け付けています。

## ライセンス

このプロジェクトはMITライセンスのもとで提供されています。