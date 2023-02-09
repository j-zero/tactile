/*
 * TODO: 
 * + Switch through windows in specific area
 * 
*/


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
using System.Drawing.Drawing2D;

namespace Tactile
{
    public partial class MainForm : Form
    {
        public bool allowShowForm = false;
        
        protected override void SetVisibleCore(bool value)
        {
            try
            {
                base.SetVisibleCore(allowShowForm ? value : allowShowForm);
            }
            catch { }
        }
        

        internal class Area
        {
            public Point From { get; set; }
            public Point To { get; set; }
        }


        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);
        [DllImport("user32.dll")]
        static extern bool SetPhysicalCursorPos([In] int x, [In] int y);

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
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        bool SetBackgroundWindow(IntPtr hWnd)
        {
            return SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr hwnd, ref RECT lpRect);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

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

        [Flags]
        internal enum MONITOR_DEFAULTTO
        {
            NULL = 0x00000000,
            PRIMARY = 0x00000001,
            NEAREST = 0x00000002,
        }

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, MONITOR_DEFAULTTO dwFlags);

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern IntPtr MonitorFromPoint([In] Point pt, MONITOR_DEFAULTTO dwFlags);

        [DllImport("User32.dll", SetLastError = true)]
        internal static extern IntPtr MonitorFromRect([In] ref RECT lprc, MONITOR_DEFAULTTO dwFlags);

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

        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public string GetWindowClass(IntPtr hWnd)
        {
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);

            if (GetClassName(hWnd, className, maxChars) > 0)
                return className.ToString();
            
            return String.Empty;
        }



        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hwnd);

        /// <summary>
        /// Calculates the required size of the window rectangle, based on the desired size of the client rectangle and the provided DPI. This window rectangle can then be passed to the CreateWindowEx function to create a window with a client area of the desired size.
        /// </summary>
        /// <param name="lpRect">A pointer to a <see cref="RECT"/> structure that contains the coordinates of the top-left and bottom-right corners of the desired client area. When the function returns, the structure contains the coordinates of the top-left and bottom-right corners of the window to accommodate the desired client area.</param>
        /// <param name="dwStyle">The Window Style of the window whose required size is to be calculated. Note that you cannot specify the <see cref="WindowStyles.WS_OVERLAPPED"/> style.</param>
        /// <param name="bMenu">Indicates whether the window has a menu.</param>
        /// <param name="dwExStyle">The Extended Window Style of the window whose required size is to be calculated.</param>
        /// <param name="dpi">The DPI to use for scaling.</param>
        /// <returns>
        /// If the function succeeds, the return value is true.
        /// If the function fails, the return value is false. To get extended error information, call <see cref="GetLastError"/>.
        /// </returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static unsafe extern bool AdjustWindowRectExForDpi(
            RECT* lpRect,
            WindowStyles dwStyle,
            [MarshalAs(UnmanagedType.Bool)] bool bMenu,
            WindowStylesEx dwExStyle,
            int dpi);



        /*
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
        */

        KeyboardHook keyboardHook = new KeyboardHook();
        int keyCounter = 0;

        int defaultRasterX = 4;
        int defaultRasterY = 2;

        int rasterX = 4;
        int rasterY = 2;

        List<Area> Areas = new List<Area>();

        Keys[,] keyMap;

        Keys[] pressedKeys = new Keys[2];
        IntPtr foreignHandle;

        bool ignoreNextKeyUp = false;

        int currentBackupIndex = 0;
        //Dictionary<IntPtr, WINDOWPLACEMENT> positionBackups = new Dictionary<IntPtr, WINDOWPLACEMENT>();
        Dictionary<IntPtr, Backup> positionBackups = new Dictionary<IntPtr, Backup>();
        Dictionary<Keys, IntPtr> macroHandles = new Dictionary<Keys, IntPtr>();

        IntPtr lastMinimizedHandle = IntPtr.Zero;
        private int areaWidth;
        private int areaHeight;
        Screen currentScreen;
        public bool ShiftIsHold { get; private set; }
        int screenIndex = 0;
        Overlay overlay;

        int margin = 2;
        int lineWidth = 2;

        Rectangle newPosition = new Rectangle();
        private bool moveOnKeyUp;
        private bool ignoreFocusLost;

        Color color2 = Color.Gold;
        Color color1 = Color.LightSkyBlue;

        Keys lastPressedKey = Keys.None;
        private int fullWidth;
        private int fullHeight;
        private bool moveMouseIfOnWindow = true;
        private bool windowWasPushed;
        private bool macroWasSet;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int LWA_ALPHA = 0x2;
        public const int LWA_COLORKEY = 0x1;

        
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Set the form click-through
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }
        

        public MainForm()
        {
            this.Visible = false;
            this.AllowTransparency = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            

            overlay = new Overlay();

            //SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);



            keyboardHook.RegisterHotKey(global::ModifierKeys.Win, Keys.Y); // TODO: Konfigurierbar machen.
            keyboardHook.KeyPressed += keyboardHook_KeyPressed;
            InitializeComponent();
            //this.BackColor = Color.FromArgb(255, 255, 0, 255);
            this.BackColor = this.TransparencyKey = Color.Magenta;
            currentScreen = Screen.PrimaryScreen;
            RefreshScreenBounds(Screen.PrimaryScreen);
            //this.Hide();

        }

        private void keyboardHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            IntPtr newHandle = GetForegroundWindow();

            this.foreignHandle = newHandle;


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
                        {Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Z, Keys.U},
                        {Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J},
                        {Keys.Y, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M }
                   };
            }
        }

        bool IsWindowDesktop(IntPtr hWnd)
        {
            string className = GetWindowClass(hWnd);
            return className == "Progman" || className == "WorkerW";
        }

        Screen GetNextScreen(int increment)
        {
            Screen curScreen = Screen.FromControl(this);
            List<Screen> screenLeftToRight = Screen.AllScreens.OrderBy(x => x.Bounds.Left).ToList();
            int curScreenIndex = screenLeftToRight.IndexOf(curScreen);

            int incScreen = curScreenIndex + increment;
            if (incScreen < 0)
                incScreen = screenLeftToRight.Count - 1;
            else if (incScreen > screenLeftToRight.Count - 1)
                incScreen = 0;

            return screenLeftToRight[incScreen];
        }

        void MoveToNextScreen(int i)
        {
            Screen newScreen = GetNextScreen(i);
            RefreshScreenBounds(newScreen);
        }


        void RefreshScreenBounds(Screen newScreen)
        {
            this.currentScreen = newScreen;
            this.Hide();
            base.Width = 100;
            this.Width = 100;


            this.Left = newScreen.WorkingArea.Left;
            this.Top = newScreen.WorkingArea.Top;

            base.Width = newScreen.WorkingArea.Width;
            this.Width = newScreen.WorkingArea.Width;
            this.Height = newScreen.WorkingArea.Height;

            label1.Text = $"WorkingArea: {newScreen.WorkingArea.ToString()}\nthis.Bounds: {this.Bounds.ToString()}\nScreen: {newScreen.DeviceName}";



            this.Invalidate();
            DrawRaster();
            this.SetRasterForScreen();
            this.Show();
        }

        void DrawRaster()
        {

        }

        void PushMeToFront(bool rebuild = true)
        {

            this.allowShowForm = true;
            
            if (rebuild) {

                this.moveOnKeyUp = false;
                RefreshScreenBounds(currentScreen);
                SetRasterForScreen();
                this.Invalidate();

            }

            HighlightWindow(this.foreignHandle);
            this.Show();
            //this.TopMost = true;
            this.BringToFront();
            this.Activate();
            this.Focus();
            ignoreFocusLost = false;


        }
        void HideMe()
        {
            this.ignoreFocusLost = false;
            this.Hide();
            this.WindowState = FormWindowState.Normal;
            //this.TopMost = false;
            overlay.Hide();
            
            pressedKeys = new Keys[2];
        }

        void HighlightWindow(IntPtr hWnd)
        {
            if (IsWindowDesktop(hWnd) || hWnd == this.Handle || hWnd == overlay.Handle)
            {
                overlay.Hide();
                return;
            }
            ignoreFocusLost = true;
            //overlay.Hide();
            int borderWidth = 2;
            var placement = GetPlacement(hWnd);
            if (placement.showCmd == ShowWindowCommands.SW_MAXIMIZE)
            {
                //e.Graphics.DrawRectangle(new Pen(color2, borderWidth), Screen.FromHandle(this.foreignHandle).WorkingArea);
                overlay.Left = Screen.FromHandle(hWnd).WorkingArea.Left;
                overlay.Top = Screen.FromHandle(hWnd).WorkingArea.Top;
                overlay.Width = Screen.FromHandle(hWnd).WorkingArea.Width;
                overlay.Height = Screen.FromHandle(hWnd).WorkingArea.Height;
            }
            else
            {

                RECT windowRect = new RECT();
                RECT clientRect = new RECT();

                GetWindowRect(hWnd, ref windowRect);
                GetClientRect(hWnd, ref clientRect);

                //int ptDiffx = (windowRect.Right - windowRect.Left) - clientRect.Right;
                //int ptDiffy = (windowRect.Bottom - windowRect.Top) - clientRect.Bottom;

                int border_thickness = ((windowRect.Right - windowRect.Left) - clientRect.Right) / 2;


                //Point scrPos = this.PointToScreen(new Point(rect.Left, rect.Top));

                //MoveWindow(overlay.Handle, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, true);

                //e.Graphics.DrawRectangle(new Pen(color2, borderWidth), rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

                overlay.Left = windowRect.Left + border_thickness;
                overlay.Top = windowRect.Top;
                overlay.Width = windowRect.Right - windowRect.Left - border_thickness * 2;
                overlay.Height = windowRect.Bottom - windowRect.Top - border_thickness;


                //MoveWindow(overlay.Handle, placement.rcNormalPosition.X, placement.rcNormalPosition.Y, placement.rcNormalPosition.Width, placement.rcNormalPosition.Height, true);
            }
            //overlay.DrawStuff();
            overlay.Invalidate();
            overlay.TopMost = true;
            overlay.Show();
            //ignoreFocusLost = false;
            //overlay.BringToFront();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //DebugForm dbgForm = new DebugForm();
            //dbgForm.Show();
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == lastPressedKey)
                return;

            var placement = GetPlacement(this.foreignHandle);

            if (e.KeyCode == Keys.Escape)
            {
                //SetForegroundWindow(this.foreignHandle);
                HideMe();
            }
            else if (e.KeyCode == Keys.Right)
            {
                MoveToNextScreen(1);
                //this.WindowState = FormWindowState.Normal;
                PushMeToFront();
            }
            else if (e.KeyCode == Keys.Left)
            {
                MoveToNextScreen(-1);
                //this.WindowState = FormWindowState.Normal;
                PushMeToFront();
            }
            else if (e.KeyCode == Keys.Tab)
            {
                if(e.Shift)
                    MoveToNextScreen(-1);
                else
                    MoveToNextScreen(1);
                //this.WindowState = FormWindowState.Normal;
                PushMeToFront();
            }
            else if (e.KeyCode >= Keys.F1 && e.KeyCode <= Keys.F12)
            {
                this.macroWasSet = true;
                if (macroHandles.ContainsKey(e.KeyCode))
                {
                    this.foreignHandle = macroHandles[e.KeyCode];
                }
                else
                {
                    macroHandles.Add(e.KeyCode, this.foreignHandle);
                }

                placement = GetPlacement(this.foreignHandle);

                if (placement.showCmd == ShowWindowCommands.SW_SHOWMINIMIZED)
                {
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_RESTORE);
                }
                else
                {
                    SetForegroundWindow(this.foreignHandle);
                    //ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MINIMIZE);
                }
            }
            else if (e.KeyCode == Keys.B)
            {
                var curWinPos = GetPlacement(this.foreignHandle).rcNormalPosition;
                SetBackgroundWindow(this.foreignHandle);

                //IntPtr hwnd = WindowFromPoint(new Point(curWinPos.X + (curWinPos.Width / 2), curWinPos.Y + (curWinPos.Height / 2)));

                //IntPtr mainHandle = GetMainWindow(hwnd);

                //BackupHandlePos(mainHandle);

                //this.foreignHandle = mainHandle;
                //this.Invalidate();
                this.ignoreFocusLost = true;
                //HighlightWindow(this.foreignHandle);

                //SetForegroundWindow(hwnd);

                HideMe();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                SetForegroundWindow(this.foreignHandle);
                HideMe();
            }
            else if (e.KeyCode == Keys.ControlKey)
            {
                ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MINIMIZE);
                this.lastMinimizedHandle = this.foreignHandle;
                HideMe();

            }
            else if (e.KeyCode == Keys.Space)
            {

                //ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_RESTORE);
                //HideMe();

                Screen foreignScreen = Screen.FromHandle(this.foreignHandle);
                Screen curScreen = Screen.FromControl(this);

                if (foreignScreen.DeviceName == curScreen.DeviceName)
                {
                    if (placement.showCmd == ShowWindowCommands.SW_NORMAL)
                        ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MAXIMIZE);
                    else
                    
                        ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_RESTORE);
                }
                else
                {
                    //placement = GetPlacement(this.foreignHandle);
                    if (placement.showCmd == ShowWindowCommands.SW_NORMAL)
                        BackupHandlePos(this.foreignHandle);

                    RECT windowRect = new RECT();
                    RECT clientRect = new RECT();

                    GetWindowRect(this.foreignHandle, ref windowRect);
                    GetClientRect(this.foreignHandle, ref clientRect);

                    //Rectangle r = new Rectangle(curScreen.WorkingArea.X + 16, curScreen.WorkingArea.Y, placement.rcNormalPosition.Width, placement.rcNormalPosition.Height);
                    Rectangle r = windowRect;
                    r.X = curScreen.WorkingArea.X + 16;
                    r.Y = curScreen.WorkingArea.Y + 16;
                    r.Width = currentScreen.WorkingArea.Width - 16;
                    r.Height = currentScreen.WorkingArea.Height - 16;

                    MoveWindowMagic(r);
                   // if (placement.showCmd == ShowWindowCommands.SW_NORMAL)
                    ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MAXIMIZE);
                    ;
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
            else if (e.KeyCode == Keys.Oem102)
            {
                if (this.lastMinimizedHandle != IntPtr.Zero)
                    ShowWindow(this.lastMinimizedHandle, (uint)ShowWindowCommands.SW_RESTORE);
            }
            else if (e.KeyCode == Keys.ShiftKey)
            {
                this.ShiftIsHold = true;
            }
            else if (e.KeyCode == Keys.Menu)
            {
                ;//this.ShiftIsHold = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                if (positionBackups.Count > 0)
                {
                    IntPtr hwnd = positionBackups.ElementAt(currentBackupIndex).Key;

                    if (e.Shift)
                    {
                        currentBackupIndex--;
                        if (currentBackupIndex < 0)
                            currentBackupIndex = positionBackups.Count - 1;
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
                RestoreBackupPosition(this.foreignHandle);
            }
            else if (KeymapContains(e.KeyCode))
            { // is valid key?
                e.SuppressKeyPress = true;
                pressedKeys[keyCounter] = e.KeyCode;
                if (!e.Shift && !e.Alt)
                    keyCounter++;

                if (keyCounter == 2)  // are two keys pressed?
                {
                    keyCounter = 0;

                    int p1 = KeymapToAreaPosition(pressedKeys[0]);
                    int p2 = KeymapToAreaPosition(pressedKeys[1]);

                    if (p1 != -1 && p2 != -1)
                    {
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

                        this.newPosition = new Rectangle(x, y, w, h);
                        this.moveOnKeyUp = true;
                    }
                    this.Invalidate();

                }
                else if (e.Shift || e.Alt)
                {
                    keyCounter = 0;
                    Area a = Areas[KeymapToAreaPosition(e.KeyCode)];
                    IntPtr hwnd = WindowFromPoint(new Point(a.From.X + (this.areaWidth / 2), a.From.Y + (this.areaHeight / 2)));
                    IntPtr mainHandle = GetMainWindow(hwnd);

                    BackupHandlePos(mainHandle);

                    this.foreignHandle = mainHandle;
                    //this.Invalidate();
                    this.ignoreFocusLost = true;
                    HighlightWindow(this.foreignHandle);

                    SetForegroundWindow(hwnd);

                    PushMeToFront(false);

                    if (e.Shift)
                    {
                        windowWasPushed = true;
                    }
                    else
                    {
                        //HideMe();
                    }
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

        void RestoreBackupPosition(IntPtr Handle)
        {
            if (positionBackups.ContainsKey(Handle))
            {


                var b = positionBackups[Handle];



                Rectangle r = b.Rect;
                //r.X = b.screen.WorkingArea.X + 16;
                //r.Y = b.screen.WorkingArea.Y + 16;

                //MoveWindowMagic(r);
                // if (placement.showCmd == ShowWindowCommands.SW_NORMAL)
                //ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_MAXIMIZE);

                //MoveWindowMagic(new Rectangle(backupPlacement.rcNormalPosition.X, backupPlacement.rcNormalPosition.Y, backupPlacement.rcNormalPosition.Width, backupPlacement.rcNormalPosition.Height));
                ShowWindow(this.foreignHandle, (uint)b.wINDOWPLACEMENT.showCmd);

                int dpi1 = GetDpiForWindow(GetMainWindow(this.foreignHandle));
                int dpi2 = b.Dpi;


                float dpiFactor = (float)dpi1 / (float)dpi2;



                int x = r.Left;
                int y = r.Top;

                int w = (int)((r.Right - r.Left) * dpiFactor);
                int h = (int)((r.Bottom - r.Top) * dpiFactor);


                MoveWindow(this.foreignHandle, x, y, w, h, true);



                //MoveWindowMagic(b.Rect, true);


                //MoveWindowMagic(backupPlacement, true);
                //
                //var currentPos = GetPlacement(this.foreignHandle);

                //MoveWindowMagic(

                //SetWindowPlacement(Handle, ref backupPlacement);

                /*
                if(pos.showCmd != currentPos.showCmd)
                    ShowWindow(this.foreignHandle, (uint)pos.showCmd);
                else
                    MoveWindow(this.foreignHandle, pos.rcNormalPosition.X, pos.rcNormalPosition.Y, pos.rcNormalPosition.Width-pos.rcNormalPosition.X, pos.rcNormalPosition.Height-pos.rcNormalPosition.Y, true);
                */
                HideMe();
            }
        }

        unsafe void MoveWindowMagic()
        {
            MoveWindowMagic(newPosition);
        }


        unsafe void MoveWindowMagic(Rectangle Position, bool ignoreDPI = false)
        {
            //RECT clientRect = new RECT();
            //GetClientRect(this.foreignHandle, ref clientRect);
            
            RECT windowRect = new RECT(Position);


            Point mousePoint = new Point();

            if (!ignoreDPI)
            {

                ShowWindow(this.foreignHandle, (uint)ShowWindowCommands.SW_NORMAL);

                RECT withoutShadowRect = new RECT();
                RECT newRect = new RECT(Position);

                GetCursorPos(ref mousePoint);
                GetWindowRect(this.foreignHandle, ref windowRect);
                DwmGetWindowAttribute(this.foreignHandle, DWMWINDOWATTRIBUTE.ExtendedFrameBounds, out withoutShadowRect, Marshal.SizeOf(typeof(RECT)));

                RECT shadowRect = windowRect - withoutShadowRect;


                int dpi1 = GetDpiForWindow(GetMainWindow(this.foreignHandle));
                int dpi2 = this.DeviceDpi;


                float dpiFactor = (float)dpi1 / (float)dpi2;


                int x = newRect.Left - Math.Abs(shadowRect.Left) + margin;
                int y = newRect.Top - Math.Abs(shadowRect.Top) + margin;

                int w = (int)((newRect.Right - newRect.Left) * dpiFactor) + (Math.Abs(shadowRect.Right) + Math.Abs(shadowRect.Left)) - (margin * 2);
                int h = (int)((newRect.Bottom - newRect.Top) * dpiFactor) + (Math.Abs(shadowRect.Bottom) + Math.Abs(shadowRect.Top)) - (margin * 2);


                //BackupHandlePos(this.foreignHandle);

                
                MoveWindow(this.foreignHandle, x, y, w, h, true);
                //
                //MoveWindow(this.foreignHandle, x, y, w, h, true);


            }
            else
            {
                MoveWindow(this.foreignHandle, windowRect.X, windowRect.Y, windowRect.Width, windowRect.Height, true);
            }

            if (moveMouseIfOnWindow && mousePoint.X >= windowRect.Left && mousePoint.X <= windowRect.Right && mousePoint.Y >= windowRect.Top && mousePoint.Y <= windowRect.Bottom) // mouse cursor on current window
            {
                /*
                int oldRelativeMousePosX = (int)((mousePoint.X - windowRect.X));
                int oldRelativeMousePosY = (int)((mousePoint.Y - windowRect.Y));

                int newRelativeMousePosX = (int)((newPosition.X + oldRelativeMousePosX) * dpiFactor);
                int newRelativeMousePosY = (int)((newPosition.Y - oldRelativeMousePosY) * dpiFactor);
                */
             //int newMouseX = newPosition.X + newPosition.Width / 2;
                int newMouseX = newPosition.X + newPosition.Width / 2;
                //int newMouseY = newPosition.Y + newPosition.Height / 2;
                int newMouseY = newPosition.Y + newPosition.Height / 2;


                MoveCursor(newMouseX, newMouseY);
            }

            


            HideMe();

            SetForegroundWindow(this.foreignHandle);
            moveOnKeyUp = false;
            this.newPosition = new Rectangle();
        }

        private void MoveCursor(int x, int y)
        {
            /*
            // Set the Current cursor, move the cursor's Position,
            // and set its clipping rectangle to the form. 

            this.Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(xpos, ypos);
            Cursor.Position = new Point(xpos, ypos); // wtf?
            */
            SetPhysicalCursorPos(x, y);
        }

        bool KeymapContains(Keys KeyCode)
        {
            if (keyMap != null)
            {
                int iL = this.keyMap.GetLength(0);
                int kL = this.keyMap.GetLength(1);
                for (int i = 0; i < rasterX; i++)
                {
                    for (int k = 0; k < rasterY; k++)
                    {
                        if (keyMap[k, i] == KeyCode)
                            return true;
                    }
                }
            }
            return false;
        }
        int KeymapToAreaPosition(Keys KeyCode)
        {
            int iL = this.keyMap.GetLength(0);
            int kL = this.keyMap.GetLength(1);
            int r = 0;

            for (int k = 0; k < rasterY; k++) 
            {
                for (int i = 0; i < rasterX; i++)
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
            RECT rect = new RECT();

            var placement = GetPlacement(Handle);
            GetWindowRect(Handle, ref rect);


            int dpi = GetDpiForWindow(GetMainWindow(Handle));

            Backup b = new Backup();

            b.wINDOWPLACEMENT = placement;
            b.Rect = rect;
            b.screen = Screen.FromHandle(Handle);
            b.Dpi = dpi;
            /*
            if (positionBackups.ContainsKey(Handle))
                positionBackups[Handle] = rect;
            else
                positionBackups.Add(Handle, rect);
            */

            if (positionBackups.ContainsKey(Handle))
                positionBackups[Handle] = b;
            else
                positionBackups.Add(Handle, b);

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {


            if (keyMap == null)
                return;

            //Areas.Clear();
            

            //this.lineWidth = 1;
            Pen pen = new Pen(this.color1, lineWidth);

            int fullWidth = this.ClientRectangle.Width;
            int fullHeight = this.ClientRectangle.Height;



            this.areaWidth = fullWidth / rasterX;
            this.areaHeight = fullHeight / rasterY;


            // linien
            for (int x = areaWidth; x < fullWidth; x += areaWidth)
            {
                e.Graphics.DrawLine(pen, x, 0, x, fullHeight);
            }
            for (int y = areaHeight; y < fullHeight; y += areaHeight)
            {
                e.Graphics.DrawLine(pen, 0, y, fullWidth, y);
            }

            e.Graphics.DrawRectangle(new Pen(color2, 4), this.newPosition);


            int i = 0;
            int k = 0;
            // text
            for (int y = 0; y < fullHeight; y += areaHeight) {
                i = 0;
                for (int x = 0; x < fullWidth; x += areaWidth)
                {
                
                    string strX = x.ToString() + "/" + y.ToString();
                    string strY = (x + areaWidth).ToString() + "/" + (y + areaHeight).ToString();

                    /*
                    Area area = new Area();

                    int screenX = currentScreen.Bounds.X + x;
                    int screenY = currentScreen.Bounds.Y + y;

                    area.From = new Point(screenX, screenY);
                    area.To = new Point(screenX + areaWidth, screenY + areaHeight);
                    
                    Areas.Add(area);
                    */
                    //string DEBUG = $"{area.From.ToString()} - {area.To.ToString()}";

                    //string DEBUG = $"{currentScreen.Bounds.ToString()}";

                    //e.Graphics.DrawString($"{DEBUG}", new Font("Tahoma", 12.0f), new SolidBrush(Color.White), x + 16, y + 96);
                    Keys ke = keyMap[k,i];

                    bool isPressed = pressedKeys.Contains(ke) && !ShiftIsHold;

                    //e.Graphics.DrawString($"{keyMap[i].ToString().ToUpper()}", new Font("Tahoma", 30.0f), new SolidBrush(isPressed ? color2 : color1), posX, posY);

                    // assuming g is the Graphics object on which you want to draw the text
                    GraphicsPath p = new GraphicsPath();

                    Keys key = keyMap[k, i];

                    string keyStr = ((char)key).ToString().ToUpper();


                    p.AddString(
                        keyStr,             // text to draw
                        FontFamily.GenericSansSerif,  // or any other font family
                        (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                        e.Graphics.DpiY * 36.0f / 72,       // em size
                        new Point(x + 16, y + 16),              // location where to draw text
                        new StringFormat());          // set options here (e.g. center alignment)


                    e.Graphics.FillPath(new SolidBrush(isPressed ? color2 : color1), p);
                    e.Graphics.DrawPath(new Pen(Color.Black, 2), p);
                   


                    // + g.FillPath if you want it filled as well


                    i++;
                    
                }
                k++;
            }



        }


        private void Form1_Resize(object sender, EventArgs e)
        {

            if (keyMap == null)
                return;

            Areas.Clear();


            this.fullWidth = this.ClientRectangle.Width;
            this.fullHeight = this.ClientRectangle.Height;

            this.areaWidth = fullWidth / rasterX;
            this.areaHeight = fullHeight / rasterY;

            int i = 0;
            int k = 0;
            for (int y = 0; y < fullHeight; y += areaHeight)
            {
                i = 0;
                for (int x = 0; x < fullWidth; x += areaWidth)
                {
                    Area area = new Area();
                    int screenX = currentScreen.Bounds.X + x;
                    int screenY = currentScreen.Bounds.Y + y;
                    area.From = new Point(screenX, screenY);
                    area.To = new Point(screenX + areaWidth, screenY + areaHeight);
                    Areas.Add(area);

                    i++;

                }
                k++;
            }

        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            if(!this.ignoreFocusLost)
                this.HideMe();
            //this.ignoreFocusLost = false;
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == lastPressedKey)
                lastPressedKey = Keys.None;

            if (e.KeyCode == Keys.Alt)
            {
                if (macroWasSet)
                    HideMe();
            }
            else if(e.KeyCode == Keys.ShiftKey)
            {
                this.ShiftIsHold = false;
                if (windowWasPushed)
                {
                    windowWasPushed = false;
                    HideMe();
                }
            }

            if (moveOnKeyUp)
            {
                BackupHandlePos(this.foreignHandle);
                MoveWindowMagic();
            }

        }

        private void RefreshScreenBounds(object sender, EventArgs e)
        {
            RefreshScreenBounds(Screen.FromControl(this));
        }

        private void MainForm_ResizeBegin(object sender, EventArgs e)
        {
            this.Invalidate();
        }
    }
}
