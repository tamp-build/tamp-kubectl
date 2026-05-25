# Changelog

All notable changes to `Tamp.Kubectl` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — Unreleased

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

### Closes

- TAM-221 — Tamp.Kubectl typed wrapper for kubectl (apply / get / delete / rollout / port-forward).
