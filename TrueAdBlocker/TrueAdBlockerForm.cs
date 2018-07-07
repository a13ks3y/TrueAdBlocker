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
        private long currentTime = DateTime.Now.Ticks;
        private long renderCurrentTime = DateTime.Now.Ticks;

        private void LoadForm(object sender, EventArgs e)
        {
            _InitialStyle = GetWindowLong(this.Handle, GWL.ExStyle);

            SetFormToTransparent();
            TopMost = true;
            // ad that needs to be hide
            test = new Bitmap(Image.FromFile("./test.jpg"));
            buffer = new Bitmap(Width, Height);
            screen = new Bitmap(Width, Height);

            var screenCaptureThread = new Thread(() =>
            {
                while(true)
                {
                    if (DateTime.Now.Ticks - currentTime > 1000)
                    {
                        lock(screen)
                        {
                            Console.Out.WriteLine("screenCaptureThread TICK! %i", currentTime);
                            Rectangle bounds = Screen.GetBounds(Point.Empty);
                            using (var g = Graphics.FromImage(screen))
                            {
                                g.CopyFromScreen(0, 0, 0, 0, bounds.Size);
                            }
                            screen.Save("fuck.jpg");
                            currentTime = DateTime.Now.Ticks;

                        }
                    }
                }
            });

            screenCaptureThread.Start();

            var renderThread = new Thread(() =>
            {
                while (true)
                {
                    if (DateTime.Now.Ticks - renderCurrentTime > 24)
                    {
                        Console.Out.WriteLine("renderThread TICK!");

                        for (var sx = 0; sx < screen.Width; sx++)
                        {
                            for (var sy = 0; sy < screen.Height; sy++)
                            {
                                var isMatch = true;
                                for (var x = 0; x < test.Width; x++)
                                {
                                    for (var y = 0; y < test.Height; y++)
                                    {
                                        var pixel = test.GetPixel(x, y);
                                        lock(screen)
                                        {
                                            var screenPixel = screen.GetPixel(sx, sy);
                                            if (ComparePixels(pixel, screenPixel))
                                            {
                                                isMatch = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (isMatch)
                                {
                                    using (var g = Graphics.FromHwnd(Handle))
                                    {
                                        g.DrawRectangle(Pens.Black, sx, sy, test.Width, test.Height);
                                    }
                                }
                            }
                        }

                        renderCurrentTime = DateTime.Now.Ticks;
                    }
                }
            });

            renderThread.Start();
          }

        private void SetFormToTransparent()
        {
            SetWindowLong(this.Handle, GWL.ExStyle, (WS_EX)(_InitialStyle | 0x80000 | 0x20));

            SetLayeredWindowAttributes(this.Handle, 0, (byte)(255 * 0.1), LWA.Alpha);
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
          
        }
    }
}
