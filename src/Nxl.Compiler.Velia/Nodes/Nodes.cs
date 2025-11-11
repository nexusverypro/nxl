using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Nxl.Compiler.Velia.Generated;

namespace Nxl.Compiler.Velia.Nodes;

public enum SyntaxKind
{
    None,
    DocumentRoot,

    PackageDecl,
    UsePackageDecl,
    AttributeDecl,
    FunctionDecl,
    FunctionParamDecl,

    BlockStmt,
    ListStmt,
    FunctionContractStmt,
    ExpressionStmt,
    ReturnStmt,

    StringLiteral,
    HexadecimalLiteral,
    BooleanLiteral,
    NumberLiteral,
    CharLiteral,

    Identifier,
    FunctionParamList,
    Type,

    LambdaExpr,
    TernaryExpr,
    AssignmentExpr,
    LogicalOrExpr,
    LogicalAndExpr,
    EqualityExpr,
    RelationalExpr,
    ShiftExpr,
    AdditiveExpr,
    MultiplicativeExpr,
    BitwiseExpr,
    UnaryExpr,
    ErrorPropagationExpr,
    FunctionCallExpr,
    ArrayAccessExpr,
    MemberAccessExpr,
    PostfixIncDecExpr,
    CompilerIntrinsicExpr,
    ParenthesizedExpr,
    ArrayLiteralExpr,
}

public abstract class GreenNode
{
    public abstract SyntaxKind Kind { get; }
    public abstract int SlotCount { get; }
    public abstract GreenNode? GetSlot(int slot);
}

public abstract class VeliaSyntaxNode : GreenNode
{
    protected readonly List<VeliaSyntaxNode> _children;

    protected VeliaSyntaxNode() => _children = new List<VeliaSyntaxNode>();
    protected VeliaSyntaxNode(IEnumerable<VeliaSyntaxNode> children)
    {
        _children = children.ToList();
    }

    public void AddChild(VeliaSyntaxNode node) => _children.Add(node);
    public void AddChild(int index, VeliaSyntaxNode node) => _children.Insert(index, node);
    public void RemoveChild(int index) => _children.RemoveAt(index);

    public string ToFullString() => $"<{GetType().Name} ({SlotCount} {(SlotCount == 1 ? "child" : "children")})>";

    public override int SlotCount => _children.Count;
    public override GreenNode? GetSlot(int slot) => _children.ElementAtOrDefault(slot);
}

public sealed class DocumentRootSyntaxNode : VeliaSyntaxNode
{
    private readonly string _filePath;
    public DocumentRootSyntaxNode(string filePath, IEnumerable<VeliaSyntaxNode> children) : base(children)
    {
        _filePath = filePath;
    }

    public string FilePath => _filePath;

    public override SyntaxKind Kind => SyntaxKind.DocumentRoot;
    public IReadOnlyList<VeliaSyntaxNode> Children => _children;
}

public abstract class TopLevelDeclarationNodeBase : VeliaSyntaxNode { }

public sealed class PackageDeclarationSyntaxNode : TopLevelDeclarationNodeBase
{
    private readonly StringLiteralSyntaxNode _packageName;
    public PackageDeclarationSyntaxNode(StringLiteralSyntaxNode packageName)
    {
        _packageName = packageName;
    }

    public StringLiteralSyntaxNode PackageName => _packageName;

    public override SyntaxKind Kind => SyntaxKind.PackageDecl;
    public override int SlotCount => 1;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _packageName,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

public abstract class LiteralNodeBase<TValueType> : ExpressionSyntaxNode
{
    private readonly TValueType _value;
    protected LiteralNodeBase(TValueType value)
    {
        _value = value;
    }

    public TValueType Value => _value;
}

public sealed class StringLiteralSyntaxNode : LiteralNodeBase<string>
{
    public StringLiteralSyntaxNode(string value) : base(value)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.StringLiteral;
    public override int SlotCount => 0;
    public override GreenNode? GetSlot(int slot) => null;
}

public sealed class HexadecimalLiteralSyntaxNode : LiteralNodeBase<string>
{
    public HexadecimalLiteralSyntaxNode(string value) : base(value)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.HexadecimalLiteral;
    public override int SlotCount => 0;
    public override GreenNode? GetSlot(int slot) => null;
}

public sealed class BooleanLiteralSyntaxNode : LiteralNodeBase<bool>
{
    public BooleanLiteralSyntaxNode(bool value) : base(value)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.BooleanLiteral;
    public override int SlotCount => 0;
    public override GreenNode? GetSlot(int slot) => null;
}

public sealed class NumberLiteralSyntaxNode : LiteralNodeBase<long>
{
    public NumberLiteralSyntaxNode(long value) : base(value)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.NumberLiteral;
    public override int SlotCount => 0;
    public override GreenNode? GetSlot(int slot) => null;
}

public sealed class CharLiteralSyntaxNode : LiteralNodeBase<char>
{
    public CharLiteralSyntaxNode(char value) : base(value)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.CharLiteral;
    public override int SlotCount => 0;
    public override GreenNode? GetSlot(int slot) => null;
}

public abstract class ChildBasedNodeBase<TValueType> : VeliaSyntaxNode 
    where TValueType : VeliaSyntaxNode
{
    protected ChildBasedNodeBase(IEnumerable<TValueType> children) 
        : base(children)
    {
    }
}

public sealed class BlockStmtSyntaxNode : ChildBasedNodeBase<VeliaSyntaxNode>
{
    public BlockStmtSyntaxNode(IEnumerable<VeliaSyntaxNode> children) : 
        base(children)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.BlockStmt;
}

public sealed class ListStmtSyntaxNode : ChildBasedNodeBase<StringLiteralSyntaxNode>
{
    public ListStmtSyntaxNode(IEnumerable<StringLiteralSyntaxNode> children) :
        base(children)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.ListStmt;
}

public sealed class UsePackageDeclSyntaxNode : VeliaSyntaxNode
{
    private readonly IEnumerable<StringLiteralSyntaxNode> _packageNames;
    public UsePackageDeclSyntaxNode(IEnumerable<StringLiteralSyntaxNode> packageNames)
    {
        _packageNames = packageNames;
    }

    public IEnumerable<StringLiteralSyntaxNode> PackageNames => _packageNames;

    public override SyntaxKind Kind => SyntaxKind.UsePackageDecl;
    public override int SlotCount => _packageNames.Count();
    public override GreenNode? GetSlot(int slot) => _packageNames.ElementAtOrDefault(slot);
}

public sealed class AttributeDeclSyntaxNode : VeliaSyntaxNode
{
    private readonly IdentifierSyntaxNode _name;
    private readonly ExpressionSyntaxNode? _expression;

    public AttributeDeclSyntaxNode(IdentifierSyntaxNode name, ExpressionSyntaxNode? expression)
    {
        _name = name;
        _expression = expression;
    }

    public IdentifierSyntaxNode Name => _name;
    public ExpressionSyntaxNode? Expression => _expression;

    public override SyntaxKind Kind => SyntaxKind.AttributeDecl;
    public override int SlotCount => _expression == null ? 1 : 2;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _name,
        1 when _expression != null => _expression,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

// TODO: FINISH
public sealed class FunctionContractStmt : VeliaSyntaxNode
{
    public override SyntaxKind Kind => SyntaxKind.FunctionContractStmt;
}

public sealed class FunctionDeclSyntaxNode : VeliaSyntaxNode
{
    private readonly IEnumerable<AttributeDeclSyntaxNode> _attributes;
    private readonly bool _isStatic;
    private readonly bool _isUnsafe;
    private readonly bool _isConstExpr;
    private readonly IdentifierSyntaxNode _name;
    private readonly FunctionParamListSyntaxNode _parameterList;
    private readonly bool _isConst;
    private readonly IEnumerable<FunctionContractStmt> _functionContracts;
    private readonly BlockStmtSyntaxNode _block;

    public FunctionDeclSyntaxNode(
        IEnumerable<AttributeDeclSyntaxNode> attributes,
        bool isStatic,
        bool isUnsafe,
        bool isConstExpr,
        IdentifierSyntaxNode name,
        FunctionParamListSyntaxNode parameterList,
        bool isConst,
        IEnumerable<FunctionContractStmt> functionContracts,
        BlockStmtSyntaxNode block)
    {
        _attributes = attributes;
        _isStatic = isStatic;
        _isUnsafe = isUnsafe;
        _isConstExpr = isConstExpr;
        _name = name;
        _parameterList = parameterList;
        _isConst = isConst;
        _functionContracts = functionContracts;
        _block = block;
    }

    public IEnumerable<AttributeDeclSyntaxNode> Attributes => _attributes;
    public bool IsStatic => _isStatic;
    public bool IsUnsafe => _isUnsafe;
    public bool IsConstExpr => _isConstExpr;
    public IdentifierSyntaxNode Name => _name;
    public FunctionParamListSyntaxNode ParameterList => _parameterList;
    public bool IsConst => _isConst;
    public IEnumerable<FunctionContractStmt> FunctionContracts => _functionContracts;
    public BlockStmtSyntaxNode Block => _block;

    public override SyntaxKind Kind => SyntaxKind.FunctionDecl;
    public override int SlotCount => _attributes.Count() + _functionContracts.Count() + 3;
    public override GreenNode? GetSlot(int slot)
    {
        int attributeCount = _attributes.Count();
        if (slot >= 0 && slot < attributeCount)
            return _attributes.ElementAt(slot);

        int contractCount = _functionContracts.Count();
        int afterAttributes = slot - attributeCount;
        if (afterAttributes >= 0 && afterAttributes < contractCount)
            return _functionContracts.ElementAt(afterAttributes);

        return (slot - attributeCount - contractCount) switch
        {
            0 => _name,
            1 => _parameterList,
            2 => _block,
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };
    }
}

public sealed class IdentifierSyntaxNode : VeliaSyntaxNode
{
    private readonly string _identifier;
    public IdentifierSyntaxNode(string identifier)
    {
        _identifier = identifier;
    }

    public string Identifier => _identifier;

    public override SyntaxKind Kind => SyntaxKind.Identifier;
}

public sealed class FunctionParamDeclSyntaxNode : VeliaSyntaxNode
{
    private readonly bool _isMutable;
    private readonly IdentifierSyntaxNode _name;
    private readonly TypeSyntaxNode _returnType;
    public FunctionParamDeclSyntaxNode(bool isMutable, IdentifierSyntaxNode name, TypeSyntaxNode returnType)
    {
        _isMutable = isMutable;
        _name = name;
        _returnType = returnType;
    }

    public bool IsMutable => _isMutable;
    public IdentifierSyntaxNode Name => _name;
    public TypeSyntaxNode ReturnType => _returnType;

    public override SyntaxKind Kind => SyntaxKind.FunctionParamDecl;
    public override int SlotCount => 2;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _name,
        1 => _returnType,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

public sealed class FunctionParamListSyntaxNode : ChildBasedNodeBase<FunctionParamDeclSyntaxNode>
{
    public FunctionParamListSyntaxNode(IEnumerable<FunctionParamDeclSyntaxNode> children) :
        base(children)
    {
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionParamList;
}

public sealed class ExpressionStmtSyntaxNode : VeliaSyntaxNode
{
    private readonly ExpressionSyntaxNode _expression;
    public ExpressionStmtSyntaxNode(ExpressionSyntaxNode expression)
    {
        _expression = expression;
    }

    public ExpressionSyntaxNode Expression => _expression;

    public override SyntaxKind Kind => SyntaxKind.ExpressionStmt;
    public override int SlotCount => 1;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _expression,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

// TODO: FINISH
public abstract class ExpressionSyntaxNode : VeliaSyntaxNode
{
}

public sealed class TypeSyntaxNode : VeliaSyntaxNode
{
    private readonly bool _isArray;
    private readonly IdentifierSyntaxNode _name;
    private readonly bool _isPointer;
    private readonly bool _isReference;

    public TypeSyntaxNode(bool isArray, IdentifierSyntaxNode name, bool isPointer, bool isReference)
    {
        _isArray = isArray;
        _name = name;
        _isPointer = isPointer;
        _isReference = isReference;
    }

    public bool IsArray => _isArray;
    public IdentifierSyntaxNode Name => _name;
    public bool IsPointer => _isPointer;
    public bool IsReference => _isReference;

    public override SyntaxKind Kind => SyntaxKind.Type;
    public override int SlotCount => 1;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _name,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

public enum BinaryOperator
{
    // arithmetic
    Add, Subtract, Multiply, Divide, Modulo,

    // comparison
    Equals, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,

    // logical
    LogicalAnd, LogicalOr,

    // bitwise
    BitwiseAnd, BitwiseOr, BitwiseXor, LeftShift, RightShift,

    // assignment
    Assign, AddAssign, SubtractAssign, MultiplyAssign, DivideAssign, ModuloAssign,
    AndAssign, OrAssign, XorAssign, LeftShiftAssign, RightShiftAssign
}

public sealed class BinaryExpressionSyntaxNode : ExpressionSyntaxNode
{
    private readonly ExpressionSyntaxNode _left;
    private readonly BinaryOperator _operator;
    private readonly ExpressionSyntaxNode _right;

    public BinaryExpressionSyntaxNode(ExpressionSyntaxNode left, BinaryOperator op, ExpressionSyntaxNode right)
    {
        _left = left;
        _operator = op;
        _right = right;
    }

    public ExpressionSyntaxNode Left => _left;
    public BinaryOperator Operator => _operator;
    public ExpressionSyntaxNode Right => _right;

    public override SyntaxKind Kind => _operator switch
    {
        BinaryOperator.Add or BinaryOperator.Subtract => SyntaxKind.AdditiveExpr,
        BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo => SyntaxKind.MultiplicativeExpr,
        BinaryOperator.Equals or BinaryOperator.NotEquals => SyntaxKind.EqualityExpr,
        BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
        BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual => SyntaxKind.RelationalExpr,
        BinaryOperator.LogicalAnd => SyntaxKind.LogicalAndExpr,
        BinaryOperator.LogicalOr => SyntaxKind.LogicalOrExpr,
        BinaryOperator.BitwiseAnd or BinaryOperator.BitwiseOr or BinaryOperator.BitwiseXor => SyntaxKind.BitwiseExpr,
        BinaryOperator.LeftShift or BinaryOperator.RightShift => SyntaxKind.ShiftExpr,
        _ when _operator >= BinaryOperator.Assign && _operator <= BinaryOperator.RightShiftAssign => SyntaxKind.AssignmentExpr,
        _ => throw new InvalidOperationException($"Unknown binary operator: {_operator}")
    };
    public override int SlotCount => 2;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _left,
        1 => _right,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

public enum UnaryOperator
{
    Not, Negate, PreIncrement, PreDecrement
}

public sealed class UnaryExpressionSyntaxNode : ExpressionSyntaxNode
{
    private readonly UnaryOperator _operator;
    private readonly ExpressionSyntaxNode _operand;

    public UnaryExpressionSyntaxNode(UnaryOperator op, ExpressionSyntaxNode operand)
    {
        _operator = op;
        _operand = operand;
    }

    public UnaryOperator Operator => _operator;
    public ExpressionSyntaxNode Operand => _operand;

    public override SyntaxKind Kind => SyntaxKind.UnaryExpr;
    public override int SlotCount => 1;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _operand,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

public sealed class TernaryExpressionSyntaxNode : ExpressionSyntaxNode
{
    private readonly ExpressionSyntaxNode _condition;
    private readonly ExpressionSyntaxNode _trueExpr;
    private readonly ExpressionSyntaxNode _falseExpr;

    public TernaryExpressionSyntaxNode(ExpressionSyntaxNode condition, ExpressionSyntaxNode trueExpr, ExpressionSyntaxNode falseExpr)
    {
        _condition = condition;
        _trueExpr = trueExpr;
        _falseExpr = falseExpr;
    }

    public ExpressionSyntaxNode Condition => _condition;
    public ExpressionSyntaxNode TrueExpr => _trueExpr;
    public ExpressionSyntaxNode FalseExpr => _falseExpr;

    public override SyntaxKind Kind => SyntaxKind.TernaryExpr;
    public override int SlotCount => 3;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 => _condition,
        1 => _trueExpr,
        2 => _falseExpr,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}

public sealed class CompilerIntrinsicExpressionSyntaxNode : ExpressionSyntaxNode
{
    private readonly IdentifierSyntaxNode _name;
    private readonly IEnumerable<ExpressionSyntaxNode>? _arguments;

    public CompilerIntrinsicExpressionSyntaxNode(IdentifierSyntaxNode name, IEnumerable<ExpressionSyntaxNode>? arguments)
    {
        _name = name;
        _arguments = arguments;
    }

    public IdentifierSyntaxNode Name => _name;
    public IEnumerable<ExpressionSyntaxNode>? Arguments => _arguments;

    public override SyntaxKind Kind => SyntaxKind.CompilerIntrinsicExpr;
    public override int SlotCount => 1 + (_arguments?.Count() ?? 0);
    public override GreenNode? GetSlot(int slot)
    {
        if (slot == 0) return _name;
        return _arguments?.ElementAtOrDefault(slot - 1);
    }
}

public sealed class ReturnStmtSyntaxNode : VeliaSyntaxNode
{
    private readonly ExpressionSyntaxNode? _expression;
    public ReturnStmtSyntaxNode(ExpressionSyntaxNode? expression)
    {
        _expression = expression;
    }

    public ExpressionSyntaxNode? Expression => _expression;

    public override SyntaxKind Kind => SyntaxKind.ReturnStmt;
    public override int SlotCount => _expression == null ? 0 : 1;
    public override GreenNode? GetSlot(int slot) => slot switch
    {
        0 when _expression != null => _expression,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}