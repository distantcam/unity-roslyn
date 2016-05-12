using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;

[InitializeOnLoad]
public static class RoslynSupportLoader
{
    static RoslynSupportLoader()
    {
        var supportedLanguages = GetSupportedLanguagesList();
        supportedLanguages.RemoveAll(lang => lang is CSharpLanguage);
        supportedLanguages.Add(new CustomLanguage());
    }

    private static List<SupportedLanguage> GetSupportedLanguagesList()
    {
        var fieldInfo = typeof(ScriptCompilers).GetField("_supportedLanguages", BindingFlags.NonPublic | BindingFlags.Static);
        return (List<SupportedLanguage>)fieldInfo.GetValue(null);
    }
}