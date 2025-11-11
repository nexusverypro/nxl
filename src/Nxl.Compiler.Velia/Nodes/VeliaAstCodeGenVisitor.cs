using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Nxl.Compiler.Velia.Generated;
using Re.Asm;

namespace Nxl.Compiler.Velia.Nodes
{
    public class VeliaAstCodeGenVisitor
    {
        private readonly IDiagnosticBag _diagnostics;
        private readonly Generator _generator;

        public VeliaAstCodeGenVisitor(IDiagnosticBag diagnostics, Generator generator)
        {
            _diagnostics = diagnostics;
            _generator = generator;
        }

        public bool VisitProgram([NotNull] DocumentRootSyntaxNode context)
        {
            return false;
        }
    }
}
