namespace FolderLocker.Views
{
    partial class MainForm
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
            txtPath = new TextBox();
            btnChoose = new Button();
            label1 = new Label();
            label3 = new Label();
            txtPassword = new TextBox();
            btnEncrypt = new Button();
            btnDecrypt = new Button();
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
            // txtPath
            // 
            txtPath.Location = new Point(21, 65);
            txtPath.Name = "txtPath";
            txtPath.PlaceholderText = "Link xuất hiện ở đây";
            txtPath.ReadOnly = true;
            txtPath.Size = new Size(289, 27);
            txtPath.TabIndex = 2;
            // 
            // btnChoose
            // 
            btnChoose.Location = new Point(216, 20);
            btnChoose.Name = "btnChoose";
            btnChoose.Size = new Size(94, 29);
            btnChoose.TabIndex = 4;
            btnChoose.Text = "Chọn";
            btnChoose.UseVisualStyleBackColor = true;
            btnChoose.Click += btnChoose_Click;
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
            // txtPassword
            // 
            txtPassword.Location = new Point(104, 129);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.PlaceholderText = "Nhập password";
            txtPassword.Size = new Size(190, 27);
            txtPassword.TabIndex = 7;
            // 
            // btnEncrypt
            // 
            btnEncrypt.Location = new Point(126, 227);
            btnEncrypt.Name = "btnEncrypt";
            btnEncrypt.Size = new Size(184, 29);
            btnEncrypt.TabIndex = 8;
            btnEncrypt.Text = "Mã hóa và Xuất";
            btnEncrypt.UseVisualStyleBackColor = true;
            btnEncrypt.Click += btnEncrypt_Click;
            // 
            // btnDecrypt
            // 
            btnDecrypt.Location = new Point(126, 274);
            btnDecrypt.Name = "btnDecrypt";
            btnDecrypt.Size = new Size(184, 29);
            btnDecrypt.TabIndex = 9;
            btnDecrypt.Text = "Giải mã và xuất";
            btnDecrypt.UseVisualStyleBackColor = true;
            btnDecrypt.Visible = false;
            btnDecrypt.Click += btnDecrypt_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(431, 336);
            Controls.Add(btnDecrypt);
            Controls.Add(btnEncrypt);
            Controls.Add(txtPassword);
            Controls.Add(label3);
            Controls.Add(label1);
            Controls.Add(btnChoose);
            Controls.Add(txtPath);
            Controls.Add(label2);
            Name = "MainForm";
            Text = "FolderLocker";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label2;
        private TextBox txtPath;
        private Button btnChoose;
        private Label label1;
        private Label label3;
        private TextBox txtPassword;
        private Button btnEncrypt;
        private Button btnDecrypt;
    }
}
