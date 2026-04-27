using System;
using UnityEngine;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    internal enum ExplosionSoundSource
    {
        Unknown,
        Grenade,
        Breakable,
        Proxy,
    }

    internal sealed class ExplosionSoundFrame
    {
        public static readonly ExplosionSoundFrame Unknown = new ExplosionSoundFrame(ExplosionSoundSource.Unknown, null, null);

        public ExplosionSoundFrame(ExplosionSoundSource source, string? typeIdStr, GameObject? sourceObject)
        {
            Source = source;
            TypeIdStr = string.IsNullOrWhiteSpace(typeIdStr) ? null : typeIdStr;
            SourceObject = sourceObject;
        }

        public ExplosionSoundSource Source { get; }
        public string? TypeIdStr { get; }
        public GameObject? SourceObject { get; }
        public string? ObservedSoundKey { get; private set; }
        public bool ObservedOriginalEvent { get; private set; }

        public void MarkObserved(string soundKey)
        {
            ObservedOriginalEvent = true;
            if (!string.IsNullOrWhiteSpace(soundKey))
            {
                ObservedSoundKey = soundKey;
            }
        }
    }

    internal readonly struct ExplosionSoundScope
    {
        private readonly ExplosionSoundFrame? previous;

        public ExplosionSoundScope(ExplosionSoundFrame? previous, ExplosionSoundFrame current)
        {
            this.previous = previous;
            Current = current;
        }

        public ExplosionSoundFrame? Current { get; }

        public void Restore()
        {
            ExplosionSoundContext.Restore(previous);
        }
    }

    internal static class ExplosionSoundContext
    {
        [ThreadStatic]
        private static ExplosionSoundFrame? current;

        public static ExplosionSoundFrame Current => current ?? ExplosionSoundFrame.Unknown;

        public static ExplosionSoundScope Enter(ExplosionSoundSource source, string? typeIdStr, GameObject? sourceObject)
        {
            var previous = current;
            var next = new ExplosionSoundFrame(source, typeIdStr, sourceObject);
            current = next;
            return new ExplosionSoundScope(previous, next);
        }

        public static void Restore(ExplosionSoundFrame? previous)
        {
            current = previous;
        }

        public static void MarkObserved(string soundKey)
        {
            current?.MarkObserved(soundKey);
        }
    }
}
