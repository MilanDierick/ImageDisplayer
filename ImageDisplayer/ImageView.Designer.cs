namespace ImageDisplayer
{
    partial class ImageView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textboxIP = new System.Windows.Forms.TextBox();
            this.textboxPORT = new System.Windows.Forms.TextBox();
            this.buttonSEND = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textboxIP
            // 
            this.textboxIP.Location = new System.Drawing.Point(601, 324);
            this.textboxIP.Name = "textboxIP";
            this.textboxIP.Size = new System.Drawing.Size(149, 20);
            this.textboxIP.TabIndex = 0;
            // 
            // textboxPORT
            // 
            this.textboxPORT.Location = new System.Drawing.Point(601, 350);
            this.textboxPORT.Name = "textboxPORT";
            this.textboxPORT.Size = new System.Drawing.Size(149, 20);
            this.textboxPORT.TabIndex = 1;
            // 
            // buttonSEND
            // 
            this.buttonSEND.Location = new System.Drawing.Point(601, 376);
            this.buttonSEND.Name = "buttonSEND";
            this.buttonSEND.Size = new System.Drawing.Size(149, 23);
            this.buttonSEND.TabIndex = 2;
            this.buttonSEND.Text = "button1";
            this.buttonSEND.UseVisualStyleBackColor = true;
            // 
            // ImageView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonSEND);
            this.Controls.Add(this.textboxPORT);
            this.Controls.Add(this.textboxIP);
            this.Name = "ImageView";
            this.Text = "ImageView";
            this.Load += new System.EventHandler(this.ImageView_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textboxIP;
        private System.Windows.Forms.TextBox textboxPORT;
        private System.Windows.Forms.Button buttonSEND;
    }
}