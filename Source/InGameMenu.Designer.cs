﻿using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;

namespace eft_dma_radar
{
    partial class InGameMenu
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
            labelMenu = new Label();
            SuspendLayout();
            // 
            // labelMenu
            // 
            labelMenu.AutoSize = true;
            labelMenu.Font = new System.Drawing.Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            labelMenu.ForeColor = Color.White;
            labelMenu.Location = new Point(8, 9);
            labelMenu.Name = "labelMenu";
            labelMenu.Size = new Size(183, 30);
            labelMenu.TabIndex = 0;
            labelMenu.Text = "Tarkov ESP Radar";
            // 
            // MenuForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(34, 34, 34);
            ClientSize = new Size(800, 450);
            Controls.Add(labelMenu);
            FormBorderStyle = FormBorderStyle.None;
            Name = "InGameMenu";
            Text = "InGameMenu";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelMenu;
    }
}