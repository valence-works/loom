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
        "tenant": "[js: variables('tenantId')]"
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
- `steps[].id`: Optional string. Required when referenced by `dependsOn` or by provider-specific interpolation.
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

Interpolation uses host-registered providers selected by a prefixed directive envelope. Loom owns the `[prefix: expression]` envelope and routes the expression to the provider registered for `prefix`; the provider owns the expression syntax.

Initial Jint provider examples:

```json
{
  "tenant": "[js: variables('tenantId')]",
  "adminId": "[js: output('create-admin', 'id')]"
}
```

Required behavior:

- Host code registers available interpolation providers; recipe JSON can only reference registered prefixes.
- Unknown prefixes produce diagnostics before execution where practical.
- Provider validation failures, missing values, unsupported expressions, and resolution failures produce diagnostics.
- The initial `js` provider exposes `variables(name)` and `output(stepId, name)` helpers.

Identifier constraints:

- Provider prefixes must match `^[A-Za-z][A-Za-z0-9_-]*$` and are case-insensitive unique within a registry.
- Step IDs referenced by `dependsOn` must match `^[A-Za-z_][A-Za-z0-9_-]*$`.
- Provider-specific variable, step ID, or output-name constraints are owned by the registered provider.

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
