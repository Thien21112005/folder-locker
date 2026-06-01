namespace FolderLocker.Views
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private Guna.UI2.WinForms.Guna2BorderlessForm borderlessForm;
        private Guna.UI2.WinForms.Guna2Panel pnlDragDrop;
        private System.Windows.Forms.Label lblDragDrop;
        private Guna.UI2.WinForms.Guna2TextBox txtPassword;
        private Guna.UI2.WinForms.Guna2TextBox txtConfirmPassword;
        private Guna.UI2.WinForms.Guna2CheckBox chkDeleteOriginal;
        private Guna.UI2.WinForms.Guna2Button btnAction;
        private Guna.UI2.WinForms.Guna2ProgressBar progressBar;
        private System.Windows.Forms.Label lblTitle;
        private Guna.UI2.WinForms.Guna2ControlBox btnClose;
        private Guna.UI2.WinForms.Guna2ControlBox btnMinimize;
        private System.Windows.Forms.Label lblSelectedPath;
        private System.Windows.Forms.Label lblIcon;
        private System.Windows.Forms.Label lblForgotPassword;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.borderlessForm = new Guna.UI2.WinForms.Guna2BorderlessForm(this.components);
            this.pnlDragDrop = new Guna.UI2.WinForms.Guna2Panel();
            this.lblDragDrop = new System.Windows.Forms.Label();
            this.lblSelectedPath = new System.Windows.Forms.Label();
            this.lblIcon = new System.Windows.Forms.Label();
            this.txtPassword = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtConfirmPassword = new Guna.UI2.WinForms.Guna2TextBox();
            this.chkDeleteOriginal = new Guna.UI2.WinForms.Guna2CheckBox();
            this.btnAction = new Guna.UI2.WinForms.Guna2Button();
            this.progressBar = new Guna.UI2.WinForms.Guna2ProgressBar();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnClose = new Guna.UI2.WinForms.Guna2ControlBox();
            this.btnMinimize = new Guna.UI2.WinForms.Guna2ControlBox();
            this.lblForgotPassword = new System.Windows.Forms.Label();
            
            this.pnlDragDrop.SuspendLayout();
            this.SuspendLayout();
            
            // borderlessForm
            this.borderlessForm.ContainerControl = this;
            this.borderlessForm.DockIndicatorTransparencyValue = 0.6D;
            this.borderlessForm.TransparentWhileDrag = true;
            this.borderlessForm.BorderRadius = 15;
            
            // btnClose
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.FillColor = System.Drawing.Color.Transparent;
            this.btnClose.IconColor = System.Drawing.Color.Gray;
            this.btnClose.Location = new System.Drawing.Point(400, 5);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(45, 29);
            this.btnClose.TabIndex = 0;
            
            // btnMinimize
            this.btnMinimize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMinimize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            this.btnMinimize.FillColor = System.Drawing.Color.Transparent;
            this.btnMinimize.IconColor = System.Drawing.Color.Gray;
            this.btnMinimize.Location = new System.Drawing.Point(355, 5);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.Size = new System.Drawing.Size(45, 29);
            this.btnMinimize.TabIndex = 1;

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(120, 25);
            this.lblTitle.Text = "Folder Locker";

            // pnlDragDrop
            this.pnlDragDrop.AllowDrop = true;
            this.pnlDragDrop.BackColor = System.Drawing.Color.Transparent;
            this.pnlDragDrop.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.pnlDragDrop.BorderRadius = 10;
            this.pnlDragDrop.BorderThickness = 2;
            this.pnlDragDrop.CustomBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pnlDragDrop.Controls.Add(this.lblIcon);
            this.pnlDragDrop.Controls.Add(this.lblSelectedPath);
            this.pnlDragDrop.Controls.Add(this.lblDragDrop);
            this.pnlDragDrop.Location = new System.Drawing.Point(25, 70);
            this.pnlDragDrop.Name = "pnlDragDrop";
            this.pnlDragDrop.Size = new System.Drawing.Size(400, 150);
            this.pnlDragDrop.TabIndex = 2;
            this.pnlDragDrop.DragDrop += new System.Windows.Forms.DragEventHandler(this.pnlDragDrop_DragDrop);
            this.pnlDragDrop.DragEnter += new System.Windows.Forms.DragEventHandler(this.pnlDragDrop_DragEnter);
            this.pnlDragDrop.Click += new System.EventHandler(this.pnlDragDrop_Click);

            // lblDragDrop
            this.lblDragDrop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDragDrop.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblDragDrop.ForeColor = System.Drawing.Color.Gray;
            this.lblDragDrop.Location = new System.Drawing.Point(0, 0);
            this.lblDragDrop.Name = "lblDragDrop";
            this.lblDragDrop.Size = new System.Drawing.Size(400, 150);
            this.lblDragDrop.Text = "Kéo & Thả File/Folder vào đây\nHoặc click để chọn";
            this.lblDragDrop.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblDragDrop.Click += new System.EventHandler(this.pnlDragDrop_Click);

            // lblSelectedPath
            this.lblSelectedPath.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSelectedPath.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblSelectedPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblSelectedPath.Location = new System.Drawing.Point(0, 120);
            this.lblSelectedPath.Name = "lblSelectedPath";
            this.lblSelectedPath.Size = new System.Drawing.Size(400, 30);
            this.lblSelectedPath.Text = "";
            this.lblSelectedPath.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSelectedPath.Visible = false;

            // lblIcon
            this.lblIcon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIcon.Font = new System.Drawing.Font("Segoe MDL2 Assets", 48F, System.Drawing.FontStyle.Regular);
            this.lblIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.lblIcon.Location = new System.Drawing.Point(0, 0);
            this.lblIcon.Name = "lblIcon";
            this.lblIcon.Size = new System.Drawing.Size(400, 120);
            this.lblIcon.Text = "";
            this.lblIcon.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblIcon.Visible = false;
            this.lblIcon.Click += new System.EventHandler(this.pnlDragDrop_Click);

            // txtPassword
            this.txtPassword.BorderRadius = 8;
            this.txtPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtPassword.DefaultText = "";
            this.txtPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtPassword.ForeColor = System.Drawing.Color.Black;
            this.txtPassword.Location = new System.Drawing.Point(25, 240);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '●';
            this.txtPassword.PlaceholderText = "Mật khẩu";
            this.txtPassword.Size = new System.Drawing.Size(400, 40);
            this.txtPassword.TabIndex = 3;
            this.txtPassword.UseSystemPasswordChar = true;

            // txtConfirmPassword
            this.txtConfirmPassword.BorderRadius = 8;
            this.txtConfirmPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtConfirmPassword.DefaultText = "";
            this.txtConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtConfirmPassword.ForeColor = System.Drawing.Color.Black;
            this.txtConfirmPassword.Location = new System.Drawing.Point(25, 290);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PasswordChar = '●';
            this.txtConfirmPassword.PlaceholderText = "Xác nhận mật khẩu";
            this.txtConfirmPassword.Size = new System.Drawing.Size(400, 40);
            this.txtConfirmPassword.TabIndex = 4;
            this.txtConfirmPassword.UseSystemPasswordChar = true;

            // chkDeleteOriginal
            this.chkDeleteOriginal.AutoSize = true;
            this.chkDeleteOriginal.Checked = true;
            this.chkDeleteOriginal.CheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.chkDeleteOriginal.CheckedState.BorderRadius = 2;
            this.chkDeleteOriginal.CheckedState.BorderThickness = 0;
            this.chkDeleteOriginal.CheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.chkDeleteOriginal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkDeleteOriginal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.chkDeleteOriginal.Location = new System.Drawing.Point(30, 345);
            this.chkDeleteOriginal.Name = "chkDeleteOriginal";
            this.chkDeleteOriginal.Size = new System.Drawing.Size(225, 19);
            this.chkDeleteOriginal.TabIndex = 5;
            this.chkDeleteOriginal.Text = "Tự động xóa file/folder gốc sau khi mã hóa";
            this.chkDeleteOriginal.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.chkDeleteOriginal.UncheckedState.BorderRadius = 2;
            this.chkDeleteOriginal.UncheckedState.BorderThickness = 1;
            this.chkDeleteOriginal.UncheckedState.FillColor = System.Drawing.Color.Transparent;

            // lblForgotPassword
            this.lblForgotPassword.AutoSize = true;
            this.lblForgotPassword.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Underline);
            this.lblForgotPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.lblForgotPassword.Location = new System.Drawing.Point(30, 370);
            this.lblForgotPassword.Name = "lblForgotPassword";
            this.lblForgotPassword.Size = new System.Drawing.Size(200, 15);
            this.lblForgotPassword.Text = "Quên mật khẩu? Dùng mã khôi phục";
            this.lblForgotPassword.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblForgotPassword.Visible = false;
            this.lblForgotPassword.Click += new System.EventHandler(this.lblForgotPassword_Click);

            // btnAction
            this.btnAction.BorderRadius = 8;
            this.btnAction.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAction.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAction.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAction.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAction.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.btnAction.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnAction.ForeColor = System.Drawing.Color.White;
            this.btnAction.Location = new System.Drawing.Point(25, 395);
            this.btnAction.Name = "btnAction";
            this.btnAction.Size = new System.Drawing.Size(400, 45);
            this.btnAction.TabIndex = 6;
            this.btnAction.Text = "MÃ HÓA DỮ LIỆU";
            this.btnAction.Click += new System.EventHandler(this.btnAction_Click);

            // progressBar
            this.progressBar.BorderRadius = 4;
            this.progressBar.Location = new System.Drawing.Point(25, 455);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(400, 10);
            this.progressBar.TabIndex = 7;
            this.progressBar.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            this.progressBar.Visible = false;

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(450, 490);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnAction);
            this.Controls.Add(this.lblForgotPassword);
            this.Controls.Add(this.chkDeleteOriginal);
            this.Controls.Add(this.txtConfirmPassword);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.pnlDragDrop);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnMinimize);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Folder Locker";
            this.pnlDragDrop.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
