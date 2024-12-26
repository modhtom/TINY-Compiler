using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace TINY_Compiler
{
    public class Node
    {
        public List<Node> Children = new List<Node>();
        
        public string Name;
        public Node(string N)
        {
            this.Name = N;
        }
    }
    public class Parser
    {
        int InputPointer = 0;
        List<Token> TokenStream;
        public Node root;
        public Node StartParsing(List<Token> TokenStream)
        {
            if (Errors.Error_List.Count > 0) return null;
            this.InputPointer = 0;
            this.TokenStream = TokenStream;
            root = new Node("Program");
            Token endToken = new Token();
            endToken.lex = "$";
            endToken.token_type = Token_Class.ENDOFSTREAM;
            TokenStream.Add(endToken);
            root.Children.AddRange(Program().Children);
            return root;
        }
        Node Program()
        {
            Node program = new Node("Program");
            //program.Children.AddRange(DataType().Children);
            program.Children.AddRange(ProgramDash().Children);
            //MessageBox.Show("Success");
            match(Token_Class.ENDOFSTREAM);
            return program;
        }
        Node ProgramDash()
        {
            Node programdash = new Node("Program'");
            //terminate if main, uses lookahead token to simplify
            if ((InputPointer + 1 < TokenStream.Count) && (TokenStream[InputPointer].token_type == Token_Class.INT || TokenStream[InputPointer].token_type == Token_Class.FLOAT ||
                TokenStream[InputPointer].token_type == Token_Class.STRING_KEYWORD) && TokenStream[InputPointer+1].token_type == Token_Class.MAIN || TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM) 
            {
                programdash.Children.Add(Main());
            }
            else //check for identifier then function
            {
                programdash.Children.Add(FunctionDeclaration());
                //programdash.Children.AddRange(DataType().Children);
                programdash.Children.AddRange(ProgramDash().Children);
            }
            return programdash;
        }
        Node Main()
        {
            Node main = new Node("Function Declaration: main");
            main.Children.AddRange(DataType().Children);
            main.Children.Add(match(Token_Class.MAIN));
            main.Children.Add(match(Token_Class.LEFT_BRACKET));
            main.Children.Add(match(Token_Class.RIGHT_BRACKET));
            main.Children.Add(FunctionBody());
            return main;
        }
        Node FunctionDeclaration()
        {
            Node functiondeclaration = new Node("Function Declaration: ");
            functiondeclaration.Children.AddRange(DataType().Children);
            String functionname = TokenStream[InputPointer].lex;
            if (TokenStream[InputPointer].token_type == Token_Class.IDENTIFIER)
            {
                functiondeclaration.Name += functionname;
            }
            functiondeclaration.Children.AddRange(Identifier().Children);
            functiondeclaration.Children.Add(match(Token_Class.LEFT_BRACKET));
            var temp = Parameters();
            if (temp.Children.Count > 0)
            {
                functiondeclaration.Children.Add(temp);
            }
            functiondeclaration.Children.Add(match(Token_Class.RIGHT_BRACKET));
            functiondeclaration.Children.Add(FunctionBody());
            //functiondeclaration.Children.AddRange(DataType().Children);
            return functiondeclaration;
        }
        Node Parameters()
        {
            Node parameters = new Node("Parameters");
            if (TokenStream[InputPointer].token_type == Token_Class.INT || TokenStream[InputPointer].token_type == Token_Class.FLOAT || TokenStream[InputPointer].token_type == Token_Class.STRING || TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM)
            {
                parameters.Children.AddRange(DataType().Children);
                parameters.Children.AddRange(Identifier().Children);
                var temp = Parameter();
                if (temp.Children.Count > 0)
                {
                    parameters.Children.AddRange(temp.Children);
                }
            }
            return parameters;
        }
        Node Parameter()
        {
            Node parameter = new Node("Parameter");
            if (TokenStream[InputPointer].token_type == Token_Class.COMMA)
            {
                parameter.Children.Add(match(Token_Class.COMMA));
                parameter.Children.AddRange(DataType().Children);
                parameter.Children.AddRange(Identifier().Children);
                var temp = Parameter();
               //if (temp.Count > 0)
                {
                    parameter.Children.Add(temp);
                }
            }
            return parameter;
        }
        Node FunctionBody()
        {
            Node functionbody = new Node("Function Body");
            functionbody.Children.Add(match(Token_Class.LEFT_CURLY_BRACKET));
            var temp = Statements();
            if (temp.Children.Count > 0)
            {
                functionbody.Children.Add(temp);
            }
            functionbody.Children.Add(ReturnStatement());
            functionbody.Children.Add(match(Token_Class.RIGHT_CURLY_BRACKET));
            return functionbody;
        }
        Node Statements()
        {
            Node statements = new Node("Statements");
            if (TokenStream[InputPointer].token_type == Token_Class.RETURN || TokenStream[InputPointer].token_type == Token_Class.UNTIL || TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM)
            {
                return statements;
            }
            else
            {
                statements.Children.Add(Statement());
                statements.Children.AddRange(Statements().Children);
            }
            return statements;
        }
        Node Statement()
        {
            Node statement = new Node("Statement");
            if (TokenStream[InputPointer].token_type == Token_Class.REPEAT) // repeat statement
            {
                statement.Name = "Repeat Statement";
                statement.Children.AddRange(RepeatStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.IF) //if statement
            {
                statement.Name = "If Statement";
                statement.Children.AddRange(IfStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.READ) //read
            {
                statement.Name = "Read Statement";
                statement.Children.AddRange(ReadStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.WRITE) //write
            {
                statement.Name = "Write Statement";
                statement.Children.AddRange(WriteStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.INT || TokenStream[InputPointer].token_type == Token_Class.FLOAT || TokenStream[InputPointer].token_type == Token_Class.STRING_KEYWORD) //declaration
            {
                statement.Name = "Declaration Statement";
                statement.Children.AddRange(DeclarationStatement().Children);
            }
            else //assignment
            {
                statement.Name = "Assignment Statement";
                statement.Children.AddRange(AssignmentStatement().Children);
            }
            return statement;
        }
        Node IfStatement()
        {
            Node ifstatement = new Node("If Statement");
            ifstatement.Children.Add(match(Token_Class.IF));
            ifstatement.Children.Add(ConditionStatement());
            ifstatement.Children.Add(match(Token_Class.THEN));
            var temp = StatementsWithReturn();
            if (temp.Children.Count > 0)
            {
                ifstatement.Children.Add(temp);
            }
            ifstatement.Children.AddRange(EndIf().Children);
            return ifstatement;
        }
        Node RepeatStatement()
        {
            Node repeatstatement = new Node("Repeat Statement");
            repeatstatement.Children.Add(match(Token_Class.REPEAT));
            repeatstatement.Children.Add(Statements());
            repeatstatement.Children.Add(match(Token_Class.UNTIL));
            repeatstatement.Children.Add(ConditionStatement());
            return repeatstatement;
        }
        Node ReadStatement()
        {
            Node readstatement = new Node("Read Statement");
            readstatement.Children.Add(match(Token_Class.READ));
            readstatement.Children.AddRange(Identifier().Children);
            readstatement.Children.Add(match(Token_Class.SEMICOLON));
            return readstatement;
        }
        Node WriteStatement()
        {
            Node writestatement = new Node("Write Statement");
            writestatement.Children.Add(match(Token_Class.WRITE));
            if (TokenStream[InputPointer].token_type == Token_Class.ENDL) //endl
            {
                writestatement.Children.Add(match(Token_Class.ENDL));
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.STRING) //string
            {
                writestatement.Children.AddRange(String().Children);
            }
            else // expression
            {
                writestatement.Children.Add(Expression());
            }
            writestatement.Children.Add(match(Token_Class.SEMICOLON));
            return writestatement;
        }
        Node DeclarationStatement()
        {
            Node declarationstatement = new Node("Declaration Statement");
            declarationstatement.Children.AddRange(DataType().Children);
            declarationstatement.Children.AddRange(Identifiers().Children);
            declarationstatement.Children.Add(match(Token_Class.SEMICOLON));
            return declarationstatement;
        }
        Node AssignmentStatement()
        {
            Node assignmentstatement = new Node("Assignment Statement");
            assignmentstatement.Children.AddRange(Identifier().Children);
            assignmentstatement.Children.Add(match(Token_Class.ASSIGN));
            assignmentstatement.Children.Add(Expression());
            assignmentstatement.Children.Add(match(Token_Class.SEMICOLON));
            return assignmentstatement;
        }
        Node StatementsWithReturn()
        {
            Node statementswithreturn = new Node("Statements");
            if (TokenStream[InputPointer].token_type == Token_Class.END || TokenStream[InputPointer].token_type == Token_Class.ELSE || TokenStream[InputPointer].token_type == Token_Class.ELSEIF || TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM)
            {
                return statementswithreturn;
            }
            else
            {
                statementswithreturn.Children.Add(StatementWithReturn());
                statementswithreturn.Children.AddRange(StatementsWithReturn().Children);
            }
            return statementswithreturn;
        }
        Node StatementWithReturn()
        {
            Node statementwithreturn = new Node("Statement With Return");
            if (TokenStream[InputPointer].token_type == Token_Class.REPEAT) // repeat statement
            {
                statementwithreturn.Name = "Repeat Statement";
                statementwithreturn.Children.AddRange(RepeatStatement().Children);

            }
            else if (TokenStream[InputPointer].token_type == Token_Class.RETURN) //return statement
            {
                //statementwithreturn.Name = "Return Statement";
                statementwithreturn.Children.Add(ReturnStatement());
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.IF) //if statement
            {
                statementwithreturn.Name = "If Statement";
                statementwithreturn.Children.AddRange(IfStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.READ) //read
            {
                statementwithreturn.Name = "Read Statement";
                statementwithreturn.Children.AddRange(ReadStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.WRITE) //write
            {
                statementwithreturn.Name = "Write Statement";
                statementwithreturn.Children.AddRange(WriteStatement().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.INT || TokenStream[InputPointer].token_type == Token_Class.FLOAT || TokenStream[InputPointer].token_type == Token_Class.STRING_KEYWORD) //declaration
            {
                statementwithreturn.Name = "Declaration Statement";
                statementwithreturn.Children.AddRange(DeclarationStatement().Children);
            }
            else //assignment
            {
                statementwithreturn.Name = "Assignment Statement";
                statementwithreturn.Children.AddRange(AssignmentStatement().Children);
            }
            return statementwithreturn;
        }
        Node ConditionStatement()
        {
            Node conditionstatement = new Node("Condition Statement");
            conditionstatement.Children.Add(Condition());
            var temp = BoolCondition();
            if (temp.Children.Count > 0)
                conditionstatement.Children.Add(temp);
            return conditionstatement;
        }
        Node Condition()
        {
            Node condition = new Node("Condition");

            condition.Children.AddRange(Identifier().Children);

             if (TokenStream[InputPointer].token_type == Token_Class.LESS_THAN)
            {
                condition.Children.Add(match(Token_Class.LESS_THAN));
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.GREATER_THAN)
            {
                condition.Children.Add(match(Token_Class.GREATER_THAN));
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.EQUAL)
            {
                condition.Children.Add(match(Token_Class.EQUAL));
            }
            else
            {
                condition.Children.Add(match(Token_Class.NOT_EQUAL));
            }

            condition.Children.AddRange(ConditionTerm().Children);

            return condition;
        }
        Node ConditionTerm()
        {
            Node conditionterm = new Node("Condition Term");
            if (TokenStream[InputPointer].token_type == Token_Class.LEFT_BRACKET) //(Condition)
            {
                conditionterm.Children.Add(match(Token_Class.LEFT_BRACKET));
                conditionterm.Children.Add(Condition());
                conditionterm.Children.Add(match(Token_Class.RIGHT_BRACKET));
            }
            if (TokenStream[InputPointer].token_type == Token_Class.NUMBER) //number
            {
                conditionterm.Children.AddRange(Number().Children);
            }
            else //function or identifier
            {
                conditionterm.Children.AddRange(Identifier().Children);
                if (TokenStream[InputPointer].token_type == Token_Class.LEFT_BRACKET)
                {
                    conditionterm.Children.Add(FunctionCall());
                }
            }
                return conditionterm;
        }
        Node BoolCondition()
        {
            Node boolcondition = new Node("Bool Condition");
            /*if (TokenStream[InputPointer].token_type == Token_Class.RIGHT_BRACKET || TokenStream[InputPointer].token_type == Token_Class.THEN || TokenStream[InputPointer].token_type == Token_Class.REPEAT
                || TokenStream[InputPointer].token_type == Token_Class.IF || TokenStream[InputPointer].token_type == Token_Class.READ || TokenStream[InputPointer].token_type == Token_Class.WRITE ||
                TokenStream[InputPointer].token_type == Token_Class.INT || TokenStream[InputPointer].token_type == Token_Class.FLOAT || TokenStream[InputPointer].token_type == Token_Class.STRING ||
                TokenStream[InputPointer].token_type == Token_Class.IDENTIFIER || TokenStream[InputPointer].token_type == Token_Class.END || TokenStream[InputPointer].token_type == Token_Class.RETURN)
            {// means its not a bool op
                return boolcondition;
            }*/
            if (TokenStream[InputPointer].token_type == Token_Class.OR)
            {
                boolcondition.Children.Add(match(Token_Class.OR));
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.AND)
            {
                boolcondition.Children.Add(match(Token_Class.AND));
            }
            if ((TokenStream[InputPointer].token_type == Token_Class.IDENTIFIER))
                boolcondition.Children.Add(Condition());
            if((TokenStream[InputPointer].token_type == Token_Class.OR) || (TokenStream[InputPointer].token_type == Token_Class.AND))
                boolcondition.Children.Add(BoolCondition());
            return boolcondition;
        }
        Node EndIf()
        {
            Node endif = new Node("End IF");
            if (TokenStream[InputPointer].token_type == Token_Class.END)
            {
                endif.Children.Add(match(Token_Class.END));
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.ELSEIF)
            {
                endif.Children.AddRange(ElseIf().Children);
            }
            else
            {
                endif.Children.AddRange(Else().Children);
            }
            return endif;
        }
        Node ElseIf()
        {
            Node elseif = new Node("ElseIf");
            elseif.Children.Add(match(Token_Class.ELSEIF));
            elseif.Children.Add(ConditionStatement());
            elseif.Children.Add(match(Token_Class.THEN));
            var temp = StatementsWithReturn();
            if (temp.Children.Count > 0)
            {
                elseif.Children.Add(temp);
            }
            elseif.Children.AddRange(EndIf().Children);
            return elseif;
        }
        Node Else()
        {
            Node _else = new Node("Else");
            _else.Children.Add(match(Token_Class.ELSE));
            var temp = StatementsWithReturn();
            if (temp.Children.Count > 0)
            {
                _else.Children.Add(temp);
            }
            _else.Children.Add(match(Token_Class.END));
            return _else;
        }
        Node ReturnStatement()
        {
            Node returnstatement = new Node("Return Statement");
            returnstatement.Children.Add(match(Token_Class.RETURN));
            returnstatement.Children.Add(Expression());
            returnstatement.Children.Add(match(Token_Class.SEMICOLON));
            return returnstatement;
        }
        Node Expression()
        {
            Node expression = new Node("Expression");
            expression.Children.AddRange(ExpressionTerm().Children);
            var temp = Exp();
            if (temp.Children.Count > 0)
            {
                expression.Children.Add(temp);
            }
            return expression;
        }
        Node ExpressionTerm()
        {
            Node expressionterm = new Node("Expression Term");
            expressionterm.Children.Add(Factor());
            var temp = Ter();
            if (temp.Children.Count > 0)
            {
                expressionterm.Children.Add(temp);
            }
            return expressionterm;
        }
        Node Exp()
        {
            Node exp = new Node("Exp");
            if (TokenStream[InputPointer].token_type == Token_Class.PLUS || TokenStream[InputPointer].token_type == Token_Class.MINUS)
            {
                if (TokenStream[InputPointer].token_type == Token_Class.PLUS)
                {
                    exp.Children.Add(match(Token_Class.PLUS));
                }
                else
                {
                    exp.Children.Add(match(Token_Class.MINUS));
                }
                exp.Children.AddRange(ExpressionTerm().Children);
                if (TokenStream[InputPointer].token_type == Token_Class.PLUS || TokenStream[InputPointer].token_type == Token_Class.MINUS)
                {
                    exp.Children.AddRange(Exp().Children);
                }
            }
            return exp;
        }
        Node Factor()
        {
            Node factor = new Node("Factor");
            if (TokenStream[InputPointer].token_type == Token_Class.LEFT_BRACKET) //(Expression)
            {
                factor.Children.Add(match(Token_Class.LEFT_BRACKET));
                factor.Children.Add(Expression());
                factor.Children.Add(match(Token_Class.RIGHT_BRACKET));
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.NUMBER) //number
            {
                factor.Children.AddRange(Number().Children);
            }
            else if (TokenStream[InputPointer].token_type == Token_Class.STRING) //number
            {
                factor.Children.AddRange(String().Children);
            }
            else //function or identifier
            {
                factor.Children.AddRange(Identifier().Children);
                if (TokenStream[InputPointer].token_type == Token_Class.LEFT_BRACKET)
                {
                    factor.Children.Add(FunctionCall());
                }
            }
            return factor;
        }
        Node Ter()
        {
            Node ter = new Node("Ter");
            if (TokenStream[InputPointer].token_type == Token_Class.MULTIPLY || TokenStream[InputPointer].token_type == Token_Class.DIVIDE)
            {
                if (TokenStream[InputPointer].token_type == Token_Class.MULTIPLY)
                {
                    ter.Children.Add(match(Token_Class.MULTIPLY));
                }
                else
                {
                    ter.Children.Add(match(Token_Class.DIVIDE));
                }
                ter.Children.Add(Factor());
                if (TokenStream[InputPointer].token_type == Token_Class.MULTIPLY || TokenStream[InputPointer].token_type == Token_Class.DIVIDE)
                {
                    ter.Children.AddRange(Ter().Children);
                }
            }
            return ter;
        }
        Node FunctionCall()
        {
            Node functioncall = new Node("Function Call");
            functioncall.Children.Add(match(Token_Class.LEFT_BRACKET));
            var temp = ArgumentPart();
            if (temp.Children.Count > 0)
                functioncall.Children.Add(temp);
            functioncall.Children.Add(match(Token_Class.RIGHT_BRACKET));
            return functioncall;
        }
        Node ArgumentPart()
        {
            Node expressionpart = new Node("ExpressionPart");
            if(TokenStream[InputPointer].token_type != Token_Class.RIGHT_BRACKET)
                expressionpart.Children.AddRange(Expression().Children);
            if (TokenStream[InputPointer].token_type == Token_Class.COMMA)
            {
                expressionpart.Children.Add(match(Token_Class.COMMA));
                expressionpart.Children.AddRange(ArgumentPart().Children);
            }
            return expressionpart;
        }
        Node DataType()
        {
            Node datatype = new Node("DataType");
                if (TokenStream[InputPointer].token_type == Token_Class.INT) // match int
                {
                    datatype.Children.Add(match(Token_Class.INT));
                }
                else if (TokenStream[InputPointer].token_type == Token_Class.FLOAT) // match float
                {
                    datatype.Children.Add(match(Token_Class.FLOAT));
                }
                else // match string
                {
                    datatype.Children.Add(match(Token_Class.STRING_KEYWORD));
                }
            return datatype;
        }
        Node Identifier()
        {
            Node identifier = new Node("IDENTIFIER: ");
            String identifiername = TokenStream[InputPointer].lex;
            bool isIdentifier = false;

            if (TokenStream[InputPointer].token_type == Token_Class.IDENTIFIER)
            {
                isIdentifier = true;
            }

            identifier.Children.Add(match(Token_Class.IDENTIFIER));
            if (isIdentifier)
            {
                identifier.Children[0].Name += ": " + identifiername;
            }
            return identifier;

        }
        Node Number()
        {
            Node number = new Node("NUMBER: ");
            String numberName = TokenStream[InputPointer].lex;
            bool isNumber = false;

            if (TokenStream[InputPointer].token_type == Token_Class.NUMBER)
            {
                isNumber = true;
            }

            number.Children.Add(match(Token_Class.NUMBER));
            if (isNumber)
            {
                number.Children[0].Name += ": " + numberName;
            }
            return number;
        }
        Node String()
        {
            Node _string = new Node("NUMBER: ");
            String stringName = TokenStream[InputPointer].lex;
            bool isString = false;

            if (TokenStream[InputPointer].token_type == Token_Class.STRING)
            {
                isString = true;
            }

            _string.Children.Add(match(Token_Class.STRING));
            if (isString)
            {
                _string.Children[0].Name += ": " + stringName;
            }
            return _string;
        }
        Node Identifiers()
        {
            Node identifiers = new Node("Identifiers");
            identifiers.Children.AddRange(Identifier().Children);
            if (TokenStream[InputPointer].token_type == Token_Class.COMMA || TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM)
            {
                identifiers.Children.Add(match(Token_Class.COMMA));
                identifiers.Children.AddRange(Identifiers().Children);
            }
            if (TokenStream[InputPointer].token_type == Token_Class.ASSIGN || TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM)
            {
                identifiers.Children.Add(match(Token_Class.ASSIGN));
                identifiers.Children.Add(Expression());
            }
            return identifiers;
        }
        public Node match(Token_Class ExpectedToken)
        {

            if (InputPointer < TokenStream.Count)
            {
                if (TokenStream[InputPointer].token_type == Token_Class.ENDOFSTREAM && InputPointer == TokenStream.Count)
                {
                    Node newNode = new Node("Success");
                    return newNode;
                }
                else if (ExpectedToken == TokenStream[InputPointer].token_type)
                {
                    if(TokenStream.Count-1 != InputPointer)
                        InputPointer++;
                    Node newNode = new Node(ExpectedToken.ToString());
                    return newNode;
                }
                else
                {
                    Errors.Error_List.Add("Parsing Error: Expected "
                        + ExpectedToken.ToString() + " and " +
                        TokenStream[InputPointer].token_type.ToString() +
                        "  found at token "+ InputPointer.ToString() +"\r\n");
                    if (TokenStream.Count - 1 != InputPointer)
                        InputPointer++;
                    return null;
                }
            }
            else
            {
                Errors.Error_List.Add("Parsing Error: Expected "
                        + ExpectedToken.ToString()  + "\r\n");
                if (TokenStream.Count-1 != InputPointer)
                    InputPointer++;
                return null;
            }
        }
        public static TreeNode PrintParseTree(Node root)
        {
            TreeNode tree = new TreeNode("Parse Tree");
            TreeNode treeRoot = PrintTree(root);
            if (treeRoot != null)
                tree.Nodes.Add(treeRoot);
            return tree;
        }
        static TreeNode PrintTree(Node root)
        {
            if (root == null || root.Name == null)
                return null;
            TreeNode tree = new TreeNode(root.Name);
            if (root.Children.Count == 0)
                return tree;
            foreach (Node child in root.Children)
            {
                if (child == null)
                    continue;
                tree.Nodes.Add(PrintTree(child));
            }
            return tree;
        }
    }
}