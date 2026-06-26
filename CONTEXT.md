# Loom

Loom is a recipe engine context for composing application provisioning and configuration work from declarative recipes and reusable steps.

## Language

**Recipe**:
A declarative description of work Loom can validate, discover, and run.
_Avoid_: Workflow, script, manifest

**Recipe step**:
One ordered item of work inside a recipe, identified by a step type and optional step ID.
_Avoid_: Task, action, operation

**Typed step**:
A recipe step authored as a .NET type whose inputs, validation, execution, and outputs are interpreted by Loom.
_Avoid_: Handler class, plugin, command

**Typed step authoring**:
The way Loom lets recipe authors define typed steps and have them participate in validation and execution like any other recipe step.
_Avoid_: Typed handler registration, reflection binding
