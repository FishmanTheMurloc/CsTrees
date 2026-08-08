using System;

namespace CsTrees.Blackboard;

/// <summary>
/// 标记 Behaviour 的属性为 Blackboard 端口。
/// Source Generator 将为带有此特性的属性生成 <c>SetupPorts</c> 和 <c>Create</c> 方法。
/// <para>属性类型必须为 <see cref="BehaviourKeyAccess{T}"/>。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BlackboardKeyAttribute : Attribute
{
    /// <summary>
    /// Blackboard 上的默认键名。如果为 <c>null</c>，则使用属性名作为键名。
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// 端口访问级别，决定 Blackboard 访问权限。默认为 <see cref="Access.Write"/>。
    /// </summary>
    public Access Access { get; set; } = Access.Write;

    /// <summary>
    /// 初始化 <see cref="BlackboardKeyAttribute"/> 的新实例，使用默认值。
    /// </summary>
    public BlackboardKeyAttribute() { }

    /// <summary>
    /// 初始化 <see cref="BlackboardKeyAttribute"/> 的新实例并指定 Blackboard 键名。
    /// </summary>
    /// <param name="key">Blackboard 上的键名。</param>
    public BlackboardKeyAttribute(string key)
    {
        Key = key;
    }
}
