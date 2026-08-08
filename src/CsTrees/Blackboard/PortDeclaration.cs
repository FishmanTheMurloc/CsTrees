namespace CsTrees.Blackboard;

/// <summary>
/// 描述一个端口的声明信息（键名、类型、访问级别），用于自省和可视化。
/// </summary>
public sealed class PortDeclaration
{
    /// <summary>默认的 Blackboard 键名。</summary>
    public string DefaultKey { get; }

    /// <summary>端口值的类型。</summary>
    public Type ValueType { get; }

    /// <summary>端口访问级别。</summary>
    public Access Access { get; }

    /// <summary>
    /// 初始化 <see cref="PortDeclaration"/> 的新实例。
    /// </summary>
    /// <param name="defaultKey">默认的 Blackboard 键名。</param>
    /// <param name="valueType">端口值的类型。</param>
    /// <param name="access">端口访问级别。</param>
    public PortDeclaration(string defaultKey, Type valueType, Access access)
    {
        DefaultKey = defaultKey;
        ValueType = valueType;
        Access = access;
    }
}
