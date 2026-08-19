namespace CsTrees.MEAI;

/// <summary>
/// 行为树工具调用的结构化返回值。
/// MAF 会将此对象序列化为 JSON 返回给 LLM。
/// </summary>
public class ToolResult
{
    /// <summary>
    /// 操作确认消息（如"已添加披萨动作"、"已关闭作用域"等）。
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// ASCII 树渲染文本。为 null 表示无树可显示（如尚未开始构建）。
    /// CompactResultChatClient 通过此字段定位并去重树预览。
    /// </summary>
    public string? Tree { get; set; }

    /// <summary>
    /// Blackboard 当前状态渲染文本。为 null 表示无 blackboard 数据。
    /// </summary>
    public string? Blackboard { get; set; }
}
