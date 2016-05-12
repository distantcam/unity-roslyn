using UnityEditor;
using UnityEditor.Modules;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;

class CustomLanguage : CSharpLanguage
{
    // Copied
    public override ScriptCompilerBase CreateCompiler(MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater)
    {
        var cSharpCompiler = GetCSharpCompiler(targetPlatform, buildingForEditor, island._output);
        if (cSharpCompiler != CSharpCompiler.Mono)
        {
            if (cSharpCompiler == CSharpCompiler.Microsoft)
            {
                return new MicrosoftCSharpCompiler(island, runUpdater);
            }
        }
        return new RoslynCSharpCompiler(island, runUpdater);
    }
}