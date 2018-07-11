using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Timers;
using Emgu;
using Emgu.CV;
using Emgu.CV.Structure;

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

        // public System.Timers.Timer ScreenShotTimer = new System.Timers.Timer(500);
        // public System.Timers.Timer RenderTimer = new System.Timers.Timer(12);


        private int _InitialStyle;
        private Bitmap test;
        private Bitmap screen;
        private List<Rectangle> rectsCache = new List<Rectangle>();

        private void LoadForm(object sender, EventArgs e)
        {
            _InitialStyle = GetWindowLong(this.Handle, GWL.ExStyle);

            SetFormToTransparent();
            TopMost = true;
            screen = new Bitmap(Width, Height);
            test = new Bitmap(Image.FromFile("./test.jpg"));

            //ScreenShotTimer.Elapsed += ScreenShotTimerTick;
            //ScreenShotTimer.AutoReset = true;
            //ScreenShotTimer.Enabled = true;
            //ScreenShotTimer.Start();

            //RenderTimer.Elapsed += RenderTimerTick;
            //RenderTimer.AutoReset = true;
            //RenderTimer.Enabled = true;
            //RenderTimer.Start();

            var currentTime = DateTime.Now.Ticks;

            var screenShotThread = new Thread(() =>
            {
                while(true) {
                    if (DateTime.Now.Ticks - currentTime > 1000)
                    {
                        ScreenShotTimerTick();
                        currentTime = DateTime.Now.Ticks;
                    }
                }
            });
            var renderCurrentTime = DateTime.Now.Ticks;
            var renderThread = new Thread(() =>
            {
                while(true)
                {
                    if (DateTime.Now.Ticks - renderCurrentTime > 60)
                    {
                        RenderTimerTick();
                        renderCurrentTime = DateTime.Now.Ticks;
                    }
                }
            });

            screenShotThread.Start();
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
            return (firstPixel.R - secondPixel.R < 250 && firstPixel.G - secondPixel.G < 250 && firstPixel.B - secondPixel.B < 250);
        }


        private void ScreenShotTimerTick()
        {
            lock (test)
            {
                using (var g = Graphics.FromImage(screen))
                {
                    g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Size);
                    rectsCache.Clear();
                    Emgu.CV.Image<Bgr, byte> screenImg = new Image<Bgr, byte>(screen);
                    Emgu.CV.Image<Bgr, byte> testImg = new Image<Bgr, byte>(test);
                    using (Image<Emgu.CV.Structure.Gray, float> result = screenImg.MatchTemplate(testImg, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
                    {
                        double[] minValues, maxValues;
                        Point[] minLocations, maxLocations;
                        result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                        // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                        if (maxValues[0] > 0.65)
                        {
                            // This is a match. Do something with it, for example draw a rectangle around it.
                            Rectangle match = new Rectangle(maxLocations[0], testImg.Size);
                            // screenImg.Draw(match, new Bgr(Color.Red), 3);
                            lock(rectsCache)
                            {
                                rectsCache.Add(match);
                            }
                        }
                    }
                }
            }
        }

        private void RenderTimerTick()
        {
            using (var g = Graphics.FromHwnd(GetDesktopWindow()))
            {
                lock(rectsCache)
                {
                    var rectsCacheArray = rectsCache.ToArray();
                    foreach (var rect in rectsCacheArray)
                    {
                        g.FillRectangle(Brushes.Black, rect);
                    }

                }
            }
        }
    }
}
