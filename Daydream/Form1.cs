using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Daydream
{
    public partial class MainForm : System.Windows.Forms.Form
    {
        #region Stolen
        #region Win32

        const int AW_HIDE = 0X10000;
        const int AW_ACTIVATE = 0X20000;
        const int AW_HOR_POSITIVE = 0X1;
        const int AW_HOR_NEGATIVE = 0X2;
        const int AW_SLIDE = 0X40000;
        const int AW_BLEND = 0X80000;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int AnimateWindow
        (IntPtr hwand, int dwTime, int dwFlags);

        #endregion

        #endregion

        KeyboardHook hook = new KeyboardHook();

        public MainForm()
        {
            InitializeComponent();
            //setAutoSaveSettings();

            #region Stolen
            // register the event that is fired after the key press.
            hook.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
            // register the control + alt + F12 combination as hot key.
            //hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt,
            //    Keys.F12);

            hook.RegisterHotKey(Daydream.ModifierKeys.Control, Keys.F12);
            #endregion
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (this.Visible == true)
                this.Visible = false;
            else
                this.Visible = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            bool _UseSlideAnimation = false; // Here just for testing purposes
            AnimateWindow(this.Handle, 1000, AW_ACTIVATE | (_UseSlideAnimation ? AW_HOR_POSITIVE | AW_SLIDE : AW_BLEND));
        }

        private void MainForm_VisibleChanged(object sender, EventArgs e)
        {
            bool _UseSlideAnimation = false;

            if (this.Visible == false)
            {
                // Start the hide animation
                AnimateWindow(this.Handle, 1000, AW_HIDE | (_UseSlideAnimation ?
                                    AW_HOR_NEGATIVE | AW_SLIDE : AW_BLEND));
            }
            else
            {
                AnimateWindow(this.Handle, 1000, AW_ACTIVATE | (_UseSlideAnimation ?
                                    AW_HOR_POSITIVE | AW_SLIDE : AW_BLEND));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }

    #region Stolen
    public sealed class KeyboardHook : IDisposable // Should be in it's own file, handy
    {
        // Registers a hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Represents the window that is used internally to get the messages.
        /// </summary>
        private class Window : NativeWindow, IDisposable
        {
            private static int WM_HOTKEY = 0x0312;

            public Window()
            {
                // create the handle for the window.
                this.CreateHandle(new CreateParams());
            }

            /// <summary>
            /// Overridden to get the notifications.
            /// </summary>
            /// <param name="m"></param>
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                // check if we got a hot key pressed.
                if (m.Msg == WM_HOTKEY)
                {
                    // get the keys.
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    ModifierKeys modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    if (KeyPressed != null)
                        KeyPressed(this, new KeyPressedEventArgs(modifier, key));
                }
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            #region IDisposable Members

            public void Dispose()
            {
                this.DestroyHandle();
            }

            #endregion
        }

        private Window _window = new Window();
        private int _currentId;

        public KeyboardHook()
        {
            // register the event of the inner native window.
            _window.KeyPressed += delegate(object sender, KeyPressedEventArgs args)
            {
                if (KeyPressed != null)
                    KeyPressed(this, args);
            };
        }

        /// <summary>
        /// Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(ModifierKeys modifier, Keys key)
        {
            // increment the counter.
            _currentId = _currentId + 1;

            // register the hot key.
            if (!RegisterHotKey(_window.Handle, _currentId, (uint)modifier, (uint)key))
                throw new InvalidOperationException("Couldn’t register the hot key.");
        }

        /// <summary>
        /// A hot key has been pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (int i = _currentId; i > 0; i--)
            {
                UnregisterHotKey(_window.Handle, i);
            }

            // dispose the inner native window.
            _window.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Event Args for the event that is fired after the hot key has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        private ModifierKeys _modifier;
        private Keys _key;

        internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
        {
            _modifier = modifier;
            _key = key;
        }

        public ModifierKeys Modifier
        {
            get { return _modifier; }
        }

        public Keys Key
        {
            get { return _key; }
        }
    }

    /// <summary>
    /// The enumeration of possible modifiers.
    /// </summary>
    [Flags]
    public enum ModifierKeys : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
    #endregion
}
