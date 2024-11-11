using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Airpass.Utility;
using Debug = UnityEngine.Debug;

public class DisplayManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    static extern bool MoveWindow( IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint );
    
    public List<DisplayLayout> displaylayouts;
    
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private Dictionary<string, List<Camera>> _displayCameras = new();
    private List<IntPtr> _unityWindowHandles = new();
    private string _enumedHwndTitle;

    public bool MoveDisplay(int displayIndex, RectInt targetRect, bool repaint = false) {
        if (displayIndex < 0 || displayIndex >= _unityWindowHandles.Count)
            return false;

        MoveWindow(_unityWindowHandles[displayIndex], 
            targetRect.x, 
            targetRect.y,
            targetRect.width, 
            targetRect.height,
            repaint);

        return true;
    }
    
    private bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
    {
        if (IsWindowVisible(hWnd))
        {
            StringBuilder windowTitle = new StringBuilder(256);
            GetWindowText(hWnd, windowTitle, windowTitle.Capacity);
            string title = windowTitle.ToString();
            if (title == "Unity Secondary Display" && !_unityWindowHandles.Contains(hWnd))
            {
                if (_enumedHwndTitle == Application.productName)
                {
                    _unityWindowHandles.Add(hWnd);
                }
            }
            else
            {
                _enumedHwndTitle = title;
            }
        }

        return true;
    }

    public void Initialize()
    {
        foreach (var camera in Camera.allCameras)
        {
            if (camera.targetDisplay < displaylayouts.Count)
            {
                string id = displaylayouts[camera.targetDisplay].id;
                _displayCameras.TryAdd(id, new List<Camera>());
                _displayCameras[id].Add(camera);
            }
        }

        // Add main display window handle.
        _unityWindowHandles.Add(Process.GetCurrentProcess().MainWindowHandle);
        for (int i = 0; i < Mathf.Min(Display.displays.Length, displaylayouts.Count); ++i)
        {
            Display.displays[i].Activate();
            EnumWindows(EnumWindowsCallback, IntPtr.Zero);
        }
    }

    void Start()
    {
        var cmdlArgs = Environment.GetCommandLineArgs();
        if (cmdlArgs.Length > 1)
        {
            if (cmdlArgs[1].Contains(')'))
            {
                displaylayouts.Clear();
                string[] rectString = cmdlArgs[1].Split(')');
                for (int i = 0; i < rectString.Length; ++i)
                {
                    rectString[i] = rectString[i].Replace("(", string.Empty);
                    if (rectString[i].Contains(','))
                    {
                        var xywhString = rectString[i].Split(',');
                        if (xywhString.Length == 4)
                        {
                            displaylayouts.Add(new DisplayLayout
                            {
                                id = i.ToString(),
                                rect = new RectInt(int.TryParse(xywhString[0].Split(':')[1], out int x) ? x : 0,
                                    int.TryParse(xywhString[1].Split(':')[1], out int y) ? y : 0,
                                    int.TryParse(xywhString[2].Split(':')[1], out int w) ? w : 0,
                                    int.TryParse(xywhString[3].Split(':')[1], out int h) ? h : 0)
                            });
                        }
                    }
                }
            }
        }
        
        Initialize();

        MoveWindow();
        float timer = Time.time;
        this.LoopUntil(() => Time.time - timer > 5.0f, MoveWindow);
    }

    private void MoveWindow()
    {
        for (int i = 0; i < Mathf.Min(Display.displays.Length, displaylayouts.Count); ++i)
        {
            Debug.Log($"Move [{i}] : {_unityWindowHandles[i]} : ({Process.GetCurrentProcess().MainWindowHandle}) to : {displaylayouts[i].rect}");
            MoveDisplay(i, displaylayouts[i].rect);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        MoveWindow();
    }
}

[Serializable]
public struct DisplayLayout
{
    public string id;
    public RectInt rect;
}
