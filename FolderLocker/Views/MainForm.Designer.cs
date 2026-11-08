namespace FolderLocker.Views
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;
        private Guna.UI2.WinForms.Guna2BorderlessForm borderlessForm = null!;
        private Guna.UI2.WinForms.Guna2DragControl dragTitle = null!;
        private System.Windows.Forms.Label lblTitle = null!;
        private Guna.UI2.WinForms.Guna2Button btnSettings = null!;
        private Guna.UI2.WinForms.Guna2ControlBox btnMinimize = null!;
        private Guna.UI2.WinForms.Guna2ControlBox btnClose = null!;
        private System.Windows.Forms.ContextMenuStrip menuSettings = null!;
        private System.Windows.Forms.ToolStripMenuItem mnuHelpRecovery = null!;
        private System.Windows.Forms.ToolStripMenuItem mnuHelpShortcuts = null!;
        private System.Windows.Forms.FlowLayoutPanel flowMain = null!;
        private FolderLocker.Views.Controls.DropZonePanel dropZone = null!;
        private System.Windows.Forms.Label lblIcon = null!;
        private System.Windows.Forms.Label lblDragDrop = null!;
        private System.Windows.Forms.Label lblSelectedPath = null!;
        private System.Windows.Forms.Label lblPassword = null!;
        private Guna.UI2.WinForms.Guna2TextBox txtPassword = null!;
        private Guna.UI2.WinForms.Guna2ProgressBar strengthBar = null!;
        private System.Windows.Forms.TableLayoutPanel tblHints = null!;
        private System.Windows.Forms.Label lblCapsLock = null!;
        private System.Windows.Forms.Label lblStrength = null!;
        private System.Windows.Forms.Label lblConfirm = null!;
        private Guna.UI2.WinForms.Guna2TextBox txtConfirmPassword = null!;
        private System.Windows.Forms.Label lblMismatch = null!;
        private Guna.UI2.WinForms.Guna2CheckBox chkDeleteOriginal = null!;
        private Guna.UI2.WinForms.Guna2GradientButton btnAction = null!;
        private Guna.UI2.WinForms.Guna2ProgressBar progressBar = null!;
        private System.Windows.Forms.ToolTip toolTip = null!;

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
            this.dragTitle = new Guna.UI2.WinForms.Guna2DragControl(this.components);
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnSettings = new Guna.UI2.WinForms.Guna2Button();
            this.btnMinimize = new Guna.UI2.WinForms.Guna2ControlBox();
            this.btnClose = new Guna.UI2.WinForms.Guna2ControlBox();
            this.menuSettings = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuHelpRecovery = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuHelpShortcuts = new System.Windows.Forms.ToolStripMenuItem();
            this.flowMain = new System.Windows.Forms.FlowLayoutPanel();
            this.dropZone = new FolderLocker.Views.Controls.DropZonePanel();
            this.lblIcon = new System.Windows.Forms.Label();
            this.lblDragDrop = new System.Windows.Forms.Label();
            this.lblSelectedPath = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new Guna.UI2.WinForms.Guna2TextBox();
            this.strengthBar = new Guna.UI2.WinForms.Guna2ProgressBar();
            this.tblHints = new System.Windows.Forms.TableLayoutPanel();
            this.lblCapsLock = new System.Windows.Forms.Label();
            this.lblStrength = new System.Windows.Forms.Label();
            this.lblConfirm = new System.Windows.Forms.Label();
            this.txtConfirmPassword = new Guna.UI2.WinForms.Guna2TextBox();
            this.lblMismatch = new System.Windows.Forms.Label();
            this.chkDeleteOriginal = new Guna.UI2.WinForms.Guna2CheckBox();
            this.btnAction = new Guna.UI2.WinForms.Guna2GradientButton();
            this.progressBar = new Guna.UI2.WinForms.Guna2ProgressBar();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);

            this.menuSettings.SuspendLayout();
            this.flowMain.SuspendLayout();
            this.dropZone.SuspendLayout();
            this.tblHints.SuspendLayout();
            this.SuspendLayout();

            // borderlessForm
            this.borderlessForm.ContainerControl = this;
            this.borderlessForm.DockIndicatorTransparencyValue = 0.6D;
            this.borderlessForm.TransparentWhileDrag = true;
            this.borderlessForm.BorderRadius = 15;

            // dragTitle — kéo cửa sổ bằng tiêu đề
            this.dragTitle.TargetControl = this.lblTitle;
            this.dragTitle.UseTransparentDrag = true;
            this.dragTitle.DockIndicatorTransparencyValue = 0.6D;

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(22, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Folder Locker";
            this.lblTitle.AccessibleRole = System.Windows.Forms.AccessibleRole.TitleBar;

            // btnSettings
            this.btnSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSettings.BorderRadius = 6;
            this.btnSettings.Location = new System.Drawing.Point(308, 6);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(45, 30);
            this.btnSettings.TabIndex = 20;
            this.btnSettings.AccessibleName = "Cài đặt và trợ giúp";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

            // btnMinimize
            this.btnMinimize.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnMinimize.ControlBoxType = Guna.UI2.WinForms.Enums.ControlBoxType.MinimizeBox;
            this.btnMinimize.Location = new System.Drawing.Point(355, 6);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.Size = new System.Drawing.Size(45, 30);
            this.btnMinimize.TabIndex = 21;
            this.btnMinimize.AccessibleName = "Thu nhỏ";

            // btnClose
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.Location = new System.Drawing.Point(400, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(45, 30);
            this.btnClose.TabIndex = 22;
            this.btnClose.AccessibleName = "Đóng";

            // menuSettings
            this.menuSettings.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.mnuHelpRecovery, this.mnuHelpShortcuts });
            this.menuSettings.Name = "menuSettings";
            this.mnuHelpRecovery.Text = "Mã khôi phục là gì?";
            this.mnuHelpRecovery.Click += new System.EventHandler(this.mnuHelpRecovery_Click);
            this.mnuHelpShortcuts.Text = "Phím tắt";
            this.mnuHelpShortcuts.Click += new System.EventHandler(this.mnuHelpShortcuts_Click);

            // flowMain — xếp dọc, dòng ẩn tự co lại
            this.flowMain.AutoSize = true;
            this.flowMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowMain.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMain.WrapContents = false;
            this.flowMain.Location = new System.Drawing.Point(24, 52);
            this.flowMain.Margin = new System.Windows.Forms.Padding(0);
            this.flowMain.Padding = new System.Windows.Forms.Padding(0);
            this.flowMain.Name = "flowMain";
            this.flowMain.BackColor = System.Drawing.Color.Transparent;
            this.flowMain.Controls.Add(this.dropZone);
            this.flowMain.Controls.Add(this.lblPassword);
            this.flowMain.Controls.Add(this.txtPassword);
            this.flowMain.Controls.Add(this.strengthBar);
            this.flowMain.Controls.Add(this.tblHints);
            this.flowMain.Controls.Add(this.lblConfirm);
            this.flowMain.Controls.Add(this.txtConfirmPassword);
            this.flowMain.Controls.Add(this.lblMismatch);
            this.flowMain.Controls.Add(this.chkDeleteOriginal);
            this.flowMain.Controls.Add(this.btnAction);
            this.flowMain.Controls.Add(this.progressBar);

            // dropZone
            this.dropZone.BorderRadius = 10;
            this.dropZone.BorderThickness = 2;
            // Control Dock=Fill phải có index thấp nhất (dock sau cùng)
            this.dropZone.Controls.Add(this.lblDragDrop);
            this.dropZone.Controls.Add(this.lblSelectedPath);
            this.dropZone.Controls.Add(this.lblIcon);
            this.dropZone.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);
            this.dropZone.Name = "dropZone";
            this.dropZone.Size = new System.Drawing.Size(402, 150);
            this.dropZone.TabIndex = 0;
            this.dropZone.AccessibleName = "Chọn file hoặc thư mục";
            this.dropZone.AccessibleDescription = "Nhấn Enter để chọn, hoặc kéo thả file hay thư mục vào đây.";
            this.dropZone.DragDrop += new System.Windows.Forms.DragEventHandler(this.dropZone_DragDrop);
            this.dropZone.DragEnter += new System.Windows.Forms.DragEventHandler(this.dropZone_DragEnter);
            this.dropZone.DragLeave += new System.EventHandler(this.dropZone_DragLeave);
            this.dropZone.Activated += new System.EventHandler(this.dropZone_Activated);

            // lblIcon
            this.lblIcon.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblIcon.Name = "lblIcon";
            this.lblIcon.Size = new System.Drawing.Size(402, 76);
            this.lblIcon.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.lblIcon.BackColor = System.Drawing.Color.Transparent;

            // lblDragDrop
            this.lblDragDrop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDragDrop.Name = "lblDragDrop";
            this.lblDragDrop.Text = "Kéo thả file hoặc thư mục vào đây\nhoặc nhấn để chọn";
            this.lblDragDrop.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.lblDragDrop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 0);
            this.lblDragDrop.BackColor = System.Drawing.Color.Transparent;

            // lblSelectedPath
            this.lblSelectedPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSelectedPath.Name = "lblSelectedPath";
            this.lblSelectedPath.Size = new System.Drawing.Size(402, 30);
            this.lblSelectedPath.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.lblSelectedPath.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblSelectedPath.AutoEllipsis = true;
            this.lblSelectedPath.Visible = false;
            this.lblSelectedPath.BackColor = System.Drawing.Color.Transparent;

            // lblPassword
            this.lblPassword.AutoSize = true;
            this.lblPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Text = "Mật khẩu";

            // txtPassword
            this.txtPassword.BorderRadius = 8;
            this.txtPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtPassword.DefaultText = "";
            this.txtPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(402, 40);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.IconRightCursor = System.Windows.Forms.Cursors.Hand;
            this.txtPassword.AccessibleName = "Mật khẩu";
            this.txtPassword.IconRightClick += new System.EventHandler(this.txtPassword_IconRightClick);
            this.txtPassword.TextChanged += new System.EventHandler(this.PasswordFields_TextChanged);
            this.txtPassword.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PasswordFields_KeyUp);
            this.txtPassword.Enter += new System.EventHandler(this.PasswordFields_Enter);

            // strengthBar
            this.strengthBar.BorderRadius = 2;
            this.strengthBar.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.strengthBar.Name = "strengthBar";
            this.strengthBar.Size = new System.Drawing.Size(402, 4);
            this.strengthBar.AccessibleName = "Độ mạnh mật khẩu";

            // tblHints — trái: Caps Lock, phải: độ mạnh
            this.tblHints.ColumnCount = 2;
            this.tblHints.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblHints.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblHints.RowCount = 1;
            this.tblHints.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblHints.Controls.Add(this.lblCapsLock, 0, 0);
            this.tblHints.Controls.Add(this.lblStrength, 1, 0);
            this.tblHints.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.tblHints.Name = "tblHints";
            this.tblHints.Size = new System.Drawing.Size(402, 20);
            this.tblHints.BackColor = System.Drawing.Color.Transparent;

            this.lblCapsLock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCapsLock.Margin = new System.Windows.Forms.Padding(0);
            this.lblCapsLock.Name = "lblCapsLock";
            this.lblCapsLock.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.lblStrength.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStrength.Margin = new System.Windows.Forms.Padding(0);
            this.lblStrength.Name = "lblStrength";
            this.lblStrength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // lblConfirm
            this.lblConfirm.AutoSize = true;
            this.lblConfirm.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblConfirm.Name = "lblConfirm";
            this.lblConfirm.Text = "Xác nhận mật khẩu";

            // txtConfirmPassword
            this.txtConfirmPassword.BorderRadius = 8;
            this.txtConfirmPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtConfirmPassword.DefaultText = "";
            this.txtConfirmPassword.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PlaceholderText = "Nhập lại mật khẩu";
            this.txtConfirmPassword.Size = new System.Drawing.Size(402, 40);
            this.txtConfirmPassword.TabIndex = 2;
            this.txtConfirmPassword.IconRightCursor = System.Windows.Forms.Cursors.Hand;
            this.txtConfirmPassword.AccessibleName = "Xác nhận mật khẩu";
            this.txtConfirmPassword.IconRightClick += new System.EventHandler(this.txtConfirmPassword_IconRightClick);
            this.txtConfirmPassword.TextChanged += new System.EventHandler(this.PasswordFields_TextChanged);
            this.txtConfirmPassword.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PasswordFields_KeyUp);
            this.txtConfirmPassword.Enter += new System.EventHandler(this.PasswordFields_Enter);

            // lblMismatch — luôn chiếm chỗ để layout không nhảy khi gõ
            this.lblMismatch.AutoSize = false;
            this.lblMismatch.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblMismatch.Name = "lblMismatch";
            this.lblMismatch.Size = new System.Drawing.Size(402, 18);
            this.lblMismatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // chkDeleteOriginal
            this.chkDeleteOriginal.AutoSize = true;
            this.chkDeleteOriginal.Checked = false;
            this.chkDeleteOriginal.CheckedState.BorderRadius = 2;
            this.chkDeleteOriginal.CheckedState.BorderThickness = 0;
            this.chkDeleteOriginal.UncheckedState.BorderRadius = 2;
            this.chkDeleteOriginal.UncheckedState.BorderThickness = 1;
            this.chkDeleteOriginal.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);
            this.chkDeleteOriginal.Name = "chkDeleteOriginal";
            this.chkDeleteOriginal.TabIndex = 3;
            this.chkDeleteOriginal.Text = "Xóa bản gốc sau khi mã hóa (chuyển vào Thùng rác)";

            // btnAction
            this.btnAction.BorderRadius = 8;
            this.btnAction.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnAction.Name = "btnAction";
            this.btnAction.Size = new System.Drawing.Size(402, 46);
            this.btnAction.TabIndex = 4;
            this.btnAction.Text = "Mã hóa";
            this.btnAction.Click += new System.EventHandler(this.btnAction_Click);

            // progressBar
            this.progressBar.BorderRadius = 3;
            this.progressBar.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(402, 6);
            this.progressBar.Visible = false;
            this.progressBar.AccessibleName = "Tiến trình";

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 560);
            this.Controls.Add(this.flowMain);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnMinimize);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Folder Locker";

            this.menuSettings.ResumeLayout(false);
            this.tblHints.ResumeLayout(false);
            this.dropZone.ResumeLayout(false);
            this.flowMain.ResumeLayout(false);
            this.flowMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
