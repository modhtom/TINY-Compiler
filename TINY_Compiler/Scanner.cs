using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public enum Token_Class
{
    INT, FLOAT, STRING, READ, WRITE, REPEAT, UNTIL, IF, ELSEIF, ELSE, THEN, RETURN, ENDL, END,
    NUMBER, IDENTIFIER, FUNCTION_CALL, STRING_KEYWORD,
    PLUS, MINUS, MULTIPLY, DIVIDE, ASSIGN, LESS_THAN, GREATER_THAN, EQUAL, NOT_EQUAL,
    AND, OR, LEFT_CURLY_BRACKET, RIGHT_CURLY_BRACKET, SEMICOLON, COMMA , LEFT_BRACKET, RIGHT_BRACKET , MAIN, ENDOFSTREAM
}
namespace TINY_Compiler
{
    public class Token
    {
        public string lex;
        public Token_Class token_type;
    }

    public class Scanner
    {
        public List<Token> Tokens = new List<Token>();

        Dictionary<string, Token_Class> ReservedWords = new Dictionary<string, Token_Class>
        {
            {"int", Token_Class.INT},
            {"float", Token_Class.FLOAT},
            {"string", Token_Class.STRING_KEYWORD},
            {"read", Token_Class.READ},
            {"write", Token_Class.WRITE},
            {"repeat", Token_Class.REPEAT},
            {"until", Token_Class.UNTIL},
            {"if", Token_Class.IF},
            {"elseif", Token_Class.ELSEIF},
            {"else", Token_Class.ELSE},
            {"then", Token_Class.THEN},
            {"return", Token_Class.RETURN},
            {"endl", Token_Class.ENDL},
            {"end", Token_Class.END},
            {"main", Token_Class.MAIN }
        };

        Dictionary<string, Token_Class> Operators = new Dictionary<string, Token_Class>
        {
            {"+", Token_Class.PLUS},
            {"-", Token_Class.MINUS},
            {"–", Token_Class.MINUS},
            {"*", Token_Class.MULTIPLY},
            {"/", Token_Class.DIVIDE},
            {":=", Token_Class.ASSIGN},
            {"<", Token_Class.LESS_THAN},
            {">", Token_Class.GREATER_THAN},
            {"=", Token_Class.EQUAL},
            {"<>", Token_Class.NOT_EQUAL},
            {"&&", Token_Class.AND},
            {";", Token_Class.SEMICOLON},  
            {",", Token_Class.COMMA},
            {"{", Token_Class.LEFT_CURLY_BRACKET},
            {"}", Token_Class.RIGHT_CURLY_BRACKET},
            {"(", Token_Class.LEFT_BRACKET},
            {")", Token_Class.RIGHT_BRACKET},
            {"||", Token_Class.OR},
        };

        public void StartScanning(string SourceCode)
        {
            int lineIndex = 1;
            for (int i = 0; i<SourceCode.Length; i++)
            {
                int j = i;
                char CurrentChar = SourceCode[i];
                string CurrentLexeme = CurrentChar.ToString();

                if (CurrentChar == ' ' || CurrentChar == '\r' || CurrentChar == '\t')
                    continue;

                if (CurrentChar == '\n')
                {
                    lineIndex++;
                    continue;
                }

                // String
                if (CurrentChar == '"')
                {
                    j++;
                    bool isClosed = false;
                    while (j < SourceCode.Length)
                    {
                        if (SourceCode[j] == '"' && (j == 0 || SourceCode[j - 1] != '\\'))
                        {
                            isClosed = true;
                            break;
                        }
                        CurrentLexeme += SourceCode[j];
                        j++;
                    }

                    if (!isClosed)
                    {
                        Errors.Error_List.Add($"Unterminated string literal: {CurrentLexeme}");
                    }
                    else
                    {
                        CurrentLexeme = CurrentLexeme + '"';
                        FindTokenClass(CurrentLexeme);
                    }
                    i = j;
                }

                // Identifier
                else if (CurrentChar >= 'A' && CurrentChar <= 'z') 
                {
                    j++;
                    while(j < SourceCode.Length && ((SourceCode[j] >= 'A' && SourceCode[j] <= 'z') || (SourceCode[j] >= '0' && SourceCode[j] <= '9')))
                    {
                        CurrentLexeme+=SourceCode[j];
                        j++;
                    }
                    j--;
                    i=j;
                    FindTokenClass(CurrentLexeme);
                }

                // Number
                else if (CurrentChar >= '0' && CurrentChar <= '9')
                {
                    j++;
                    while (j < SourceCode.Length && ((SourceCode[j] >= '0' && SourceCode[j] <= '9') || (SourceCode[j] >= 'A' && SourceCode[j] <= 'z' || (SourceCode[j] == '.'))))
                    {
                        CurrentLexeme+=SourceCode[j];
                        j++;
                    }
                    j--;
                    i=j;
                    FindTokenClass(CurrentLexeme);
                }

                // Comment
                else if (CurrentChar == '/' && j + 1 < SourceCode.Length && SourceCode[j + 1] == '*')
                {
                    bool closed = false;
                    CurrentLexeme="";
                    CurrentLexeme += SourceCode[j];
                    CurrentLexeme += SourceCode[j + 1];
                    j += 2;

                    while (j < SourceCode.Length - 1)
                    {
                        if ((SourceCode[j] == '*' && SourceCode[j + 1] == '/'))
                        {
                            CurrentLexeme += SourceCode[j];
                            CurrentLexeme += SourceCode[j + 1];
                            j += 2;
                            closed = true;
                            break;
                        }
                        CurrentLexeme += SourceCode[j];
                        j++;
                    }
                  CurrentLexeme += SourceCode[j];
                    if (/*CurrentLexeme.Length < 4 ||*/ !closed)
                    {
                        //i = j;
                        Errors.Error_List.Add("COMMENT ERROR: " + CurrentLexeme);
                    }
                    else
                    {
                        i = j-1;
                        Comment.Comment_List.Add($"Line: {lineIndex}\nCOMMENT: " + CurrentLexeme + "\n");
                    }

                }

                // :=
                else if (j < SourceCode.Length-1 && (SourceCode[j] == ':' && SourceCode[j+1]=='='))
                {
                    j += 1;
                    CurrentLexeme=":=";
                    i=j;
                    FindTokenClass(CurrentLexeme);
                }

                // ||
                else if (j < SourceCode.Length - 1 && (SourceCode[j] == '|' && SourceCode[j + 1] == '|'))
                {
                    j += 1;
                    CurrentLexeme = "||";
                    i = j;
                    FindTokenClass(CurrentLexeme);
                }

                // &&
                else if (j < SourceCode.Length - 1 && (SourceCode[j] == '&' && SourceCode[j + 1] == '&'))
                {
                    j += 1;
                    CurrentLexeme = "&&";
                    i = j;
                    FindTokenClass(CurrentLexeme);
                }

                // <>
                else if (j < SourceCode.Length - 1 && (SourceCode[j] == '<' && SourceCode[j + 1] == '>'))
                {
                    j += 1;
                    CurrentLexeme = "||";
                    i = j;
                    FindTokenClass(CurrentLexeme);
                }

                else
                {
                    CurrentLexeme = CurrentChar.ToString();
                    FindTokenClass(CurrentLexeme);
                }

            }

            TINY_Compiler.TokenStream = Tokens;
        }
        void FindTokenClass(string Lex)
        {
           // Token_Class TC;
            Token Tok = new Token();
            Tok.lex = Lex;

            if (Lex.StartsWith("\"")&&Lex.EndsWith("\"") && Lex.Length > 1)
            {
                Tok.token_type =  Token_Class.STRING;
            }
            else if (ReservedWords.ContainsKey(Lex.ToLower()))
            {
                Tok.token_type = ReservedWords[Lex.ToLower()];
            }
            else if (isIdentifier(Lex))
            {
                Tok.token_type = Token_Class.IDENTIFIER;
            }
            else if (isConstant(Lex))
            {
                Tok.token_type = Token_Class.NUMBER;
            }
            else if (Operators.ContainsKey(Lex))
            {
                Tok.token_type = Operators[Lex];
            }
            else
            {
                Errors.Error_List.Add("UNDEFINED TOKEN: "+Lex);
                return;
            }

            Tokens.Add(Tok);
        }

        bool isIdentifier(string lex)
        {
            var rx = new Regex(@"^[a-zA-Z][a-zA-Z0-9]*$");
            return rx.IsMatch(lex);
        }
        bool isConstant(string lex)
        { 
            var rx = new Regex(@"^[0-9]+(\.[0-9]+)?$");
            return rx.IsMatch(lex);
        }
    }
}
