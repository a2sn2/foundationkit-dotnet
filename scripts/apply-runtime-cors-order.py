from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    file_path = Path(path)
    text = file_path.read_text(encoding="utf-8")
    if new in text:
        return
    if old not in text:
        raise SystemExit(f"Expected source block not found in {path}")
    file_path.write_text(text.replace(old, new, 1), encoding="utf-8")


replace_once(
    "tools/FoundationKit.Composer/ComposerBlazorRuntimeGenerator.cs",
    '''            const string pipelineNeedle = "app.UseFoundationRequestDiagnostics();";
            if (!program.Contains(pipelineNeedle, StringComparison.Ordinal))
                throw new ComposerGenerationException("Generated API Program.cs has an unexpected middleware shape.");

            var pipelineReplacement = $$"""
                {{pipelineNeedle}}
                if (app.Environment.IsDevelopment())
                    app.UseCors("{{LocalCorsPolicy}}");
                """;
            program = program.Replace(pipelineNeedle, pipelineReplacement, StringComparison.Ordinal);
''',
    '''            const string pipelineNeedle = "app.UseSwagger();";
            if (!program.Contains(pipelineNeedle, StringComparison.Ordinal))
                throw new ComposerGenerationException("Generated API Program.cs has an unexpected middleware shape.");

            var pipelineReplacement = $$"""
                if (app.Environment.IsDevelopment())
                    app.UseCors("{{LocalCorsPolicy}}");
                {{pipelineNeedle}}
                """;
            program = program.Replace(pipelineNeedle, pipelineReplacement, StringComparison.Ordinal);
''',
)

replace_once(
    "tests/FoundationKit.Tests/ComposerBlazorRuntimeGenerationTests.cs",
    '''            Assert.Contains("GeneratedLocalClient", apiProgram, StringComparison.Ordinal);
            Assert.Contains("uri.IsLoopback", apiProgram, StringComparison.Ordinal);
            Assert.Contains("app.UseCors", apiProgram, StringComparison.Ordinal);
''',
    '''            Assert.Contains("GeneratedLocalClient", apiProgram, StringComparison.Ordinal);
            Assert.Contains("uri.IsLoopback", apiProgram, StringComparison.Ordinal);
            Assert.Contains("app.UseCors", apiProgram, StringComparison.Ordinal);
            var corsIndex = apiProgram.IndexOf("app.UseCors", StringComparison.Ordinal);
            var swaggerIndex = apiProgram.IndexOf("app.UseSwagger();", StringComparison.Ordinal);
            Assert.True(corsIndex >= 0 && swaggerIndex >= 0 && corsIndex < swaggerIndex,
                "Development CORS must run before Swagger so browser clients can read runtime OpenAPI cross-origin.");
''',
)

workflow = Path(".github/workflows/composer-blazor-runtime.yml")
workflow_text = workflow.read_text(encoding="utf-8")
marker = "      - name: Smoke-test generated Client static host and FoundationKit assets\n"
if "Smoke-test browser-origin access to generated runtime OpenAPI" not in workflow_text:
    if marker not in workflow_text:
        raise SystemExit("Runnable Blazor workflow insertion marker was not found")
    cors_steps = r'''      - name: Generate temporary SQL credential for browser-origin runtime proof
        shell: bash
        run: |
          password="Fkit!$(openssl rand -hex 12)Aa1"
          echo "::add-mask::$password"
          echo "FOUNDATIONKIT_SQL_PASSWORD=$password" >> "$GITHUB_ENV"

      - name: Start SQL Server for browser-origin runtime proof
        shell: bash
        run: |
          docker run --detach \
            --name foundationkit-blazor-runtime-sql \
            --publish 127.0.0.1:14335:1433 \
            --env ACCEPT_EULA=Y \
            --env MSSQL_PID=Developer \
            --env MSSQL_SA_PASSWORD="$FOUNDATIONKIT_SQL_PASSWORD" \
            mcr.microsoft.com/mssql/server:2022-latest

      - name: Wait for SQL Server browser-origin runtime proof
        shell: bash
        run: |
          sqlcmd_path=""
          for attempt in $(seq 1 120); do
            if [ -z "$sqlcmd_path" ]; then
              sqlcmd_path="$(docker exec foundationkit-blazor-runtime-sql sh -lc 'if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then echo /opt/mssql-tools18/bin/sqlcmd; elif [ -x /opt/mssql-tools/bin/sqlcmd ]; then echo /opt/mssql-tools/bin/sqlcmd; fi' 2>/dev/null || true)"
            fi
            if [ -n "$sqlcmd_path" ] && docker exec \
              --env SQLCMDPASSWORD="$FOUNDATIONKIT_SQL_PASSWORD" \
              foundationkit-blazor-runtime-sql \
              "$sqlcmd_path" -S localhost -U sa -C -Q "SELECT 1" >/dev/null 2>&1; then
              exit 0
            fi
            sleep 2
          done
          docker logs foundationkit-blazor-runtime-sql --tail 300
          exit 1

      - name: Smoke-test browser-origin access to generated runtime OpenAPI
        shell: bash
        run: |
          connection="Server=127.0.0.1,14335;Database=FoundationKitBlazorCorsProof;User Id=sa;Password=$FOUNDATIONKIT_SQL_PASSWORD;TrustServerCertificate=True;Encrypt=False"
          echo "::add-mask::$connection"
          ASPNETCORE_URLS="http://127.0.0.1:5100" \
          ASPNETCORE_ENVIRONMENT=Development \
          ConnectionStrings__Generated="$connection" \
          dotnet run \
            --project artifacts/composer-blazor-runtime/src/ComposerBlazorRuntime.Api/ComposerBlazorRuntime.Api.csproj \
            --configuration Release --no-build --no-restore --no-launch-profile \
            >/tmp/composer-blazor-api.log 2>&1 &
          api_pid=$!
          cleanup() {
            kill "$api_pid" 2>/dev/null || true
            docker rm -f foundationkit-blazor-runtime-sql >/dev/null 2>&1 || true
          }
          trap cleanup EXIT

          for attempt in $(seq 1 120); do
            if curl -fsS http://127.0.0.1:5100/api/foundationkit/health >/dev/null; then
              break
            fi
            if ! kill -0 "$api_pid" 2>/dev/null; then
              cat /tmp/composer-blazor-api.log
              exit 1
            fi
            sleep 1
          done

          curl -fsS \
            -H 'Origin: http://localhost:7560' \
            -D /tmp/openapi-loopback.headers \
            http://127.0.0.1:5100/swagger/v1/swagger.json \
            -o /tmp/openapi-loopback.json
          grep -Eiq '^access-control-allow-origin: http://localhost:7560\r?$' /tmp/openapi-loopback.headers
          python3 - <<'PY'
          import json
          with open('/tmp/openapi-loopback.json', encoding='utf-8') as handle:
              document = json.load(handle)
          assert document.get('paths'), 'runtime OpenAPI paths must not be empty'
          PY

          curl -fsS \
            -H 'Origin: https://not-loopback.example' \
            -D /tmp/openapi-external.headers \
            http://127.0.0.1:5100/swagger/v1/swagger.json \
            -o /dev/null
          if grep -Eiq '^access-control-allow-origin:' /tmp/openapi-external.headers; then
            echo "Non-loopback origin unexpectedly received a CORS allow-origin header."
            cat /tmp/openapi-external.headers
            exit 1
          fi

'''
    workflow.write_text(workflow_text.replace(marker, cors_steps + marker, 1), encoding="utf-8")
