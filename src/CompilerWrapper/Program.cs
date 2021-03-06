﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

internal class Program
{
    private const string LANGUAGE_SUPPORT_DIR = "RoslynSupport";

    private static int Main(string[] args)
    {
        int exitCode;

        using (var logger = new Logger())
        {
            try
            {
                exitCode = Compile(args, logger);
            }
            catch (Exception e)
            {
                exitCode = -1;
                Console.Error.Write($"Compiler redirection error: {e.GetType()}{Environment.NewLine}{e.Message} {e.StackTrace}");
            }
        }

        return exitCode;
    }

    private static int Compile(string[] args, Logger logger)
    {
        logger.AppendHeader();

        var responseFile = args[0];
        var compilationOptions = File.ReadAllLines(responseFile.TrimStart('@'));
        var unityEditorDataDir = GetUnityEditorDataDir();
        var projectDir = Directory.GetCurrentDirectory();
        var targetAssembly = compilationOptions.First(line => line.StartsWith("-out:")).Substring(10).Trim('\'');

        logger.Append($"CSharpCompilerWrapper.exe version: {Assembly.GetExecutingAssembly().GetName().Version}");
        logger.Append($"Platform: {CurrentPlatform}");
        logger.Append($"Target assembly: {targetAssembly}");
        logger.Append($"Project directory: {projectDir}");
        logger.Append($"Unity 'Data' or 'Frameworks' directory: {unityEditorDataDir}");

        //if (CurrentPlatform == Platform.Linux)
        //{
        //    logger.Append("");
        //    logger.Append("Platform is not supported");
        //    return -1;
        //}

        var compiler = FindSuitableCompiler(logger, CurrentPlatform, projectDir, compilationOptions, unityEditorDataDir);

        logger.Append($"Compiler: {compiler.Name}");
        logger.Append("");
        logger.Append("- Compilation -----------------------------------------------");
        logger.Append("");

        var stopwatch = Stopwatch.StartNew();
        var exitCode = compiler.Compile(responseFile);
        stopwatch.Stop();

        logger.Append($"Elapsed time: {stopwatch.ElapsedMilliseconds / 1000f:F2} sec");
        logger.Append("");
        compiler.PrintCompilerOutputAndErrors();

        if (exitCode != 0 || compiler.NeedsPostProcessing == false)
        {
            return exitCode;
        }

        logger.Append("");
        logger.Append("- Post Process ----------------------------------------------");
        logger.Append("");

        stopwatch.Reset();
        stopwatch.Start();

        var targetAssemblyPath = Path.Combine("Temp", targetAssembly);
        compiler.PostProcess(targetAssemblyPath);

        stopwatch.Stop();
        logger.Append($"Elapsed time: {stopwatch.ElapsedMilliseconds / 1000f:F2} sec");
        logger.Append("");
        compiler.PrintPostProcessingOutputAndErrors();

        return 0;
    }

    private static Compiler FindSuitableCompiler(Logger logger, Platform platform, string projectDir, string[] compilationOptions, string unityEditorDataDir)
    {
        Compiler compiler = null;

        var languageSupportDirectory = Path.Combine(projectDir, LANGUAGE_SUPPORT_DIR);

        // Looking for Roslyn C# 6.0 compiler
        if (RoslynCompiler.IsAvailable(languageSupportDirectory))
        {
            compiler = new RoslynCompiler(platform, unityEditorDataDir, logger, languageSupportDirectory);
        }

        if (compiler == null)
        {
            // Looking for Mono C# 6.0 compiler
            if (Mono60Compiler.IsAvailable(languageSupportDirectory))
            {
                compiler = new Mono60Compiler(platform, unityEditorDataDir, logger, languageSupportDirectory);
            }
        }

        if (compiler == null && compilationOptions.Any(line => line.Contains("AsyncBridge.Net35.dll")))
        {
            // Using Mono C# 5.0 compiler
            var bleedingEdgeCompilerPath = Path.Combine(unityEditorDataDir, @"MonoBleedingEdge/lib/mono/4.5/mcs.exe");
            compiler = new Mono50Compiler(platform, unityEditorDataDir, logger, bleedingEdgeCompilerPath);
        }

        if (compiler == null)
        {
            // Using stock Mono C# 3.0 compiler
            var stockCompilerPath = Path.Combine(unityEditorDataDir, @"Mono/lib/mono/2.0/gmcs.exe");
            compiler = new Mono30Compiler(platform, unityEditorDataDir, logger, stockCompilerPath);
        }

        return compiler;
    }

    private static Platform CurrentPlatform
    {
        get
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                    {
                        return Platform.Mac;
                    }
                    return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }
    }

    /// <summary>
    /// Returns the directory that contains Mono and MonoBleedingEdge directories
    /// </summary>
    private static string GetUnityEditorDataDir()
    {
        // Windows:
        // MONO_PATH: C:\Program Files\Unity\Editor\Data\Mono\lib\mono\2.0
        //
        // Mac OS X:
        // MONO_PATH: /Applications/Unity/Unity.app/Contents/Frameworks/Mono/lib/mono/2.0

        var monoPath = Environment.GetEnvironmentVariable("MONO_PATH").Replace("\\", "/");
        var index = monoPath.IndexOf("/Mono/lib/", StringComparison.InvariantCultureIgnoreCase);
        var path = monoPath.Substring(0, index);
        return path;
    }
}