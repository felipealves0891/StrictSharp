---
name: spectro-gates
description: Run configured local quality gates for a Spectro feature, capture reproducible evidence, and produce a structured gates.json result.
metadata:
  short-description: Validate a Spectro implementation
---

# Spectro Gates

Use this skill after implementation or after a correction that invalidates prior evidence.

## Inputs

- Feature artifacts: `spec.md`, `plan.md`, `tasks.md`, and `.spectro/config.yaml`.
- Current code revision and repository status.
- Gate commands configured for the project.

## Workflow

1. Resolve the feature directory and record the code revision, configuration version, artifact hashes, and execution ID.
2. Load configured gates and classify each as required or optional.
3. Run gates in fail-fast order: build, static analysis, security, unit tests, integration tests, performance when enabled, then acceptance.
4. Capture command, exit status, duration, tool version, concise diagnostics, and evidence path for every gate.
5. Preserve evidence without recording secrets; clean up temporary resources even on failure.
6. Write `gates.json` using the schema in `references/gate-contract.md`.
7. Aggregate the result: required failures, blocks, or errors prevent approval; optional skips remain visible.

## Rules

- A failed external security scan is not evidence that security passed.
- Evidence from a different code or configuration revision is invalid.
- Do not silently skip a configured required gate.
- Distinguish implementation defects, test defects, environment failures, specification gaps, and planning defects.
- After any code or configuration change, invalidate and rerun affected gates.
- Use `scripts/recover.ps1` after a failed execution to classify the failure, enforce the retry limit, persist state, and optionally run an explicitly authorized correction command.

## Completion

Report the gates result, failed or blocked diagnostics, evidence paths, aggregate status, and recommended return stage.

## Local runner

Use `scripts/run_gates.ps1` from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File skills/spectro-gates/scripts/run_gates.ps1 `
  -FeatureId 001-feature-name
```

Configure commands in `.spectro/config.yaml` under `gates.commands`. An empty command produces a visible `blocked` result; it is never treated as a pass. Validate an existing report with `scripts/validate_gates.ps1`.

Recovery state is stored in `.spectro/features/<feature-id>/state.json`; previous gate reports remain available for audit and are marked invalidated after a correction.
