using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class MonitorSwap
{
    const int HOTKEY_ID = 1;
    const uint MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004;
    const int WM_HOTKEY = 0x0312, GWL_EXSTYLE = -20;
    const long WS_EX_TOOLWINDOW = 0x00000080L;

    [DllImport("user32.dll", SetLastError=true)] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr extra);
    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr extra);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int max);
    [DllImport("user32.dll", EntryPoint="GetWindowLong", SetLastError=true)] static extern int GetWindowLong32(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtr", SetLastError=true)] static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc cb, IntPtr data);
    delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

    sealed class WindowInfo { public IntPtr Handle; public int Monitor; public RECT Rect; }
    sealed class HotkeyForm : Form
    {
        NotifyIcon tray;
        public HotkeyForm()
        {
            ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.FixedToolWindow; WindowState = FormWindowState.Minimized; Opacity = 0;
            tray = new NotifyIcon();
            try { tray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application; }
            catch { tray.Icon = SystemIcons.Application; }
            tray.Text = "Monitor Swap - Ctrl+Shift+Z";

            var menu = new ContextMenuStrip();
            var credit = new ToolStripMenuItem("Build by Gamgo");
            credit.Enabled = false;
            credit.Font = new Font(menu.Font, FontStyle.Bold);
            menu.Items.Add(credit);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("창 교환: Ctrl+Shift+Z", null, (s,e)=>Swap());
            menu.Items.Add("종료", null, (s,e)=>Close());
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
        }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Hide(); if (!RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Keys.Z)) MessageBox.Show("단축키를 등록할 수 없습니다. 다른 프로그램이 Ctrl+Shift+Z를 사용 중인지 확인하세요.", "Monitor Swap"); }
        protected override void WndProc(ref Message m) { if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) Swap(); base.WndProc(ref m); }
        protected override void OnFormClosed(FormClosedEventArgs e) { UnregisterHotKey(Handle, HOTKEY_ID); tray.Visible = false; tray.Dispose(); base.OnFormClosed(e); }
        void Swap()
        {
            List<Rectangle> work = new List<Rectangle>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr mon, IntPtr dc, IntPtr r, IntPtr data) {
                MONITORINFO mi = new MONITORINFO(); mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                if (GetMonitorInfo(mon, ref mi)) work.Add(new Rectangle(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Right-mi.rcWork.Left, mi.rcWork.Bottom-mi.rcWork.Top));
                return true;
            }, IntPtr.Zero);
            if (work.Count < 2) { tray.ShowBalloonTip(1500, "Monitor Swap", "모니터가 2대 이상 필요합니다.", ToolTipIcon.Warning); return; }
            List<WindowInfo> windows = new List<WindowInfo>();
            EnumWindows(delegate(IntPtr hwnd, IntPtr extra) {
                if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
                System.Text.StringBuilder title = new System.Text.StringBuilder(512); GetWindowText(hwnd, title, title.Capacity);
                if (title.Length == 0) return true;
                long ex = IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, GWL_EXSTYLE).ToInt64() : GetWindowLong32(hwnd, GWL_EXSTYLE);
                if ((ex & WS_EX_TOOLWINDOW) != 0) return true;
                RECT r; if (!GetWindowRect(hwnd, out r)) return true;
                POINT center = new POINT { X = (r.Left+r.Right)/2, Y = (r.Top+r.Bottom)/2 };
                int index = -1;
                for (int i=0;i<work.Count;i++) { Rectangle a=work[i]; if (center.X>=a.Left && center.X<a.Right && center.Y>=a.Top && center.Y<a.Bottom) { index=i; break; } }
                if (index==0 || index==1) windows.Add(new WindowInfo { Handle=hwnd, Monitor=index, Rect=r });
                return true;
            }, IntPtr.Zero);
            foreach (WindowInfo win in windows) {
                Rectangle src=work[win.Monitor], dst=work[1-win.Monitor];
                int width=win.Rect.Right-win.Rect.Left, height=win.Rect.Bottom-win.Rect.Top;
                double rx=(double)(win.Rect.Left-src.Left)/Math.Max(1,src.Width), ry=(double)(win.Rect.Top-src.Top)/Math.Max(1,src.Height);
                int x=dst.Left+(int)Math.Round(rx*dst.Width), y=dst.Top+(int)Math.Round(ry*dst.Height);
                x=Math.Max(dst.Left, Math.Min(x, dst.Right-Math.Min(width,dst.Width)));
                y=Math.Max(dst.Top, Math.Min(y, dst.Bottom-Math.Min(height,dst.Height)));
                SetWindowPos(win.Handle, IntPtr.Zero, x, y, width, height, 0x0004 | 0x0010);
            }
        }
    }
    [STAThread] static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new HotkeyForm()); }
}