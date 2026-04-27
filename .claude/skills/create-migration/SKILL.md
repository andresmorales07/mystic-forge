---
name: create-migration
description: Add a new EF Core migration to MysticForge — wraps dotnet ef migrations add with the correct project flags
disable-model-invocation: true
---

Run the following command, replacing `<MigrationName>` with the PascalCase name the user provides:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/MysticForge.Infrastructure \
  --startup-project src/MysticForge.Api
```

After the command completes:
1. Open the generated `src/MysticForge.Infrastructure/Migrations/<timestamp>_<MigrationName>.cs` file and confirm the Up/Down methods look correct.
2. Do NOT edit the `.Designer.cs` file or `MysticForgeDbContextModelSnapshot.cs` — those are machine-generated.
3. Remind the user to run `dotnet ef database update --project src/MysticForge.Infrastructure --startup-project src/MysticForge.Api` when ready to apply.
