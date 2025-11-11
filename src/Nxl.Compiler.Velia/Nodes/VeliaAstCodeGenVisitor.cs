using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Nxl.Compiler.Velia.Generated;
using Nxl.Compiler.Velia.Optimizer;
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

        private bool VisitFunctionDecl([NotNull] FunctionDeclSyntaxNode context)
        {
            using (_generator.Function(context.Name.Identifier))
            {
                int argCount = context.ParameterList.SlotCount;
                RegisterOperand[] locals = new RegisterOperand[argCount];
                RegisterOperand[] scratch = { Register.R10, Register.R11, Register.R12, Register.R13, Register.R14, Register.R15 };
                for (int i = 0; i < argCount; i++)
                {
                    locals[i] = scratch[i];
                }

                _generator.LoadFunctionCallLocals(
                    Abi.SystemV,
                    argCount,
                    locals
                );

                _generator.Nop();
            }

            return true;
        }

        public bool VisitProgram([NotNull] DocumentRootSyntaxNode context)
        {
            // write functions
            foreach (var decl in context.Children.OfType<FunctionDeclSyntaxNode>())
                if (!VisitFunctionDecl(decl))
                    return false;

            return true;
        }
    }
}
