<!-- spectro-managed: true -->
# Review: Redução de falsos positivos na PERF101

## Summary

- Review ID: `review-20261105T134553Z`
- Code revision: `d98166c730f7e7794667f3f9520517208c66bf79`
- Decision: `changes_requested`
- Review packet: `.spectro/features/perf101-falsos-positivos/reviews/review-20261105T134553Z/`

The implementation preserves the `PERF101` identifier and keeps complexity classification separate from the analyzer adapter. The recorded build, test, security, and coverage gates pass for the reviewed revision, and traceability has no uncovered requirements or acceptance criteria. Approval is prevented by one unresolved High finding in the opportunity inference.

## Artifacts examined

- `spec.md`, `clarifications.md`, `plan.md`, `tasks.md`
- `gates.json` and evidence from `gate-20261105T134418Z`
- `.spectro/constitution.md`
- Review packet, manifest, diff, and traceability generated for this review
- `Perf101Analyzer.cs`, `ComplexityOpportunityInference.cs`, `ComplexityOpportunity.cs`, `CostEvidence.cs`, `V5DiagnosticAnalysis.cs`, `RuleAnalysis.cs`
- PERF101/V5 analyzer and inference tests and the related documentation

## Revision and gate verification

- `gates.json.code_revision` matches `HEAD` and the review manifest revision.
- The artifact hashes in `gates.json` match the review packet manifest at preparation time.
- `GATE-001` build: passed.
- `GATE-002` tests: passed, 362 tests.
- `GATE-003` security: passed; no vulnerable packages reported.
- `GATE-004` coverage: passed; line 96.52%, branch 85.86%, new line 95.75%, new branch 85.30%.
- `validate_traceability.ps1`: `complete`; no uncovered requirements or acceptance criteria.

## Verified requirements

The reviewed diff satisfies the observable parts of `FR-001`, `FR-004`, `FR-005`, `NFR-002`, and `CON-001` that are covered by the current tests: a bare linear traversal is silent, the positive example reports `PERF101` with `O(n)` and no percentage claim, the ID/severity remain unchanged, and the implementation is deterministic/local. The tests also cover unresolved key syntax, direct mutation in the containing loop, non-`List` sources, and factory-call sources.

`FR-002`, `FR-003`, `NFR-003`, `CON-004`, and `AC-003`/`AC-004`/`AC-006` are not sufficiently verified because the implementation accepts a broader set of semantic situations than the approved contract proves safe.

## Findings

### FINDING-001

- Severity: `High`
- Category: Functional / consistency
- Location: `src/StrictSharp.Analyzers/V5/ComplexityOpportunityInference.cs:29-55`, `src/StrictSharp.Analyzers/Analyzers/Perf101Analyzer.cs:27-34`
- Evidence: `IsRepeatedStableListSearch` returns `true` for any resolved `List<T>.Contains` or `List<T>.IndexOf` invocation inside a loop when the argument is a local/parameter of the element type and the directly inspected source is not mutated in that loop. It does not establish that replacing the list with a hash-based structure preserves order, duplicate-sensitive behavior, later `IndexOf` use, ownership/aliases, indirect mutation, or construction cost. For example, a method that calls `values.Contains(key)` and then uses `values.IndexOf(key)` is accepted, even though a hash-based replacement cannot preserve the indexed/order-dependent operation. A call such as `Mutate(values)` is also not treated as mutation by the helper, so an unresolved alias can still be reported.
- Impact: `PERF101` can emit the prescriptive opportunity warning in cases covered by `AC-004` and `CON-004`, reintroducing the false-positive class the feature is intended to remove. Because the analyzer applies one method-level opportunity to every complexity item (`Perf101Analyzer.cs:31-34`), an unrelated known complexity item in the same method can also inherit the opportunity explanation.
- Recommendation: Make opportunity evidence tied to the specific repeated-search operation and require demonstrable semantic compatibility before returning `RepeatedListSearch`. Conservatively reject order/duplicate-sensitive uses, aliases or unresolved calls that may mutate the source, and cases where construction/ownership/thread-safety cannot be established. Add analyzer tests for `IndexOf`/order usage, duplicate-sensitive behavior, indirect mutation, and a method containing an unrelated complexity item. Keep unsupported cases silent.
- Return stage: `implement`

### FINDING-002

- Severity: `Medium`
- Category: Consistency
- Location: `.spectro/features/perf101-falsos-positivos/tasks.md:10,24` and `gates.json`/gate evidence
- Evidence: `tasks.md` still marks `TASK-005` as `blocked` because the security gate lacked feed access, while the current `gate-20261105T134418Z` records `GATE-003` as passed and its evidence reports no vulnerable packages. The feature state is `completed`, and all four current gates pass.
- Impact: The workflow artifacts disagree about completion and security evidence, which can mislead a later reviewer about whether the approved gate set is valid.
- Recommendation: Update the task progress/status to reflect the current gate execution, or record the exact reason the earlier block remains relevant. Re-run or invalidate dependent evidence if the artifact state is intentionally changed.
- Return stage: `gates`

## Residual risks

- The accepted opportunity pattern is intentionally narrow, so recall may remain low after the High finding is fixed.
- Coverage thresholds pass, but coverage does not demonstrate semantic equivalence of data-structure substitutions; focused negative tests are required.
- The current review packet is an immutable snapshot. The remediation task added after packet creation is `TASK-REV-001` and must be completed before re-review.

## Final decision

`changes_requested`. The implementation must return to `implement` for `FINDING-001`; after remediation and relevant tests, the gates and review should be renewed. `FINDING-002` should be reconciled during the gate/evidence update. No Critical findings were identified.
