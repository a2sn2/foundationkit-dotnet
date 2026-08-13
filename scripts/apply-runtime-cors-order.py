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
