﻿using System;
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
    public partial class DebugForm : Form
    {
        public DebugForm()
        {
            InitializeComponent();
        }

        private void DebugForm_Move(object sender, EventArgs e)
        {
            this.Text = $"{this.Left}/{this.Top}";
        }

        private void DebugForm_Load(object sender, EventArgs e)
        {

        }
    }
}
