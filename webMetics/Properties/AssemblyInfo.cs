using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("webMetics.Tests")]

// The InternalsVisibleTo attribute should include a public key token to prevent unauthorized assemblies from accessing internal members. Without it, any assembly named 'webMetics.Tests' can access internals, creating a potential security vulnerability.