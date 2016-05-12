using System.Diagnostics;

internal class Mono30Compiler : Compiler
{
    public Mono30Compiler(Platform platform, string unityEditorDataDir, Logger logger, string compilerPath) : base(platform, unityEditorDataDir, logger, compilerPath)
    {
    }

    public override string Name => "Mono C# 3.0";

    protected override Process CreateCompilerProcess(string responseFile)
    {
        var process = new Process();
        process.StartInfo = CreateOSDependentStartInfo(platform, ProcessRuntime.CLR20, compilerPath, responseFile, unityEditorDataDir);
        return process;
    }
}