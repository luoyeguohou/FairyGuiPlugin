using FairyGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Main;
using System;
using System.Threading.Tasks;
using UnityEngine.UIElements;

public partial class FGUIUtil
{
    public static void SetWorldPos(GObject g, Vector3 pos)
    {
        g.position = g.parent.GlobalToLocal(pos);
    }
    public static Vector3 GetWorldPos(GObject g)
    {
        return g.LocalToGlobal(new Vector3());
    }

    public static void SetSamePos(GObject follower, GObject aim)
    {
        SetWorldPos(follower, GetWorldPos(aim));
    }

    public static T CreateWindow<T>(string name) where T : FairyWindow
    {
        GComponent gcom = UIPackage.CreateObject("Main", name).asCom;
        GRoot.inst.AddChild(gcom);
        gcom.MakeFullScreen();
        return (T)gcom;
    }

    public static void RefreshAllWindowsLayout()
    {
        if (GRoot.inst == null)
            return;

        GRoot.inst.ApplyContentScaleFactor();
        foreach (FairyWindow win in UIManager.windows.ToArray())
        {
            if (win == null || win.isDisposed)
                continue;

            win.MakeFullScreen();
        }
    }

    public static void ClearHint(GObject g)
    {
        g.onRollOver.Clear();
        g.onRollOut.Clear();
    }

    public static void SetText(GTextField field, string varName, string value)
    {
        if (!string.IsNullOrEmpty(field.text) && field.text.Contains("{"))
            field.SetVar(varName, value).FlushVars();
        else
            field.text = value;
    }

    public static void AttachDropdown(GComboBox combo, string[] items, string[] values, Action onChanged = null)
    {
        combo.items = items;
        combo.values = values;

        combo.dropdown = null;

        combo.onClick.Clear();
        combo.onClick.Add(() =>
        {
            var menu = new PopupMenu(UI_ComboBox1_popup.URL);
            for (int i = 0; i < items.Length; i++)
            {
                string value = values[i];
                menu.AddItem(items[i], () =>
                {
                    combo.value = value;
                    onChanged?.Invoke();
                });
            }
            menu.Show(combo);
        });
    }

    public static void AttachDropdown(GComboBox combo, string[] values, Action onChanged = null) =>
        AttachDropdown(combo, values, values, onChanged);
}
