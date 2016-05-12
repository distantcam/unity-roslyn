using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;

class RoslynCSharpCompiler : MonoCSharpCompiler
{
    public RoslynCSharpCompiler(MonoIsland island, bool runUpdater)
        : base(island, runUpdater)
    {
    }

    // Copied
    private string[] GetAdditionalReferences()
    {
        return new string[]
        {
            "System.Runtime.Serialization.dll",
            "System.Xml.Linq.dll"
        };
    }

    private string GetCompilerPath(List<string> arguments)
    {
        var roslynSupportPath = Path.Combine(Directory.GetCurrentDirectory(), "RoslynSupport");
        var compilerPath = Path.Combine(roslynSupportPath, "CompilerWrapper.exe");
        if (File.Exists(compilerPath))
        {
            return compilerPath;
        }
        throw new ApplicationException("Unable to find csharp compiler in " + roslynSupportPath);
    }

    // Copied
    protected override Program StartCompiler()
    {
        List<string> list = new List<string>
        {
            "-debug",
            "-target:library",
            "-nowarn:0169",
            "-out:" + ScriptCompilerBase.PrepareFileName(this._island._output)
        };
        string[] references = this._island._references;
        for (int i = 0; i < references.Length; i++)
        {
            string fileName = references[i];
            list.Add("-r:" + ScriptCompilerBase.PrepareFileName(fileName));
        }
        foreach (string current in this._island._defines.Distinct<string>())
        {
            list.Add("-define:" + current);
        }
        string[] files = this._island._files;
        for (int j = 0; j < files.Length; j++)
        {
            string fileName2 = files[j];
            list.Add(ScriptCompilerBase.PrepareFileName(fileName2));
        }
        string[] additionalReferences = this.GetAdditionalReferences();
        for (int k = 0; k < additionalReferences.Length; k++)
        {
            string path = additionalReferences[k];
            string text = Path.Combine(base.GetProfileDirectory(), path);
            if (File.Exists(text))
            {
                list.Add("-r:" + ScriptCompilerBase.PrepareFileName(text));
            }
        }
        return base.StartCompiler(this._island._target, this.GetCompilerPath(list), list);
    }
}