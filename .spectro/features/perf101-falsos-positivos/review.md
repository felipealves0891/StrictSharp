<!-- spectro-managed: true -->
# Review: Redução de falsos positivos na PERF101

## Summary

- Review ID: `review-20261105T141306Z`
- Code revision: `3753a38636913b870a73a752047b8d3ff1f36157`
- Decision: `changes_requested`
- Review packet: `.spectro/features/perf101-falsos-positivos/reviews/review-20261105T141306Z/`

The implementation addresses several parts of the previous finding: it rejects properties, `IndexOf`, multiple recognized searches, direct/indirect source uses inside the containing loop, and unrelated loops. The required gates pass for the reviewed code revision and traceability is complete. Approval remains blocked by a residual High semantic-safety gap.

## Artifacts examined

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`
- `.spectro/constitution.md`
- `gates.json` and evidence from `gate-20261105T141138Z`
- Review packet, manifest, diff, and traceability for this review
- `Perf101Analyzer.cs`, `ComplexityOpportunityInference.cs`, `ComplexityOpportunity.cs`, `CostEvidence.cs`, `V5DiagnosticAnalysis.cs`, `RuleAnalysis.cs`
- PERF101/V5 tests and documentation

## Revision, traceability, and gates

- `gates.json.code_revision` matches `HEAD` at review preparation: `3753a38636913b870a73a752047b8d3ff1f36157`.
- The gate artifact hashes match the review packet manifest at preparation time.
- `validate_gates.mjs`: valid report.
- `validate_traceability.ps1`: `complete`; no uncovered requirements or acceptance criteria.
- Build: passed.
- Tests: passed, 365 tests.
- Security: passed; no vulnerable packages reported.
- Coverage: passed; line 96.24%, branch 85.72%, new line 95.43%, new branch 85.08%.

## Verified requirements

The current implementation and tests verify the silent behavior for necessary linear traversal, unresolved key shapes, property sources, `IndexOf` use in the loop, direct/indirect source use in the loop, multiple recognized searches, unrelated loops, and non-`List` sources. The analyzer preserves `PERF101`, severity, V5 placement, cancellation checks, local deterministic analysis, and messages without percentage or runtime claims.

`FR-003`, `NFR-003`, `CON-004`, `AC-003`, and `AC-004` remain not fully verified because semantic compatibility is still inferred from a narrow syntactic pattern rather than from all uses of the source in the method.

## Findings

### FINDING-003

- Severity: `High`
- Category: Functional / consistency
- Location: `src/StrictSharp.Analyzers/V5/ComplexityOpportunityInference.cs:79-109`
- Evidence: `HasOtherSourceUse` scans only the containing loop. A method such as `int M(List<int> values, int[] keys) { foreach (var key in keys) if (values.Contains(key)) _ = key; return values[0]; }` has exactly one accepted `Contains`, no other source use in that loop, and therefore returns a known opportunity. Replacing the list with a hash-based structure cannot preserve the later index-based access. The same gap applies to order/duplicate-sensitive uses after the loop.
- Impact: `PERF101` can still emit the prescriptive opportunity warning when the method's complete semantics do not support the proposed structural replacement, violating `AC-004` and `CON-004`.
- Recommendation: Analyze all uses of the source symbol in the containing method (or conservatively require the source to have no other use beyond the recognized receiver) and reject index/order/duplicate-sensitive operations, aliases, escaping calls, and other unresolved uses anywhere in the method. Add behavior-focused analyzer and inference tests for post-loop indexing, enumeration, count/order use, and alias escape. Keep unsupported cases silent.
- Return stage: `implement`

### FINDING-004

- Severity: `Medium`
- Category: Consistency
- Location: `.spectro/features/perf101-falsos-positivos/tasks.md:21-24`
- Evidence: The progress section contains two `Concluídas` entries, one ending at `TASK-004` and another ending at `TASK-REV-001`. `TASK-REV-001` is marked completed even though this review identified a residual High finding within the same semantic-safety scope.
- Impact: The workflow record can suggest that the remediation is complete before the review requirement is actually satisfied.
- Recommendation: Reopen or extend `TASK-REV-001` for `FINDING-003`, remove the duplicate progress entry, and mark it completed only after implementation, renewed gates, and review approval.
- Return stage: `implement`

## Residual risks

- The approved opportunity pattern is intentionally conservative and may have low recall.
- Passing coverage does not prove semantic equivalence of changing collection structures.
- The current review packet is an immutable snapshot; remediation must be followed by new gates and a new review.

## Final decision

`changes_requested`. Return to `implement` for `FINDING-003`, reconcile the task progress record, then renew gates and review. No Critical findings were identified.
