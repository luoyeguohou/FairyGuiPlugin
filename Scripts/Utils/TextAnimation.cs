using System;
using System.Collections.Generic;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>Inline UBB effects and per-character float-in for ordinary and rich text fields.</summary>
    public static class TextAnimation
    {
        internal const int Shake = 1, Wave = 2, Pulse = 4, Rainbow = 8;
        private static readonly Dictionary<GTextField, TextAnimationPlayer> Players = new Dictionary<GTextField, TextAnimationPlayer>();
        private static UBBParser registeredParser;

        /// <summary>Register effect tags. Animation still starts through SetText or FloatIn.</summary>
        public static void Initialize() => RegisterTags();

        /// <summary>Show the full text immediately and animate its [shake/wave/pulse/rainbow] spans.</summary>
        public static TextAnimationPlayer SetText(GTextField target, string text)
        {
            return Play(target, text, false, 0, 0, 0);
        }

        /// <summary>Float each character upwards into its final position, with a fade-in.</summary>
        public static TextAnimationPlayer FloatIn(GTextField target, string text,
            float interval = 0.025f, float duration = 0.12f, float distance = 8)
        {
            return Play(target, text, true, interval, duration, distance);
        }

        /// <summary>Float in the component's current text, including template variables.</summary>
        public static TextAnimationPlayer FloatIn(GTextField target,
            float interval = 0.025f, float duration = 0.12f, float distance = 8)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return FloatIn(target, target.text, interval, duration, distance);
        }

        /// <summary>Reveal everything immediately; inline effects continue.</summary>
        public static void Complete(GTextField target)
        {
            if (target != null && Players.TryGetValue(target, out TextAnimationPlayer player))
                player.Complete();
        }

        /// <summary>Reveal everything and remove all animation from this component.</summary>
        public static void Stop(GTextField target)
        {
            if (target != null && Players.TryGetValue(target, out TextAnimationPlayer player))
                player.Dispose();
        }

        internal static void Remove(GTextField target, TextAnimationPlayer player)
        {
            if (Players.TryGetValue(target, out TextAnimationPlayer current) && ReferenceEquals(current, player))
                Players.Remove(target);
        }

        private static TextAnimationPlayer Play(GTextField target, string text, bool reveal,
            float interval, float duration, float distance)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target.isDisposed) throw new ObjectDisposedException(nameof(target));
            if (target is GTextInput) throw new ArgumentException("Text animations are for display text, not input fields.", nameof(target));
            if (!IsFinite(interval) || !IsFinite(duration) || !IsFinite(distance) || interval < 0 || duration < 0)
                throw new ArgumentOutOfRangeException(nameof(interval), "Animation timing must be finite and non-negative.");

            Stop(target);
            RegisterTags();
            target.UBBEnabled = true;
            target.text = text ?? string.Empty;
            var player = new TextAnimationPlayer(target, reveal, interval, duration, distance);
            Players.Add(target, player);
            player.Start();
            return player;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void RegisterTags()
        {
            if (ReferenceEquals(registeredParser, UBBParser.inst)) return;
            registeredParser = UBBParser.inst;
            registeredParser.handlers["shake"] = EffectTag;
            registeredParser.handlers["wave"] = EffectTag;
            registeredParser.handlers["pulse"] = EffectTag;
            registeredParser.handlers["rainbow"] = EffectTag;
        }

        private static string EffectTag(string tag, bool end, string attribute)
        {
            if (end) return "</font>";
            int effect = tag == "shake" ? Shake : tag == "wave" ? Wave : tag == "pulse" ? Pulse : Rainbow;
            return "<font effect=\"" + effect + "\">";
        }
    }

    /// <summary>A single component's animation; automatically cleaned up when removed or replaced.</summary>
    public sealed class TextAnimationPlayer : IDisposable
    {
        private readonly GTextField target;
        private readonly TextField textField;
        private readonly string sourceText;
        private readonly float duration;
        private readonly float distance;
        private readonly float[] revealTimes;
        private readonly float revealEnd;
        private readonly bool hasEffects;
        private bool revealing;
        private bool disposed;
        private float elapsed;

        public bool IsPlaying => !disposed;
        public bool IsRevealing => !disposed && revealing && elapsed < revealEnd;

        internal TextAnimationPlayer(GTextField target, bool reveal, float interval, float duration, float distance)
        {
            this.target = target;
            this.duration = duration;
            this.distance = distance;
            revealing = reveal;
            textField = target is GRichTextField rich ? rich.richTextField.textField : (TextField)target.displayObject;
            if (textField.glyphModifier != null)
                throw new InvalidOperationException("This text field already has a glyph modifier.");
            textField.Redraw();
            sourceText = textField.text;

            string parsed = textField.parsedText;
            revealTimes = new float[parsed.Length];
            int count = 0;
            for (int i = 0; i < parsed.Length; i++)
            {
                revealTimes[i] = count * interval;
                if (!char.IsWhiteSpace(parsed[i])) count++;
            }
            revealEnd = count == 0 ? 0 : (count - 1) * interval + duration;
            hasEffects = textField.textFormat.effectFlags != 0;
            foreach (HtmlElement element in textField.htmlElements)
                if (element.type == HtmlElementType.Text && element.format.effectFlags != 0) hasEffects = true;
        }

        internal void Start()
        {
            textField.glyphModifier = ModifyGlyph;
            target.onRemovedFromStage.Add(OnRemoved);
            Redraw();
            if ((!revealing || revealEnd <= 0) && !hasEffects)
                Dispose();
            else if (Application.isPlaying)
            {
                Timers.inst.AddUpdate(Update);
                ticking = true;
            }
        }

        private bool ticking;

        private void Update(object unused)
        {
            if (target.isDisposed || textField.isDisposed || textField.text != sourceText)
            {
                Dispose();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            if (revealing && elapsed >= revealEnd)
            {
                revealing = false;
                if (!hasEffects)
                {
                    Dispose();
                    return;
                }
            }
            Redraw();
        }

        private void Redraw()
        {
            textField.graphics.SetMeshDirty();
            textField.Redraw();
            target.InvalidateBatchingState();
        }

        private void ModifyGlyph(VertexBuffer buffer, int start, int count, int charIndex, TextFormat format)
        {
            if (count == 0) return;
            float alpha = 1;
            Vector3 offset = Vector3.zero;
            if (revealing)
            {
                float delay = charIndex < revealTimes.Length ? revealTimes[charIndex] : revealEnd;
                float progress = duration <= 0 ? (elapsed >= delay ? 1 : 0) : Mathf.Clamp01((elapsed - delay) / duration);
                alpha = 1 - Mathf.Pow(1 - progress, 3);
                offset.y -= distance * (1 - alpha);
            }

            int flags = format.effectFlags;
            float phase = charIndex * 0.65f;
            if ((flags & TextAnimation.Shake) != 0)
            {
                offset.x += Mathf.Sin(elapsed * 61 + phase * 2.7f) * 1.2f;
                offset.y += Mathf.Sin(elapsed * 79 + phase * 1.9f) * 1.2f;
            }
            if ((flags & TextAnimation.Wave) != 0)
                offset.y += Mathf.Sin(elapsed * 4 + phase) * 3;
            float scale = (flags & TextAnimation.Pulse) != 0 ? 1 + Mathf.Sin(elapsed * Mathf.PI) * 0.08f : 1;
            Color32 rainbow = Color.HSVToRGB(Mathf.Repeat(elapsed * 0.2f + charIndex * 0.045f, 1), 0.75f, 1);

            Vector3 min = buffer.vertices[start], max = min;
            for (int i = start + 1; i < start + count; i++)
            {
                min = Vector3.Min(min, buffer.vertices[i]);
                max = Vector3.Max(max, buffer.vertices[i]);
            }
            Vector3 center = (min + max) * 0.5f;
            for (int i = start; i < start + count; i++)
            {
                buffer.vertices[i] = center + (buffer.vertices[i] - center) * scale + offset;
                Color32 color = buffer.colors[i];
                if ((flags & TextAnimation.Rainbow) != 0)
                {
                    color.r = rainbow.r;
                    color.g = rainbow.g;
                    color.b = rainbow.b;
                }
                color.a = (byte)(color.a * alpha);
                buffer.colors[i] = color;
            }
            buffer._alphaInVertexColor = true;
        }

        public void Complete()
        {
            if (disposed) return;
            revealing = false;
            if (!hasEffects) Dispose();
            else Redraw();
        }

        private void OnRemoved() => Dispose();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ticking) Timers.inst.Remove(Update);
            target.onRemovedFromStage.Remove(OnRemoved);
            TextAnimation.Remove(target, this);
            if (!textField.isDisposed && textField.glyphModifier == ModifyGlyph)
            {
                textField.glyphModifier = null;
                if (!target.isDisposed) Redraw();
            }
        }
    }
}

