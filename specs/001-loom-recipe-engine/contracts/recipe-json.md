# Contract: V1 Recipe JSON

V1 serialized recipes use JSON only.

## Top-Level Recipe Shape

```json
{
  "name": "Initial Setup",
  "version": "1.0.0",
  "description": "Optional description",
  "metadata": {
    "owner": "platform"
  },
  "variables": {
    "tenantId": "acme"
  },
  "steps": [
    {
      "id": "create-admin",
      "type": "create-user",
      "input": {
        "email": "admin@acme.local"
      }
    },
    {
      "id": "enable-workflows",
      "type": "enable-feature",
      "dependsOn": ["create-admin"],
      "input": {
        "feature": "Workflows",
        "tenant": "{{ variables.tenantId }}"
      }
    }
  ]
}
```

## Required Fields

- `name`: Required non-empty string.
- `steps`: Required non-empty array.
- `steps[].type`: Required non-empty string.

## Optional Fields

- `version`: Optional string. Participates in recipe identity when present.
- `description`: Optional string.
- `metadata`: Optional object.
- `variables`: Optional object.
- `steps[].id`: Optional string. Required when referenced by `dependsOn` or previous-output interpolation.
- `steps[].dependsOn`: Optional array of step IDs. Validation-only in V1.
- `steps[].input`: Optional object containing handler-owned input.

## Identity Rules

- Recipe identity is `name + version`.
- Missing `version` means one unversioned identity for the recipe name.

## Dependency Rules

- `dependsOn` values reference step IDs.
- Referenced step IDs must exist.
- Referenced step IDs must be unique within the recipe.
- Cycles produce validation diagnostics.
- Dependencies do not affect execution order in V1.

## Interpolation Rules

Supported V1 examples:

```json
{
  "tenant": "{{ variables.tenantId }}",
  "adminId": "{{ steps.create-admin.id }}"
}
```

Required behavior:

- Variables are referenced through `variables.<name>`.
- Previous step outputs are referenced through `steps.<stepId>.<outputName>`.
- Step output references require the referenced step to have an ID.
- Missing variables or step outputs produce diagnostics.

Identifier constraints:

- Variable names, step IDs used in interpolation, and output names referenced by interpolation must match `^[A-Za-z_][A-Za-z0-9_-]*$`.
- `.` is not allowed inside variable names, step IDs used in interpolation, or output names referenced by interpolation because `.` is the V1 path delimiter.
- V1 does not support escaping or bracket syntax for interpolation paths. Recipes needing delimiter-like characters in display names should keep those values in metadata or handler-owned input, not in interpolation identifiers.
- Step IDs referenced by `dependsOn` must follow the same identifier rule, so dependency and interpolation references share one identifier model.

Out of scope for V1:

- Generated values.
- Environment lookup.
- Conditionals.
- Date/time helpers.
- Configuration lookup.
- Custom expression functions/providers.

## Diagnostics and Redaction

When validation or execution reports diagnostics for JSON recipe input:

- Show recipe name/version, source, step ID, step type, field names, reference names, and JSON location where practical.
- Redact recipe variable values, step input values, and handler output values by default.
