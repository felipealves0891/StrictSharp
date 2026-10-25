# Gate contract

`gates.json` must contain:

```json
{
  "schema_version": "1.0",
  "feature_id": "001-feature-name",
  "execution_id": "gate-2026-01-01T120000Z",
  "code_revision": "git-or-local-revision",
  "config_hash": "sha256:...",
  "artifact_hashes": {},
  "started_at": "2026-01-01T12:00:00Z",
  "duration_ms": 0,
  "gates": [
    {
      "id": "GATE-001",
      "name": "build",
      "required": true,
      "status": "passed",
      "command": "...",
      "exit_code": 0,
      "duration_ms": 0,
      "diagnostics": [],
      "evidence": []
    }
  ],
  "result": "passed"
}
```

Allowed gate statuses are `passed`, `failed`, `blocked`, `error`, and `skipped`. Allowed aggregate results are `passed`, `failed`, `blocked`, and `error`.
