namespace Tamp.Kubectl;

/// <summary>
/// Facade for the <c>kubectl</c> CLI. Every verb consumes a <c>kubectl</c>
/// <see cref="Tool"/>; kubectl is tool-bound (one binary, one CLI surface).
/// </summary>
/// <remarks>
/// <para>Resolve the tool via <c>[FromPath("kubectl")]</c>:</para>
/// <code>
/// [FromPath("kubectl")] readonly Tool KubectlBin = null!;
///
/// Target Deploy => _ => _.Executes(() => Kubectl.Apply(KubectlBin, s => s
///     .SetNamespace("prod")
///     .SetContext("prod-cluster")
///     .AddFile("manifests/")
///     .SetRecursive()));
/// </code>
/// <para>
/// Each verb exposes both a fluent <c>(Tool, Action&lt;TSettings&gt;)</c> overload
/// (canonical in docs and templates) and an object-init <c>(Tool, TSettings)</c>
/// overload. Both produce identical <see cref="CommandPlan"/>s.
/// </para>
/// <para>
/// Common knobs (context / namespace / kubeconfig / impersonation / dry-run / output
/// format / verbosity / request timeout) live on <see cref="KubectlSettingsBase"/>
/// and are inherited by every verb's settings class. <c>KUBECONFIG</c> flows as
/// an environment variable rather than a CLI flag so the path stays out of the
/// OS process table.
/// </para>
/// </remarks>
public static class Kubectl
{
    /// <summary><c>kubectl apply -f &lt;file&gt; [-k &lt;kustomize&gt;]</c> — declarative create-or-update.</summary>
    public static CommandPlan Apply(Tool kubectl, Action<KubectlApplySettings> configure) => Build(kubectl, configure);
    public static CommandPlan Apply(Tool kubectl, KubectlApplySettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl get &lt;resource&gt; [&lt;name&gt;...]</c> — query the API server.</summary>
    public static CommandPlan Get(Tool kubectl, Action<KubectlGetSettings> configure) => Build(kubectl, configure);
    public static CommandPlan Get(Tool kubectl, KubectlGetSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl delete &lt;resource&gt; [&lt;name&gt;...]</c> — remove resources.</summary>
    public static CommandPlan Delete(Tool kubectl, Action<KubectlDeleteSettings> configure) => Build(kubectl, configure);
    public static CommandPlan Delete(Tool kubectl, KubectlDeleteSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl describe &lt;resource&gt; [&lt;name&gt;]</c> — human-readable resource detail.</summary>
    public static CommandPlan Describe(Tool kubectl, Action<KubectlDescribeSettings> configure) => Build(kubectl, configure);
    public static CommandPlan Describe(Tool kubectl, KubectlDescribeSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl rollout status &lt;resource&gt;</c> — wait for / report rollout progress.</summary>
    public static CommandPlan RolloutStatus(Tool kubectl, Action<KubectlRolloutStatusSettings> configure) => Build(kubectl, configure);
    public static CommandPlan RolloutStatus(Tool kubectl, KubectlRolloutStatusSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl rollout restart &lt;resource&gt;</c> — trigger a rolling restart by patching the template.</summary>
    public static CommandPlan RolloutRestart(Tool kubectl, Action<KubectlRolloutRestartSettings> configure) => Build(kubectl, configure);
    public static CommandPlan RolloutRestart(Tool kubectl, KubectlRolloutRestartSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl rollout undo &lt;resource&gt; [--to-revision N]</c> — roll back to a previous revision.</summary>
    public static CommandPlan RolloutUndo(Tool kubectl, Action<KubectlRolloutUndoSettings> configure) => Build(kubectl, configure);
    public static CommandPlan RolloutUndo(Tool kubectl, KubectlRolloutUndoSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl port-forward &lt;resource&gt; &lt;ports...&gt;</c> — local port → remote port tunnel.</summary>
    public static CommandPlan PortForward(Tool kubectl, Action<KubectlPortForwardSettings> configure) => Build(kubectl, configure);
    public static CommandPlan PortForward(Tool kubectl, KubectlPortForwardSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl logs &lt;pod-or-resource&gt;</c> — stream container logs.</summary>
    public static CommandPlan Logs(Tool kubectl, Action<KubectlLogsSettings> configure) => Build(kubectl, configure);
    public static CommandPlan Logs(Tool kubectl, KubectlLogsSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl exec &lt;pod&gt; -- &lt;command&gt;</c> — execute a command in a container.</summary>
    public static CommandPlan Exec(Tool kubectl, Action<KubectlExecSettings> configure) => Build(kubectl, configure);
    public static CommandPlan Exec(Tool kubectl, KubectlExecSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl scale &lt;resource&gt; --replicas N</c> — set replica count.</summary>
    public static CommandPlan Scale(Tool kubectl, Action<KubectlScaleSettings> configure) => Build(kubectl, configure);
    public static CommandPlan Scale(Tool kubectl, KubectlScaleSettings settings) => Plan(kubectl, settings);

    /// <summary><c>kubectl set image &lt;resource&gt; &lt;container&gt;=&lt;image&gt;</c> — update container image(s).</summary>
    public static CommandPlan SetImage(Tool kubectl, Action<KubectlSetImageSettings> configure) => Build(kubectl, configure);
    public static CommandPlan SetImage(Tool kubectl, KubectlSetImageSettings settings) => Plan(kubectl, settings);

    // ---- Internal helpers ----

    private static CommandPlan Build<T>(Tool tool, Action<T> configure) where T : KubectlSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new T();
        configure(s);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Plan<T>(Tool tool, T settings) where T : KubectlSettingsBase
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }
}
