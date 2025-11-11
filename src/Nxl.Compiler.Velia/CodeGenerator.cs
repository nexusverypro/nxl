using System;
using System.Runtime.InteropServices;
using Nxl.Compiler.Velia.Nodes;
using Re.Asm;

namespace Nxl.Compiler.Velia;

// TODO: it would be nice to be able to optimize assembly instructions soon
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
                _assemblyGenerator.Xor(Register.EBP, Register.EBP); // mark the end of stack frames
                _assemblyGenerator.Mov(Register.EDI, Memory.Base(Register.RSP)); // get argc from the stack
                _assemblyGenerator.Lea(Memory.Base(Register.RSP, 8), Register.RSI); // take the address of argv from the stack
                _assemblyGenerator.Lea(Memory.New(Register.RSP, Register.RDI, 8, 16), Register.RDX); // take the address of envp from the stack
                _assemblyGenerator.Xor(Register.EAX, Register.EAX); // per ABI and compatibility with icc
                _assemblyGenerator.Call("main"); // main(%edi, %rsi, %rdx)
                _assemblyGenerator.Mov(Register.EDI, Register.EAX); // transfer the return of main to first arg of _nxl_crt_exit0
                _assemblyGenerator.Xor(Register.EAX, Register.EAX); // per ABI and compatibility with icc
                _assemblyGenerator.Call("_nxl_crt_exit0"); // _nxl_crt_exit0(%edi)
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
