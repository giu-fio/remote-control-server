using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;

namespace ServerWPF
{
    public class MouseExecutor
    {
        #region DLL_COSTANTI_MOUSE_TASTIERA_CLIPBOARD

        //, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int cButtons, uint dwExtraInfo);
        
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        //serve per ottenere la posizione del mouse
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out PointAPI lpPoint);

        struct PointAPI
        {
            public int X;
            public int Y;
        }

        /* COSTANTI AZIONI MOUSE*/

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;
        private const int MOUSEEVENTF_MOVE = 0X01;
        private const int MOUSEEVENTF_WHEEL = 0X800;

        #endregion

        private double widthRatio;
        private double heightRatio;
        private double myWidthMonitor;
        private double myHeightMonitor;

        public MouseExecutor(double heigthMonitorClient,double widthMonitorClient)
        {
            myHeightMonitor = (int)SystemParameters.VirtualScreenHeight;
            myWidthMonitor = (int)SystemParameters.VirtualScreenWidth;
            this.widthRatio = myWidthMonitor/widthMonitorClient;
            this.heightRatio = myHeightMonitor/heigthMonitorClient;
        }

        public double WidthRatio
        {
            get { return widthRatio; }
            set
            {
                if (value < 1) { widthRatio = 1; }
                else { widthRatio = value; }
            }

        }

        public double HeightRatio
        {
            get { return heightRatio; }
            set
            {
                if (value < 1) { heightRatio = 1; }
                else { heightRatio = value; }
            }

        }

        public void ExecuteMouseMove(int x, int y)
        {
            //setta x e y a seconda della risoluzione 
            double p = x * widthRatio;
            int X = (int)p;
            p = y * heightRatio;
            int Y = (int)p;
            SetCursorPos(X, Y);
        }

        public void ExecuteMouseWeel(int delta)
        {
            doActionMouse(MOUSEEVENTF_WHEEL, delta);
        }

        public void ExecuteMouseUpRight()
        {
            doActionMouse(MOUSEEVENTF_RIGHTUP, 0);
        }

        public void ExecuteMouseUpMiddle()
        {
            doActionMouse(MOUSEEVENTF_MIDDLEUP, 0);
        }

        public void ExecuteMouseUpLeft()
        {
            doActionMouse(MOUSEEVENTF_LEFTUP, 0);
        }

        public void ExecuteMouseDownRight()
        {
            doActionMouse(MOUSEEVENTF_RIGHTDOWN, 0);
        }

        public void ExecuteMouseDownMiddle()
        {
            doActionMouse(MOUSEEVENTF_MIDDLEDOWN, 0);
        }

        public void ExecuteMouseDownLeft()
        {

            doActionMouse(MOUSEEVENTF_LEFTDOWN, 0);
        }

        /* La string argument serve per la presenza di un eventuale DELTA in causa di presenza d'evento MOUSE WHEEL */
        private void doActionMouse(uint p, int delta)
        {
            PointAPI point = new PointAPI();
            GetCursorPos(out point);
            int X = point.X;
            int Y = point.Y;
            mouse_event(p, (uint)X, (uint)Y, delta, 0);
        }

    }
}
