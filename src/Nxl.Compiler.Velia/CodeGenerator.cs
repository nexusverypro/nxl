using System;
using System.Runtime.InteropServices;
using Nxl.Compiler.Velia.Nodes;
using Re.Asm;

namespace Nxl.Compiler.Velia;

public sealed class CodeGenerator
{
    private const string IDENT_FORMAT_STR = "Re.Asm (via .NET, runtime {0}) on {1} ({2})";

    private readonly IDiagnosticBag _diagnostics;
    private readonly ProjectStructure _projectStructure;
    private readonly string _sourceFilePath;
    private readonly bool _isEntryPoint;
    private readonly Generator _assemblyGenerator;
    private readonly DocumentRootSyntaxNode _documentRootSyntaxNode;
    private readonly string _identName;
    private readonly bool _isWindows;

    public CodeGenerator(IDiagnosticBag diagnostics, ProjectStructure projectStructure, string sourceFilePath, DocumentRootSyntaxNode documentRoot, [Optional] Serializer<Instruction> serializer)
    {
        _diagnostics = diagnostics;
        _projectStructure = projectStructure;
        _sourceFilePath = sourceFilePath;
        _isEntryPoint = Path.GetFileNameWithoutExtension(sourceFilePath) == "main";
        _assemblyGenerator = new Generator();
        if (serializer != null) _assemblyGenerator.InstructionSerializer = serializer;
        _documentRootSyntaxNode = documentRoot;
        _identName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()).Replace("-", "_");
        _isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
    }

    // TODO: add .ident and .section stuff for identification
    // ... possibly add .file too
    public async Task<string> GenerateAsync()
    {
        if (_isWindows) _assemblyGenerator.Extern("ExitProcess");

        // validate existence of main() on main.nxx files
        if (_isEntryPoint && !_documentRootSyntaxNode.Children.Any(x => x is FunctionDeclSyntaxNode functionDecl && functionDecl.Name.Identifier == "main"))
        {
            _diagnostics.Add(DiagnosticMessages.VL3000_NoMainFunctionFoundInProgram);
            return string.Empty;
        }

        // write everything
        var codeGenVisitor = new VeliaAstCodeGenVisitor(_diagnostics, _assemblyGenerator);
        if (!codeGenVisitor.VisitProgram(_documentRootSyntaxNode))
        {
            _diagnostics.Add(DiagnosticMessages.VL3001_FailedToGenerateCodeGen);
            return string.Empty;
        }

        // if we are main.nxx, generate entry point
        if (_isEntryPoint)
        {
            var startLabel = _assemblyGenerator.Label("_start");
            _assemblyGenerator.Global(startLabel);
            {
                _assemblyGenerator.Sub(Register.RSP, Immediate.Byte(8));    // enable stack frame
                _assemblyGenerator.Call("main");                            // call main()
                _assemblyGenerator.Add(Register.RSP, Immediate.Byte(8));    // disable stack frame

                if (_isWindows)
                {
                    // ExitProcess(RCX)
                    _assemblyGenerator.Mov(Register.RCX, Register.RAX);
                    _assemblyGenerator.Jmp("ExitProcess");
                }
                else
                {
                    // sys_exit(RDI)
                    _assemblyGenerator.Mov(Register.RDI, Register.RAX);
                    _assemblyGenerator.Mov(Register.RAX, Immediate.Dword(60));
                    _assemblyGenerator.Syscall();
                }
            }
        }

        // final: write ident if entrypoint
        if (_isEntryPoint)
        {
            _assemblyGenerator.Label("_" + _identName + "_ident");
            {
                // write section
                if (!_isWindows) _assemblyGenerator.WriteLine($".section .note.rasm-stack,\"\",@progbits");
                else _assemblyGenerator.WriteLine(".section .rdata");

                // write ident
                _assemblyGenerator.WriteLine(
                    $".{(_isWindows ? "ident" : "ascii")} " +
                    $"\"{string.Format(IDENT_FORMAT_STR, Environment.Version, Environment.OSVersion.Platform, Environment.OSVersion.VersionString)}\"");
            }
        }

        // write to intermediate file
        var fullPath = Path.Combine(
            _projectStructure.IntermediatePath,
            Path.ChangeExtension(Path.GetFileNameWithoutExtension(_sourceFilePath) + "." + Path.GetRandomFileName(), ".S")
        );

        await File.WriteAllTextAsync(fullPath, _assemblyGenerator.Generate());
        return fullPath;
    }
}
