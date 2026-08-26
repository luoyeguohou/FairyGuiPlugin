using FairyGUI;
using Main;
using System.Collections.Generic;

public partial class UIManager
{
    private static void ResetPackageTranslationFlags()
    {
        foreach (UIPackage package in UIPackage.GetPackages())
        {
            foreach (PackageItem item in package.GetItems())
                item.translated = false;
        }
    }

    private static void OnLanguageChanged(object[] p)
    {
        ApplyStaticTextLanguage();
        RefreshMainWindowIfVisible();
    }

    private static void RefreshMainWindowIfVisible()
    {
        UI_MainWin mainWin = GetType<UI_MainWin>();
        if (mainWin == null)
            return;

        mainWin.Dispose();
        FGUIUtil.CreateWindow<UI_MainWin>("MainWin");
    }

    public static List<FairyWindow> windows = new();

    public static FairyWindow GetCurrWindow()
    {
        if (windows.Count == 0) return null;
        return windows[^1];
    }

    public static bool IsCurrMainWin()
    {
        FairyWindow win = GetCurrWindow();
        if (win == null) return false;
        return win is UI_MainWin;
    }

    public static bool HasType<T>() where T : FairyWindow
    {
        foreach (FairyWindow win in windows)
            if (win.GetType() == typeof(T)) return true;
        return false;
    }
    public static T GetType<T>() where T : FairyWindow
    {
        foreach (FairyWindow win in windows)
            if (win.GetType() == typeof(T)) return (T)win;
        return null;
    }
}

namespace Main
{
    public partial class UI_MainWin : FairyWindow { }
}
