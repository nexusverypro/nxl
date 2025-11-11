using System;
using System.Diagnostics;

namespace Nxl.Compiler.Velia;

public sealed class Assembler
{
    private readonly IDiagnosticBag _diagnostics;
    private readonly ProjectStructure _projectStructure;
    private readonly string _intermediateFile;

    public Assembler(IDiagnosticBag diagnostics, ProjectStructure projectStructure, string intermediateFile)
    {
        _diagnostics = diagnostics;
        _projectStructure = projectStructure;
        _intermediateFile = intermediateFile;
    }

    public async Task<string> CompileAsync()
    {
        if (!File.Exists(_intermediateFile))
            throw new FileNotFoundException("Assembly file not found", _intermediateFile);

        var outputFile = Path.Combine(
            _projectStructure.IntermediatePath,
            Path.GetFileNameWithoutExtension(_intermediateFile) + ".o"
        );

        var startInfo = new ProcessStartInfo
        {
            FileName = "as", // GNU assembler
            Arguments = $"-o \"{outputFile}\" \"{_intermediateFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new StdOutErrException("assembler failed with exit code " + process.ExitCode, stdOut, stdErr);
            
        return outputFile;
    }
}
