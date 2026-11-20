# Changelog

All notable changes to `Tamp.Kubectl` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-11-20 - .NET 11 in, .NET 8 and .NET 9 out

### Changed

- **Multi-targets `net10.0;net11.0`** instead of `net8.0;net9.0;net10.0` ([ADR 0015](https://github.com/tamp-build/tamp/blob/main/docs/adr/0015-target-framework-strategy.md)). .NET 11 shipped 2026-11-10 and .NET 8 and .NET 9 reached end of support the same day; the rule is to add a TFM the day a release ships and drop one the day Microsoft ends its support. `global.json` pins SDK 11.0.100.

  **If you target `net8.0` or `net9.0`, stay on the previous version** — this one carries no assets your project can resolve.

- **`Tamp.*` pinned to 1.21.0**, which carries a SARIF fix worth knowing about if this project's output is read as evidence: before 1.21.0, a SARIF result whose scanner omitted `level` was read as `none` — the least severe value — instead of the declared `warning`, on every runtime before net11 ([tamp#166](https://github.com/tamp-build/tamp/issues/166)).

- **Publishing moved to nuget.org trusted publishing (OIDC).** The `tamp_nuget_api_key` org secret expired 2026-11-01 along with every other pre-2026-08-17 nuget.org key. The publish job now mints a short-lived key from its own OIDC token, so there is no long-lived credential to rotate or leak.

## [0.1.1] — 2026-09-27

### Added

- Initial release. 12-verb Tamp wrapper for the `kubectl` CLI:
  - `Kubectl.Apply` — `-f file/url/dir` or `-k kustomize-dir`; `-R` recursive; `--prune` + `--selector`; `--server-side` + `--field-manager`; `--force`.
  - `Kubectl.Get` — resource + names; `-l` label / `--field-selector`; `-A` all-namespaces; `--watch`; `--show-labels`; `--no-headers`.
  - `Kubectl.Delete` — resource+names mode OR file mode OR `--all`; `--grace-period`, `--force`, `--wait`, `--ignore-not-found` (idempotent CI delete).
  - `Kubectl.Describe` — resource + names; label selector.
  - `Kubectl.RolloutStatus` — watch toggle, `--timeout` (Go-duration), `--revision`.
  - `Kubectl.RolloutRestart` — patch-driven rolling restart.
  - `Kubectl.RolloutUndo` — optional `--to-revision`.
  - `Kubectl.PortForward` — string OR `(int local, int remote)` mappings; `--address`; `--pod-running-timeout`.
  - `Kubectl.Logs` — pod or label selector; `-c` container / `--all-containers`; `-f`; `--tail`; `--previous`; `--since`; `--timestamps`.
  - `Kubectl.Exec` — pod + container; `-i` / `-t`; command list emitted after `--`.
  - `Kubectl.Scale` — `--replicas` + optional compare-and-set via `--current-replicas`; timeout.
  - `Kubectl.SetImage` — multiple `container=image` pairs; `--all`; label selector.
- Common base `KubectlSettingsBase` shared across every verb: `--context`, `-n` namespace, impersonation (`--as` / `--as-group`), dry-run strategy, `-o` output, `-v` verbosity, request-timeout, working directory, env vars.
- **Kubeconfig flows as `KUBECONFIG` env var** rather than `--kubeconfig` CLI flag — keeps the path out of the OS process table.
- Fluent + object-init authoring parity on every verb.
- Multi-target `net8.0;net9.0;net10.0`.
- 67 unit tests across all three TFMs covering per-verb arg shape, validation, mutex constraints (file/kustomize, all-namespaces/namespace, resource/file delete modes), Go-duration formatting, kubeconfig env-var-vs-flag posture, object-init parity, realistic deploy chain composition.
- Package now ships XML documentation files (`.xml`) alongside the assembly, so consumers get IntelliSense and API docs. (Mirrors [tamp-build/tamp#3](https://github.com/tamp-build/tamp/pull/50).)

### Closes

- TAM-221 — Tamp.Kubectl typed wrapper for kubectl (apply / get / delete / rollout / port-forward).
