using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Nxl.Compiler.Velia.Generated;

namespace Nxl.Compiler.Velia.Nodes
{
    public class VeliaAstGenerationVisitor : VeliaBaseVisitor<IEnumerable<VeliaSyntaxNode>>
    {
        private readonly string _filePath;
        private readonly IDiagnosticBag _diagnostics;
        public VeliaAstGenerationVisitor(string filePath, IDiagnosticBag diagnostics)
        {
            _filePath = filePath;
            _diagnostics = diagnostics;
        }

        public override IEnumerable<VeliaSyntaxNode> VisitProgram(VeliaParser.ProgramContext context)
        {
            var nodes = context.children.SelectMany(child => Visit(child) ?? Enumerable.Empty<VeliaSyntaxNode>());
            return [new DocumentRootSyntaxNode(_filePath, nodes.ToList())];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitPackageDeclaration([NotNull] VeliaParser.PackageDeclarationContext context)
        {
            var stringLiteralToken = context.STRING_LITERAL();
            if (stringLiteralToken == null)
            {
                _diagnostics.Add(DiagnosticMessages.VL1004_PackageDeclHasNoLiteral);
                return [];
            }

            var payloadStringLiteral = stringLiteralToken.GetText();
            if (payloadStringLiteral.Length >= 2 &&
                payloadStringLiteral.StartsWith("\"") &&
                payloadStringLiteral.EndsWith("\""))
            {
                payloadStringLiteral = payloadStringLiteral.Substring(1, payloadStringLiteral.Length - 2);
            }

            if (string.IsNullOrEmpty(payloadStringLiteral))
            {
                _diagnostics.Add(DiagnosticMessages.VL1004_PackageDeclHasNoLiteral);
                return [];
            }

            var stringLiteralNode = new StringLiteralSyntaxNode(payloadStringLiteral);
            return [new PackageDeclarationSyntaxNode(stringLiteralNode)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitUseDeclaration([NotNull] VeliaParser.UseDeclarationContext context)
        {
            var stringLiteralTokens = context.STRING_LITERAL();
            if (stringLiteralTokens == null || stringLiteralTokens.Length == 0)
            {
                _diagnostics.Add(DiagnosticMessages.VL1005_UsePackageDeclHasNoLiteral);
                return [];
            }

            List<string> literals = new List<string>();
            foreach (var stringLiteral in stringLiteralTokens)
            {
                var payloadStringLiteral = stringLiteral.GetText();
                if (payloadStringLiteral.Length >= 2 &&
                    payloadStringLiteral.StartsWith("\"") &&
                    payloadStringLiteral.EndsWith("\""))
                {
                    payloadStringLiteral = payloadStringLiteral.Substring(1, payloadStringLiteral.Length - 2);
                }

                if (string.IsNullOrEmpty(payloadStringLiteral))
                {
                    _diagnostics.Add(DiagnosticMessages.VL1005_UsePackageDeclHasNoLiteral);
                    return [];
                }
            }

            return [new UsePackageDeclSyntaxNode(literals.Select(x => new StringLiteralSyntaxNode(x)))];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitType([NotNull] VeliaParser.TypeContext context)
        {
            return [
                new TypeSyntaxNode(
                    context.arrayPrefix() != null,
                    new IdentifierSyntaxNode(context.primaryType().qualifiedName().GetText()),
                    context.typeSuffix().Any(x => x.STAR() != null),
                    context.typeSuffix().Any(x => x.AMPERSAND() != null)
                )
            ];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitParameterList([NotNull] VeliaParser.ParameterListContext context)
        {
            var parameterList = new List<FunctionParamDeclSyntaxNode>();
            foreach (var parameter in context.parameter())
            {
                if (parameter == null) return [];

                var isMutable = parameter.MUTABLE() != null;
                var name = new IdentifierSyntaxNode(parameter.IDENTIFIER().GetText());
                var type = VisitType(parameter.type()).FirstOrDefault() as TypeSyntaxNode;
                if (type == null) return [];

                parameterList.Add(new FunctionParamDeclSyntaxNode(isMutable, name, type));
            }

            return [new FunctionParamListSyntaxNode(parameterList)];
        }

        // returns ExpressionStmtSyntaxNode
        public override IEnumerable<VeliaSyntaxNode> VisitExpressionStatement([NotNull] VeliaParser.ExpressionStatementContext context)
        {
            var expression = context.expression();
            if (expression == null)
            {
                _diagnostics.Add(DiagnosticMessages.VL1010_ExprStmtHasInvalidExpr);
                return [];
            }

            var exprNode = VisitExpression(expression).FirstOrDefault() as ExpressionSyntaxNode;
            if (exprNode == null) return [];

            return [new ExpressionStmtSyntaxNode(exprNode)];
        }

        // returns ExpressionSyntaxNode
        private IEnumerable<VeliaSyntaxNode> VisitExpression([NotNull] VeliaParser.ExpressionContext context)
        {
            return context switch
            {
                VeliaParser.LambdaContext lambdaCtx => VisitLambda(lambdaCtx),
                VeliaParser.TernaryContext ternaryCtx => VisitTernary(ternaryCtx),
                VeliaParser.AssignmentContext assignmentCtx => VisitAssignment(assignmentCtx),
                VeliaParser.LogicalOrContext logicalOrCtx => VisitLogicalOr(logicalOrCtx),
                VeliaParser.LogicalAndContext logicalAndCtx => VisitLogicalAnd(logicalAndCtx),
                VeliaParser.EqualityContext equalityCtx => VisitEquality(equalityCtx),
                VeliaParser.RelationalContext relationalCtx => VisitRelational(relationalCtx),
                VeliaParser.ShiftContext shiftCtx => VisitShift(shiftCtx),
                VeliaParser.AdditiveContext additiveCtx => VisitAdditive(additiveCtx),
                VeliaParser.MultiplicativeContext multiplicativeCtx => VisitMultiplicative(multiplicativeCtx),
                VeliaParser.BitwiseContext bitwiseCtx => VisitBitwise(bitwiseCtx),
                VeliaParser.UnaryContext unaryCtx => VisitUnary(unaryCtx),
                VeliaParser.ErrorPropagationContext errorPropCtx => VisitErrorPropagation(errorPropCtx),
                VeliaParser.FunctionCallContext funcCallCtx => VisitFunctionCall(funcCallCtx),
                VeliaParser.ArrayAccessContext arrayAccessCtx => VisitArrayAccess(arrayAccessCtx),
                VeliaParser.MemberAccessContext memberAccessCtx => VisitMemberAccess(memberAccessCtx),
                VeliaParser.PostfixIncDecContext postfixCtx => VisitPostfixIncDec(postfixCtx),
                VeliaParser.CompilerIntrinsicContext intrinsicCtx => VisitCompilerIntrinsic(intrinsicCtx),
                VeliaParser.ParenthesizedContext parenCtx => VisitParenthesized(parenCtx),
                VeliaParser.ArrayLitContext arrayLitCtx => VisitArrayLit(arrayLitCtx),
                VeliaParser.IdentifierContext identifierCtx => VisitIdentifier(identifierCtx),
                VeliaParser.NumberLitContext numberLitCtx => VisitNumberLit(numberLitCtx),
                VeliaParser.HexLitContext hexLitCtx => VisitHexLit(hexLitCtx),
                VeliaParser.StringLitContext stringLitCtx => VisitStringLit(stringLitCtx),
                VeliaParser.CharLitContext charLitCtx => VisitCharLit(charLitCtx),
                VeliaParser.BooleanLitContext booleanLitCtx => VisitBooleanLit(booleanLitCtx),
                _ => []
            };
        }

        public override IEnumerable<VeliaSyntaxNode> VisitLambda([NotNull] VeliaParser.LambdaContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitTernary([NotNull] VeliaParser.TernaryContext context)
        {
            var condition = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var trueExpr = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            var falseExpr = VisitExpression(context.expression(2)).FirstOrDefault() as ExpressionSyntaxNode;

            if (condition == null || trueExpr == null || falseExpr == null) return [];
            return [new TernaryExpressionSyntaxNode(condition, trueExpr, falseExpr)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitUnary([NotNull] VeliaParser.UnaryContext context)
        {
            var operand = VisitExpression(context.expression()).FirstOrDefault() as ExpressionSyntaxNode;
            if (operand == null) return [];

            var operatorText = context.children[0].GetText();
            var op = operatorText switch
            {
                "!" or "not" => UnaryOperator.Not,
                "-" => UnaryOperator.Negate,
                "++" => UnaryOperator.PreIncrement,
                "--" => UnaryOperator.PreDecrement,
                _ => throw new InvalidOperationException($"Unknown unary operator: {operatorText}")
            };

            return [new UnaryExpressionSyntaxNode(op, operand)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitErrorPropagation([NotNull] VeliaParser.ErrorPropagationContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitFunctionCall([NotNull] VeliaParser.FunctionCallContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitArrayAccess([NotNull] VeliaParser.ArrayAccessContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitMemberAccess([NotNull] VeliaParser.MemberAccessContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitPostfixIncDec([NotNull] VeliaParser.PostfixIncDecContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitCompilerIntrinsic([NotNull] VeliaParser.CompilerIntrinsicContext context)
        {
            var name = new IdentifierSyntaxNode(context.IDENTIFIER().GetText());

            IEnumerable<ExpressionSyntaxNode>? arguments = null;
            if (context.argumentList() != null)
            {
                arguments = context.argumentList().expression()
                    .Select(expr => VisitExpression(expr).FirstOrDefault() as ExpressionSyntaxNode)
                    .Where(e => e != null)
                    .Cast<ExpressionSyntaxNode>()
                    .ToList();
            }

            return [new CompilerIntrinsicExpressionSyntaxNode(name, arguments)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitParenthesized([NotNull] VeliaParser.ParenthesizedContext context)
        {
            return VisitExpression(context.expression());
        }

        public override IEnumerable<VeliaSyntaxNode> VisitArrayLit([NotNull] VeliaParser.ArrayLitContext context)
        {
            // TODO: FINISH
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitIdentifier([NotNull] VeliaParser.IdentifierContext context)
        {
            var identifier = context.IDENTIFIER().GetText();
            return [new IdentifierSyntaxNode(identifier)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitNumberLit([NotNull] VeliaParser.NumberLitContext context)
        {
            var text = context.NUMBER_LITERAL().GetText();
            if (long.TryParse(text, out var value))
            {
                return [new NumberLiteralSyntaxNode(value)];
            }
            _diagnostics.Add(DiagnosticMessages.VL1011_InvalidNumberLiteral);
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitHexLit([NotNull] VeliaParser.HexLitContext context)
        {
            var text = context.HEX_LITERAL().GetText();
            if (text.StartsWith("0x") || text.StartsWith("0X"))
                text = text.Substring(2);
            return [new HexadecimalLiteralSyntaxNode(text)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitStringLit([NotNull] VeliaParser.StringLitContext context)
        {
            var text = context.STRING_LITERAL().GetText();
            if (text.Length >= 2 && text.StartsWith("\"") && text.EndsWith("\""))
            {
                text = text.Substring(1, text.Length - 2);
            }
            return [new StringLiteralSyntaxNode(text)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitCharLit([NotNull] VeliaParser.CharLitContext context)
        {
            var text = context.CHAR_LITERAL().GetText();
            if (text.Length >= 2 && text.StartsWith("'") && text.EndsWith("'"))
            {
                text = text.Substring(1, text.Length - 2);
            }

            if (text.Length == 1) return [new CharLiteralSyntaxNode(text[0])];
            _diagnostics.Add(DiagnosticMessages.VL1012_InvalidCharLiteral);
            return [];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitBooleanLit([NotNull] VeliaParser.BooleanLitContext context)
        {
            var text = context.BOOLEAN_LITERAL().GetText();
            var value = text == "true";
            return [new BooleanLiteralSyntaxNode(value)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitEquality([NotNull] VeliaParser.EqualityContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText == "==" ? BinaryOperator.Equals : BinaryOperator.NotEquals;

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitRelational([NotNull] VeliaParser.RelationalContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText switch
            {
                "<" => BinaryOperator.LessThan,
                "<=" => BinaryOperator.LessThanOrEqual,
                ">" => BinaryOperator.GreaterThan,
                ">=" => BinaryOperator.GreaterThanOrEqual,
                _ => throw new InvalidOperationException($"Unknown relational operator: {operatorText}")
            };

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitAdditive([NotNull] VeliaParser.AdditiveContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;

            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText == "+" ? BinaryOperator.Add : BinaryOperator.Subtract;

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitMultiplicative([NotNull] VeliaParser.MultiplicativeContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText switch
            {
                "*" => BinaryOperator.Multiply,
                "/" => BinaryOperator.Divide,
                "%" => BinaryOperator.Modulo,
                _ => throw new InvalidOperationException($"Unknown multiplicative operator: {operatorText}")
            };

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitLogicalAnd([NotNull] VeliaParser.LogicalAndContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            return [new BinaryExpressionSyntaxNode(left, BinaryOperator.LogicalAnd, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitLogicalOr([NotNull] VeliaParser.LogicalOrContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            return [new BinaryExpressionSyntaxNode(left, BinaryOperator.LogicalOr, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitBitwise([NotNull] VeliaParser.BitwiseContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText switch
            {
                "&" => BinaryOperator.BitwiseAnd,
                "|" => BinaryOperator.BitwiseOr,
                "^" => BinaryOperator.BitwiseXor,
                _ => throw new InvalidOperationException($"Unknown bitwise operator: {operatorText}")
            };

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitShift([NotNull] VeliaParser.ShiftContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText == "<<" ? BinaryOperator.LeftShift : BinaryOperator.RightShift;

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitAssignment([NotNull] VeliaParser.AssignmentContext context)
        {
            var left = VisitExpression(context.expression(0)).FirstOrDefault() as ExpressionSyntaxNode;
            var right = VisitExpression(context.expression(1)).FirstOrDefault() as ExpressionSyntaxNode;
            if (left == null || right == null) return [];

            var operatorText = context.children[1].GetText();
            var op = operatorText switch
            {
                "=" => BinaryOperator.Assign,
                "+=" => BinaryOperator.AddAssign,
                "-=" => BinaryOperator.SubtractAssign,
                "*=" => BinaryOperator.MultiplyAssign,
                "/=" => BinaryOperator.DivideAssign,
                "%=" => BinaryOperator.ModuloAssign,
                "&=" => BinaryOperator.AndAssign,
                "|=" => BinaryOperator.OrAssign,
                "^=" => BinaryOperator.XorAssign,
                "<<=" => BinaryOperator.LeftShiftAssign,
                ">>=" => BinaryOperator.RightShiftAssign,
                _ => throw new InvalidOperationException($"Unknown assignment operator: {operatorText}")
            };

            return [new BinaryExpressionSyntaxNode(left, op, right)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitFunctionDeclaration([NotNull] VeliaParser.FunctionDeclarationContext context)
        {
            var attributes = new List<AttributeDeclSyntaxNode>();
            foreach (var attribute in context.attribute())
            {
                if (attribute == null)
                {
                    _diagnostics.Add(DiagnosticMessages.VL1008_FunctionDeclHasInvalidAttr);
                    return [];
                }

                var name = new IdentifierSyntaxNode(attribute.IDENTIFIER().GetText());
                ExpressionSyntaxNode? expressionStatement = attribute.expression() == null 
                    ? null 
                    : VisitExpression(attribute.expression()).FirstOrDefault() as ExpressionSyntaxNode;

                // validate expression
                if (attribute.expression() != null && expressionStatement == null)
                {
                    _diagnostics.Add(DiagnosticMessages.VL1009_FunctionDeclAttrHasInvalidExpr);
                    return [];
                }

                attributes.Add(new AttributeDeclSyntaxNode(name, expressionStatement));
            }

            bool isStatic = context.STATIC() != null;
            bool isUnsafe = context.UNSAFE() != null;
            bool isConstExpr = context.CONSTEXPR() != null;
            var functionName = new IdentifierSyntaxNode(context.IDENTIFIER().GetText());
            var returnTypeName = new IdentifierSyntaxNode(context.type().primaryType().qualifiedName().GetText());
            bool isConst = context.CONST() != null;

            var parameterList = VisitParameterList(context.parameterList()).FirstOrDefault() as FunctionParamListSyntaxNode;
            if (parameterList == null)
            {
                _diagnostics.Add(DiagnosticMessages.VL1006_FunctionDeclHasNoParamList);
                return [];
            }

            var blockStmt = VisitBlock(context.block()).FirstOrDefault() as BlockStmtSyntaxNode;
            if (blockStmt == null)
            {
                _diagnostics.Add(DiagnosticMessages.VL1007_FunctionDeclHasNoBlock);
                return [];
            }

            // TODO: ADD CONTRACTS
            return [new FunctionDeclSyntaxNode(attributes, isStatic, isUnsafe, isConstExpr, functionName, parameterList, isConst, [], blockStmt)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitReturnStatement([NotNull] VeliaParser.ReturnStatementContext context)
        {
            ExpressionSyntaxNode? expressionStatement = context.expression() == null
                ? null
                : VisitExpression(context.expression()).FirstOrDefault() as ExpressionSyntaxNode;

            // validate expression
            if (context.expression() != null && expressionStatement == null)
            {
                _diagnostics.Add(DiagnosticMessages.VL1013_ReturnStmtHasInvalidExpr);
                return [];
            }

            return [new ReturnStmtSyntaxNode(expressionStatement)];
        }

        public override IEnumerable<VeliaSyntaxNode> VisitBlock([NotNull] VeliaParser.BlockContext context)
        {
            var statements = context.statement()
                .SelectMany(stmt => Visit(stmt) ?? Enumerable.Empty<VeliaSyntaxNode>())
                .ToList();
            return [new BlockStmtSyntaxNode(statements)];
        }
    }
}
