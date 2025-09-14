# Chat-Bro Constitution


## Core Principles

### PRINCIPLE I. Test-First (NON-NEGOTIABLE)
TDD mandatory: Tests written → User approved → Tests fail → Then implement; 
Red-Green-Refactor cycle strictly enforced.


### PRINCIPLE II. Integration Testing
Integration test scenarios should be written with Gherkin language. Scenario should include 
feature description and value it provides.

Focus areas requiring integration tests: 
- New library contract tests, 
- Contract changes, 
- Inter-service communication, 
- Shared schemas

Integration tests run should provide console and html file reports with scenarios executed and status.


### PRINCIPLE III. Observability
Structured logging required. OTEL distributed traces required.


### PRINCIPLE IV. Simplicity
Start simple. Prefere minimal code changes.


### PRINCIPLE V. End-to-End Incremental Features (NON-NEGOTIABLE)
Features must be implemented as minimal but end-to-end slices, starting from the user-facing entry point (UI or API) down through the stack. Each task must deliver visible, verifiable user value.
Stub implementations are acceptable in early iterations but must be progressively replaced with real implementations in subsequent tasks.


## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

[GOVERNANCE_RULES]
<!-- Example: All PRs/reviews must verify compliance; Complexity must be justified; Use [GUIDANCE_FILE] for runtime development guidance -->

**Version**: [CONSTITUTION_VERSION] | **Ratified**: [RATIFICATION_DATE] | **Last Amended**: [LAST_AMENDED_DATE]
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->