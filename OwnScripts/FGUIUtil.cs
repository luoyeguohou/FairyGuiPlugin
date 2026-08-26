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

    /// <summary>
    /// GTextField.text (getter) returns the raw authored template, e.g. "id:{id=xxx}", until
    /// SetVar/FlushVars has run. So checking it here at the call site tells us, for this exact
    /// field, whether it was built with a {var} template or is just plain text - no need to
    /// guess from the .fui binary.
    /// </summary>
    public static void SetText(GTextField field, string varName, string value)
    {
        if (!string.IsNullOrEmpty(field.text) && field.text.Contains("{"))
            field.SetVar(varName, value).FlushVars();
        else
            field.text = value;
    }

    /// <summary>
    /// GComboBox needs its own "dropdown" resource wired up in FairyGUI (a component containing
    /// a GList literally named "list") before it can show anything - if that was never set up for
    /// a given ComboBox instance, clicking it opens nothing and .items/.values sit there unused.
    /// This bypasses that requirement entirely: it drives the combo via FairyGUI's generic
    /// PopupMenu instead, reusing UI_ComboBox1_popup (the default popup template FairyGUI already
    /// generated the first time a ComboBox was ever placed in this project) as the popup's
    /// visual. Every future combo can just call this and never needs any FairyGUI-side dropdown
    /// setup at all.
    /// </summary>
    public static void AttachDropdown(GComboBox combo, string[] items, string[] values, Action onChanged = null)
    {
        combo.items = items;
        combo.values = values;

        // GComboBox wires its own native dropdown-opening logic in ConstructExtension via
        // displayObject.onTouchBegin (not onClick, and not removable from outside code) - it
        // fires ShowDropdown() whenever combo.dropdown is non-null. For this project's combos
        // that resource is assigned but misconfigured, so leaving it in place meant BOTH the
        // broken native popup (onTouchBegin) and this PopupMenu (onClick) opened on the same
        // click, as two separate, differently-sized popups. Nulling the public `dropdown` field
        // disables that native path for good, leaving only the PopupMenu below.
        combo.dropdown = null;

        // Safe to clear: this only ever removes a handler this same method added on a previous
        // call (this gets called again each time the combo's options are refreshed) - without
        // it, repeated calls would stack up duplicate handlers and pop several menus at once.
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

    /// <summary>Same as AttachDropdown(items, values), for the common case where what's shown
    /// and what's stored are the same raw id (e.g. a ContentId combo with no separate display
    /// name) - no need to pass the same array in twice.</summary>
    public static void AttachDropdown(GComboBox combo, string[] values, Action onChanged = null) =>
        AttachDropdown(combo, values, values, onChanged);
}
