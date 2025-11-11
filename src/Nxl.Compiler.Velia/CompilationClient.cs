using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Nxl.Compiler.Velia.Antlr;
using Nxl.Compiler.Velia.Generated;
using Nxl.Compiler.Velia.Nodes;
using Nxl.Compiler.Velia.Syntax;

using Re.Asm;
using Re.Asm.Serializers;

namespace Nxl.Compiler.Velia
{
    public interface ICompilationClient
    {
        Task<CompilationResult> CompileAsync();
    }

    public sealed class CompilationClient : ICompilationClient
    {
        private readonly IDiagnosticBag _diagnostics;
        private readonly ProjectStructure _projectStructure;
        private readonly bool _saveTemp;
        private readonly Stopwatch _stopwatch;

        public CompilationClient(ProjectStructure projectStructure, bool saveTemp = false)
        {
            _projectStructure = projectStructure;
            _saveTemp = saveTemp;
            _diagnostics = DiagnosticBag.Instance;
            _stopwatch = new Stopwatch();
        }

        public async Task<CompilationResult> CompileAsync()
        {
            _stopwatch.Restart();

            // load source files
            var sourceFiles = LoadSourceFiles();

            // parse all source files
            var parsedFiles = await ParseSourceFilesAsync(sourceFiles);
            if (HasCriticalDiagnostics())
                return CompilationResult.Failed("Critical diagnostics after parsing");

            // prepare intermediate directory
            PrepareDirectories();

            // generate intermediate assembly files
            var intermediateFiles = await GenerateIntermediateFilesAsync(parsedFiles);
            if (HasCriticalDiagnostics())
                return CompilationResult.Failed("Critical diagnostics after code generation");

            // compile to object files
            var objectFiles = await CompileToObjectFilesAsync(intermediateFiles);
            if (HasCriticalDiagnostics())
                return CompilationResult.Failed("Critical diagnostics after assembly");

            // link final executable
            var finalExecPath = await LinkExecutableAsync(objectFiles);
            if (HasCriticalDiagnostics())
                return CompilationResult.Failed("Critical diagnostics after linking", finalExecPath);

            _stopwatch.Stop();
            return CompilationResult.Success(finalExecPath, _stopwatch.ElapsedMilliseconds);
        }

        private List<string> LoadSourceFiles()
        {
            return Directory.GetFiles(_projectStructure.DirectoryPath, "*.nxx", SearchOption.AllDirectories)
                           .ToList();
        }

        private async Task<List<(string file, DocumentRootSyntaxNode ast)>> ParseSourceFilesAsync(List<string> sourceFiles)
        {
            var parsedFiles = new List<(string file, DocumentRootSyntaxNode ast)>();
            foreach (var file in sourceFiles)
            {
                var ast = await ParseSingleFileAsync(file);
                if (ast != null)
                {
                    VeliaAst.PrintDebugString(ast);
                    parsedFiles.Add((file, ast));
                }
                else _diagnostics.Add(DiagnosticMessages.VL1000_FailedToParse.WithFormat(file, string.Empty));
            }

            if (parsedFiles.Count != sourceFiles.Count)
                _diagnostics.Add(DiagnosticMessages.VL1001_ParsedCountActualMismatch
                    .WithFormat(parsedFiles.Count, sourceFiles.Count));

            return parsedFiles;
        }

        private async Task<DocumentRootSyntaxNode?> ParseSingleFileAsync(string file)
        {
            await using var fileStream = File.OpenRead(file);
            var input = CharStreams.fromStream(fileStream);
            if (input == null) return null;

            // lexing phase
            var lexer = new VeliaLexer(input, TextWriter.Null, TextWriter.Null);
            var tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill();

            // save tokens if requested
            if (_saveTemp) await SaveTokensAsync(lexer, tokenStream, file);

            // parsing phase
            var parser = new VeliaParser(tokenStream, TextWriter.Null, TextWriter.Null);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new VeliaErrorListener(_diagnostics, file));

            VeliaParser.ProgramContext rootContext;
            try
            {
                rootContext = parser.program();
            }
            catch (RecognitionException ex)
            {
                _diagnostics.Add(DiagnosticMessages.VL1000_FailedToParse.WithFormat(file, ex.Message));
                return null;
            }

            if (_saveTemp) await SaveTextAsync(rootContext.ToStringTree(parser), file);

            // convert parse tree to AST
            var visitor = new VeliaAstGenerationVisitor(_diagnostics);
            var enumerable = visitor.Visit(rootContext);
            return enumerable.FirstOrDefault() as DocumentRootSyntaxNode;
        }

        private async Task SaveTextAsync(string @str, string file)
        {
            var fullPath = Path.Combine(
                _projectStructure.IntermediatePath,
                Path.ChangeExtension(Path.GetFileNameWithoutExtension(file), ".temp.tree.txt")
            );

            await File.WriteAllTextAsync(
                fullPath,
                @str
            );
        }

        private async Task SaveTokensAsync(VeliaLexer lexer, CommonTokenStream tokenStream, string file)
        {
            var allLexTokens = tokenStream.GetTokens();
            var fullPath = Path.Combine(
                _projectStructure.IntermediatePath,
                Path.ChangeExtension(Path.GetFileNameWithoutExtension(file), ".temp.tok.json")
            );

            await File.WriteAllTextAsync(
                fullPath,
                JsonSerializer.Serialize(allLexTokens.Select(t => new
                {
                    Type = lexer.Vocabulary.GetSymbolicName(t.Type),
                    Text = t.Text,
                    Line = t.Line,
                    Column = t.Column
                }),
                new JsonSerializerOptions { WriteIndented = true })
            );
        }

        private void PrepareDirectories()
        {
            Directory.CreateDirectory(_projectStructure.OutputPath);
            Directory.CreateDirectory(_projectStructure.IntermediatePath);
            foreach (var file in Directory.GetFiles(_projectStructure.IntermediatePath))
                File.Delete(file);
        }

        private async Task<List<string>> GenerateIntermediateFilesAsync(
            List<(string file, DocumentRootSyntaxNode ast)> parsedFiles)
        {
            var intermediateFiles = new List<string>();
            foreach (var (file, ast) in parsedFiles)
            {
                var generator = new CodeGenerator(_diagnostics, _projectStructure, file, ast, GnuSerializer.Shared);
                var intermediateFile = await generator.GenerateAsync(); 
                if (!string.IsNullOrEmpty(intermediateFile)) intermediateFiles.Add(intermediateFile);
            }

            return intermediateFiles;
        }

        private async Task<List<string>> CompileToObjectFilesAsync(List<string> intermediateFiles)
        {
            var objectFiles = new List<string>();
            foreach (var file in intermediateFiles)
            {
                var assembler = new Assembler(_diagnostics, _projectStructure, file);
                var objectFile = await assembler.CompileAsync();
                if (!string.IsNullOrEmpty(objectFile)) objectFiles.Add(objectFile);
            }

            return objectFiles;
        }

        private async Task<string> LinkExecutableAsync(List<string> objectFiles)
        {
            var linker = new Linker(_diagnostics, _projectStructure, objectFiles);
            return await linker.LinkAsync();
        }

        private bool HasCriticalDiagnostics() => _diagnostics is DiagnosticBag bag && bag.AnyCritical();
    }

    public sealed class CompilationResult
    {
        public bool IsSuccess { get; }
        public string? OutputPath { get; }
        public long ElapsedMilliseconds { get; }
        public string? ErrorMessage { get; }

        private CompilationResult(bool success, string? outputPath, long elapsed, string? error)
        {
            IsSuccess = success;
            OutputPath = outputPath;
            ElapsedMilliseconds = elapsed;
            ErrorMessage = error;
        }

        public static CompilationResult Success(string outputPath, long elapsed) => new(true, outputPath, elapsed, null);
        public static CompilationResult Failed(string error, string? partialOutput = null) => new(false, partialOutput, 0, error);
    }
}