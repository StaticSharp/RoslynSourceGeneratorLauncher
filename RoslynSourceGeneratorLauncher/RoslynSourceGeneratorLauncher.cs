using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynSourceGeneratorLauncher
{
    public static class RoslynSourceGeneratorLauncher
    {
        public static async Task Launch(ISourceGenerator generator, string targetProjectPath, string outputDirectory)
        {
            MSBuildLocator.RegisterDefaults();
            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(targetProjectPath);
            project = project.WithAnalyzerReferences(Enumerable.Empty<AnalyzerReference>()); // Remove all source code generators from target project
            var inputCompilation = await project.GetCompilationAsync();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            var firstGeneratorResult = driver.GetRunResult().Results.FirstOrDefault();
            var generationResult = firstGeneratorResult.GeneratedSources.FirstOrDefault();
            var dataToSave = firstGeneratorResult.GeneratedSources.ToDictionary(_ => _.HintName, _ => _.SourceText.ToString());

            var outputDirectoryInfo = Directory.CreateDirectory(outputDirectory);
            var filesToDelete = outputDirectoryInfo.GetFiles("*.cs", SearchOption.AllDirectories).Select(x => x.FullName).ToList();

            foreach (var r in dataToSave)
            {
                var outputPath = Path.GetFullPath(Path.Combine(outputDirectory, r.Key));
                filesToDelete.Remove(outputPath);
                File.WriteAllText(outputPath, r.Value);
            }

            foreach (var f in filesToDelete)
            {
                File.Delete(f);
            }
        }
    }
}