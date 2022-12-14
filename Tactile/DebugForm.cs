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
    public partial class DebugForm : PerPixelAlphaForm
    {
        public DebugForm()
        {
            InitializeComponent();
            this.DrawStuff(255);
        }

        private void DebugForm_Load(object sender, EventArgs e)
        {

        }
    }
}
