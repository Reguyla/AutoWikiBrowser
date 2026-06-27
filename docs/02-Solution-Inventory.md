## Plugin Strategy

Initial assessment: no bundled plugin is considered essential to the core modernization effort.

The plugin architecture itself should be preserved, because future extensions may use it. However, most existing bundled plugins can be excluded from the initial migration path to reduce noise and complexity.

### Initial classification

- Essential bundled plugins: none
- Candidate plugins to retain/review: TBD
- Candidate plugins to remove/defer: most bundled plugins

### Migration implication

The first modernization pass should focus on the core application, shared libraries, and plugin interface/host behavior rather than upgrading every bundled plugin project.