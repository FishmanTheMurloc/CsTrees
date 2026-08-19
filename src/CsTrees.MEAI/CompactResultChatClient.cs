using System.Text.Json;
using Microsoft.Extensions.AI;

namespace CsTrees.MEAI;

/// <summary>
/// IChatClient 装饰器，去除冗余的行为树预览。
/// 每次发给 LLM 的消息中，可能包含多个工具调用结果（FunctionResultContent），
/// 其中每个都可能含 Tree 字段。本装饰器只保留最后一个的 Tree 字段，
/// 将其余的 Tree 置为 null，大幅减少 token 消耗。
/// 跨轮次也有效：因为每次 API 调用都发送完整历史，所以修改历史中的结果不影响 LLM。
/// </summary>
public sealed class CompactResultChatClient : DelegatingChatClient
{
    /// <summary>
    /// 创建包装指定内部客户端的实例。
    /// </summary>
    /// <param name="innerClient">被装饰的内部 IChatClient。</param>
    public CompactResultChatClient(IChatClient innerClient) : base(innerClient) { }

    /// <summary>
    /// 压缩历史中的树预览后，调用内部客户端获取非流式响应。
    /// </summary>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var compacted = CompactTreePreviews(messages);
        return await base.GetResponseAsync(compacted, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 压缩历史中的树预览后，调用内部客户端获取流式响应。
    /// </summary>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var compacted = CompactTreePreviews(messages);
        await foreach (var update in base.GetStreamingResponseAsync(compacted, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// 遍历消息，找到所有含 Tree 字段的 FunctionResultContent，
    /// 只保留最后一个的 Tree 值，将其余的 Tree 设为 null。
    /// </summary>
    private static List<ChatMessage> CompactTreePreviews(IEnumerable<ChatMessage> messages)
    {
        var msgList = messages as List<ChatMessage> ?? messages.ToList();

        // 第一遍：找到最后一个含 Tree 字段的 FunctionResultContent 索引
        int lastPreviewMsgIndex = -1;
        int lastPreviewContentIndex = -1;

        for (int mi = 0; mi < msgList.Count; mi++)
        {
            var msg = msgList[mi];
            for (int ci = 0; ci < msg.Contents.Count; ci++)
            {
                if (msg.Contents[ci] is FunctionResultContent frc
                    && HasTreeField(frc))
                {
                    lastPreviewMsgIndex = mi;
                    lastPreviewContentIndex = ci;
                }
            }
        }

        // 没有预览或只有一个，无需处理
        if (lastPreviewMsgIndex == -1)
            return msgList;

        // 第二遍：替换需要去重的 FunctionResultContent
        var result = new List<ChatMessage>(msgList.Count);
        for (int mi = 0; mi < msgList.Count; mi++)
        {
            var msg = msgList[mi];
            bool needsModification = false;

            for (int ci = 0; ci < msg.Contents.Count; ci++)
            {
                if (msg.Contents[ci] is FunctionResultContent frc
                    && HasTreeField(frc)
                    && !(mi == lastPreviewMsgIndex && ci == lastPreviewContentIndex))
                {
                    needsModification = true;
                    break;
                }
            }

            if (!needsModification)
            {
                result.Add(msg);
                continue;
            }

            // 重建这条消息，将非最后的 FunctionResultContent 中的 Tree 字段置为 null
            var newContents = new List<AIContent>();
            for (int ci = 0; ci < msg.Contents.Count; ci++)
            {
                var content = msg.Contents[ci];
                if (content is FunctionResultContent frc
                    && HasTreeField(frc)
                    && !(mi == lastPreviewMsgIndex && ci == lastPreviewContentIndex))
                {
                    newContents.Add(StripTreeField(frc));
                }
                else
                {
                    newContents.Add(content);
                }
            }

            var newMsg = new ChatMessage(msg.Role, newContents)
            {
                AuthorName = msg.AuthorName,
                RawRepresentation = msg.RawRepresentation,
            };
            foreach (var kvp in msg.AdditionalProperties ?? [])
            {
                newMsg.AdditionalProperties ??= new();
                newMsg.AdditionalProperties[kvp.Key] = kvp.Value;
            }
            result.Add(newMsg);
        }

        return result;
    }

    /// <summary>
    /// 检查 FunctionResultContent 是否含非 null 的 Tree 字段。
    /// AIFunctionFactory 会将 ToolResult 序列化为 JsonElement（camelCase，键为 "tree"），
    /// 所以 Result 实际类型是 JsonElement。
    /// </summary>
    private static bool HasTreeField(FunctionResultContent frc)
    {
        return frc.Result is JsonElement je
            && je.ValueKind == JsonValueKind.Object
            && je.TryGetProperty("tree", out var treeEl)
            && treeEl.ValueKind != JsonValueKind.Null;
    }

    /// <summary>
    /// 创建一个新的 FunctionResultContent，移除 Tree 字段。
    /// 关键：将结果序列化为 JSON 字符串而非保留 JsonElement，
    /// 这样 OpenAIChatClient 会用 "result as string" 直接取到字符串，
    /// 避免二次序列化导致 Tree 字段又被包含。
    /// </summary>
    private static FunctionResultContent StripTreeField(FunctionResultContent frc)
    {
        // Result 已由 HasTreeField 确认为含非空 "tree" 的 JsonElement（camelCase，键为 "tree"）
        if (frc.Result is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(je);
                if (dict is not null && dict.ContainsKey("tree"))
                {
                    dict.Remove("tree"); // 完全移除 tree 字段
                    var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    });
                    return new FunctionResultContent(frc.CallId, json);
                }
            }
            catch { }
        }

        // 无法处理，原样返回
        return frc;
    }
}
