---
name: verify-api
description: Start the MysticForge API and verify /healthz responds Healthy — confirms DI and config are wired up correctly
disable-model-invocation: true
---

1. Ensure Docker containers are running: `docker compose up -d`
2. Start the API in the background:
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/MysticForge.Api --no-launch-profile --urls=http://localhost:5181 &
   API_PID=$!
   ```
3. Wait for startup (up to 15 seconds), polling /healthz:
   ```bash
   for i in $(seq 1 15); do
     result=$(curl -s http://localhost:5181/healthz 2>/dev/null)
     [ "$result" = "Healthy" ] && echo "API healthy after ${i}s" && break
     sleep 1
   done
   ```
4. Report the result. If not healthy after 15s, show the last few lines of output.
5. Kill the background process: `kill $API_PID 2>/dev/null`

Connection string to use (reads from Docker compose defaults):
`Host=localhost;Port=5432;Database=mysticforge;Username=mysticforge;Password=devpassword`
