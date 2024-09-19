namespace GitUI.HelperDialogs
{
    partial class FormStatus
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Ok = new Button();
            ProgressBar = new ProgressBar();
            KeepDialogOpen = new CheckBox();
            ShowPassword = new CheckBox();
            Abort = new Button();
            pnlOutput = new Panel();
            PasswordPanel = new Panel();
            Password = new TextBox();
            PasswordSend = new Button();
            MainPanel.SuspendLayout();
            ControlsPanel.SuspendLayout();
            PasswordPanel.SuspendLayout();
            SuspendLayout();
            // 
            // MainPanel
            // 
            MainPanel.Controls.Add(pnlOutput);
            MainPanel.Padding = new Padding(0);
            MainPanel.Size = new Size(549, 246);
            // 
            // ControlsPanel
            // 
            ControlsPanel.Controls.Add(Abort);
            ControlsPanel.Controls.Add(Ok);
            ControlsPanel.Controls.Add(KeepDialogOpen);
            ControlsPanel.Controls.Add(ShowPassword);
            ControlsPanel.Location = new Point(0, 288);
            ControlsPanel.Size = new Size(549, 39);
            // 
            // Ok
            // 
            Ok.AutoSize = true;
            Ok.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Ok.Location = new Point(380, 8);
            Ok.MinimumSize = new Size(75, 23);
            Ok.Name = "Ok";
            Ok.Size = new Size(75, 23);
            Ok.TabIndex = 0;
            Ok.Text = "OK";
            Ok.UseCompatibleTextRendering = true;
            Ok.UseVisualStyleBackColor = true;
            Ok.Click += Ok_Click;
            // 
            // ProgressBar
            // 
            ProgressBar.Dock = DockStyle.Bottom;
            ProgressBar.Location = new Point(0, 246);
            ProgressBar.Margin = new Padding(0);
            ProgressBar.MarqueeAnimationSpeed = 1;
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(549, 3);
            ProgressBar.Step = 50;
            ProgressBar.Style = ProgressBarStyle.Marquee;
            ProgressBar.TabIndex = 0;
            // 
            // KeepDialogOpen
            // 
            KeepDialogOpen.AutoSize = true;
            KeepDialogOpen.Location = new Point(254, 8);
            KeepDialogOpen.Name = "KeepDialogOpen";
            KeepDialogOpen.Size = new Size(120, 22);
            KeepDialogOpen.TabIndex = 3;
            KeepDialogOpen.Text = "&Keep dialog open";
            KeepDialogOpen.UseCompatibleTextRendering = true;
            KeepDialogOpen.UseVisualStyleBackColor = true;
            KeepDialogOpen.CheckedChanged += KeepDialogOpen_CheckedChanged;
            // 
            // ShowPassword
            // 
            ShowPassword.AutoSize = true;
            ShowPassword.Location = new Point(108, 8);
            ShowPassword.Name = "ShowPassword";
            ShowPassword.Size = new Size(140, 22);
            ShowPassword.TabIndex = 2;
            ShowPassword.Text = "Show &password input";
            ShowPassword.UseCompatibleTextRendering = true;
            ShowPassword.UseVisualStyleBackColor = true;
            ShowPassword.CheckedChanged += ShowPassword_CheckedChanged;
            // 
            // Abort
            // 
            Abort.AutoSize = true;
            Abort.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Abort.DialogResult = DialogResult.Cancel;
            Abort.Location = new Point(461, 8);
            Abort.MinimumSize = new Size(75, 23);
            Abort.Name = "Abort";
            Abort.Size = new Size(75, 23);
            Abort.TabIndex = 1;
            Abort.Text = "&Abort";
            Abort.UseCompatibleTextRendering = true;
            Abort.UseVisualStyleBackColor = true;
            Abort.Click += Abort_Click;
            // 
            // pnlOutput
            // 
            pnlOutput.Dock = DockStyle.Fill;
            pnlOutput.Location = new Point(0, 0);
            pnlOutput.Name = "pnlOutput";
            pnlOutput.Padding = new Padding(12);
            pnlOutput.Size = new Size(549, 246);
            pnlOutput.TabIndex = 0;
            // 
            // PasswordPanel
            // 
            PasswordPanel.Controls.Add(Password);
            PasswordPanel.Controls.Add(PasswordSend);
            PasswordPanel.Dock = DockStyle.Bottom;
            PasswordPanel.Location = new Point(0, 249);
            PasswordPanel.Margin = new Padding(0);
            PasswordPanel.Name = "PasswordPanel";
            PasswordPanel.Padding = new Padding(8);
            PasswordPanel.Size = new Size(549, 39);
            PasswordPanel.TabIndex = 2;
            // 
            // Password
            // 
            Password.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            Password.Location = new Point(12, 8);
            Password.Margin = new Padding(0);
            Password.Name = "Password";
            Password.Size = new Size(443, 23);
            Password.TabIndex = 0;
            Password.UseSystemPasswordChar = true;
            // 
            // PasswordSend
            // 
            PasswordSend.Anchor = AnchorStyles.Right;
            PasswordSend.Location = new Point(461, 8);
            PasswordSend.MinimumSize = new Size(75, 23);
            PasswordSend.Name = "PasswordSend";
            PasswordSend.Size = new Size(75, 23);
            PasswordSend.TabIndex = 1;
            PasswordSend.Text = "Send input";
            PasswordSend.UseCompatibleTextRendering = true;
            PasswordSend.UseVisualStyleBackColor = true;
            PasswordSend.Click += PasswordSend_Click;
            // 
            // FormStatus
            // 
            AcceptButton = Ok;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = Abort;
            ClientSize = new Size(549, 327);
            Controls.Add(ProgressBar);
            Controls.Add(PasswordPanel);
            MinimumSize = new Size(500, 200);
            Name = "FormStatus";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Process";
            Controls.SetChildIndex(ControlsPanel, 0);
            Controls.SetChildIndex(PasswordPanel, 0);
            Controls.SetChildIndex(ProgressBar, 0);
            Controls.SetChildIndex(MainPanel, 0);
            MainPanel.ResumeLayout(false);
            ControlsPanel.ResumeLayout(false);
            ControlsPanel.PerformLayout();
            PasswordPanel.ResumeLayout(false);
            PasswordPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar ProgressBar;
        protected Panel PasswordPanel;
        protected TextBox Password;
        protected Button PasswordSend;
        protected Button Ok;
        protected CheckBox KeepDialogOpen;
        protected CheckBox ShowPassword;
        protected Button Abort;
        protected Panel pnlOutput;
    }
}
