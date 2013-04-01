using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            loadCustomFormats();
        }

        private bool text_changed = false;

        private void loadCustomFormats() {
            formatsCombo.Items.Clear();

            foreach(string name in config.getCustomFormatNames())
                formatsCombo.Items.Add(name);

            if (formatsCombo.Items.Count == 0) {
                formattingText.Text = "";
                formatsCombo.SelectedIndex = -1;
            }
            else {
                formatsCombo.SelectedIndex = 0;
            }
        }

        private void deleteCustomFormat() {
            if (MessageBox.Show(this, "Are you sure you want to delete this?", "Double Sure?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                return;

            string name = formatsCombo.Text;
            text_changed = false;

            if (config.getCustomFormatNames().Contains(name)) {
                int i = formatsCombo.SelectedIndex;
                config.deleteCustomFormat(name);
                loadCustomFormats();
                if (i >= formatsCombo.Items.Count)
                    i--;
                formatsCombo.SelectedIndex = i;
            }
            else {
                if(formatsCombo.Items.Count==0)
                    formatsCombo.SelectedIndex = -1;
                else
                    formatsCombo.SelectedIndex = 0;
            }

        }

        private bool saveCustomFormat(string name_override) {
            string name;
            if (name_override != null)
                name = name_override;
            else
                name = formatsCombo.Text;

            string statement = formattingText.Text;

            if(name==""||!Regex.IsMatch(name,"[-_A-Za-z0-9 ]*")) {
                MessageBox.Show(this, "The entered name is not acceptable.\nLetter, numbers, spaces, dashes and underscores only.");
                return false;
            }

            if(config.saveCustomFormat(name,statement)) 
                text_changed = false;

            loadCustomFormats();

            formatsCombo.SelectedItem = name;
            return true;
        }
        bool interrupt_text_box = false;
        string _prev_text;
        string previous_text {
            get {
                return _prev_text;
            }
            set {
                _prev_text = value;
            }
        }
        private void formatsCombo_SelectedIndexChanged(object sender, EventArgs e) {
            if (interrupt_text_box)
                return;
            if (text_changed) {
                System.Windows.Forms.DialogResult result = MessageBox.Show(this, "You have changed the format text since last saving.\nWould you like to save it as the entered name?","Just checking, but...", MessageBoxButtons.YesNoCancel);
                interrupt_text_box = true;
                switch(result) {
                    case System.Windows.Forms.DialogResult.Cancel:
                        text_changed = false;
                        formatsCombo.SelectedIndex = -1;
                        formatsCombo.Text += previous_text;
                        text_changed = true;
                        interrupt_text_box = false;
                        return;
                    case System.Windows.Forms.DialogResult.Yes:
                        string name = formatsCombo.Text;
                        if (this.saveCustomFormat(previous_text)) {
                            text_changed = false;
                            formatsCombo.Text = name;
                            break;
                        }
                        else {
                            text_changed = false;
                            formatsCombo.SelectedIndex = -1;
                            formatsCombo.Text += previous_text;
                            text_changed = true;
                            interrupt_text_box = false;
                            return;
                        }
                    case System.Windows.Forms.DialogResult.No:
                        break;
                }
                interrupt_text_box = false;
            }
            formattingText.Text = config.getCustomFormat((string)formatsCombo.SelectedItem);
            text_changed = false;
            previous_text = formatsCombo.Text;
        }


        private SettingsHandler config = new SettingsHandler();
        private void processText()
        {
            Char[] seperator = new char[2];
            switch (seperatorCombo.SelectedIndex)
            {
                case 0:
                    seperator = Environment.NewLine.ToCharArray();
                    break;
                case 1:
                    seperator[0] = ',';
                    break;
                case 2:
                    seperator[0] = ' ';
                    break;
            }

            StringBuilder input = new StringBuilder(inputText.Text);
            input.Replace(Environment.NewLine, "\r");

            string[] temp_texts = input.ToString().Split(seperator);


            Int32 i = 1;
            StringBuilder output = new StringBuilder();
            //if (addQueryCheck.Checked)
              //  output.Append(queryStartText.Text);
            bool printCloseQuery = true;
            int perLineCount = 1;
            int text_count = 0, group_count = 1;

            List<string> texts = new List<string>();
            foreach(string temp_text in temp_texts) {
                if (temp_text.Equals("") && removBlanksCheck.Checked)
                    continue;

                text_count++;
                if (!duplicateCheck.Checked||
                    texts.Count == 0 ||
                    !texts.Contains(temp_text)) {
                        texts.Add(temp_text);
                }
            }
            if (sortCheck.Checked) {
                texts.Sort();
            }

            //if (addQueryCheck.Checked && printCloseQuery)
  //              output.Append(queryEndText.Text);

            output.Clear();
            int item_num = 0;
            string format;
            if (customFormatCheck.Checked) {
                format = formattingText.Text;
            }
            else {
                StringBuilder default_format = new StringBuilder("$1");
                if (singleQuoteCheck.Checked)
                    default_format.Append(":'");
                if (commaCheck.Checked)
                    default_format.Append(":,");
                if (lineBreakCheck.Checked)
                    default_format.Append(":N");

                format = default_format.ToString();
            }
            
            StringBuilder line;
            if(texts.Count>0&&format.Contains("$1")) {
                while (item_num < texts.Count) {
                    i = 1;
                    line = new StringBuilder(format);
                    // Iterates through all the insert statements
                    while(format.Contains("$" + i)) {
                        // This contains the complete insert statement, together with options, to better facilitate replacing it
                        StringBuilder complete_insert_statement = new  StringBuilder("$" + i);
                        // Loads all the options applied to the insert statements
                        int location = format.IndexOf(complete_insert_statement.ToString())+2;
                        int group_size = (int)perGroup.Value;
                        int line_size = (int)perLineNumber.Value;
                        bool single_quotes = false, double_quotes = false, newline = false, commas = false;
                        while (location+1<format.Length&&
                            format.Substring(location, 1).Equals(":")) {
                            location++;
                            int test_number;
                            int start = location;
                            int length = 1;
                            switch (format.Substring(start, 1)) {
                                case ",":
                                    commas = true;
                                    break;
                                case "'":
                                    single_quotes = true;
                                    break;
                                case "\"":
                                    double_quotes = true;
                                    break;
                                case "N":
                                    newline = true;
                                    while (start + length < format.Length &&
                                        Int32.TryParse(format.Substring(start + 1, length), out test_number)) {
                                        line_size = test_number;
                                        length++;
                                    }
                                    break;
                                default:
                                    while(start+length<=format.Length&&
                                        Int32.TryParse(format.Substring(start, length), out test_number)) {
                                        group_size = test_number;
                                        length++;
                                    }
                                    length--;
                                    break;
                            }
                            complete_insert_statement.Append(":");
                            complete_insert_statement.Append(format.Substring(start, length));
                            location += length;
                        }

                        
                        StringBuilder group = new StringBuilder();
                        for (int j = 0; j<group_size&&item_num<texts.Count; j++) {
                            StringBuilder item = new StringBuilder(texts[item_num]);
                            if (double_quotes) {
                                item.Insert(0, "\"");
                                item.Append("\"");
                            }
                            if (single_quotes) {
                                item.Insert(0, "'");
                                item.Append("'");
                            }


                            if (commas&&j!=group_size-1&&item_num!=texts.Count-1) {
                                item.Append(",");
                            }

                            if (newline) {
                                if((j+1)%line_size==0)
                                    group.AppendLine(item.ToString());
                                else
                                    group.Append(item.ToString());
                            }
                            else
                                group.Append(item.ToString());
                            item_num++;
                        }
                        
                        line.Replace(complete_insert_statement.ToString(),group.ToString().Trim(','));
                        i++;
                    }
                    output.Append(line);
                    output.AppendLine();
                }
            }

            outputText.Text = output.ToString().Trim();

            itemCount.Text = text_count + " Items";
            lineCount.Text = CountLinesInString(outputText.Text) + " Lines";

            outputText.SelectAll();
        }

        static long CountLinesInString(string s) {
            long count = 1;
            int start = 0;
            while ((start = s.IndexOf('\n', start)) != -1) {
                count++;
                start++;
            }
            return count;
        }
        private void inputText_TextChanged(object sender, EventArgs e)
        {
            processText();
        }

        private void singleQuoteCheck_CheckedChanged(object sender, EventArgs e)
        {
            processText();

        }

        private void commaCheck_CheckedChanged(object sender, EventArgs e)
        {
            processText();

        }

        private void lineBreakCheck_CheckedChanged(object sender, EventArgs e)
        {
            perLineNumber.Enabled = lineBreakCheck.Checked;
            processText();
        }

        private void seperatorCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            processText();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            seperatorCombo.SelectedIndex = 0;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(outputText.Text);
        }

        private void addQueryCheck_CheckedChanged(object sender, EventArgs e)
        {
            formatBox.Visible = customFormatCheck.Checked;
            formattingText.Enabled = customFormatCheck.Checked;
            formatLegendBox.Visible = customFormatCheck.Checked;
            customFormatPanel.Visible = customFormatCheck.Checked;
            processText();
        }

        private void thousandCheck_CheckedChanged(object sender, EventArgs e)
        {
            processText();

        }

        private void queryStartText_TextChanged(object sender, EventArgs e)
        {
            processText();

        }

        private void queryEndText_TextChanged(object sender, EventArgs e)
        {
            processText();

        }

        private void perLineNumber_ValueChanged(object sender, EventArgs e) {
            processText();
        }

        private void perGroup_ValueChanged(object sender, EventArgs e) {
            processText();
        }

        private void duplicateCheck_CheckedChanged(object sender, EventArgs e) {
            processText();
        }

        private void sortCheck_CheckedChanged(object sender, EventArgs e) {
            processText();
        }

        private void formattingText_TextChanged(object sender, EventArgs e) {
            text_changed = true;
            processText();
        }

        private void saveBtn_Click(object sender, EventArgs e) {
            saveCustomFormat(null);
        }

        private void deleteBtn_Click(object sender, EventArgs e) {
            deleteCustomFormat();
        }

        private void formatsCombo_TextUpdate(object sender, EventArgs e) {
            previous_text = formatsCombo.Text;
        }

        private void button2_Click(object sender, EventArgs e) {
            if (saveFileDialog1.ShowDialog(this) != System.Windows.Forms.DialogResult.Cancel) {
                TextWriter writer = null;
                try {
                    writer = File.CreateText(saveFileDialog1.FileName);
                    writer.Write(outputText.Text);
                } catch (Exception ex) {
                } finally {
                    writer.Close();
                }
            }
        }

        private void inputText_KeyDown(object sender, KeyEventArgs e)
        {
            TextBoxBase box = sender as TextBoxBase;
            if (box == null)
            {
                return;
            }
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        box.SelectAll();
                        break;
                }
            }

        }

    }
}
