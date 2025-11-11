using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Nxl.Compiler.Velia.Antlr
{
    public sealed class VeliaErrorListener : BaseErrorListener
    {
        private readonly IDiagnosticBag _diagnostics;
        private readonly string _sourceFile;

        public VeliaErrorListener(IDiagnosticBag diagnostics, string sourceFile)
        {
            _diagnostics = diagnostics;
            _sourceFile = sourceFile;
        }

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            var diagnostic = new DiagnosticMessage(DiagnosticSeverity.Critical, 1005, $"Syntax error in '{_sourceFile}' at line {line}, column {charPositionInLine}, token '{offendingSymbol.Text}': {msg}");
            _diagnostics.Add(diagnostic);
        }
    }
}
