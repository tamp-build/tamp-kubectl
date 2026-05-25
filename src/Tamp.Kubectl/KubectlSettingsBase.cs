namespace Tamp.Kubectl;

/// <summary>
/// kubectl <c>--dry-run</c> strategy. <see cref="None"/> omits the flag entirely.
/// </summary>
public enum KubectlDryRunStrategy
{
    /// <summary>Omit <c>--dry-run</c> (the kubectl default; the command runs).</summary>
    None,
    /// <summary><c>--dry-run=client</c> — local validation, no API server contact.</summary>
    Client,
    /// <summary><c>--dry-run=server</c> — server-side dry-run; admission controllers see the request.</summary>
    Server,
}

/// <summary>
/// Common base for <c>kubectl &lt;verb&gt;</c> settings. Cross-cutting knobs that
/// apply to every verb: <c>--context</c>, <c>--namespace</c>, <c>--kubeconfig</c>
/// (via env var so the path stays out of argv), <c>--as</c> / <c>--as-group</c>,
/// <c>--dry-run</c>, <c>-o</c> output format, <c>-v</c> verbosity, working directory,
/// arbitrary env vars.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Tool-bound.</strong> Every verb consumes a single <c>kubectl</c>
/// <see cref="Tool"/>; the executable comes from the tool, not from the settings.
/// Mirrors the <c>Tamp.Helm.V3.HelmSettingsBase</c> shape.
/// </para>
/// <para>
/// <strong>Kubeconfig handling.</strong> When <see cref="Kubeconfig"/> is set, the
/// wrapper emits the path as the <c>KUBECONFIG</c> environment variable on the
/// child process rather than as a <c>--kubeconfig</c> CLI argument. This keeps
/// the path out of the OS process table for the lifetime of the kubectl
/// invocation, matching the standard Tamp secret-handling posture for paths
/// adjacent to credentials.
/// </para>
/// </remarks>
public abstract class KubectlSettingsBase
{
    /// <summary>Working directory for the spawned process. Defaults to the tool's working directory.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Per-invocation environment variables (in addition to <c>KUBECONFIG</c> when <see cref="Kubeconfig"/> is set).</summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>kubeconfig context to target (<c>--context</c>).</summary>
    public string? Context { get; set; }

    /// <summary>Namespace scope (<c>-n</c> / <c>--namespace</c>). Mutually exclusive with <see cref="AllNamespaces"/> on verbs that support both.</summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Path to a non-default kubeconfig file. Emitted as the <c>KUBECONFIG</c>
    /// environment variable rather than a CLI flag so the path stays out of the
    /// OS process table. Multi-path (path1:path2 on POSIX, path1;path2 on Windows)
    /// is supported per the upstream kubeconfig merge semantics.
    /// </summary>
    public string? Kubeconfig { get; set; }

    /// <summary>Impersonate as user (<c>--as</c>).</summary>
    public string? ImpersonateUser { get; set; }

    /// <summary>Impersonate as group (<c>--as-group</c>, repeatable).</summary>
    public List<string> ImpersonateGroups { get; } = new();

    /// <summary>Dry-run strategy (<c>--dry-run=client</c> / <c>--dry-run=server</c>).</summary>
    public KubectlDryRunStrategy DryRun { get; set; } = KubectlDryRunStrategy.None;

    /// <summary>Output format (<c>-o</c>). Common values: <c>json</c>, <c>yaml</c>, <c>wide</c>, <c>name</c>, <c>jsonpath=...</c>, <c>go-template=...</c>, <c>custom-columns=...</c>.</summary>
    public string? Output { get; set; }

    /// <summary>klog verbosity (<c>-v</c>). Useful values 0–10; 6+ shows HTTP traffic.</summary>
    public int? Verbosity { get; set; }

    /// <summary>Suppress confirmation prompts where kubectl asks for them (<c>--request-timeout</c> isn't this; this is a behavioral default for non-prompted use).</summary>
    public TimeSpan? RequestTimeout { get; set; }

    /// <summary>Subclasses produce the verb name + per-verb positionals + per-verb flags.</summary>
    protected abstract IEnumerable<string> BuildVerbArguments();

    /// <summary>Subclasses extending the secret list (override on verbs that carry sensitive material).</summary>
    protected virtual IEnumerable<Secret> BuildSecrets() => Array.Empty<Secret>();

    /// <summary>Subclasses providing stdin content (override on verbs that pipe data in).</summary>
    protected virtual string? BuildStandardInput() => null;

    /// <summary>Per-verb validation. Default no-op; subclasses throw <see cref="InvalidOperationException"/> on missing requirements.</summary>
    protected virtual void Validate() { }

    /// <summary>Materialise this settings instance into a <see cref="CommandPlan"/>.</summary>
    public CommandPlan ToCommandPlan(Tool tool)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        Validate();

        var args = new List<string>(BuildVerbArguments());

        if (!string.IsNullOrEmpty(Context)) { args.Add("--context"); args.Add(Context!); }
        if (!string.IsNullOrEmpty(Namespace)) { args.Add("-n"); args.Add(Namespace!); }
        if (!string.IsNullOrEmpty(ImpersonateUser)) { args.Add("--as"); args.Add(ImpersonateUser!); }
        foreach (var group in ImpersonateGroups) { args.Add("--as-group"); args.Add(group); }

        if (DryRun != KubectlDryRunStrategy.None)
        {
            args.Add(DryRun switch
            {
                KubectlDryRunStrategy.Client => "--dry-run=client",
                KubectlDryRunStrategy.Server => "--dry-run=server",
                _ => throw new InvalidOperationException($"Unhandled DryRun strategy: {DryRun}"),
            });
        }

        if (!string.IsNullOrEmpty(Output)) { args.Add("-o"); args.Add(Output!); }
        if (Verbosity is int v) { args.Add($"-v={v.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
        if (RequestTimeout is TimeSpan rt) { args.Add($"--request-timeout={FormatGoDuration(rt)}"); }

        // KUBECONFIG flows as an env var, not as --kubeconfig, so the path stays out of argv.
        var env = new Dictionary<string, string>(EnvironmentVariables);
        if (!string.IsNullOrEmpty(Kubeconfig)) env["KUBECONFIG"] = Kubeconfig!;

        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = env,
            WorkingDirectory = WorkingDirectory ?? tool.WorkingDirectory,
            Secrets = BuildSecrets().ToArray(),
            StandardInput = BuildStandardInput(),
        };
    }

    /// <summary>kubectl accepts Go-duration strings (<c>30s</c>, <c>5m</c>, <c>2h30m</c>) for timeouts.</summary>
    internal static string FormatGoDuration(TimeSpan ts)
    {
        if (ts.TotalSeconds < 1) return $"{ts.TotalMilliseconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture)}ms";
        if (ts.TotalMinutes < 1) return $"{((int)ts.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)}s";
        if (ts.TotalHours < 1) return $"{((int)ts.TotalMinutes).ToString(System.Globalization.CultureInfo.InvariantCulture)}m";
        return $"{((int)ts.TotalHours).ToString(System.Globalization.CultureInfo.InvariantCulture)}h{((int)(ts.TotalMinutes % 60)).ToString(System.Globalization.CultureInfo.InvariantCulture)}m";
    }
}

/// <summary>Fluent setters shared by every kubectl verb.</summary>
public static class KubectlSettingsBaseExtensions
{
    public static T SetWorkingDirectory<T>(this T s, string? cwd) where T : KubectlSettingsBase { s.WorkingDirectory = cwd; return s; }
    public static T SetEnv<T>(this T s, string name, string value) where T : KubectlSettingsBase { s.EnvironmentVariables[name] = value; return s; }
    public static T SetContext<T>(this T s, string? context) where T : KubectlSettingsBase { s.Context = context; return s; }
    public static T SetNamespace<T>(this T s, string? ns) where T : KubectlSettingsBase { s.Namespace = ns; return s; }
    public static T SetKubeconfig<T>(this T s, string? path) where T : KubectlSettingsBase { s.Kubeconfig = path; return s; }
    public static T SetImpersonateUser<T>(this T s, string? user) where T : KubectlSettingsBase { s.ImpersonateUser = user; return s; }
    public static T AddImpersonateGroup<T>(this T s, string group) where T : KubectlSettingsBase { s.ImpersonateGroups.Add(group); return s; }
    public static T SetDryRun<T>(this T s, KubectlDryRunStrategy strategy) where T : KubectlSettingsBase { s.DryRun = strategy; return s; }
    public static T SetOutput<T>(this T s, string? format) where T : KubectlSettingsBase { s.Output = format; return s; }
    public static T SetVerbosity<T>(this T s, int? v) where T : KubectlSettingsBase { s.Verbosity = v; return s; }
    public static T SetRequestTimeout<T>(this T s, TimeSpan? timeout) where T : KubectlSettingsBase { s.RequestTimeout = timeout; return s; }
}
