using FairyGUI;
using Main;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManager 
{
    public static async void Init()
    {
        GameSettingUtil.ApplySavedSettings();
        UIPackage.RemoveAllPackages();

        ApplyStaticTextLanguage();
        Msg.Bind((int)MsgID.LanguageChanged, OnLanguageChanged);
        //UIConfig.defaultFont = "Font2";
        //UIConfig.buttonSound = (NAudioClip)UIPackage.GetItemAssetByURL("ui://Main/buttonEff");

        MainBinder.BindAll();
        
        UIPackage.AddPackage("UI/Main");
        await StoryAniUtil.PlayStartStory();
        FGUIUtil.CreateWindow<UI_MainWin>("MainWin");
    }

    public static void ApplyStaticTextLanguage()
    {
        string resourcePath = "I18N/" + Cfg.language.ToString().ToLowerInvariant();
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogWarning($"UI i18n resource not found: {resourcePath}");
            return;
        }

        FairyGUI.Utils.XML xml = new(textAsset.text);
        UIPackage.SetStringsSource(xml);
        ResetPackageTranslationFlags();
    }

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
