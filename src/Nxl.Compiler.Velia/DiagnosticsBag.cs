using System;
using System.Collections;

namespace Nxl.Compiler.Velia;

public enum DiagnosticSeverity
{
    Note,
    Warning,
    Critical
}

public readonly struct DiagnosticMessage : IEquatable<DiagnosticMessage>
{
    public readonly DiagnosticSeverity Severity;
    public readonly ushort Code;
    public readonly string Message;

    public DiagnosticMessage(DiagnosticSeverity severity, ushort code, string message)
    {
        Severity = severity;
        Code = code;
        Message = message;
    }

    public bool Equals(DiagnosticMessage other) => Code == other.Code;
    public override string ToString()
    {
        string severityStr = Severity.ToString().PadRight(8);
        string codeStr = Code.ToString("D4");
        return $"{severityStr} : VL{codeStr} -> {Message}";
    }

    public DiagnosticMessage WithFormat(params object[] args)
    {
        return new DiagnosticMessage(Severity, Code, string.Format(Message, args));
    }
}

public static class DiagnosticMessages
{
    public static readonly DiagnosticMessage VL0001_NoDocumentElements = new DiagnosticMessage(DiagnosticSeverity.Critical, 0001, "Document must contain at least one element.");

    // parsing
    public static readonly DiagnosticMessage VL1000_FailedToParse = new DiagnosticMessage(DiagnosticSeverity.Critical, 1000, "Failed to parse file from lexed tokens. {0}. {1}");
    public static readonly DiagnosticMessage VL1001_ParsedCountActualMismatch = new DiagnosticMessage(DiagnosticSeverity.Critical, 1001, "Mismatch between parsed files and actual files. Parsed {0} file(s), expected {1} totally.");
    public static readonly DiagnosticMessage VL1002_ParseError = new DiagnosticMessage(DiagnosticSeverity.Critical, 1002, "Parse error: '{0}'");
    public static readonly DiagnosticMessage VL1003_ProgramHasNoChild = new DiagnosticMessage(DiagnosticSeverity.Critical, 1003, "Program has no child, unable to parse into green AST");
    public static readonly DiagnosticMessage VL1004_PackageDeclHasNoLiteral = new DiagnosticMessage(DiagnosticSeverity.Critical, 1004, "Package declaration has no String literal");
    public static readonly DiagnosticMessage VL1005_UsePackageDeclHasNoLiteral = new DiagnosticMessage(DiagnosticSeverity.Critical, 1005, "Use package declaration has no String literal");
    public static readonly DiagnosticMessage VL1006_FunctionDeclHasNoParamList = new DiagnosticMessage(DiagnosticSeverity.Critical, 1006, "Function declaration has no Parameter list");
    public static readonly DiagnosticMessage VL1007_FunctionDeclHasNoBlock = new DiagnosticMessage(DiagnosticSeverity.Critical, 1007, "Function declaration has no Block");
    public static readonly DiagnosticMessage VL1008_FunctionDeclHasInvalidAttr = new DiagnosticMessage(DiagnosticSeverity.Critical, 1008, "Function declaration has invalid Attribute");
    public static readonly DiagnosticMessage VL1009_FunctionDeclAttrHasInvalidExpr = new DiagnosticMessage(DiagnosticSeverity.Critical, 1009, "Function declaration Attribute has invalid Expression");
    public static readonly DiagnosticMessage VL1010_ExprStmtHasInvalidExpr = new DiagnosticMessage(DiagnosticSeverity.Critical, 1010, "Expression statement has invalid Expression");
    public static readonly DiagnosticMessage VL1011_InvalidNumberLiteral = new DiagnosticMessage(DiagnosticSeverity.Critical, 1011, "Invalid number literal");
    public static readonly DiagnosticMessage VL1012_InvalidCharLiteral = new DiagnosticMessage(DiagnosticSeverity.Critical, 1012, "Invalid char literal");
    public static readonly DiagnosticMessage VL1013_ReturnStmtHasInvalidExpr = new DiagnosticMessage(DiagnosticSeverity.Critical, 1013, "Return statement has invalid Expression");

    // lexing
    public static readonly DiagnosticMessage VL2001_UnterminatedStringLiteral = new DiagnosticMessage(DiagnosticSeverity.Critical, 2001, "Unterminated string literal in file '{0}'.");
    public static readonly DiagnosticMessage VL2002_UnrecognizedCharacter = new DiagnosticMessage(DiagnosticSeverity.Warning, 2002, "Unrecognized character '{0}' in source file.");

    // code gen
    public static readonly DiagnosticMessage VL3000_NoMainFunctionFoundInProgram = new DiagnosticMessage(DiagnosticSeverity.Critical, 3000, "No 'main()' function found in program");
    public static readonly DiagnosticMessage VL3001_FailedToGenerateCodeGen = new DiagnosticMessage(DiagnosticSeverity.Critical, 3001, "Failed to generate assembly via Visitor pattern");
}

public interface IDiagnosticBag : IEnumerable<DiagnosticMessage>
{
    void Add(DiagnosticMessage message);
    void AddRange(IEnumerable<DiagnosticMessage> messages);
    void Clear();

    bool Any { get; }
    bool AnyCritical();
}

public sealed class DiagnosticBag : IDiagnosticBag, IEnumerable<DiagnosticMessage>
{
    public static IDiagnosticBag Instance { get; } = new DiagnosticBag();

    private readonly List<DiagnosticMessage> _messages = new();
    private DiagnosticBag() { }

    public void Add(DiagnosticMessage message)
    {
        _messages.Add(message);
        PrintMessage(message);
    }

    public void AddRange(IEnumerable<DiagnosticMessage> messages)
    {
        _messages.AddRange(messages);
        foreach (var msg in messages) PrintMessage(msg);
    }

    public void Clear() => _messages.Clear();

    public IEnumerator<DiagnosticMessage> GetEnumerator() => _messages.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _messages.GetEnumerator();

    public int Count => _messages.Count;
    public bool Any => _messages.Count > 0;
    public bool AnyCritical() => _messages.Any(m => m.Severity == DiagnosticSeverity.Critical);

    private void PrintMessage(DiagnosticMessage message)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = message.Severity switch
        {
            DiagnosticSeverity.Note => ConsoleColor.Cyan,
            DiagnosticSeverity.Warning => ConsoleColor.Yellow,
            DiagnosticSeverity.Critical => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        Console.WriteLine(message.ToString());
        Console.ForegroundColor = originalColor;
    }
}
