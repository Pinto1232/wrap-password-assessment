using WrapPassword.Application.Services;
using WrapPassword.Application.UseCases;
using WrapPassword.Infrastructure.Files;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    PrintUsage();
    return 0;
}

if (!string.Equals(args[0], "generate", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    PrintUsage();
    return 1;
}

if (args.Length > 2)
{
    Console.Error.WriteLine("The generate command accepts at most one output path.");
    PrintUsage();
    return 1;
}

var outputPath = args.Length == 2 ? args[1] : "dict.txt";
var generator = new PasswordDictionaryGenerator();
var writer = new PasswordDictionaryFileWriter();
var generateDictionary = new GeneratePasswordDictionary(generator, writer);

try
{
    var result = await generateDictionary.ExecuteAsync(outputPath);

    Console.WriteLine($"Generated {result.CandidateCount:N0} password candidates.");
    Console.WriteLine($"Dictionary: {result.OutputPath}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Dictionary generation failed: {exception.Message}");
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Wrap Password Assessment");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/WrapPassword.Cli -- generate [output-path]");
}
