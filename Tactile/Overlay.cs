using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tactile
{
    public partial class Overlay : Form
    {
        
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                const int WS_EX_TRANSPARENT = 0x20;
                const int WS_EX_LAYERED = 0x80000;
                const int WS_EX_NOACTIVATE = 0x8000000;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
                return cp;
            }
        }
        
        
        public Overlay()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            //this.DrawStuff(255);
        }

        private void MarkerForm_Paint(object sender, PaintEventArgs e)
        {
            
            using (var pen = new Pen(Color.OrangeRed, 4))
            {
                e.Graphics.DrawRectangle(pen, this.ClientRectangle);
            }
            
        }

        private void MarkerForm_Load(object sender, EventArgs e)
        {

        }

        private void Overlay_Move(object sender, EventArgs e)
        {
            this.Text = $"{this.Left}/{this.Top}";
        }
    }
}
