using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nxl.Compiler.Velia;

public sealed class Linker
{
    private readonly IDiagnosticBag _diagnostics;
    private readonly ProjectStructure _projectStructure;
    private readonly IEnumerable<string> _objectFiles;
    private readonly bool _isWindows;

    public Linker(IDiagnosticBag diagnostics, ProjectStructure projectStructure, IEnumerable<string> objectFiles)
    {
        _diagnostics = diagnostics;
        _projectStructure = projectStructure;
        _objectFiles = objectFiles;
        _isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
    }

    private string ChangeExtensionForPlatform(string fileName)
    {
        if (_isWindows)
            return Path.ChangeExtension(fileName, ".exe");
        return _projectStructure.FileName;
    }

    public async Task<string> LinkAsync()
    {
        var validFiles = _objectFiles.Where(File.Exists).ToList();
        if (!validFiles.Any())
            throw new Exception("No valid object files provided for linking.");

        var outputPath = Path.Combine(_projectStructure.OutputPath, ChangeExtensionForPlatform(_projectStructure.FileName));
        string args, linker;

        if (_isWindows)
        {
            linker = "gcc";
            args = string.Join(' ', validFiles.Select(f => $"\"{f}\""))
                + $" -o \"{outputPath}\" -nostdlib -Wl,-e,_start -lkernel32";
        }
        else
        {
            linker = "ld";
            args = string.Join(' ', validFiles.Select(f => $"\"{f}\"")) + $" -o \"{outputPath}\" -e _start";
        }

        await Console.Out.WriteLineAsync($"linking with '{linker}' and args '{args}'");

        var startInfo = new ProcessStartInfo
        {
            FileName = linker,
            Arguments = args,
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
            throw new StdOutErrException($"Linker ({linker}) failed with exit code {process.ExitCode}", stdOut, stdErr);

        return outputPath;
    }
}
