using System.Text;

namespace WrapPassword.IntegrationTests;

internal sealed class TemporarySubmissionRepository : IDisposable
{
    private static readonly IReadOnlyDictionary<string, string> RequiredTextFiles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [".editorconfig"] = "root = true\n",
            [".env.example"] = "WRAP_PASSWORD_EMAIL=\n",
            [".gitignore"] = "bin/\nobj/\n",
            ["appsettings.Development.json"] = "{}\n",
            ["appsettings.json"] = "{}\n",
            ["Directory.Build.props"] = "<Project />\n",
            ["Program.cs"] = "Console.WriteLine(\"test\");\n",
            ["README.md"] = "# Test submission\n",
            ["WrapPassword.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />\n",
            ["WrapPassword.sln"] = "Microsoft Visual Studio Solution File\n",
            ["dict.txt"] = "password\n",
            ["docs/AI_ASSISTANCE.md"] = "# AI assistance\n",
            ["docs/IMPLEMENTATION_PLAN.md"] = "# Plan\n",
            ["src/Example/Example.cs"] = "namespace Example;\n",
            ["src/Example/Example.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />\n",
            ["WrapPassword.Tests/ExampleTests/ExampleTests.cs"] = "namespace ExampleTests;\n",
            ["WrapPassword.Tests/ExampleTests/ExampleTests.csproj"] =
                "<Project Sdk=\"Microsoft.NET.Sdk\" />\n",
            ["src/Example/bin/Debug/net9.0/Generated.cs"] = "secret build output\n",
            ["Database/local.db"] = "local database\n",
            ["artifacts/old.zip"] = "old archive\n",
            [".git/config"] = "git metadata\n",
            [".env"] = "SECRET=value\n",
        };

    private TemporarySubmissionRepository(string rootPath)
    {
        RootPath = rootPath;
        CvPath = Path.Combine(rootPath, "candidate-cv.pdf");
        DictionaryPath = Path.Combine(rootPath, "dict.txt");
    }

    public string RootPath { get; }

    public string CvPath { get; }

    public string DictionaryPath { get; }

    public static async Task<TemporarySubmissionRepository> CreateAsync()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            $"WrapPassword.SubmissionTests-{Guid.NewGuid():N}");
        var repository = new TemporarySubmissionRepository(rootPath);

        foreach (var file in RequiredTextFiles)
        {
            var filePath = Path.Combine(rootPath, file.Key);
            var directoryPath = Path.GetDirectoryName(filePath)
                ?? throw new InvalidOperationException("A test file directory could not be resolved.");

            Directory.CreateDirectory(directoryPath);
            await File.WriteAllTextAsync(filePath, file.Value, Encoding.UTF8);
        }

        await File.WriteAllBytesAsync(
            repository.CvPath,
            "%PDF-1.4\nTest CV\n"u8.ToArray());

        return repository;
    }

    public string GetPath(string relativePath)
    {
        return Path.Combine(RootPath, relativePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
