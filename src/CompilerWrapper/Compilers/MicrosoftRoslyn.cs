using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

internal class RoslynCompiler : Compiler
{
    public override string Name => "Microsoft Roslyn";
    public override bool NeedsPostProcessing => true;

    public RoslynCompiler(Platform platform, string unityEditorDataDir, Logger logger, string directory) : base(platform, unityEditorDataDir, logger, Path.Combine(directory, Path.Combine("Roslyn", "csc.exe")), Path.Combine(directory, "PostProcessor.exe"))
    {
    }

    public static bool IsAvailable(string directory) => File.Exists(Path.Combine(directory, Path.Combine("Roslyn", "csc.exe")));

    protected override Process CreateCompilerProcess(string responseFile)
    {
        var systemDllPath = Path.Combine(unityEditorDataDir, @"Mono/lib/mono/2.0/System.dll");
        var systemCoreDllPath = Path.Combine(unityEditorDataDir, @"Mono/lib/mono/2.0/System.Core.dll");
        var systemXmlDllPath = Path.Combine(unityEditorDataDir, @"Mono/lib/mono/2.0/System.Xml.dll");
        var mscorlibDllPath = Path.Combine(unityEditorDataDir, @"Mono/lib/mono/2.0/mscorlib.dll");

        string processArguments = "-nostdlib+ -noconfig "
                                  + $"-r:\"{mscorlibDllPath}\" "
                                  + $"-r:\"{systemDllPath}\" "
                                  + $"-r:\"{systemCoreDllPath}\" "
                                  + $"-r:\"{systemXmlDllPath}\" " + responseFile;

        var process = new Process();
        process.StartInfo = CreateOSDependentStartInfo(platform, ProcessRuntime.CLR40, compilerPath, processArguments, unityEditorDataDir);
        return process;
    }

    public override void PostProcess(string targetAssemblyPath)
    {
        outputLines.Clear();

        var process = new Process();
        process.StartInfo = CreateOSDependentStartInfo(platform, ProcessRuntime.CLR40, postProcessPath, targetAssemblyPath, unityEditorDataDir);
        process.OutputDataReceived += (sender, e) => outputLines.Add(e.Data);

        logger.Append($"Process: {process.StartInfo.FileName}");
        logger.Append($"Arguments: {process.StartInfo.Arguments}");

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        logger.Append($"Exit code: {process.ExitCode}");

        var pdb2mdbPath = Path.Combine(Path.GetDirectoryName(postProcessPath), @"pdb2mdb.exe");

        process = new Process();
        process.StartInfo = CreateOSDependentStartInfo(platform, ProcessRuntime.CLR40, pdb2mdbPath, targetAssemblyPath, unityEditorDataDir);
        process.OutputDataReceived += (sender, e) => outputLines.Add(e.Data);

        logger.Append($"Process: {process.StartInfo.FileName}");
        logger.Append($"Arguments: {process.StartInfo.Arguments}");

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
        logger.Append($"Exit code: {process.ExitCode}");

        File.Delete(Path.ChangeExtension(targetAssemblyPath, ".pdb"));
    }

    public override void PrintCompilerOutputAndErrors()
    {
        // Microsoft's compiler writes all warnings and errors to the standard output channel,
        // so move them to the error channel skipping first 3 lines that are just part of the header.

        while (outputLines.Count > 3)
        {
            var line = outputLines[3];
            outputLines.RemoveAt(3);
            errorLines.Add(line);
        }

        base.PrintCompilerOutputAndErrors();
    }

    public override void PrintPostProcessingOutputAndErrors()
    {
        var lines = (from line in outputLines
                     let trimmedLine = line?.Trim()
                     where !string.IsNullOrEmpty(trimmedLine)
                     select trimmedLine).ToList();

        logger.Append($"- postprocess.exe output ({lines.Count} {(lines.Count == 1 ? "line" : "lines")}):");

        for (int i = 0; i < lines.Count; i++)
        {
            Console.Out.WriteLine(lines[i]);
            logger.Append($"{i}: {lines[i]}");
        }
    }
}