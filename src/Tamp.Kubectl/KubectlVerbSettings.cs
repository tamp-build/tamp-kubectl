namespace Tamp.Kubectl;

// ============================================================================
// apply — kubectl apply -f <file/url/dir> [-k <kustomize-dir>] [--prune] [-R]
// ============================================================================

/// <summary>Settings for <c>kubectl apply</c>.</summary>
public sealed class KubectlApplySettings : KubectlSettingsBase
{
    /// <summary>Files / URLs / directories to apply (<c>-f</c>, repeatable). Either this or <see cref="KustomizeDir"/> required.</summary>
    public List<string> Files { get; } = new();

    /// <summary>Kustomize directory (<c>-k</c>). Mutually exclusive with <see cref="Files"/>.</summary>
    public string? KustomizeDir { get; set; }

    /// <summary>Recurse into directories specified by <see cref="Files"/> (<c>-R</c>).</summary>
    public bool Recursive { get; set; }

    /// <summary>Prune resources that are managed by the same applied labels but not in the current input (<c>--prune</c>). Requires <see cref="PruneSelector"/>.</summary>
    public bool Prune { get; set; }

    /// <summary>Label selector that scopes <see cref="Prune"/> (<c>--prune --selector=…</c>). Required when <see cref="Prune"/> is true.</summary>
    public string? PruneSelector { get; set; }

    /// <summary>Force the operation even on conflict (<c>--force</c>).</summary>
    public bool Force { get; set; }

    /// <summary>Server-side apply (<c>--server-side</c>) — apply via SSA instead of strategic-merge.</summary>
    public bool ServerSide { get; set; }

    /// <summary>Field manager name for server-side apply (<c>--field-manager</c>).</summary>
    public string? FieldManager { get; set; }

    protected override void Validate()
    {
        if (Files.Count == 0 && string.IsNullOrEmpty(KustomizeDir))
            throw new InvalidOperationException("Apply requires at least one file (AddFile) OR a kustomize directory (SetKustomizeDir).");
        if (Files.Count > 0 && !string.IsNullOrEmpty(KustomizeDir))
            throw new InvalidOperationException("Apply takes either Files (-f) OR a kustomize directory (-k), not both.");
        if (Prune && string.IsNullOrEmpty(PruneSelector))
            throw new InvalidOperationException("Prune requires PruneSelector (--selector=…) to scope which managed resources are eligible for pruning.");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "apply";
        foreach (var f in Files) { yield return "-f"; yield return f; }
        if (!string.IsNullOrEmpty(KustomizeDir)) { yield return "-k"; yield return KustomizeDir!; }
        if (Recursive) yield return "-R";
        if (Prune)
        {
            yield return "--prune";
            yield return $"--selector={PruneSelector}";
        }
        if (Force) yield return "--force";
        if (ServerSide) yield return "--server-side";
        if (!string.IsNullOrEmpty(FieldManager)) yield return $"--field-manager={FieldManager}";
    }
}

public static class KubectlApplySettingsExtensions
{
    public static KubectlApplySettings AddFile(this KubectlApplySettings s, string path) { s.Files.Add(path); return s; }
    public static KubectlApplySettings SetKustomizeDir(this KubectlApplySettings s, string? dir) { s.KustomizeDir = dir; return s; }
    public static KubectlApplySettings SetRecursive(this KubectlApplySettings s, bool v = true) { s.Recursive = v; return s; }
    public static KubectlApplySettings SetPrune(this KubectlApplySettings s, string selector) { s.Prune = true; s.PruneSelector = selector; return s; }
    public static KubectlApplySettings SetForce(this KubectlApplySettings s, bool v = true) { s.Force = v; return s; }
    public static KubectlApplySettings SetServerSide(this KubectlApplySettings s, bool v = true) { s.ServerSide = v; return s; }
    public static KubectlApplySettings SetFieldManager(this KubectlApplySettings s, string? name) { s.FieldManager = name; return s; }
}

// ============================================================================
// get — kubectl get <resource> [<name>] [-l <selector>] [-A] [--watch]
// ============================================================================

/// <summary>Settings for <c>kubectl get</c>.</summary>
public sealed class KubectlGetSettings : KubectlSettingsBase
{
    /// <summary>Resource type (positional). Required. e.g. <c>pods</c>, <c>deployments</c>, <c>svc/my-service</c>.</summary>
    public string? Resource { get; set; }

    /// <summary>Optional name(s) to scope the query (positional, repeatable).</summary>
    public List<string> Names { get; } = new();

    /// <summary>Label selector (<c>-l</c>).</summary>
    public string? LabelSelector { get; set; }

    /// <summary>Field selector (<c>--field-selector</c>).</summary>
    public string? FieldSelector { get; set; }

    /// <summary>Query across all namespaces (<c>-A</c>). Mutually exclusive with <see cref="KubectlSettingsBase.Namespace"/>.</summary>
    public bool AllNamespaces { get; set; }

    /// <summary>Watch for changes (<c>--watch</c>).</summary>
    public bool Watch { get; set; }

    /// <summary>Show labels in the default text output (<c>--show-labels</c>).</summary>
    public bool ShowLabels { get; set; }

    /// <summary>Don't print headers in text output (<c>--no-headers</c>).</summary>
    public bool NoHeaders { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("Get requires Resource (SetResource).");
        if (AllNamespaces && !string.IsNullOrEmpty(Namespace))
            throw new InvalidOperationException("AllNamespaces (-A) and Namespace (-n) are mutually exclusive.");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "get";
        yield return Resource!;
        foreach (var name in Names) yield return name;
        if (!string.IsNullOrEmpty(LabelSelector)) { yield return "-l"; yield return LabelSelector!; }
        if (!string.IsNullOrEmpty(FieldSelector)) yield return $"--field-selector={FieldSelector}";
        if (AllNamespaces) yield return "-A";
        if (Watch) yield return "--watch";
        if (ShowLabels) yield return "--show-labels";
        if (NoHeaders) yield return "--no-headers";
    }
}

public static class KubectlGetSettingsExtensions
{
    public static KubectlGetSettings SetResource(this KubectlGetSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlGetSettings AddName(this KubectlGetSettings s, string name) { s.Names.Add(name); return s; }
    public static KubectlGetSettings SetLabelSelector(this KubectlGetSettings s, string? selector) { s.LabelSelector = selector; return s; }
    public static KubectlGetSettings SetFieldSelector(this KubectlGetSettings s, string? selector) { s.FieldSelector = selector; return s; }
    public static KubectlGetSettings SetAllNamespaces(this KubectlGetSettings s, bool v = true) { s.AllNamespaces = v; return s; }
    public static KubectlGetSettings SetWatch(this KubectlGetSettings s, bool v = true) { s.Watch = v; return s; }
    public static KubectlGetSettings SetShowLabels(this KubectlGetSettings s, bool v = true) { s.ShowLabels = v; return s; }
    public static KubectlGetSettings SetNoHeaders(this KubectlGetSettings s, bool v = true) { s.NoHeaders = v; return s; }
}

// ============================================================================
// delete — kubectl delete <resource> [<name>...] OR -f <file>
// ============================================================================

/// <summary>Settings for <c>kubectl delete</c>.</summary>
public sealed class KubectlDeleteSettings : KubectlSettingsBase
{
    /// <summary>Resource type (positional, for resource-name based delete).</summary>
    public string? Resource { get; set; }

    /// <summary>Names (positional, repeatable). Used with <see cref="Resource"/>.</summary>
    public List<string> Names { get; } = new();

    /// <summary>Files to delete from (<c>-f</c>, repeatable). Mutually exclusive with <see cref="Resource"/>.</summary>
    public List<string> Files { get; } = new();

    /// <summary>Label selector (<c>-l</c>).</summary>
    public string? LabelSelector { get; set; }

    /// <summary>Grace period in seconds before force-deleting (<c>--grace-period</c>). 0 = immediate, -1 = use default.</summary>
    public int? GracePeriodSeconds { get; set; }

    /// <summary>Force deletion (<c>--force</c>). Requires <c>--grace-period=0</c> in modern kubectl.</summary>
    public bool Force { get; set; }

    /// <summary>Wait for deletion to complete (<c>--wait</c>). Default true in kubectl; false disables.</summary>
    public bool? Wait { get; set; }

    /// <summary>Delete all matching resources in scope (<c>--all</c>).</summary>
    public bool All { get; set; }

    /// <summary>Ignore not-found errors (<c>--ignore-not-found</c>) — useful for idempotent delete in CI.</summary>
    public bool IgnoreNotFound { get; set; }

    protected override void Validate()
    {
        var modes = (string.IsNullOrEmpty(Resource) ? 0 : 1) + (Files.Count > 0 ? 1 : 0);
        if (modes == 0 && !All)
            throw new InvalidOperationException("Delete requires either Resource (+optional Names), Files (AddFile), or All (SetAll).");
        if (modes > 1)
            throw new InvalidOperationException("Delete takes either Resource OR Files, not both.");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "delete";
        if (!string.IsNullOrEmpty(Resource))
        {
            yield return Resource!;
            foreach (var name in Names) yield return name;
        }
        foreach (var f in Files) { yield return "-f"; yield return f; }
        if (!string.IsNullOrEmpty(LabelSelector)) { yield return "-l"; yield return LabelSelector!; }
        if (GracePeriodSeconds is int g) yield return $"--grace-period={g.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        if (Force) yield return "--force";
        if (Wait is bool w) yield return w ? "--wait=true" : "--wait=false";
        if (All) yield return "--all";
        if (IgnoreNotFound) yield return "--ignore-not-found";
    }
}

public static class KubectlDeleteSettingsExtensions
{
    public static KubectlDeleteSettings SetResource(this KubectlDeleteSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlDeleteSettings AddName(this KubectlDeleteSettings s, string name) { s.Names.Add(name); return s; }
    public static KubectlDeleteSettings AddFile(this KubectlDeleteSettings s, string path) { s.Files.Add(path); return s; }
    public static KubectlDeleteSettings SetLabelSelector(this KubectlDeleteSettings s, string? selector) { s.LabelSelector = selector; return s; }
    public static KubectlDeleteSettings SetGracePeriodSeconds(this KubectlDeleteSettings s, int? secs) { s.GracePeriodSeconds = secs; return s; }
    public static KubectlDeleteSettings SetForce(this KubectlDeleteSettings s, bool v = true) { s.Force = v; return s; }
    public static KubectlDeleteSettings SetWait(this KubectlDeleteSettings s, bool? wait) { s.Wait = wait; return s; }
    public static KubectlDeleteSettings SetAll(this KubectlDeleteSettings s, bool v = true) { s.All = v; return s; }
    public static KubectlDeleteSettings SetIgnoreNotFound(this KubectlDeleteSettings s, bool v = true) { s.IgnoreNotFound = v; return s; }
}

// ============================================================================
// describe — kubectl describe <resource> [<name>]
// ============================================================================

/// <summary>Settings for <c>kubectl describe</c>.</summary>
public sealed class KubectlDescribeSettings : KubectlSettingsBase
{
    /// <summary>Resource type (positional). Required.</summary>
    public string? Resource { get; set; }

    /// <summary>Optional name(s) to scope (positional).</summary>
    public List<string> Names { get; } = new();

    /// <summary>Label selector (<c>-l</c>).</summary>
    public string? LabelSelector { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("Describe requires Resource (SetResource).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "describe";
        yield return Resource!;
        foreach (var name in Names) yield return name;
        if (!string.IsNullOrEmpty(LabelSelector)) { yield return "-l"; yield return LabelSelector!; }
    }
}

public static class KubectlDescribeSettingsExtensions
{
    public static KubectlDescribeSettings SetResource(this KubectlDescribeSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlDescribeSettings AddName(this KubectlDescribeSettings s, string name) { s.Names.Add(name); return s; }
    public static KubectlDescribeSettings SetLabelSelector(this KubectlDescribeSettings s, string? selector) { s.LabelSelector = selector; return s; }
}

// ============================================================================
// rollout — kubectl rollout {status|restart|undo} <resource>
// ============================================================================

/// <summary>Sub-verb for <c>kubectl rollout</c>.</summary>
internal enum KubectlRolloutVerb { Status, Restart, Undo }

/// <summary>Settings for <c>kubectl rollout status</c>.</summary>
public sealed class KubectlRolloutStatusSettings : KubectlSettingsBase
{
    /// <summary>Resource (positional). Required. e.g. <c>deployment/my-app</c>.</summary>
    public string? Resource { get; set; }

    /// <summary>Watch until the rollout finishes (<c>--watch</c>). Default true in kubectl; set false to single-shot.</summary>
    public bool? Watch { get; set; }

    /// <summary>Timeout for the watch (<c>--timeout</c>).</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Revision to query (<c>--revision</c>).</summary>
    public int? Revision { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("RolloutStatus requires Resource (SetResource).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "rollout";
        yield return "status";
        yield return Resource!;
        if (Watch is bool w) yield return w ? "--watch=true" : "--watch=false";
        if (Timeout is TimeSpan t) yield return $"--timeout={FormatGoDuration(t)}";
        if (Revision is int r) yield return $"--revision={r.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}

public static class KubectlRolloutStatusSettingsExtensions
{
    public static KubectlRolloutStatusSettings SetResource(this KubectlRolloutStatusSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlRolloutStatusSettings SetWatch(this KubectlRolloutStatusSettings s, bool? watch) { s.Watch = watch; return s; }
    public static KubectlRolloutStatusSettings SetTimeout(this KubectlRolloutStatusSettings s, TimeSpan? timeout) { s.Timeout = timeout; return s; }
    public static KubectlRolloutStatusSettings SetRevision(this KubectlRolloutStatusSettings s, int? revision) { s.Revision = revision; return s; }
}

/// <summary>Settings for <c>kubectl rollout restart</c>.</summary>
public sealed class KubectlRolloutRestartSettings : KubectlSettingsBase
{
    /// <summary>Resource (positional). Required.</summary>
    public string? Resource { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("RolloutRestart requires Resource (SetResource).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "rollout";
        yield return "restart";
        yield return Resource!;
    }
}

public static class KubectlRolloutRestartSettingsExtensions
{
    public static KubectlRolloutRestartSettings SetResource(this KubectlRolloutRestartSettings s, string resource) { s.Resource = resource; return s; }
}

/// <summary>Settings for <c>kubectl rollout undo</c>.</summary>
public sealed class KubectlRolloutUndoSettings : KubectlSettingsBase
{
    /// <summary>Resource (positional). Required.</summary>
    public string? Resource { get; set; }

    /// <summary>Roll back to a specific revision (<c>--to-revision</c>). Omit for the immediately-previous revision.</summary>
    public int? ToRevision { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("RolloutUndo requires Resource (SetResource).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "rollout";
        yield return "undo";
        yield return Resource!;
        if (ToRevision is int r) yield return $"--to-revision={r.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}

public static class KubectlRolloutUndoSettingsExtensions
{
    public static KubectlRolloutUndoSettings SetResource(this KubectlRolloutUndoSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlRolloutUndoSettings SetToRevision(this KubectlRolloutUndoSettings s, int? revision) { s.ToRevision = revision; return s; }
}

// ============================================================================
// port-forward — kubectl port-forward <resource> <port-mappings>
// ============================================================================

/// <summary>Settings for <c>kubectl port-forward</c>.</summary>
public sealed class KubectlPortForwardSettings : KubectlSettingsBase
{
    /// <summary>Resource to forward to (positional). Required. e.g. <c>pod/foo</c>, <c>svc/bar</c>, <c>deployment/baz</c>.</summary>
    public string? Resource { get; set; }

    /// <summary>Port mappings (positional, one or more). Format: <c>localPort:remotePort</c> or just <c>remotePort</c> for ephemeral local. Required (at least one).</summary>
    public List<string> PortMappings { get; } = new();

    /// <summary>Address to bind locally (<c>--address</c>). Default <c>localhost</c>; set to <c>0.0.0.0</c> for external access.</summary>
    public string? Address { get; set; }

    /// <summary>Pod creation timeout (<c>--pod-running-timeout</c>).</summary>
    public TimeSpan? PodRunningTimeout { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("PortForward requires Resource (SetResource).");
        if (PortMappings.Count == 0)
            throw new InvalidOperationException("PortForward requires at least one port mapping (AddPortMapping).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "port-forward";
        yield return Resource!;
        foreach (var mapping in PortMappings) yield return mapping;
        if (!string.IsNullOrEmpty(Address)) yield return $"--address={Address}";
        if (PodRunningTimeout is TimeSpan t) yield return $"--pod-running-timeout={FormatGoDuration(t)}";
    }
}

public static class KubectlPortForwardSettingsExtensions
{
    public static KubectlPortForwardSettings SetResource(this KubectlPortForwardSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlPortForwardSettings AddPortMapping(this KubectlPortForwardSettings s, string mapping) { s.PortMappings.Add(mapping); return s; }
    public static KubectlPortForwardSettings AddPortMapping(this KubectlPortForwardSettings s, int localPort, int remotePort) { s.PortMappings.Add($"{localPort}:{remotePort}"); return s; }
    public static KubectlPortForwardSettings SetAddress(this KubectlPortForwardSettings s, string? address) { s.Address = address; return s; }
    public static KubectlPortForwardSettings SetPodRunningTimeout(this KubectlPortForwardSettings s, TimeSpan? timeout) { s.PodRunningTimeout = timeout; return s; }
}

// ============================================================================
// logs — kubectl logs <pod-or-resource> [-c <container>] [-f] [--tail N]
// ============================================================================

/// <summary>Settings for <c>kubectl logs</c>.</summary>
public sealed class KubectlLogsSettings : KubectlSettingsBase
{
    /// <summary>Pod or resource (positional). Required. e.g. <c>my-pod</c>, <c>deployment/my-app</c>.</summary>
    public string? Resource { get; set; }

    /// <summary>Specific container in a multi-container pod (<c>-c</c>).</summary>
    public string? Container { get; set; }

    /// <summary>All containers in the pod (<c>--all-containers</c>).</summary>
    public bool AllContainers { get; set; }

    /// <summary>Follow / stream logs (<c>-f</c>).</summary>
    public bool Follow { get; set; }

    /// <summary>Number of lines from the tail (<c>--tail</c>).</summary>
    public int? Tail { get; set; }

    /// <summary>Show logs from the previous container instance (<c>--previous</c>) — useful for diagnosing a crash-loop.</summary>
    public bool Previous { get; set; }

    /// <summary>Show logs since this duration ago (<c>--since</c>).</summary>
    public TimeSpan? Since { get; set; }

    /// <summary>Include timestamps (<c>--timestamps</c>).</summary>
    public bool Timestamps { get; set; }

    /// <summary>Label selector (<c>-l</c>) — for selecting pods by label rather than name.</summary>
    public string? LabelSelector { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource) && string.IsNullOrEmpty(LabelSelector))
            throw new InvalidOperationException("Logs requires Resource (SetResource) or LabelSelector (SetLabelSelector).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "logs";
        if (!string.IsNullOrEmpty(Resource)) yield return Resource!;
        if (!string.IsNullOrEmpty(Container)) { yield return "-c"; yield return Container!; }
        if (AllContainers) yield return "--all-containers";
        if (Follow) yield return "-f";
        if (Tail is int t) yield return $"--tail={t.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        if (Previous) yield return "--previous";
        if (Since is TimeSpan s) yield return $"--since={FormatGoDuration(s)}";
        if (Timestamps) yield return "--timestamps";
        if (!string.IsNullOrEmpty(LabelSelector)) { yield return "-l"; yield return LabelSelector!; }
    }
}

public static class KubectlLogsSettingsExtensions
{
    public static KubectlLogsSettings SetResource(this KubectlLogsSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlLogsSettings SetContainer(this KubectlLogsSettings s, string? container) { s.Container = container; return s; }
    public static KubectlLogsSettings SetAllContainers(this KubectlLogsSettings s, bool v = true) { s.AllContainers = v; return s; }
    public static KubectlLogsSettings SetFollow(this KubectlLogsSettings s, bool v = true) { s.Follow = v; return s; }
    public static KubectlLogsSettings SetTail(this KubectlLogsSettings s, int? lines) { s.Tail = lines; return s; }
    public static KubectlLogsSettings SetPrevious(this KubectlLogsSettings s, bool v = true) { s.Previous = v; return s; }
    public static KubectlLogsSettings SetSince(this KubectlLogsSettings s, TimeSpan? since) { s.Since = since; return s; }
    public static KubectlLogsSettings SetTimestamps(this KubectlLogsSettings s, bool v = true) { s.Timestamps = v; return s; }
    public static KubectlLogsSettings SetLabelSelector(this KubectlLogsSettings s, string? selector) { s.LabelSelector = selector; return s; }
}

// ============================================================================
// exec — kubectl exec <pod> [-c <container>] [-i] [-t] -- <command...>
// ============================================================================

/// <summary>Settings for <c>kubectl exec</c>.</summary>
public sealed class KubectlExecSettings : KubectlSettingsBase
{
    /// <summary>Pod (positional). Required.</summary>
    public string? Pod { get; set; }

    /// <summary>Specific container in a multi-container pod (<c>-c</c>).</summary>
    public string? Container { get; set; }

    /// <summary>Pass stdin (<c>-i</c>) — required for interactive commands.</summary>
    public bool Stdin { get; set; }

    /// <summary>Allocate a TTY (<c>-t</c>) — usually combined with <see cref="Stdin"/>.</summary>
    public bool Tty { get; set; }

    /// <summary>Command + arguments to execute. Required (at least the command). Emitted after <c>--</c>.</summary>
    public List<string> Command { get; } = new();

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Pod))
            throw new InvalidOperationException("Exec requires Pod (SetPod).");
        if (Command.Count == 0)
            throw new InvalidOperationException("Exec requires at least one command argument (AddCommand).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "exec";
        yield return Pod!;
        if (!string.IsNullOrEmpty(Container)) { yield return "-c"; yield return Container!; }
        if (Stdin) yield return "-i";
        if (Tty) yield return "-t";
        yield return "--";
        foreach (var arg in Command) yield return arg;
    }
}

public static class KubectlExecSettingsExtensions
{
    public static KubectlExecSettings SetPod(this KubectlExecSettings s, string pod) { s.Pod = pod; return s; }
    public static KubectlExecSettings SetContainer(this KubectlExecSettings s, string? container) { s.Container = container; return s; }
    public static KubectlExecSettings SetStdin(this KubectlExecSettings s, bool v = true) { s.Stdin = v; return s; }
    public static KubectlExecSettings SetTty(this KubectlExecSettings s, bool v = true) { s.Tty = v; return s; }
    public static KubectlExecSettings AddCommand(this KubectlExecSettings s, string arg) { s.Command.Add(arg); return s; }
    public static KubectlExecSettings SetCommand(this KubectlExecSettings s, params string[] command) { s.Command.Clear(); s.Command.AddRange(command); return s; }
}

// ============================================================================
// scale — kubectl scale <resource> --replicas N [--current-replicas M]
// ============================================================================

/// <summary>Settings for <c>kubectl scale</c>.</summary>
public sealed class KubectlScaleSettings : KubectlSettingsBase
{
    /// <summary>Resource (positional). Required. e.g. <c>deployment/my-app</c>, <c>statefulset/foo</c>.</summary>
    public string? Resource { get; set; }

    /// <summary>Target replica count (<c>--replicas</c>). Required.</summary>
    public int? Replicas { get; set; }

    /// <summary>Optional precondition: only scale if current matches (<c>--current-replicas</c>). Compare-and-set.</summary>
    public int? CurrentReplicas { get; set; }

    /// <summary>Scale operation timeout (<c>--timeout</c>).</summary>
    public TimeSpan? Timeout { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("Scale requires Resource (SetResource).");
        if (Replicas is null)
            throw new InvalidOperationException("Scale requires Replicas (SetReplicas).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "scale";
        yield return Resource!;
        yield return $"--replicas={Replicas!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        if (CurrentReplicas is int c) yield return $"--current-replicas={c.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        if (Timeout is TimeSpan t) yield return $"--timeout={FormatGoDuration(t)}";
    }
}

public static class KubectlScaleSettingsExtensions
{
    public static KubectlScaleSettings SetResource(this KubectlScaleSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlScaleSettings SetReplicas(this KubectlScaleSettings s, int replicas) { s.Replicas = replicas; return s; }
    public static KubectlScaleSettings SetCurrentReplicas(this KubectlScaleSettings s, int? replicas) { s.CurrentReplicas = replicas; return s; }
    public static KubectlScaleSettings SetTimeout(this KubectlScaleSettings s, TimeSpan? timeout) { s.Timeout = timeout; return s; }
}

// ============================================================================
// set image — kubectl set image <resource> <container>=<image> [...]
// ============================================================================

/// <summary>Settings for <c>kubectl set image</c>.</summary>
public sealed class KubectlSetImageSettings : KubectlSettingsBase
{
    /// <summary>Resource (positional). Required.</summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Container image updates (<c>container=image</c>, repeatable). At least one required.
    /// Special container name <c>*</c> updates every container in the pod template.
    /// </summary>
    public Dictionary<string, string> ContainerImages { get; } = new();

    /// <summary>Apply changes to all matching resources (<c>--all</c>).</summary>
    public bool All { get; set; }

    /// <summary>Label selector (<c>-l</c>).</summary>
    public string? LabelSelector { get; set; }

    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Resource))
            throw new InvalidOperationException("SetImage requires Resource (SetResource).");
        if (ContainerImages.Count == 0)
            throw new InvalidOperationException("SetImage requires at least one container/image pair (SetContainerImage).");
    }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        yield return "set";
        yield return "image";
        yield return Resource!;
        foreach (var kv in ContainerImages) yield return $"{kv.Key}={kv.Value}";
        if (All) yield return "--all";
        if (!string.IsNullOrEmpty(LabelSelector)) { yield return "-l"; yield return LabelSelector!; }
    }
}

public static class KubectlSetImageSettingsExtensions
{
    public static KubectlSetImageSettings SetResource(this KubectlSetImageSettings s, string resource) { s.Resource = resource; return s; }
    public static KubectlSetImageSettings SetContainerImage(this KubectlSetImageSettings s, string containerName, string image) { s.ContainerImages[containerName] = image; return s; }
    public static KubectlSetImageSettings SetAll(this KubectlSetImageSettings s, bool v = true) { s.All = v; return s; }
    public static KubectlSetImageSettings SetLabelSelector(this KubectlSetImageSettings s, string? selector) { s.LabelSelector = selector; return s; }
}
