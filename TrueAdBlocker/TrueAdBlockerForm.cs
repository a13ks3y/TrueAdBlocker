using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrueAdBlocker
{
    public partial class TrueAdBlockerForm : Form
    {
        public enum GWL : int
        {
            ExStyle = -20
        }

        public enum WS_EX : int
        {
            Transparent = 0x20,
            Layered = 0x80000
        }
        public enum LWA : int
        {
            ColorKey = 0x1,
            Alpha = 0x2
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong(IntPtr _hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, WS_EX dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int _crKey, Byte alpha, LWA dwFlags);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        public TrueAdBlockerForm()
        {
            InitializeComponent();
        }

        private int _InitialStyle;
        private Bitmap buffer;
        private Bitmap test;
        private Bitmap screen;
        private List<Rectangle> rectsCache = new List<Rectangle>();

        private void LoadForm(object sender, EventArgs e)
        {
            _InitialStyle = GetWindowLong(this.Handle, GWL.ExStyle);

            SetFormToTransparent();
            TopMost = true;

            //Process process = Process.GetProcessesByName("chrome")[0];
            //Console.Out.WriteLine(process.Id);
            //IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);

            //int bytesRead = 0;
            //byte[] buffer = new byte[24]; //'Hello World!' takes 12*2 bytes because of Unicode 


            //// 0x0046A3B8 is the address where I found the string, replace it with what you found
            //ReadProcessMemory((int)processHandle, 0x0046A3B8, buffer, buffer.Length, ref bytesRead);

            //Console.WriteLine(Encoding.Unicode.GetString(buffer) +
            //   " (" + bytesRead.ToString() + "bytes)");
            //Console.ReadLine();
            buffer = new Bitmap(Width, Height);
            screen = new Bitmap(Width, Height);
            test = new Bitmap(Image.FromFile("./test.jpg"));

            var currentTime = DateTime.Now.Ticks;

            var render = new Thread(() =>
            {
                while(true)
                {
                    if (DateTime.Now.Ticks - currentTime > 1000)
                    {
                        Console.Out.WriteLine("One second is passed");
                        rectsCache.Clear();
                    }
                    if (rectsCache.Count > 0)
                    {
                        using (Graphics formG = CreateGraphics())
                        {
                            Color customColor = Color.FromArgb(255, Color.Black);
                            SolidBrush shadowBrush = new SolidBrush(customColor);
                            formG.FillRectangles(shadowBrush, rectsCache.ToArray());
                        }
                    }
                    else
                    {
                        using (var bg = Graphics.FromImage(screen))
                        {
                            bg.CopyFromScreen(0, 0, 0, 0, new Size());
                        }
                        // DrawToBitmap(screen, new Rectangle(0, 0, Width, Height));
                        using (Graphics formG = CreateGraphics())
                        {
                            for (int x = 0; x < screen.Width; x++)
                            {
                                for (int y = 0; y < screen.Height; y++)
                                {
                                    // Look for first rectangle
                                    bool isMatch = true;
                                    for (int tx = 0; tx < 16; tx++)
                                    {
                                        for (int ty = 0; ty < 16; ty++)
                                        {
                                            if (!ComparePixels(test.GetPixel(tx, ty), screen.GetPixel(x, y)))
                                            {
                                                isMatch = false;
                                            }
                                        }
                                    }
                                    if (isMatch)
                                    {
                                        Console.Out.WriteLine("We have a match!");
                                        var rectToPatch = new Rectangle(x, y, test.Width, test.Height);
                                        using (Graphics g = Graphics.FromImage(buffer))
                                        {
                                            Color customColor = Color.FromArgb(255, Color.Black);
                                            SolidBrush shadowBrush = new SolidBrush(customColor);
                                            g.FillRectangles(shadowBrush, new RectangleF[] { rectToPatch });
                                        }
                                        rectsCache.Add(rectToPatch);
                                    }
                                    formG.DrawImage(buffer, 0, 0);
                                }
                            }
                        }
                    }
                }
            });

            render.Start();
        }

        private void SetFormToTransparent()
        {
            SetWindowLong(this.Handle, GWL.ExStyle, (WS_EX)(_InitialStyle | 0x80000 | 0x20));

            SetLayeredWindowAttributes(this.Handle, 0, (byte)(255 * 0.7), LWA.Alpha);
        }
        private void SetFormToOpaque()
        {
            SetWindowLong(this.Handle, GWL.ExStyle, (WS_EX)_InitialStyle | WS_EX.Layered);

            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA.Alpha);
        }

        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
          int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        private bool ComparePixels(Color firstPixel, Color secondPixel)
        {
            return (firstPixel.R - secondPixel.R < 50 && firstPixel.G - secondPixel.G < 50 && firstPixel.B - secondPixel.B < 50);
        }

        private void Render(object sender, PaintEventArgs e)
        {
            //DrawToBitmap(screen, new Rectangle(0, 0, Width, Height));
            //for (int x = 0; x < screen.Width; x++)
            //{
            //    for (int y = 0; y < screen.Height; y++)
            //    {
            //        // Look for first rectangle
            //        bool isMatch = true;
            //        for (int tx = 0; tx < 16; tx++)
            //        {
            //            for (int ty = 0; ty < 16; ty++)
            //            {
            //                if (!ComparePixels(test.GetPixel(tx, ty), screen.GetPixel(x, y)))
            //                {
            //                    isMatch = false;
            //                }
            //            }
            //        }
            //        if (isMatch)
            //        {
            //            Console.Out.WriteLine("We have a match!");
            //            using (Graphics g = Graphics.FromImage(buffer))
            //            {
            //                Color customColor = Color.FromArgb(255, Color.Black);
            //                SolidBrush shadowBrush = new SolidBrush(customColor);
            //                g.FillRectangles(shadowBrush, new RectangleF[] { new Rectangle(x, y, test.Width, test.Height) });
            //            }
            //        }
            //        e.Graphics.DrawImage(buffer, 0, 0);
            //    }
            //}
        }
    }
}
