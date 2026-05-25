# Tamp.Kubectl

> Tamp CommandPlan wrappers for the `kubectl` CLI — `Apply`, `Get`, `Delete`, `Describe`, `Rollout` (Status / Restart / Undo), `PortForward`, `Logs`, `Exec`, `Scale`, `SetImage`. Common settings (context / namespace / kubeconfig / impersonation / dry-run / output / verbosity / timeout) live on the shared base; per-verb settings layer on top.

| Package | Status |
|---|---|
| `Tamp.Kubectl` | 0.1.0 (initial) |

## Install

```bash
dotnet add package Tamp.Kubectl
```

Multi-targets net8 / net9 / net10. Requires `kubectl` on PATH or injected via a `[FromPath("kubectl")]` `Tool`. Wraps any kubectl version Kubernetes considers in support — kubectl honors the [+/- 1 minor version skew policy](https://kubernetes.io/releases/version-skew-policy/) against the API server.

## Quick start

```csharp
using Tamp;
using Tamp.Kubectl;

class Build : TampBuild
{
    public static int Main(string[] args) => Execute<Build>(args);

    [FromPath("kubectl")] readonly Tool KubectlBin = null!;
    [Parameter] readonly string Environment = "staging";

    Target Deploy => _ => _.Executes(() => Kubectl.Apply(KubectlBin, s => s
        .SetContext($"{Environment}-cluster")
        .SetNamespace("billing")
        .AddFile("manifests/")
        .SetRecursive()
        .SetServerSide()
        .SetFieldManager("tamp-deploy")));

    Target WaitForRollout => _ => _
        .DependsOn(Deploy)
        .Executes(() => Kubectl.RolloutStatus(KubectlBin, s => s
            .SetContext($"{Environment}-cluster")
            .SetNamespace("billing")
            .SetResource("deployment/billing-api")
            .SetTimeout(TimeSpan.FromMinutes(5))));
}
```

## Verb surface (v1)

12 verbs, each with fluent + object-init authoring parity:

| Verb | Wraps | Notes |
|---|---|---|
| `Kubectl.Apply` | `kubectl apply -f ...` | Files (`-f`, repeatable) **or** kustomize dir (`-k`). Optional `--prune`, `--server-side`, `--field-manager`, `--force`, `-R`. |
| `Kubectl.Get` | `kubectl get <resource>` | Names, label / field selectors, all-namespaces, watch, no-headers, show-labels. |
| `Kubectl.Delete` | `kubectl delete <resource>` / `-f` | Resource+names mode OR file mode OR `--all`. Grace period, force, wait toggle, ignore-not-found (idempotent CI delete). |
| `Kubectl.Describe` | `kubectl describe <resource>` | Names, label selector. |
| `Kubectl.RolloutStatus` | `kubectl rollout status <resource>` | Watch toggle, timeout (Go-duration), revision. |
| `Kubectl.RolloutRestart` | `kubectl rollout restart <resource>` | — |
| `Kubectl.RolloutUndo` | `kubectl rollout undo <resource>` | Optional `--to-revision`. |
| `Kubectl.PortForward` | `kubectl port-forward <resource> <ports>` | String or `(int local, int remote)` mappings; `--address`; `--pod-running-timeout`. |
| `Kubectl.Logs` | `kubectl logs <pod-or-resource>` | Container / all-containers, follow, tail, previous, since, timestamps, label selector. |
| `Kubectl.Exec` | `kubectl exec <pod> -- <command>` | Container, stdin (`-i`), tty (`-t`), command list. |
| `Kubectl.Scale` | `kubectl scale <resource> --replicas N` | Optional compare-and-set via `--current-replicas`; timeout. |
| `Kubectl.SetImage` | `kubectl set image <resource> <container>=<image>` | Multiple container/image pairs; `--all`; label selector. |

## Common settings (every verb)

These live on `KubectlSettingsBase` and apply to every verb:

| Setter | CLI |
|---|---|
| `.SetContext(name)` | `--context <name>` |
| `.SetNamespace(name)` | `-n <name>` |
| `.SetKubeconfig(path)` | **`KUBECONFIG=<path>` env var** — see [Kubeconfig handling](#kubeconfig-handling-kubeconfig-env-var-not---kubeconfig-flag) |
| `.SetImpersonateUser(user)` | `--as <user>` |
| `.AddImpersonateGroup(group)` | `--as-group <group>` (repeatable) |
| `.SetDryRun(KubectlDryRunStrategy.Client \| Server)` | `--dry-run=client` / `--dry-run=server` |
| `.SetOutput(format)` | `-o <format>` (`json`, `yaml`, `wide`, `name`, `jsonpath=...`, ...) |
| `.SetVerbosity(n)` | `-v=<n>` |
| `.SetRequestTimeout(timespan)` | `--request-timeout=<go-duration>` |
| `.SetWorkingDirectory(path)` | child process cwd |
| `.SetEnv(name, value)` | child process env var |

### Kubeconfig handling: `KUBECONFIG` env var, not `--kubeconfig` flag

When `.SetKubeconfig(path)` is set, the wrapper emits the path as the **`KUBECONFIG`** environment variable on the spawned process — NOT as a `--kubeconfig` CLI argument. This keeps the kubeconfig path out of the OS process table for the lifetime of the kubectl invocation, matching the Tamp secret-handling posture for paths adjacent to credentials.

Multi-path kubeconfigs (the upstream `KUBECONFIG=path1:path2` merge syntax on POSIX, `path1;path2` on Windows) are supported transparently — pass the joined string and kubectl handles the merge.

## Exit-code semantics

`kubectl` exits 0 on success and non-zero on any error. Unlike linter wrappers (`Tamp.OpenGrep` / `Tamp.Eslint.V9`), there's no "findings = exit 1, success" convention — every non-zero exit is a real failure that the security-pipeline target should propagate.

## Settings authoring — fluent or object-init

Both styles produce identical `CommandPlan`s; fluent is canonical in docs.

```csharp
// Fluent
Kubectl.Scale(KubectlBin, s => s
    .SetResource("deployment/my-app")
    .SetReplicas(5)
    .SetCurrentReplicas(3));   // compare-and-set

// Object-init
Kubectl.Scale(KubectlBin, new KubectlScaleSettings
{
    Resource = "deployment/my-app",
    Replicas = 5,
    CurrentReplicas = 3,
});
```

## Realistic deploy chain

```csharp
Target Deploy => _ => _.Executes(() =>
{
    // 1. Apply manifests (server-side, attributed)
    ProcessRunner.Execute(Kubectl.Apply(KubectlBin, s => s
        .SetContext("prod-cluster").SetNamespace("billing")
        .AddFile("manifests").SetRecursive()
        .SetServerSide().SetFieldManager("tamp-deploy")));

    // 2. Wait for rollout
    ProcessRunner.Execute(Kubectl.RolloutStatus(KubectlBin, s => s
        .SetContext("prod-cluster").SetNamespace("billing")
        .SetResource("deployment/billing-api")
        .SetTimeout(TimeSpan.FromMinutes(5))));

    // 3. Diagnostic logs on failure (via OnFailureOf in a real Build.cs)
});
```

For multi-cluster / canary deploys, pair with `Tamp.AzureCli.V2` (or your cloud's CLI) for cluster-credential rotation, and `Tamp.Helm.V3` for chart-driven deploys where `kubectl apply -f` isn't enough.

## License

MIT — see [LICENSE](LICENSE).
