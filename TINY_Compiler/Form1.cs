using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TINY_Compiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        void PrintTokens()
        {
            for (int i = 0; i < TINY_Compiler.TINY_Scanner.Tokens.Count; i++)
            {
                dataGridView1.Rows.Add(i, TINY_Compiler.TINY_Scanner.Tokens.ElementAt(i).lex, TINY_Compiler.TINY_Scanner.Tokens.ElementAt(i).token_type);
            }
        }
        void PrintErrors()
        {
            for (int i = 0; i<Errors.Error_List.Count; i++)
            {
                richTextBox2.Text += Errors.Error_List[i];
                richTextBox2.Text += "\r\n";
            }
        }
        void PrintComments()
        {
            for (int i = 0; i<Comment.Comment_List.Count; i++)
            {
                richTextBox3.Text += Comment.Comment_List[i];
                richTextBox3.Text += "\r\n";
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            richTextBox2.Clear();
            string Code = richTextBox1.Text;
            TINY_Compiler.Start_Compiling(Code);
            PrintTokens();
            PrintErrors();
            PrintComments();
            treeView1.Nodes.Add(Parser.PrintParseTree(TINY_Compiler.treeroot));
            if (checkBox1.Checked)
                treeView1.ExpandAll();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Errors.Error_List.Clear();
            Comment.Comment_List.Clear();
            TINY_Compiler.TokenStream.Clear();
            richTextBox2.Clear();
            richTextBox3.Clear();
            dataGridView1.Rows.Clear();
            treeView1.Nodes.Clear();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
