grammar Velia;

// --------------------
// Parser Rules
// --------------------

program
    : packageDeclaration? useDeclaration* topLevelDeclaration* EOF
    ;

packageDeclaration
    : 'package' STRING_LITERAL ';'
    ;

useDeclaration
    : 'use' 'pkg' STRING_LITERAL ('{' STRING_LITERAL (',' STRING_LITERAL)* '}')? ';'
    ;

topLevelDeclaration
    : functionDeclaration
    | structDeclaration
    | traitDeclaration
    | extendDeclaration
    | typeAliasDeclaration
    | enumDeclaration
    | variableDeclaration
    | externBlock
    | preprocessorDirective
    | attribute
    ;

// Attributes
attribute
    : '[' '@' IDENTIFIER ('(' expression ')')? ']'
    ;

// Preprocessor
preprocessorDirective
    : '#if' expression block ('else' block)?
    ;

// Structs
structDeclaration
    : 'struct' IDENTIFIER '{' structMember* '}'
    ;

structMember
    : unionBlock
    | fieldDeclaration
    ;

unionBlock
    : 'union' '{' fieldDeclaration+ '}'
    ;

fieldDeclaration
    : IDENTIFIER ':' type ';'
    ;

// Traits
traitDeclaration
    : 'trait' IDENTIFIER '{' traitMember* '}'
    ;

traitMember
    : functionSignature ';'
    ;

// Extend (impl)
extendDeclaration
    : 'extend' type 'with' IDENTIFIER '{' functionDeclaration* '}'
    ;

// Type Aliases
typeAliasDeclaration
    : 'type' IDENTIFIER '=' type ';'
    ;

// Enums
enumDeclaration
    : 'enum' IDENTIFIER '{' enumMember (',' enumMember)* ','? '}' ';'
    ;

enumMember
    : IDENTIFIER ('=' expression)?
    ;

// Extern
externBlock
    : 'extern' STRING_LITERAL '{' externDeclaration* '}'
    ;

externDeclaration
    : functionSignature ';'
    ;

// Functions
functionDeclaration
    : attribute* 
      STATIC?
      ('unsafe' | 'constexpr')? 
      'fn' IDENTIFIER 
      '(' parameterList? ')' 
      (':' type)? 
      ('const')? 
      functionContract*
      block
    ;

functionSignature
    : 'fn' IDENTIFIER '(' parameterList? ')' (':' type)? ('const')?
    ;

functionContract
    : 'and' 'requires' '(' expression ')' 'with' 'message' '(' STRING_LITERAL ')'
    ;

parameterList
    : parameter (',' parameter)*  
    ;

parameter
    : 'mutable'? IDENTIFIER ('&' | '*')? ':' type
    ;

// Variables
variableDeclaration
    : ('static' | 'const')* ('unsafe')? 'let' IDENTIFIER ':' type ('=' expression)? ';'
    ;

// Statements
statement
    : block
    | variableDeclaration
    | ifStatement
    | whileStatement
    | forStatement
    | matchStatement
    | returnStatement
    | throwStatement
    | expressionStatement
    | inlineAsmStatement
    | 'break' ';'
    | 'continue' ';'
    ;

block
    : '{' statement* '}'
    ;

ifStatement
    : 'if' expression block ('else' (ifStatement | block))?
    ;

whileStatement
    : 'while' expression block
    ;

forStatement
    : 'for' 
      ('let' IDENTIFIER ':' type)? 
      (';' | '<' | '>' | '<=' | '>=') 
      expression ';' 
      expression 
      block
    ;

matchStatement
    : 'match' expression '{' matchArm (',' matchArm)* ','? '}'
    ;

matchArm
    : matchPattern '=>' expression
    ;

matchPattern
    : '>' expression  # GreaterThanPattern
    | '<' expression  # LessThanPattern
    | expression      # ExactPattern
    | '_'             # WildcardPattern
    ;

returnStatement
    : 'return' expression? ';'
    ;

throwStatement
    : 'throw' expression ';'
    ;

expressionStatement
    : expression ';'
    ;

inlineAsmStatement
    : 'inline' 'asm' '{' asmLine* '}' ';'
    ;

asmLine
    : STRING_LITERAL
    | ':' STRING_LITERAL '(' IDENTIFIER ')'
    ;

// --------------------
// Expressions (with precedence)
// --------------------

expression
    : lambdaExpression                                                    # Lambda
    | expression '?' expression ':' expression                            # Ternary
    | expression ('=' | '+=' | '-=' | '*=' | '/=' | '%=' | '&=' | '|=' | '^=' | '<<=' | '>>=') expression  # Assignment
    | expression ('||' | 'or') expression                                 # LogicalOr
    | expression ('&&' | 'and') expression                                # LogicalAnd
    | expression ('==' | '!=') expression                                 # Equality
    | expression ('<' | '<=' | '>' | '>=') expression                     # Relational
    | expression ('<<' | '>>') expression                                 # Shift
    | expression ('+' | '-') expression                                   # Additive
    | expression ('*' | '/' | '%') expression                             # Multiplicative
    | expression ('&' | '|' | '^') expression                             # Bitwise
    | ('!' | 'not' | '-' | '++' | '--') expression                        # Unary
    | expression '?'                                                      # ErrorPropagation
    | expression '(' argumentList? ')'                                    # FunctionCall
    | expression '[' expression ']'                                       # ArrayAccess
    | expression ('.' | '->') IDENTIFIER                                  # MemberAccess
    | expression ('++' | '--')                                            # PostfixIncDec
    | '@' IDENTIFIER ('(' argumentList? ')')?                             # CompilerIntrinsic
    | '(' expression ')'                                                  # Parenthesized
    | arrayLiteral                                                        # ArrayLit
    | IDENTIFIER                                                          # Identifier
    | NUMBER_LITERAL                                                      # NumberLit
    | HEX_LITERAL                                                         # HexLit
    | STRING_LITERAL                                                      # StringLit
    | CHAR_LITERAL                                                        # CharLit
    | BOOLEAN_LITERAL                                                     # BooleanLit
    ;

lambdaExpression
    : '(' parameterList? ')' block
    ;

arrayLiteral
    : '{' expression (',' expression)* ','? '}'
    ;

argumentList
    : expression (',' expression)* 
    ;

// --------------------
// Types
// --------------------
// Supports Go-style prefix arrays (e.g. []std.string), postfix pointer/reference (*, &),
// and qualified names (e.g. std.string, core.io.File)

type
    : arrayPrefix? primaryType typeSuffix*
    ;

arrayPrefix
    : ARRAY arrayPrefix?                // allows nested arrays: [][]T
    ;

typeSuffix
    : '*'                              // pointer
    | '&'                              // reference
    ;

primaryType
    : qualifiedName
    | functionType
    ;

functionType
    : 'fn' '(' typeList? ')' (':' type)?
    ;

qualifiedName
    : IDENTIFIER ('.' IDENTIFIER)*
    ;

typeList
    : type (',' type)*
    ;

// --------------------
// Lexer Rules
// --------------------

// Keywords
PACKAGE : 'package';
USE : 'use';
PKG : 'pkg';
STRUCT : 'struct';
UNION : 'union';
TRAIT : 'trait';
EXTEND : 'extend';
WITH : 'with';
IMPL : 'impl';
TYPE : 'type';
ENUM : 'enum';
EXTERN : 'extern';
FN : 'fn';
LET : 'let';
CONST : 'const';
STATIC : 'static';
UNSAFE : 'unsafe';
CONSTEXPR : 'constexpr';
MUTABLE : 'mutable';
INLINE : 'inline';
ASM : 'asm';
IF : 'if';
ELSE : 'else';
WHILE : 'while';
FOR : 'for';
MATCH : 'match';
RETURN : 'return';
BREAK : 'break';
CONTINUE : 'continue';
THROW : 'throw';
AND : 'and';
OR : 'or';
NOT : 'not';
REQUIRES : 'requires';
MESSAGE : 'message';

// Literals
BOOLEAN_LITERAL : 'true' | 'false';
NUMBER_LITERAL : [0-9]+ ('.' [0-9]+)?;
HEX_LITERAL : '0x' [0-9a-fA-F]+;
STRING_LITERAL : '"' (~["\r\n\\] | '\\' .)* '"';
CHAR_LITERAL : '\'' (~['\r\n\\] | '\\' .) '\'';

// Identifiers
IDENTIFIER : [a-zA-Z_][a-zA-Z0-9_]*;

// Operators and punctuation
LPAREN : '(';
RPAREN : ')';
LBRACE : '{';
RBRACE : '}';
ARRAY : '[]';
LBRACKET : '[';
RBRACKET : ']';
SEMICOLON : ';';
COMMA : ',';
DOT : '.';
ARROW : '->';
COLON : ':';
DOUBLE_COLON : '::';
AT : '@';
HASH : '#';
QUESTION : '?';
UNDERSCORE : '_';

// Assignment
ASSIGN : '=';
PLUS_ASSIGN : '+=';
MINUS_ASSIGN : '-=';
STAR_ASSIGN : '*=';
SLASH_ASSIGN : '/=';
MOD_ASSIGN : '%=';
AND_ASSIGN : '&=';
OR_ASSIGN : '|=';
XOR_ASSIGN : '^=';
LSHIFT_ASSIGN : '<<=';
RSHIFT_ASSIGN : '>>=';

// Comparison
EQ : '==';
NE : '!=';
LT : '<';
LE : '<=';
GT : '>';
GE : '>=';

// Arithmetic
PLUS : '+';
MINUS : '-';
STAR : '*';
SLASH : '/';
MOD : '%';

// Logical
AND_OP : '&&';
OR_OP : '||';
NOT_OP : '!';

// Bitwise
AMPERSAND : '&';
PIPE : '|';
CARET : '^';
LSHIFT : '<<';
RSHIFT : '>>';

// Increment/Decrement
INC : '++';
DEC : '--';

// Comments
LINE_COMMENT : '//' ~[\r\n]* -> skip;
BLOCK_COMMENT : '/*' .*? '*/' -> skip;

// Whitespace
WS : [ \t\r\n]+ -> skip;
