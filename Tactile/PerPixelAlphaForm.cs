#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
#endregion

public partial class PerPixelAlphaForm : Form
{

    public Bitmap BackgroundGraphics = null;

    IntPtr hBitmap = IntPtr.Zero;
    IntPtr hOldBitmap = IntPtr.Zero;

    public PerPixelAlphaForm()
    {
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        this.ShowInTaskbar = false;
        // this.StartPosition = FormStartPosition.CenterScreen;
        this.Load += PerPixelAlphaForm_Load;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        //DrawStuff(255);
        //Invalidate();
    }

    void PerPixelAlphaForm_Load(object sender, EventArgs e)
    {
        this.TopMost = true;
    }
    protected override CreateParams CreateParams
    {
        get
        {
            // Add the layered extended style (WS_EX_LAYERED) to this window.
            CreateParams createParams = base.CreateParams;
            if (!DesignMode)
                createParams.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            return createParams;
        }
    }
    /// <summary>
    /// Let Windows drag this window for us (thinks its hitting the title 
    /// bar of the window)
    /// </summary>
    /// <param name="message"></param>
    protected override void WndProc(ref Message message)
    {

        if (message.Msg == WM_NCHITTEST)
        {
            // Tell Windows that the user is on the title bar (caption)
            message.Result = (IntPtr)HTCAPTION;
        }
        else
        {
            base.WndProc(ref message);
        }

        //base.WndProc(ref message);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bitmap">
    /// 
    /// </param>
    /// <param name="opacity">
    /// Specifies an alpha transparency value to be used on the entire source 
    /// bitmap. The SourceConstantAlpha value is combined with any per-pixel 
    /// alpha values in the source bitmap. The value ranges from 0 to 255. If 
    /// you set SourceConstantAlpha to 0, it is assumed that your image is 
    /// transparent. When you only want to use per-pixel alpha values, set 
    /// the SourceConstantAlpha value to 255 (opaque).
    /// </param>
    public void DrawStuff(int opacity = 255)
    {
        // Get device contexts
        IntPtr screenDc = GetDC(IntPtr.Zero);
        IntPtr memDc = CreateCompatibleDC(screenDc);

        try
        {


            // Get handle to the new bitmap and select it into the current 
            // device context.
            if (BackgroundGraphics == null)
            {
                BackgroundGraphics = new Bitmap(this.Width, this.Height);

                using (Graphics gfx = Graphics.FromImage(BackgroundGraphics))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, 255, 0, 0)))
                    {
                        gfx.FillRectangle(brush, 0, 0, BackgroundGraphics.Width, BackgroundGraphics.Height);
                    }
                    /*
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 0, 255, 0)))
                    {
                        gfx.FillRectangle(brush, 20, 20, 20, 20);
                    }
                    */
                }
            }

            hBitmap = BackgroundGraphics.GetHbitmap(Color.FromArgb(0));
            hOldBitmap = SelectObject(memDc, hBitmap);

            // Set parameters for layered window update.
            //Size newSize = new Size(bitmap.Width, bitmap.Height);

            Point newLocation = new Point(this.Left, this.Top);
            Size newSize = new Size(this.Width, this.Height);


            Point sourceLocation = new Point(0, 0);

            BLENDFUNCTION blend = new BLENDFUNCTION();
            blend.BlendOp = AC_SRC_OVER;
            blend.BlendFlags = 0;
            blend.SourceConstantAlpha = (byte)opacity;
            blend.AlphaFormat = AC_SRC_ALPHA;

            // Update the window.
            UpdateLayeredWindow(
                this.Handle,     // Handle to the layered window
                screenDc,        // Handle to the screen DC
                ref newLocation, // New screen position of the layered window
                ref newSize,     // New size of the layered window
                memDc,           // Handle to the layered window surface DC
                ref sourceLocation, // Location of the layer in the DC
                0,               // Color key of the layered window
                ref blend,       // Transparency of the layered window
                ULW_ALPHA        // Use blend as the blend function
                );
        }
        catch(Exception ex)
        {
            ;
        }
        finally
        {
            // Release device context.
            ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero)
            {
                SelectObject(memDc, hOldBitmap);
                DeleteObject(hBitmap);
            }
            DeleteDC(memDc);
            BackgroundGraphics.Dispose();
            GC.Collect();
        }
    }
    #region Native Methods and Structures

    const Int32 WS_EX_TRANSPARENT = 0x00000020;
    const Int32 WS_EX_LAYERED = 0x80000;
    const Int32 HTCAPTION = 0x02;
    const Int32 WM_NCHITTEST = 0x84;
    const Int32 ULW_ALPHA = 0x02;
    const byte AC_SRC_OVER = 0x00;
    const byte AC_SRC_ALPHA = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    struct Point
    {
        public Int32 x;
        public Int32 y;

        public Point(Int32 x, Int32 y)
        { this.x = x; this.y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Size
    {
        public Int32 cx;
        public Int32 cy;

        public Size(Int32 cx, Int32 cy)
        { this.cx = cx; this.cy = cy; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ARGB
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        public byte Alpha;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
        ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc,
        Int32 crKey, ref BLENDFUNCTION pblend, Int32 dwFlags);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DeleteObject(IntPtr hObject);

    #endregion


    private void PerPixelAlphaForm_Load_1(object sender, EventArgs e)
    {

    }

    private void InitializeComponent()
    {
            this.SuspendLayout();
            // 
            // PerPixelAlphaForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "PerPixelAlphaForm";
            this.Load += new System.EventHandler(this.PerPixelAlphaForm_Load_2);
            this.ResumeLayout(false);

    }

    private void PerPixelAlphaForm_Load_2(object sender, EventArgs e)
    {

    }
}
