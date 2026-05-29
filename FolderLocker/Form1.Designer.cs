namespace FolderLocker
{
    partial class FolderLocker
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label2 = new Label();
            textBox1 = new TextBox();
            button2 = new Button();
            label1 = new Label();
            label3 = new Label();
            textBox2 = new TextBox();
            button1 = new Button();
            button3 = new Button();
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 24);
            label2.Name = "label2";
            label2.Size = new Size(198, 20);
            label2.TabIndex = 1;
            label2.Text = "Chọn file/folder cần mã hóa:";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(21, 65);
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "Link xuất hiện ở đây";
            textBox1.ReadOnly = true;
            textBox1.Size = new Size(237, 27);
            textBox1.TabIndex = 2;
            // 
            // button2
            // 
            button2.Location = new Point(216, 20);
            button2.Name = "button2";
            button2.Size = new Size(94, 29);
            button2.TabIndex = 4;
            button2.Text = "Chọn";
            button2.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(41, 119);
            label1.Name = "label1";
            label1.Size = new Size(0, 20);
            label1.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(21, 132);
            label3.Name = "label3";
            label3.Size = new Size(77, 20);
            label3.TabIndex = 6;
            label3.Text = "Password: ";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(104, 125);
            textBox2.Name = "textBox2";
            textBox2.PlaceholderText = "Nhập password";
            textBox2.Size = new Size(190, 27);
            textBox2.TabIndex = 7;
            textBox2.UseSystemPasswordChar = true;
            // 
            // button1
            // 
            button1.Location = new Point(126, 241);
            button1.Name = "button1";
            button1.Size = new Size(184, 29);
            button1.TabIndex = 8;
            button1.Text = "Mã hóa và Xuất";
            button1.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Location = new Point(126, 276);
            button3.Name = "button3";
            button3.Size = new Size(184, 29);
            button3.TabIndex = 9;
            button3.Text = "Giải mã và xuất";
            button3.UseVisualStyleBackColor = true;
            // 
            // FolderLocker
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(482, 346);
            Controls.Add(button3);
            Controls.Add(button1);
            Controls.Add(textBox2);
            Controls.Add(label3);
            Controls.Add(label1);
            Controls.Add(button2);
            Controls.Add(textBox1);
            Controls.Add(label2);
            Name = "FolderLocker";
            Text = "FolderLocker";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label2;
        private TextBox textBox1;
        private Button button2;
        private Label label1;
        private Label label3;
        private TextBox textBox2;
        private Button button1;
        private Button button3;
    }
}
