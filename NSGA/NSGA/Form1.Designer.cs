namespace NSGAI
{
    partial class Form1
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
            buttonLoad = new Button();
            btnRunAlgorithm = new Button();
            pbCanvas = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbCanvas).BeginInit();
            SuspendLayout();
            // 
            // buttonLoad
            // 
            buttonLoad.Location = new Point(73, 83);
            buttonLoad.Name = "buttonLoad";
            buttonLoad.Size = new Size(137, 29);
            buttonLoad.TabIndex = 0;
            buttonLoad.Text = "Load File";
            buttonLoad.UseVisualStyleBackColor = true;
            // 
            // btnRunAlgorithm
            // 
            btnRunAlgorithm.Location = new Point(72, 139);
            btnRunAlgorithm.Name = "btnRunAlgorithm";
            btnRunAlgorithm.Size = new Size(138, 29);
            btnRunAlgorithm.TabIndex = 1;
            btnRunAlgorithm.Text = "Start NSGAI";
            btnRunAlgorithm.UseVisualStyleBackColor = true;
            // 
            // pbCanvas
            // 
            pbCanvas.Location = new Point(297, 67);
            pbCanvas.Name = "pbCanvas";
            pbCanvas.Size = new Size(274, 216);
            pbCanvas.TabIndex = 2;
            pbCanvas.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pbCanvas);
            Controls.Add(btnRunAlgorithm);
            Controls.Add(buttonLoad);
            Name = "Form1";
            Text = "NSGAI";
            ((System.ComponentModel.ISupportInitialize)pbCanvas).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonLoad;
        private Button btnRunAlgorithm;
        private PictureBox pbCanvas;
    }
}