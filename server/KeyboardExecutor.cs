using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerWPF
{
    public class KeyboardExecutor
    {

        #region DLL_&_costanti_Keyboard

        [DllImport("user32.dll")]
        public static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        /* COSTANTI AZIONI TASTIERA */

        private const int KEY_DOWN = 0x0000;
        private const int KEY_UP = 0x0002;
        private const int KEY_EXTENDED = 0x0001;

        #endregion

        #region keyboard_methods

        internal void KeyPressDown(byte codTasto)
        {
            doActionKeyboard(KEY_DOWN, codTasto);
        }

        internal void KeyPressUp(byte codTasto)
        {
            doActionKeyboard(KEY_UP, codTasto);
        }

        private void doActionKeyboard(int azione, byte tasto)
        {
            keybd_event(tasto, 0, KEY_EXTENDED | azione, 0);
        }

        #endregion

    }
}
