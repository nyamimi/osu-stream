﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Support;
using osum.Audio;

namespace osum.GameplayElements
{
    internal delegate void HitCircleDelegate(HitObject h);

    [Flags]
    internal enum HitObjectType
    {
        Circle = 1,
        Slider = 2,
        NewCombo = 4,
        NormalNewCombo = 5,
        SliderNewCombo = 6,
        Spinner = 8
    }

    [Flags]
    internal enum HitObjectSoundType
    {
        Normal = 0,
        Whistle = 2,
        Finish = 4,
        WhistleFinish = 6,
        Clap = 8
    }

    [Flags]
    internal enum IncreaseScoreType
    {
        MissHpOnlyNoCombo = -524288,
        MissHpOnly = -262144,
        Miss = -131072,
        Ignore = 0,
        MuAddition = 1,
        KatuAddition = 2,
        GekiAddition = 4,
        SliderTick = 8,
        FruitTickTiny = 16,
        FruitTickTinyMiss = 32,
        SliderRepeat = 64,
        SliderEnd = 128,
        Hit50 = 256,
        Hit100 = 512,
        Hit300 = 1024,
        Hit50m = Hit50 | MuAddition,
        Hit100m = Hit100 | MuAddition,
        Hit300m = Hit300 | MuAddition,
        Hit100k = Hit100 | KatuAddition,
        Hit300k = Hit300 | KatuAddition,
        Hit300g = Hit300 | GekiAddition,
        FruitTick = 2048,
        SpinnerSpin = 4096,
        SpinnerSpinPoints = 8192,
        SpinnerBonus = 16384,
        TaikoDrumRoll = 32768,
        TaikoLargeHitBoth = 65536,
        TaikoDenDenHit = 1048576,
        TaikoDenDenComplete = 2097152,
        TaikoLargeHitFirst = 4194304,
        TaikoLargeHitSecond = 8388608,
        HitValuesOnly = Hit50 | Hit100 | Hit300 | GekiAddition | KatuAddition,
        ComboAddition = MuAddition | KatuAddition | GekiAddition,
        NonScoreModifiers = TaikoLargeHitBoth | TaikoLargeHitFirst | TaikoLargeHitSecond
    }

    internal abstract class HitObject : pSpriteCollection, IComparable<HitObject>, IComparable<int>, IUpdateable
    {
        #region General & Timing

        internal int StartTime;
        internal int EndTime;

        internal IncreaseScoreType hitValue;

        internal HitObjectType Type;

        public virtual void Update()
        {
        }

        internal virtual bool NewCombo { get; set; }

        private Color4 colour;
        internal virtual Color4 Colour
        {
            get
            {
                return colour;
            }

            set
            {
                colour = value;

                float dimFactor = 0.75f;
                ColourDim = new Color4(colour.R * dimFactor, colour.G * dimFactor, colour.B * dimFactor, 255);
            }
        }


        internal bool IsHit { get; private set; }
        
        internal virtual IncreaseScoreType Hit()
        {
            IsHit = true;
            return IncreaseScoreType.Ignore;
        }

        internal virtual void Dispose()
        {
        }

        internal abstract HitObject Clone();

        #endregion

        #region Drawing

        /// <summary>
        /// Sprites which should be dimmed when not the active object.
        /// </summary>
        protected internal List<pSprite> DimCollection = new List<pSprite>();

        internal Vector2 Position;
        internal int StackCount;

        internal abstract int ComboNumber { get; set; }

        /// <summary>
        /// Id this hitObject visible at the current audio time?
        /// </summary>
        internal abstract bool IsVisible { get; }

        #endregion

        #region Sound

        internal Color4 ColourDim;
        internal bool Dimmed;
        internal bool Sounded;
        internal HitObjectSoundType SoundType;
        internal bool Drawable;
        /// <summary>
        /// Whether to add this object's score to the counters (hit300 count etc.)
        /// </summary>
        public bool IsScorable = true;
        public int TagNumeric;
        public int scoreValue;
        public bool LastInCombo;

        internal bool Whistle
        {
            get { return (HitObjectSoundType.Whistle & SoundType) > 0; }
            set
            {
                if (value)
                    SoundType |= HitObjectSoundType.Whistle;
                else
                    SoundType &= ~HitObjectSoundType.Whistle;
            }
        }


        internal bool Finish
        {
            get { return (HitObjectSoundType.Finish & SoundType) > 0; }
            set
            {
                if (value)
                    SoundType |= HitObjectSoundType.Finish;
                else
                    SoundType &= ~HitObjectSoundType.Finish;
            }
        }

        internal bool Clap
        {
            get { return (HitObjectSoundType.Clap & SoundType) > 0; }
            set
            {
                if (value)
                    SoundType |= HitObjectSoundType.Clap;
                else
                    SoundType &= ~HitObjectSoundType.Clap;
            }
        }

        internal virtual void PlaySound()
        {
            
            //HitObjectManager.OnHitSound(SoundType);

            if ((SoundType & HitObjectSoundType.Finish) > 0)
                AudioEngine.PlaySample(OsuSamples.HitFinish);
                //AudioEngine.PlaySample(AudioEngine.s_HitFinish, AudioEngine.VolumeSample, 0, PositionalSound);

            if ((SoundType & HitObjectSoundType.Whistle) > 0)
                AudioEngine.PlaySample(OsuSamples.HitWhistle);

            if ((SoundType & HitObjectSoundType.Clap) > 0)
                AudioEngine.PlaySample(OsuSamples.HitClap);

            //if (SkinManager.Current.LayeredHitSounds || SoundType == HitObjectSoundType.Normal)
            AudioEngine.PlaySample(OsuSamples.HitNormal);
            
        }

        protected virtual float PositionalSound { get { return Position.X / 512f - 0.5f; } }

        /// <summary>
        /// Gets the hittable end time (valid active object time for sliders etc. - used in taiko to extend when hits are valid).
        /// </summary>
        /// <value>The hittable end time.</value>
        internal virtual int HittableEndTime
        {
            get { return EndTime; }
        }

        /// <summary>
        /// Gets the hittable end time (valid active object time for sliders etc. - used in taiko to extend when hits are valid).
        /// </summary>
        /// <value>The hittable end time.</value>
        internal virtual int HittableStartTime
        {
            get { return StartTime; }
        }

        #endregion

        #region IComparable<HitObject> Members

        public int CompareTo(HitObject other)
        {
            return EndTime.CompareTo(other.EndTime);
        }

        #endregion

        internal abstract IncreaseScoreType GetScorePoints(Vector2 currentMousePos);

        internal virtual void StopSound()
        {
        }

        internal abstract void SetEndTime(int time);

        public int CompareTo(int other)
        {
            return EndTime.CompareTo(other);
        }

        internal virtual bool HitTest(TrackingPoint tracking)
        {
            float radius = 50;

            return (IsVisible ||
                  (StartTime - DifficultyManager.PreEmpt <= Clock.AudioTime &&
                   StartTime + DifficultyManager.HitWindow50 >= Clock.AudioTime && !IsHit)) &&
                 (pMathHelper.DistanceSquared(tracking.GamefieldPosition, Position) <= radius * radius
                 //||                  (pMathHelper.DistanceSquared(tracking.GamefieldPosition, Position2) <= radius * radius)
                  );
        }

        internal virtual void Shake()
        {
            foreach (pSprite p in SpriteCollection)
            {
                Transformation previousShake = p.Transformations.FindLast(t => t.Type == TransformationType.Movement);

                Vector2 startPos = previousShake != null ? previousShake.EndVector : p.Position;

                p.Transform(new Transformation(startPos, startPos + new Vector2(8, 0),
                    Clock.AudioTime, Clock.AudioTime + 20));
                p.Transform(new Transformation(startPos + new Vector2(8, 0), startPos - new Vector2(8, 0),
                    Clock.AudioTime + 20, Clock.AudioTime + 40));
                p.Transform(new Transformation(startPos - new Vector2(8, 0), startPos + new Vector2(8, 0),
                    Clock.AudioTime + 40, Clock.AudioTime + 60));
                p.Transform(new Transformation(startPos + new Vector2(8, 0), startPos - new Vector2(8, 0),
                    Clock.AudioTime + 60, Clock.AudioTime + 80));
                p.Transform(new Transformation(startPos + new Vector2(8, 0), startPos - new Vector2(8, 0),
                    Clock.AudioTime + 80, Clock.AudioTime + 100));
                p.Transform(new Transformation(startPos + new Vector2(8, 0), startPos,
                    Clock.AudioTime + 100, Clock.AudioTime + 120));
            }
        }

        public override string ToString()
        {
            return this.Type + ": " + this.StartTime + "-" + this.EndTime + " stack:" + this.StackCount;
        }

        public int Length { get { return EndTime - StartTime; } }
    }
}
