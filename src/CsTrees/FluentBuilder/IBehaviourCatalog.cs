namespace CsTrees.FluentBuilder
{
    /// <summary>
    /// 编译期锚点接口，用于向 Source Generator 提供行为目录类型 <typeparamref name="TCatalog"/>。
    /// </summary>
    /// <typeparam name="TCatalog">
    /// 行为目录类型，其实例方法会被 SG 识别为行为树节点工厂。
    /// 接口不约束 <typeparamref name="TCatalog"/> 的实例化方式：实现方可自行 new、
    /// 通过构造注入、共享单例等任意途径提供 Catalog 实例。
    /// </typeparam>
    public interface IBehaviourCatalog<TCatalog> where TCatalog : class
    {
        /// <summary>
        /// 行为目录的运行时实例，供 SG 生成的 Builder 方法调用节点工厂。
        /// 由实现方提供，接口不规定其创建方式。
        /// </summary>
        TCatalog Catalog { get; }
    }
}
