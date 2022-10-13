namespace IoTClient.Tool
{
    partial class IndexForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IndexForm));
            this.BACnet = new System.Windows.Forms.TabPage();
            this.tabContorl = new System.Windows.Forms.TabControl();
            this.ModbusTcp = new System.Windows.Forms.TabPage();
            this.Profinet = new System.Windows.Forms.TabPage();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabContorl.SuspendLayout();
            this.SuspendLayout();
            // 
            // BACnet
            // 
            this.BACnet.Location = new System.Drawing.Point(4, 25);
            this.BACnet.Margin = new System.Windows.Forms.Padding(4);
            this.BACnet.Name = "BACnet";
            this.BACnet.Padding = new System.Windows.Forms.Padding(4);
            this.BACnet.Size = new System.Drawing.Size(1184, 784);
            this.BACnet.TabIndex = 2;
            this.BACnet.Text = " BACnet ";
            this.BACnet.UseVisualStyleBackColor = true;
            // 
            // tabContorl
            // 
            this.tabContorl.AccessibleName = "";
            this.tabContorl.Controls.Add(this.ModbusTcp);
            this.tabContorl.Controls.Add(this.BACnet);
            this.tabContorl.Controls.Add(this.Profinet);
            this.tabContorl.Controls.Add(this.tabPage1);
            this.tabContorl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabContorl.Location = new System.Drawing.Point(0, 0);
            this.tabContorl.Margin = new System.Windows.Forms.Padding(4);
            this.tabContorl.Multiline = true;
            this.tabContorl.Name = "tabContorl";
            this.tabContorl.SelectedIndex = 0;
            this.tabContorl.Size = new System.Drawing.Size(1232, 813);
            this.tabContorl.TabIndex = 2;
            this.tabContorl.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // ModbusTcp
            // 
            this.ModbusTcp.Location = new System.Drawing.Point(4, 25);
            this.ModbusTcp.Margin = new System.Windows.Forms.Padding(4);
            this.ModbusTcp.Name = "ModbusTcp";
            this.ModbusTcp.Padding = new System.Windows.Forms.Padding(4);
            this.ModbusTcp.Size = new System.Drawing.Size(1224, 784);
            this.ModbusTcp.TabIndex = 0;
            this.ModbusTcp.Text = "ModbusTcp";
            this.ModbusTcp.UseVisualStyleBackColor = true;
            // 
            // Profinet
            // 
            this.Profinet.Location = new System.Drawing.Point(4, 25);
            this.Profinet.Name = "Profinet";
            this.Profinet.Padding = new System.Windows.Forms.Padding(3);
            this.Profinet.Size = new System.Drawing.Size(1184, 784);
            this.Profinet.TabIndex = 3;
            this.Profinet.Text = "Profinet";
            this.Profinet.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "Fin";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1184, 784);
            this.tabPage1.TabIndex = 4;
            this.tabPage1.Text = "Fin";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // IndexForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1232, 813);
            this.Controls.Add(this.tabContorl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "IndexForm";
            this.Text = "IoTClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IndexForm_FormClosing);
            this.tabContorl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabPage BACnet;
        private System.Windows.Forms.TabControl tabContorl;
        private System.Windows.Forms.TabPage ModbusTcp;
        private System.Windows.Forms.TabPage Profinet;
        private System.Windows.Forms.TabPage tabPage1;
    }
}