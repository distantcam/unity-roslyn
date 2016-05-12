using System.Diagnostics;
using System.IO;

internal class Mono50Compiler : Compiler
{
    public Mono50Compiler(Platform platform, string unityEditorDataDir, Logger logger, string compilerPath) : base(platform, unityEditorDataDir, logger, compilerPath)
    {
    }

    public override string Name => "Mono C# 5.0";

    protected override Process CreateCompilerProcess(string responseFile)
    {
        var systemCoreDllPath = Path.Combine(unityEditorDataDir, @"Mono/lib/mono/2.0/System.Core.dll");

        string processArguments;
        if (platform == Platform.Windows)
        {
            processArguments = $"-sdk:2 -debug+ -langversion:Future -r:\"{systemCoreDllPath}\" {responseFile}";
        }
        else
        {
            processArguments = $"-sdk:2 -debug+ -langversion:Future {responseFile}";
        }

        var process = new Process();
        process.StartInfo = CreateOSDependentStartInfo(platform, ProcessRuntime.CLR40, compilerPath, processArguments, unityEditorDataDir);
        return process;
    }
}