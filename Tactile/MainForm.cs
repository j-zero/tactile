﻿using System;
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
using System.Drawing.Drawing2D;

namespace Tactile
{
    public partial class MainForm : Form
    {

        internal class Area
        {
            public Point From { get; set; }
            public Point To { get; set; }
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPlacement(IntPtr hWnd,
           [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpString, int nMaxCount);

        public IntPtr GetMainWindow(IntPtr handle)
        {
            IntPtr windowParent = IntPtr.Zero;

            while (handle != IntPtr.Zero)
            {
                windowParent = handle;
                handle = GetParent(handle);
            }

            return windowParent;
        }






        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        KeyboardHook keyboardHook = new KeyboardHook();
        int keyCounter = 0;

        int defaultRasterX = 4;
        int defaultRasterY = 2;

        int rasterX = 4;
        int rasterY = 2;

        List<Area> Areas = new List<Area>();

        //char[] keyMapChar = new char[] { 'q','w','e','r','a','s','d','f' };
        Keys[,] keyMap;

        Keys[] pressedKeys = new Keys[2];
        IntPtr foreignHandle;

        bool ignoreNextKeyUp = false;

        int currentBackupIndex = 0;
        Dictionary<IntPtr, WINDOWPLACEMENT> positionBackups = new Dictionary<IntPtr, WINDOWPLACEMENT>();

        IntPtr lastMinimizedHandle = IntPtr.Zero;
        private int areaWidth;
        private int areaHeight;
        Screen currentScreen;
        public bool ShiftIsHold { get; private set; }
        int screenIndex = 0;
        MarkerForm markerForm;
        public MainForm()
        {
            markerForm = new MarkerForm();
            
            //SetStyle(ControlStyles.UserPaint, true);
            //SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            

            keyboardHook.RegisterHotKey(global::ModifierKeys.Win, Keys.Y); // TODO: Konfigurierbar machen.
            keyboardHook.KeyPressed += keyboardHook_KeyPressed;
            InitializeComponent();
            this.BackColor = Color.FromArgb(255, 255, 0, 255);
            //this.BackColor = Color.FromArgb(32, 31, 144, 255);
            currentScreen = Screen.PrimaryScreen;

        }

        private void keyboardHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            this.foreignHandle = GetForegroundWindow();
            currentScreen = Screen.FromHandle(this.foreignHandle);
            PushMeToFront();
        }

        void SetRasterForScreen()
        {
            Screen scr = currentScreen;

            if (scr.WorkingArea.Width >= scr.WorkingArea.Height) // horizontal/quadratisch
            {
                rasterX = defaultRasterX;
                rasterY = defaultRasterY;
            }
            else if (scr.WorkingArea.Width < scr.WorkingArea.Height) // vertikal
            {
                rasterX = defaultRasterY <= 6 ? defaultRasterY: 6;
                rasterY = defaultRasterX <= 4 ? defaultRasterX : 4;
            }

            if (rasterY > 3)
            {
                this.keyMap = new Keys[4, 7] {
                        {Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7},
                        {Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Z, Keys.U},
                        {Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J},
                        {Keys.Y, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M }
                   };
            }
            else
            {
                this.keyMap = new Keys[3, 7] {
                        //{Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7},
                        {Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Z, Keys.U},
                        {Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J},
                        {Keys.Y, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M }
                   };
            }
        }

        void PushMeToFront()
        {
            SetRasterForScreen();
            this.MaximizedBounds = currentScreen.WorkingArea;
            this.WindowState = FormWindowState.Maximized;

            Rect rect = new Rect();
            GetWindowRect(this.foreignHandle, ref rect);
            markerForm.Left = rect.Left;
            markerForm.Top = rect.Top;
            markerForm.Width = rect.Right - rect.Left;
            markerForm.Height = rect.Bottom - rect.Top;

            markerForm.Show();

            this.Invalidate();
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
            markerForm.Hide();
            this.Hide();
            pressedKeys = new Keys[2];
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
            
            if (e.KeyCode == Keys.Escape)
            {
                HideMe();
            }
            if (e.KeyCode == Keys.Right)
            {
                screenIndex++;
                if (screenIndex >= Screen.AllScreens.Length)
                    screenIndex = 0;

                currentScreen = Screen.AllScreens[screenIndex];

                this.WindowState = FormWindowState.Normal;
                PushMeToFront();
            }
            else if(e.KeyCode == Keys.Enter)
            {
                SetForegroundWindow(this.foreignHandle);
                HideMe();
            }
            else if (e.KeyCode == Keys.Space)
            {
                
                //ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_RESTORE);
                //HideMe();


                var placement = GetPlacement(this.foreignHandle);
                if (placement.showCmd == ShowWindowCommands.SW_NORMAL)
                {
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MAXIMIZE);
                    
                }
                else
                {
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_RESTORE);
                }
                
                HideMe();
                SetForegroundWindow(this.foreignHandle);
                /*
                if (this.lastMinimizedHandle != IntPtr.Zero)
                {
                    ShowWindow(this.lastMinimizedHandle, (uint)ShowWindowCommands.SW_RESTORE);
                    this.lastMinimizedHandle = IntPtr.Zero;
                }
                else
                {
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MINIMIZE);
                    this.lastMinimizedHandle = this.foreignHandle;
                    HideMe();
                }
                */
            }
            else if (e.KeyCode == Keys.Up)
            {
                ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MAXIMIZE);
            }

            else if (e.KeyCode == Keys.Down)
            {
                var placement = GetPlacement(this.foreignHandle);
                if (placement.showCmd == ShowWindowCommands.SW_MAXIMIZE)
                {
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_RESTORE);
                }
                else
                {
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MINIMIZE);
                    this.lastMinimizedHandle = this.foreignHandle;
                    HideMe();
                }

            }
            else if (e.KeyCode == Keys.PageUp)
            {
                if (this.lastMinimizedHandle != IntPtr.Zero)
                    ShowWindow(this.lastMinimizedHandle, (uint)ShowWindowCommands.SW_RESTORE);
            }
            else if (e.KeyCode == Keys.ShiftKey)
            {
                this.ShiftIsHold = true;
            }
            else if (e.KeyCode == Keys.Tab)
            {
                if (positionBackups.Count > 0)
                {
                    IntPtr hwnd = positionBackups.ElementAt(currentBackupIndex).Key;

                    if (e.Shift)
                    {
                        currentBackupIndex--;
                        if (currentBackupIndex < 0)
                            currentBackupIndex = positionBackups.Count-1;
                    }
                    else
                    {
                        currentBackupIndex++;
                        if (currentBackupIndex >= positionBackups.Count)
                            currentBackupIndex = 0;
                    }

                    SetForegroundWindow(hwnd);
                    this.foreignHandle = hwnd;

                    PushMeToFront();
                    this.Invalidate();
                }
            }
            else if (e.KeyCode == Keys.Back) // Backspace
            {
                if (positionBackups.ContainsKey(this.foreignHandle))
                {

                    var placement = positionBackups[this.foreignHandle];
                    //var currentPos = GetPlacement(this.foreignHandle);

                    SetWindowPlacement(this.foreignHandle, ref placement);

                    /*
                    if(pos.showCmd != currentPos.showCmd)
                        ShowWindow(this.foreignHandle, (uint)pos.showCmd);
                    else
                        MoveWindow(this.foreignHandle, pos.rcNormalPosition.X, pos.rcNormalPosition.Y, pos.rcNormalPosition.Width-pos.rcNormalPosition.X, pos.rcNormalPosition.Height-pos.rcNormalPosition.Y, true);
                    */
                    HideMe();
                }
            }
            else if (KeymapContains(e.KeyCode))
            { // is valid key?
                e.SuppressKeyPress = true;
                pressedKeys[keyCounter] = e.KeyCode;
                if (!e.Shift)
                    keyCounter++;

                if (keyCounter == 2)  // are two keys pressed?
                {

                    keyCounter = 0;


                    int p1 = KeymapToAreaPosition(pressedKeys[0]);
                    int p2 = KeymapToAreaPosition(pressedKeys[1]);

                    Area a1 = Areas[p1];
                    Area a2 = Areas[p2];

                    int x = a1.From.X;
                    int y = a1.From.Y;
                    int w = a2.To.X - a1.From.X;
                    int h = a2.To.Y - a1.From.Y;

                    if (a1.From.X > a2.From.X)
                    {
                        x = a2.From.X;
                        w = a1.To.X - a2.From.X;
                    }

                    if (a1.From.Y > a2.From.Y)
                    {
                        y = a2.From.Y;
                        h = a1.To.Y - a2.From.Y;
                    }

                    BackupHandlePos(this.foreignHandle);

                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_NORMAL);
                    MoveWindow(this.foreignHandle, x, y, w, h, true);

                    HideMe();

                    SetForegroundWindow(this.foreignHandle);

                }
                else if (e.Shift)
                {
                    keyCounter = 0;
                    Area a = Areas[KeymapToAreaPosition(e.KeyCode)];
                    IntPtr hwnd = WindowFromPoint(new Point(a.From.X + (this.areaWidth / 2), a.From.Y + (this.areaHeight / 2)));
                    IntPtr mainHandle = GetMainWindow(hwnd);

                    BackupHandlePos(mainHandle);

                    this.foreignHandle = mainHandle;
                    this.Invalidate();
                    //SetForegroundWindow(hwnd);
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

        bool KeymapContains(Keys KeyCode)
        {
            int iL = this.keyMap.GetLength(0);
            int kL = this.keyMap.GetLength(1);
            for(int i = 0; i < rasterX; i++)
            {
                for(int k= 0; k < rasterY; k++)
                {
                    if (keyMap[k,i] == KeyCode)
                        return true;
                }
            }
            ;
            return false;
        }
        int KeymapToAreaPosition(Keys KeyCode)
        {
            int iL = this.keyMap.GetLength(0);
            int kL = this.keyMap.GetLength(1);
            int r = 0;
            for (int i = 0; i < rasterX; i++)
            {
                for (int k = 0; k < rasterY; k++)
                {
                    if (keyMap[k, i] == KeyCode)
                    {
                        r = k * rasterX + i;
                        return r;
                    }
                }
            }
    ;
            return -1;
        }

        void BackupHandlePos(IntPtr Handle)
        {
            //Rect rect = new Rect();
            //GetWindowRect(Handle, ref rect);
            var placement = GetPlacement(Handle);

            /*
            if (positionBackups.ContainsKey(Handle))
                positionBackups[Handle] = rect;
            else
                positionBackups.Add(Handle, rect);
            */

            if (positionBackups.ContainsKey(Handle))
                positionBackups[Handle] = placement;
            else
                positionBackups.Add(Handle, placement);

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {

            Areas.Clear();
            

            // e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

            Color color1 = Color.LightSkyBlue;
            Color color2 = Color.OrangeRed;


            int penwidth = 1;
            Pen pen = new Pen(color1, penwidth);

            int fullWidth = this.ClientRectangle.Width;
            int fullHeight = this.ClientRectangle.Height;

            //e.Graphics.FillRectangle(new SolidBrush(colorBack), this.ClientRectangle);

            this.areaWidth = fullWidth / rasterX;
            this.areaHeight = fullHeight / rasterY;

            //var placement = GetPlacement(this.foreignHandle);


            // linien
            for (int x = areaWidth; x < fullWidth; x += areaWidth)
            {
                e.Graphics.DrawLine(pen, x, 0, x, fullHeight);
            }
            for (int y = areaHeight; y < fullHeight; y += areaHeight)
            {
                e.Graphics.DrawLine(pen, 0, y, fullWidth, y);
            }


            int borderWidth = 2;
            var placement = GetPlacement(this.foreignHandle);

            /*
            if (placement.showCmd == ShowWindowCommands.SW_MAXIMIZE)
            {
                e.Graphics.DrawRectangle(new Pen(color2, borderWidth), Screen.FromHandle(this.foreignHandle).WorkingArea);
            }
            else
            {
                Rect rect = new Rect();
                GetWindowRect(this.foreignHandle, ref rect);
                e.Graphics.DrawRectangle(new Pen(color2, borderWidth), rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            */


            
            
            

            int i = 0;
            int k = 0;
            // text
            for (int y = 0; y < fullHeight; y += areaHeight) {
                i = 0;
                for (int x = 0; x < fullWidth; x += areaWidth)
                {
                
                    string strX = x.ToString() + "/" + y.ToString();
                    string strY = (x + areaWidth).ToString() + "/" + (y + areaHeight).ToString();


                    Area area = new Area();

                    int screenX = currentScreen.Bounds.X + x;
                    int screenY = currentScreen.Bounds.Y + y;

                    area.From = new Point(screenX, screenY);
                    area.To = new Point(screenX + areaWidth, screenY + areaHeight);
                    
                    Areas.Add(area);

                    //e.Graphics.DrawString($"{i++.ToString()}: {strX}-{strY}", new Font("Tahoma", 18.0f), new SolidBrush(color), posX, posY);
                    Keys ke = keyMap[k,i];

                    bool isPressed = pressedKeys.Contains(ke) && !ShiftIsHold;

                    //e.Graphics.DrawString($"{keyMap[i].ToString().ToUpper()}", new Font("Tahoma", 30.0f), new SolidBrush(isPressed ? color2 : color1), posX, posY);

                    // assuming g is the Graphics object on which you want to draw the text
                    GraphicsPath p = new GraphicsPath();
                    p.AddString(
                        $"{keyMap[k,i].ToString().ToUpper()}",             // text to draw
                        FontFamily.GenericSansSerif,  // or any other font family
                        (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                        e.Graphics.DpiY * 48.0f / 72,       // em size
                        new Point(x + 16, y + 16),              // location where to draw text
                        new StringFormat());          // set options here (e.g. center alignment)

                    e.Graphics.FillPath(new SolidBrush(isPressed ? color2 : color1), p);
                    e.Graphics.DrawPath(new Pen(Color.Black, 1), p);
                   
                    // + g.FillPath if you want it filled as well


                    i++;
                    
                }
                k++;
            }



        }


        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.HideMe();
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.ShiftKey)
            {
                this.ShiftIsHold = false;
            }

        }
    }
}
