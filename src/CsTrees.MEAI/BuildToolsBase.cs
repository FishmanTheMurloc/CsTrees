using CsTrees.Blackboard;
using CsTrees.FluentBuilder;
using System.ComponentModel;
using System.Text;
using TreeDisplay = CsTrees.Display.Display;

namespace CsTrees.MEAI
{
    /// <summary>
    /// 工具调用宿主基类。继承此类的 partial 类会被 Source Generator 自动处理，
    /// 为 <c>TBuilder</c> 的每个 IBehaviourCatalog 工厂方法生成对应的工具方法。
    /// </summary>
    /// <remarks>
    /// 生成的方法会检查 <c>builtTree</c> 状态，委托调用 <c>builder</c> 上的同名方法，
    /// 返回带 ASCII 树预览和 Blackboard 状态的 <c>ToolResult</c>。
    /// </remarks>
    /// <typeparam name="TBuilder">关联的 TreeBuilder 类型。SG 将根据此类型的 IBehaviourCatalog 成员生成工具调用方法。</typeparam>
    public abstract class BuildToolsBase<TBuilder> where TBuilder : TreeBuilder<TBuilder>
    {
        /// <summary>
        /// 关联的 TreeBuilder 实例。SG 生成的工具方法会将调用委托到此属性的同名方法上。
        /// </summary>
        protected readonly TBuilder builder;

        /// <summary>
        /// 已完成的树根节点。为 <c>null</c> 时树仍在构建中；非 <c>null</c> 时后续工具调用将返回错误。
        /// </summary>
        protected Behaviour? builtTree;

        /// <summary>
        /// 构造时记录的检查点。用于 <see cref="ResetTree"/> 回滚到初始状态。
        /// </summary>
        readonly int _checkpoint;

        /// <summary>
        /// 初始化工具调用宿主，设置 builder 并记录初始检查点。
        /// </summary>
        /// <param name="builder">关联的 TreeBuilder 实例。</param>
        protected BuildToolsBase(TBuilder builder)
        {
            this.builder = builder;
            this._checkpoint = builder.Checkpoint();
        }

        /// <summary>
        /// 构造成功结果：附带当前树预览（BuiltTree 优先，否则渲染 builder.Preview()）
        /// 与 Blackboard 状态文本。
        /// </summary>
        /// <param name="message">操作确认消息。</param>
        /// <param name="showStatus">显示树运行状态。</param>
        protected virtual ToolResult FormatStatus(string message, bool showStatus = false)
        {
            string? tree = null;
            if (this.builtTree is not null)
            {
                tree = TreeDisplay.AsciiTree(this.builtTree, showStatus: showStatus, showFeedbackMessage: true);
            }
            else
            {
                try
                {
                    tree = TreeDisplay.AsciiTree(this.builder.Preview(), showStatus: false, showFeedbackMessage: true);
                }
                catch (InvalidOperationException)
                {
                    // builder 尚无任何节点时 Preview 抛出，保持 tree 为 null
                }
            }

            string? blackboard = null;
            var bb = builder.GetCurrentBlackboard();
            if (bb is not null)
            {
                var items = bb.GetItems();
                if (items.Any())
                {
                    blackboard = TreeDisplay.AsciiBlackboard(items);
                }
            }

            return new ToolResult { Message = message, Tree = tree, Blackboard = blackboard };
        }

        /// <summary>
        /// 构造错误结果，Message 以"错误:"前缀标识。
        /// </summary>
        protected virtual ToolResult Error(string message) => new() { Message = $"错误: {message}" };

        /// <summary>
        /// 关闭当前打开的节点作用域并返回上一级。根作用域关闭后树构建完成。
        /// </summary>
        [Description("关闭当前打开的作用域。每个 push 操作必须对应一个 end_scope。所有作用域关闭后才可 build_tree。")]
        public virtual ToolResult End()
        {
            if (builtTree is not null)
                return Error("树已构建，不能再修改。");
            try
            {
                builder.End();
                return FormatStatus("已关闭当前作用域");
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }
        }

        /// <summary>
        /// 构建行为树并返回其 ASCII 树形结构。会自动关闭剩余的所有打开作用域并生成树根节点。
        /// 构建完成后不能再添加或修改节点，需调用 <see cref="ResetTree"/> 才能重建。
        /// </summary>
        [Description("构建行为树并显示其 ASCII 树形结构。会顺带关闭剩下所有作用域。此操作后不能再添加节点，需 reset_tree 才能重建。")]
        public virtual ToolResult BuildTree()
        {
            if (builtTree is not null)
                return FormatStatus("树已构建。如需重建请先调用 reset_tree。");

            try
            {
                while (builder.FrameCount > 0)
                    builder.End();
                builtTree = builder.Build();
                return FormatStatus("行为树构建成功！");
            }
            catch (InvalidOperationException ex)
            {
                return Error($"构建失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 查看当前行为树的构建状态与树形预览。只读操作，不修改任何状态，
        /// 可随时调用来了解当前构建进度和树的形状。
        /// </summary>
        [Description("查看当前行为树的构建状态及在建树的预览。不执行任何修改操作，可随时调用来了解当前进度和树的形状。")]
        public virtual ToolResult ShowTreeStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine(builtTree is null ? "树未构建" : "树已构建");
            if (builtTree is null)
                sb.AppendLine("[所有作用域已关闭，可以构建]");

            return FormatStatus(sb.ToString());
        }

        /// <summary>
        /// 运行已构建的行为树若干次 tick，返回最终的树形状态。
        /// 树必须已通过 <see cref="BuildTree"/> 构建后才可运行。
        /// </summary>
        [Description("运行已构建的行为树，返回每次 tick 的树根状态（Success/Failure/Running）。树必须先 build_tree 后才能运行。")]
        public virtual async Task<ToolResult> RunTree()
        {
            if (builtTree is null)
                return Error("树尚未构建。请先调用 build_tree 构建行为树后再运行。");

            var random = new Random();
            int tickCount = random.Next(1, 6); // 1~5

            for (int i = 0; i < tickCount; i++)
            {
                await builtTree.TickOnce();
            }

            return FormatStatus($"运行完成", showStatus: true);
        }

        /// <summary>
        /// 重置构建器到初始检查点，清空黑板与已构建的树，使行为树可以从头重新构建。
        /// </summary>
        [Description("重置行为树构建器，清除所有已添加的节点和状态，从头开始。")]
        public ToolResult ResetTree()
        {
            builder.GetCurrentBlackboard()?.Clear();
            builder.ResetTo(_checkpoint);
            builtTree = null;
            return new ToolResult { Message = "构建器已重置，可以从头开始构建行为树。" };
        }
    }
}
