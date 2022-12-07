using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace Tactile
{
    public partial class MainForm : Form
    {

        internal class Area
        {
            public Point From { get; set; }
            public Point To { get; set; }
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hWnd, uint nCmdShow);

        KeyboardHook keyboardHook = new KeyboardHook();
        int keyCounter = 0;

        int rasterX = 4;
        int rasterY = 2;

        List<Area> Areas = new List<Area>();

        char[] keyMap = new char[] { 'q','w','e','r','a','s','d','f' };

        char[] pressedKeys = new char[2];
        IntPtr foreignHandle;

        public MainForm()
        {

            //SetStyle(ControlStyles.UserPaint, true);
            //SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            

            keyboardHook.RegisterHotKey(global::ModifierKeys.Win, Keys.Y); // TODO: Konfigurierbar machen.
            keyboardHook.KeyPressed += keyboardHook_KeyPressed;
            InitializeComponent();
            this.BackColor = Color.FromArgb(255, 255, 0, 255);
            //this.BackColor = Color.FromArgb(32, 31, 144, 255);
        }

        private void keyboardHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            this.foreignHandle = GetForegroundWindow();

            
            //this.BackgroundImage = CaptureScreen(Screen.FromHandle(Handle));
            PushMeToFront();
            //MoveWindow(handle, 600, 600, 600, 600, true);
        }

        void PushMeToFront()
        {
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
            this.WindowState = FormWindowState.Maximized;
            
            this.Show();
            this.TopMost = true;
            this.BringToFront();
            this.Activate();
            this.Focus();
            
        }
        void HideMe()
        {
            this.WindowState = FormWindowState.Minimized;
            this.TopMost = false;
            this.Hide();
            pressedKeys = new char[2];
        }

        private Bitmap CaptureScreen(Screen screen)
        {
            try
            {
                Rectangle captureRectangle = screen.Bounds;
                Bitmap captureBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);
                Graphics captureGraphics = Graphics.FromImage(captureBitmap);
                captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
                return captureBitmap;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (keyMap.Contains(e.KeyChar)) { // is valid key?

                pressedKeys[keyCounter] = e.KeyChar;

                if (++keyCounter == 2)  // are two keys pressed?
                {
                    
                    keyCounter = 0;

                    int p1 = Array.IndexOf(keyMap, pressedKeys[0]);
                    int p2 = Array.IndexOf(keyMap, pressedKeys[1]);


                    Area a1 = Areas[p1];
                    Area a2 = Areas[p2];

                    int x = a1.From.X;
                    int y = a1.From.Y;
                    int w = a2.To.X - a1.From.X;
                    int h = a2.To.Y - a1.From.Y;

                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_NORMAL);
                    MoveWindow(this.foreignHandle, x, y, w, h, true);
                    HideMe();

                }
                else
                {
                    //label2.Text += e.KeyChar.ToString();
                    this.Invalidate();
                }
            }
            else
            {
                HideMe();
                
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Areas.Clear();
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

            Color color1 = Color.FromArgb(255, 30, 144, 255);
            Color color2 = Color.FromArgb(255, 255, 144, 30);


            int penwidth = 2;
            Pen pen = new Pen(color1, penwidth);

            int fullWidth = this.ClientRectangle.Width;
            int fullHeight = this.ClientRectangle.Height;

            //e.Graphics.FillRectangle(new SolidBrush(colorBack), this.ClientRectangle);

            int factorX = fullWidth / rasterX;
            int factorY = fullHeight / rasterY;


            int i = 0;
            // text
            for (int y = 0; y < fullHeight; y += factorY) { 
                for (int x = 0; x < fullWidth; x += factorX)
                {
                
                    string strX = x.ToString() + "/" + y.ToString();
                    string strY = (x + factorX).ToString() + "/" + (y + factorY).ToString();

                    int posX = x + 16;
                    int posY = y + 16;
                    Area area = new Area();

                    area.From = new Point(x, y);
                    area.To = new Point(x + factorX, y + factorY);

                    Areas.Add(area);

                    //e.Graphics.DrawString($"{i++.ToString()}: {strX}-{strY}", new Font("Tahoma", 18.0f), new SolidBrush(color), posX, posY);
                    char c = keyMap[i];
                    bool isPressed = pressedKeys.Contains(c);
                    e.Graphics.DrawString($"{keyMap[i].ToString().ToUpper()}", new Font("Tahoma", 30.0f), new SolidBrush(isPressed ? color2 : color1), posX, posY);

                    i++;
                }
            }

            // lines
            for (int x = factorX; x < fullWidth; x+= factorX)
            {
                e.Graphics.DrawLine(pen, x, 0, x, fullHeight);
            }
            for (int y = factorY; y < fullHeight; y += factorY)
            {
                e.Graphics.DrawLine(pen, 0, y, fullWidth, y);
            }


            //
        }


        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.HideMe();
        }
    }
}
