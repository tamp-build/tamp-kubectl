using System.Linq;
using Bogus;
using Tamp;
using Tamp.Kubectl;
using Xunit;

namespace Tamp.Kubectl.Tests;

public sealed class KubectlTests
{
    private static readonly string FakeToolPath = OperatingSystem.IsWindows() ? "C:\\fake\\kubectl.exe" : "/fake/kubectl";

    private static Tool FakeTool() => new(AbsolutePath.Create(FakeToolPath));

    private static int IndexOf(IReadOnlyList<string> args, string token)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == token) return i;
        return -1;
    }

    // ============================== apply ==============================

    [Fact]
    public void Apply_File_Required_Or_Kustomize()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Kubectl.Apply(FakeTool(), _ => { }));
        Assert.Contains("file", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_Single_File_Emits_apply_f_path()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s.AddFile("manifests/svc.yaml"));
        Assert.Equal("apply", plan.Arguments[0]);
        Assert.Equal("-f", plan.Arguments[1]);
        Assert.Equal("manifests/svc.yaml", plan.Arguments[2]);
    }

    [Fact]
    public void Apply_Multiple_Files_Each_Get_f_Flag()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s
            .AddFile("svc.yaml").AddFile("deploy.yaml").AddFile("hpa.yaml"));
        Assert.Equal(3, plan.Arguments.Count(a => a == "-f"));
    }

    [Fact]
    public void Apply_Kustomize_Mutex_With_Files()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Kubectl.Apply(FakeTool(), s => s
            .AddFile("svc.yaml").SetKustomizeDir("base/")));
        Assert.Contains("kustomize", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_Kustomize_Emits_k_Flag()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s.SetKustomizeDir("overlays/prod"));
        Assert.Equal("apply", plan.Arguments[0]);
        var kIdx = IndexOf(plan.Arguments, "-k");
        Assert.Equal("overlays/prod", plan.Arguments[kIdx + 1]);
    }

    [Fact]
    public void Apply_Recursive_Server_Side_Force_Field_Manager()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s
            .AddFile("manifests")
            .SetRecursive()
            .SetServerSide()
            .SetForce()
            .SetFieldManager("deploy-bot"));
        Assert.Contains("-R", plan.Arguments);
        Assert.Contains("--server-side", plan.Arguments);
        Assert.Contains("--force", plan.Arguments);
        Assert.Contains("--field-manager=deploy-bot", plan.Arguments);
    }

    [Fact]
    public void Apply_Prune_Requires_Selector()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var s = new KubectlApplySettings();
            s.Files.Add("manifests/");
            s.Prune = true;
            Kubectl.Apply(FakeTool(), s);
        });
        Assert.Contains("PruneSelector", ex.Message);
    }

    [Fact]
    public void Apply_Prune_Emits_Selector()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s
            .AddFile("manifests/")
            .SetPrune("app=foo,env=prod"));
        Assert.Contains("--prune", plan.Arguments);
        Assert.Contains("--selector=app=foo,env=prod", plan.Arguments);
    }

    // ============================== get ==============================

    [Fact]
    public void Get_Requires_Resource()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Get(FakeTool(), _ => { }));
    }

    [Fact]
    public void Get_Single_Resource_Emits_get_resource()
    {
        var plan = Kubectl.Get(FakeTool(), s => s.SetResource("pods"));
        Assert.Equal(new[] { "get", "pods" }, plan.Arguments.Take(2).ToArray());
    }

    [Fact]
    public void Get_With_Name_And_Selector_And_AllNamespaces()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .AddName("my-pod")
            .SetLabelSelector("app=foo")
            .SetAllNamespaces()
            .SetOutput("json"));
        Assert.Contains("my-pod", plan.Arguments);
        Assert.Contains("-l", plan.Arguments);
        Assert.Contains("app=foo", plan.Arguments);
        Assert.Contains("-A", plan.Arguments);
        Assert.Contains("-o", plan.Arguments);
        Assert.Contains("json", plan.Arguments);
    }

    [Fact]
    public void Get_AllNamespaces_Mutex_With_Namespace()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Get(FakeTool(), s => s
            .SetResource("pods").SetAllNamespaces().SetNamespace("prod")));
    }

    [Fact]
    public void Get_Watch_ShowLabels_NoHeaders_FieldSelector()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .SetWatch()
            .SetShowLabels()
            .SetNoHeaders()
            .SetFieldSelector("status.phase=Running"));
        Assert.Contains("--watch", plan.Arguments);
        Assert.Contains("--show-labels", plan.Arguments);
        Assert.Contains("--no-headers", plan.Arguments);
        Assert.Contains("--field-selector=status.phase=Running", plan.Arguments);
    }

    // ============================== delete ==============================

    [Fact]
    public void Delete_Requires_Resource_Or_Files_Or_All()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Delete(FakeTool(), _ => { }));
    }

    [Fact]
    public void Delete_Resource_Mutex_With_File()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Delete(FakeTool(), s => s
            .SetResource("pod").AddFile("delete.yaml")));
    }

    [Fact]
    public void Delete_Resource_With_Names()
    {
        var plan = Kubectl.Delete(FakeTool(), s => s
            .SetResource("pods").AddName("a").AddName("b"));
        Assert.Equal(new[] { "delete", "pods", "a", "b" }, plan.Arguments.Take(4).ToArray());
    }

    [Fact]
    public void Delete_File_Mode()
    {
        var plan = Kubectl.Delete(FakeTool(), s => s.AddFile("cleanup.yaml"));
        Assert.Contains("-f", plan.Arguments);
        Assert.Contains("cleanup.yaml", plan.Arguments);
    }

    [Fact]
    public void Delete_All_Mode()
    {
        var plan = Kubectl.Delete(FakeTool(), s => s.SetAll());
        Assert.Contains("--all", plan.Arguments);
    }

    [Fact]
    public void Delete_GracePeriod_Force_Wait_IgnoreNotFound()
    {
        var plan = Kubectl.Delete(FakeTool(), s => s
            .SetResource("pod").AddName("p")
            .SetGracePeriodSeconds(0)
            .SetForce()
            .SetWait(false)
            .SetIgnoreNotFound());
        Assert.Contains("--grace-period=0", plan.Arguments);
        Assert.Contains("--force", plan.Arguments);
        Assert.Contains("--wait=false", plan.Arguments);
        Assert.Contains("--ignore-not-found", plan.Arguments);
    }

    // ============================== describe ==============================

    [Fact]
    public void Describe_Resource_Required()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Describe(FakeTool(), _ => { }));
    }

    [Fact]
    public void Describe_Resource_And_Name()
    {
        var plan = Kubectl.Describe(FakeTool(), s => s.SetResource("pod").AddName("foo"));
        Assert.Equal(new[] { "describe", "pod", "foo" }, plan.Arguments.Take(3).ToArray());
    }

    // ============================== rollout ==============================

    [Fact]
    public void RolloutStatus_Resource_Watch_Timeout_Revision()
    {
        var plan = Kubectl.RolloutStatus(FakeTool(), s => s
            .SetResource("deployment/my-app")
            .SetWatch(true)
            .SetTimeout(TimeSpan.FromSeconds(120))
            .SetRevision(7));
        Assert.Equal(new[] { "rollout", "status", "deployment/my-app" }, plan.Arguments.Take(3).ToArray());
        Assert.Contains("--watch=true", plan.Arguments);
        Assert.Contains("--timeout=2m", plan.Arguments);  // FormatGoDuration auto-reduces 120s → 2m
        Assert.Contains("--revision=7", plan.Arguments);
    }

    [Fact]
    public void RolloutRestart_Resource_Required()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.RolloutRestart(FakeTool(), _ => { }));
        var plan = Kubectl.RolloutRestart(FakeTool(), s => s.SetResource("deployment/my-app"));
        Assert.Equal(new[] { "rollout", "restart", "deployment/my-app" }, plan.Arguments.Take(3).ToArray());
    }

    [Fact]
    public void RolloutUndo_With_ToRevision()
    {
        var plan = Kubectl.RolloutUndo(FakeTool(), s => s
            .SetResource("deployment/my-app")
            .SetToRevision(4));
        Assert.Equal(new[] { "rollout", "undo", "deployment/my-app" }, plan.Arguments.Take(3).ToArray());
        Assert.Contains("--to-revision=4", plan.Arguments);
    }

    [Fact]
    public void RolloutUndo_Without_ToRevision_Omits_Flag()
    {
        var plan = Kubectl.RolloutUndo(FakeTool(), s => s.SetResource("deployment/my-app"));
        Assert.DoesNotContain(plan.Arguments, a => a.StartsWith("--to-revision", StringComparison.Ordinal));
    }

    // ============================== port-forward ==============================

    [Fact]
    public void PortForward_Requires_Resource_And_Mappings()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.PortForward(FakeTool(), _ => { }));
        Assert.Throws<InvalidOperationException>(() => Kubectl.PortForward(FakeTool(), s => s.SetResource("svc/foo")));
    }

    [Fact]
    public void PortForward_String_Mapping()
    {
        var plan = Kubectl.PortForward(FakeTool(), s => s
            .SetResource("svc/foo")
            .AddPortMapping("8080:80"));
        Assert.Equal(new[] { "port-forward", "svc/foo", "8080:80" }, plan.Arguments.Take(3).ToArray());
    }

    [Fact]
    public void PortForward_Int_Mapping_Convenience()
    {
        var plan = Kubectl.PortForward(FakeTool(), s => s
            .SetResource("pod/foo")
            .AddPortMapping(8080, 80)
            .AddPortMapping(9090, 9090));
        Assert.Contains("8080:80", plan.Arguments);
        Assert.Contains("9090:9090", plan.Arguments);
    }

    [Fact]
    public void PortForward_Address_And_PodRunningTimeout()
    {
        var plan = Kubectl.PortForward(FakeTool(), s => s
            .SetResource("svc/foo")
            .AddPortMapping("8080:80")
            .SetAddress("0.0.0.0")
            .SetPodRunningTimeout(TimeSpan.FromSeconds(30)));
        Assert.Contains("--address=0.0.0.0", plan.Arguments);
        Assert.Contains("--pod-running-timeout=30s", plan.Arguments);
    }

    // ============================== logs ==============================

    [Fact]
    public void Logs_Requires_Resource_Or_Selector()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Logs(FakeTool(), _ => { }));
    }

    [Fact]
    public void Logs_Pod_Container_Follow_Tail_Previous_Since_Timestamps()
    {
        var plan = Kubectl.Logs(FakeTool(), s => s
            .SetResource("my-pod")
            .SetContainer("sidecar")
            .SetFollow()
            .SetTail(100)
            .SetPrevious()
            .SetSince(TimeSpan.FromMinutes(5))
            .SetTimestamps());
        Assert.Equal("logs", plan.Arguments[0]);
        Assert.Equal("my-pod", plan.Arguments[1]);
        Assert.Contains("-c", plan.Arguments);
        Assert.Contains("sidecar", plan.Arguments);
        Assert.Contains("-f", plan.Arguments);
        Assert.Contains("--tail=100", plan.Arguments);
        Assert.Contains("--previous", plan.Arguments);
        Assert.Contains("--since=5m", plan.Arguments);
        Assert.Contains("--timestamps", plan.Arguments);
    }

    [Fact]
    public void Logs_Via_LabelSelector_Only()
    {
        var plan = Kubectl.Logs(FakeTool(), s => s
            .SetLabelSelector("app=my-app"));
        Assert.Equal("logs", plan.Arguments[0]);
        Assert.Contains("-l", plan.Arguments);
        Assert.Contains("app=my-app", plan.Arguments);
    }

    [Fact]
    public void Logs_AllContainers()
    {
        var plan = Kubectl.Logs(FakeTool(), s => s.SetResource("my-pod").SetAllContainers());
        Assert.Contains("--all-containers", plan.Arguments);
    }

    // ============================== exec ==============================

    [Fact]
    public void Exec_Requires_Pod_And_Command()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Exec(FakeTool(), s => s.AddCommand("ls")));
        Assert.Throws<InvalidOperationException>(() => Kubectl.Exec(FakeTool(), s => s.SetPod("foo")));
    }

    [Fact]
    public void Exec_Single_Command_Emits_Pod_Then_DashDash_Then_Command()
    {
        var plan = Kubectl.Exec(FakeTool(), s => s
            .SetPod("my-pod")
            .AddCommand("sh").AddCommand("-c").AddCommand("ls -la /app"));
        var idx = IndexOf(plan.Arguments, "--");
        Assert.True(idx > 0);
        Assert.Equal("my-pod", plan.Arguments[1]);
        Assert.Equal("sh", plan.Arguments[idx + 1]);
        Assert.Equal("-c", plan.Arguments[idx + 2]);
        Assert.Equal("ls -la /app", plan.Arguments[idx + 3]);
    }

    [Fact]
    public void Exec_Stdin_Tty_Container()
    {
        var plan = Kubectl.Exec(FakeTool(), s => s
            .SetPod("p")
            .SetContainer("c")
            .SetStdin().SetTty()
            .AddCommand("bash"));
        Assert.Contains("-c", plan.Arguments);
        Assert.Contains("c", plan.Arguments);
        Assert.Contains("-i", plan.Arguments);
        Assert.Contains("-t", plan.Arguments);
    }

    [Fact]
    public void Exec_SetCommand_Replaces_List()
    {
        var plan = Kubectl.Exec(FakeTool(), s => s
            .SetPod("p")
            .AddCommand("old")
            .SetCommand("new1", "new2"));
        Assert.DoesNotContain("old", plan.Arguments);
        Assert.Contains("new1", plan.Arguments);
        Assert.Contains("new2", plan.Arguments);
    }

    // ============================== scale ==============================

    [Fact]
    public void Scale_Requires_Resource_And_Replicas()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.Scale(FakeTool(), s => s.SetReplicas(3)));
        Assert.Throws<InvalidOperationException>(() => Kubectl.Scale(FakeTool(), s => s.SetResource("deployment/my-app")));
    }

    [Fact]
    public void Scale_Emits_Replicas()
    {
        var plan = Kubectl.Scale(FakeTool(), s => s
            .SetResource("deployment/my-app")
            .SetReplicas(5));
        Assert.Equal(new[] { "scale", "deployment/my-app", "--replicas=5" }, plan.Arguments.Take(3).ToArray());
    }

    [Fact]
    public void Scale_CurrentReplicas_And_Timeout()
    {
        var plan = Kubectl.Scale(FakeTool(), s => s
            .SetResource("deployment/my-app")
            .SetReplicas(10)
            .SetCurrentReplicas(5)
            .SetTimeout(TimeSpan.FromMinutes(2)));
        Assert.Contains("--current-replicas=5", plan.Arguments);
        Assert.Contains("--timeout=2m", plan.Arguments);
    }

    // ============================== set image ==============================

    [Fact]
    public void SetImage_Requires_Resource_And_At_Least_One_Image()
    {
        Assert.Throws<InvalidOperationException>(() => Kubectl.SetImage(FakeTool(), s => s.SetResource("deployment/my-app")));
        Assert.Throws<InvalidOperationException>(() => Kubectl.SetImage(FakeTool(), s => s.SetContainerImage("c", "i")));
    }

    [Fact]
    public void SetImage_Single_Container_Image()
    {
        var plan = Kubectl.SetImage(FakeTool(), s => s
            .SetResource("deployment/my-app")
            .SetContainerImage("api", "registry.example/api:v1.2.3"));
        Assert.Equal(new[] { "set", "image", "deployment/my-app", "api=registry.example/api:v1.2.3" }, plan.Arguments.Take(4).ToArray());
    }

    [Fact]
    public void SetImage_Multiple_Containers_All_Emit()
    {
        var plan = Kubectl.SetImage(FakeTool(), s => s
            .SetResource("deployment/my-app")
            .SetContainerImage("api", "registry.example/api:v1")
            .SetContainerImage("worker", "registry.example/worker:v1")
            .SetAll()
            .SetLabelSelector("tier=app"));
        Assert.Contains("api=registry.example/api:v1", plan.Arguments);
        Assert.Contains("worker=registry.example/worker:v1", plan.Arguments);
        Assert.Contains("--all", plan.Arguments);
        Assert.Contains("-l", plan.Arguments);
        Assert.Contains("tier=app", plan.Arguments);
    }

    // ============================== common (base) ==============================

    [Fact]
    public void Context_Namespace_Output_Verbosity_Common_Across_Verbs()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .SetContext("prod")
            .SetNamespace("billing")
            .SetOutput("yaml")
            .SetVerbosity(6));
        Assert.Contains("--context", plan.Arguments);
        Assert.Contains("prod", plan.Arguments);
        Assert.Contains("-n", plan.Arguments);
        Assert.Contains("billing", plan.Arguments);
        Assert.Contains("-o", plan.Arguments);
        Assert.Contains("yaml", plan.Arguments);
        Assert.Contains("-v=6", plan.Arguments);
    }

    [Fact]
    public void Impersonate_User_And_Groups()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .SetImpersonateUser("scott@example.com")
            .AddImpersonateGroup("system:cluster-admins")
            .AddImpersonateGroup("system:authenticated"));
        Assert.Contains("--as", plan.Arguments);
        Assert.Contains("scott@example.com", plan.Arguments);
        Assert.Equal(2, plan.Arguments.Count(a => a == "--as-group"));
        Assert.Contains("system:cluster-admins", plan.Arguments);
        Assert.Contains("system:authenticated", plan.Arguments);
    }

    [Theory]
    [InlineData(KubectlDryRunStrategy.Client, "--dry-run=client")]
    [InlineData(KubectlDryRunStrategy.Server, "--dry-run=server")]
    public void DryRun_Strategy_Emits_Flag(KubectlDryRunStrategy strategy, string expected)
    {
        var plan = Kubectl.Apply(FakeTool(), s => s.AddFile("svc.yaml").SetDryRun(strategy));
        Assert.Contains(expected, plan.Arguments);
    }

    [Fact]
    public void DryRun_None_Omits_Flag()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s.AddFile("svc.yaml"));
        Assert.DoesNotContain(plan.Arguments, a => a.StartsWith("--dry-run", StringComparison.Ordinal));
    }

    [Fact]
    public void RequestTimeout_Emits_Flag()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .SetRequestTimeout(TimeSpan.FromSeconds(30)));
        Assert.Contains("--request-timeout=30s", plan.Arguments);
    }

    // ---- Kubeconfig (env var, NOT CLI flag — keep path out of OS process table) ----

    [Fact]
    public void Kubeconfig_Path_Flows_As_Env_Var_Not_As_CLI_Flag()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .SetKubeconfig("/secure/kubeconfig.yaml"));

        Assert.DoesNotContain(plan.Arguments, a => a == "--kubeconfig" || a.StartsWith("--kubeconfig=", StringComparison.Ordinal));
        Assert.Equal("/secure/kubeconfig.yaml", plan.Environment["KUBECONFIG"]);
    }

    [Fact]
    public void Kubeconfig_Unset_Does_Not_Add_KUBECONFIG_Env()
    {
        var plan = Kubectl.Get(FakeTool(), s => s.SetResource("pods"));
        Assert.False(plan.Environment.ContainsKey("KUBECONFIG"));
    }

    [Fact]
    public void Custom_Env_Vars_Coexist_With_Kubeconfig()
    {
        var plan = Kubectl.Get(FakeTool(), s => s
            .SetResource("pods")
            .SetKubeconfig("/path/kc")
            .SetEnv("FOO", "bar")
            .SetEnv("HTTPS_PROXY", "http://proxy:8080"));
        Assert.Equal("/path/kc", plan.Environment["KUBECONFIG"]);
        Assert.Equal("bar", plan.Environment["FOO"]);
        Assert.Equal("http://proxy:8080", plan.Environment["HTTPS_PROXY"]);
    }

    [Fact]
    public void WorkingDirectory_Propagates()
    {
        var plan = Kubectl.Apply(FakeTool(), s => s.AddFile("svc.yaml").SetWorkingDirectory("/repo/web"));
        Assert.Equal("/repo/web", plan.WorkingDirectory);
    }

    // ============================== object-init parity ==============================

    [Fact]
    public void Apply_ObjectInit_Identical_To_Fluent()
    {
        var fluent = Kubectl.Apply(FakeTool(), s => s
            .AddFile("svc.yaml").AddFile("deploy.yaml")
            .SetRecursive()
            .SetNamespace("prod"));

        var settings = new KubectlApplySettings { Recursive = true, Namespace = "prod" };
        settings.Files.Add("svc.yaml");
        settings.Files.Add("deploy.yaml");
        var objInit = Kubectl.Apply(FakeTool(), settings);

        Assert.Equal(fluent.Arguments, objInit.Arguments);
    }

    [Fact]
    public void Scale_ObjectInit_Identical_To_Fluent()
    {
        var fluent = Kubectl.Scale(FakeTool(), s => s
            .SetResource("deployment/my-app").SetReplicas(5).SetCurrentReplicas(3));

        var settings = new KubectlScaleSettings
        {
            Resource = "deployment/my-app",
            Replicas = 5,
            CurrentReplicas = 3,
        };
        var objInit = Kubectl.Scale(FakeTool(), settings);

        Assert.Equal(fluent.Arguments, objInit.Arguments);
    }

    // ============================== executable ==============================

    [Fact]
    public void Executable_Is_Tool_Path()
    {
        var plan = Kubectl.Get(FakeTool(), s => s.SetResource("pods"));
        Assert.Equal(FakeToolPath, plan.Executable);
    }

    // ============================== realistic deploy chain ==============================

    [Fact]
    public void Realistic_Deploy_Apply_Then_Rollout_Status_Then_Logs()
    {
        // 1. Apply manifests
        var applyPlan = Kubectl.Apply(FakeTool(), s => s
            .SetContext("prod-cluster")
            .SetNamespace("billing")
            .AddFile("manifests")
            .SetRecursive()
            .SetServerSide()
            .SetFieldManager("tamp-deploy"));
        Assert.Contains("apply", applyPlan.Arguments);
        Assert.Contains("--server-side", applyPlan.Arguments);

        // 2. Wait for rollout
        var rolloutPlan = Kubectl.RolloutStatus(FakeTool(), s => s
            .SetContext("prod-cluster")
            .SetNamespace("billing")
            .SetResource("deployment/billing-api")
            .SetTimeout(TimeSpan.FromMinutes(5)));
        Assert.Contains("rollout", rolloutPlan.Arguments);
        Assert.Contains("status", rolloutPlan.Arguments);
        Assert.Contains("--timeout=5m", rolloutPlan.Arguments);

        // 3. Tail logs if something went wrong
        var logsPlan = Kubectl.Logs(FakeTool(), s => s
            .SetContext("prod-cluster")
            .SetNamespace("billing")
            .SetLabelSelector("app=billing-api")
            .SetTail(200)
            .SetTimestamps());
        Assert.Contains("logs", logsPlan.Arguments);
        Assert.Contains("--tail=200", logsPlan.Arguments);
    }

    // ============================== boundary fuzz ==============================

    [Theory]
    [InlineData("manifests/path with spaces/")]
    [InlineData("manifests/Δ-π/")]
    [InlineData("manifests/sub'quote/")]
    public void File_Path_Roundtrips_Verbatim(string path)
    {
        var plan = Kubectl.Apply(FakeTool(), s => s.AddFile(path));
        Assert.Contains(path, plan.Arguments);
    }

    [Fact]
    public void Bulk_SetImage_All_Pairs_Emit()
    {
        var faker = new Faker();
        var pairs = Enumerable.Range(0, 12)
            .Select(_ => (Container: faker.Hacker.Noun() + faker.Random.AlphaNumeric(4), Image: $"reg.example/{faker.Hacker.Verb()}:v{faker.Random.Int(1, 99)}"))
            .GroupBy(p => p.Container).Select(g => g.First())
            .ToList();

        var plan = Kubectl.SetImage(FakeTool(), s =>
        {
            s.SetResource("deployment/my-app");
            foreach (var (c, i) in pairs) s.SetContainerImage(c, i);
        });

        foreach (var (c, i) in pairs)
        {
            Assert.Contains($"{c}={i}", plan.Arguments);
        }
    }
}

/// <summary>Tests for the <see cref="KubectlSettingsBase.FormatGoDuration"/> helper (exercised indirectly elsewhere; pinned here so changes are explicit).</summary>
public sealed class KubectlGoDurationTests
{
    [Theory]
    [InlineData(0.5, "500ms")]
    [InlineData(30, "30s")]
    [InlineData(60, "1m")]
    [InlineData(90, "1m")]
    [InlineData(3600, "1h0m")]
    [InlineData(5400, "1h30m")]
    public void FormatGoDuration_Renders_Expected(double seconds, string expected)
    {
        var actual = KubectlSettingsBase.FormatGoDuration(TimeSpan.FromSeconds(seconds));
        Assert.Equal(expected, actual);
    }
}
