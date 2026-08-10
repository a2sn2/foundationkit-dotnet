using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1720:Identifier contains type name",
    Justification = "ComposerResourceIdType intentionally mirrors the closed manifest idType vocabulary: guid, string, long, int.",
    Scope = "type",
    Target = "~T:FoundationKit.Composer.ComposerResourceIdType")]

[assembly: SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "The parser helper returns IReadOnlyList to preserve the immutable manifest-model boundary; its bounded input is at most the closed behavior set.",
    Scope = "member",
    Target = "~M:FoundationKit.Composer.ComposerManifestParser.NormalizeBehaviors(System.Collections.Generic.IReadOnlyList{System.String},System.String,System.String)~System.Collections.Generic.IReadOnlyList{FoundationKit.Composer.ComposerResourceBehavior}")]

[assembly: SuppressMessage(
    "Globalization",
    "CA1305:Specify IFormatProvider",
    Justification = "The generated Markdown report formats only bounded non-negative configuration integers; canonical JSON and generated C# remain culture-independent and are the machine-readable source of truth.",
    Scope = "member",
    Target = "~M:FoundationKit.Composer.ComposerProjectModelGenerator.BuildProjectModelReport(FoundationKit.Composer.ComposerManifest)~System.String")]

[assembly: SuppressMessage(
    "Globalization",
    "CA1305:Specify IFormatProvider",
    Justification = "The executable generator interpolates only validated identifiers, closed enum names, deterministic hashes and bounded configuration integers into normalized generated source/report text; no locale-sensitive business data is formatted.",
    Scope = "type",
    Target = "~T:FoundationKit.Composer.ComposerExecutableResourceGenerator")]
