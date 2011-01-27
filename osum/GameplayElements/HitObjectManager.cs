#define OPTIMISED_PROCESSING

#region Using Statements

using System;
using System.Collections.Generic;
using osum.GameplayElements.HitObjects;
using osum.GameplayElements.HitObjects.Osu;
using osum.Graphics.Renderers;
using osum.GameplayElements.Beatmaps;
using osum.GameplayElements.HitObjects;
using osum.Graphics.Skins;
using osum.Graphics.Sprites;
using osum.Graphics.Renderers;
using osum.Helpers;
using osum.GameModes;
using osu_common.Helpers;
using osum.Support;
using OpenTK.Graphics;
using osum.Graphics.Primitives;
using OpenTK;

#endregion

namespace osum.GameplayElements
{
    /// <summary>
    /// Class that handles loading of content from a Beatmap, and general handling of anything that involves hitObjects as a group.
    /// </summary>
    internal partial class HitObjectManager : IDrawable, IDisposable
    {
        /// <summary>
        /// The loaded beatmap.
        /// </summary>
        Beatmap beatmap;

        /// <summary>
        /// A factory to create necessary hitObjects.
        /// </summary>
        HitFactory hitFactory;

        /// <summary>
        /// The complete list of hitObjects.
        /// </summary>
        internal pList<HitObject> hitObjects = new pList<HitObject>();
        private int hitObjectsCount;
		
		private int processFrom;

        /// <summary>
        /// Internal spriteManager for drawing all hitObject related content.
        /// </summary>
        internal SpriteManager spriteManager = new SpriteManager();

        internal SliderTrackRenderer sliderTrackRenderer = new SliderTrackRenderer();

        public HitObjectManager(Beatmap beatmap)
        {
            this.beatmap = beatmap;
            hitFactory = new HitFactoryOsu(this);

            sliderTrackRenderer.Initialize();

            ResetComboCounts();

            GameBase.OnScreenLayoutChanged += GameBase_OnScreenLayoutChanged;
        }

        void GameBase_OnScreenLayoutChanged()
        {
            foreach (HitObject h in hitObjects.FindAll(h => h is Slider))
                ((Slider)h).DisposePathTexture();
        }

        public void Dispose()
        {
            spriteManager.Dispose();

            GameBase.OnScreenLayoutChanged -= GameBase_OnScreenLayoutChanged;
        }

        /// <summary>
        /// Counter for assigning combo numbers to hitObjects during load-time.
        /// </summary>
        int currentComboNumber = 1;

        /// <summary>
        /// Index counter for assigning combo colours during load-time.
        /// </summary>
        int colourIndex = 0;

        /// <summary>
        /// Adds a new hitObject to be managed by this manager.
        /// </summary>
        /// <param name="h">The hitObject to manage.</param>
        void Add(HitObject h)
        {
            if (h.NewCombo)
            {
                currentComboNumber = 1;
                colourIndex = (colourIndex + 1 + h.ComboOffset)%TextureManager.DefaultColours.Length;
            }

            bool sameTimeAsLastAdded = hitObjectsCount != 0 && h.StartTime == hitObjects[hitObjectsCount - 1].StartTime;

            if (sameTimeAsLastAdded)
            {
                currentComboNumber = Math.Max(1, --currentComboNumber);

                HitObject hLast = hitObjects[hitObjectsCount - 1];
				
				Connect(h, hLast);
            }

            h.ComboNumber = currentComboNumber;

            if (h.IncrementCombo)
                //don't increase on simultaneous notes.
                currentComboNumber++;

            h.ColourIndex = colourIndex;

            hitObjects.AddInPlace(h);
			
			h.Index = hitObjectsCount++;

            spriteManager.Add(h);
        }
		
		void Connect(HitObject h1, HitObject h2)
		{
				Vector2 p1 = h1.SpriteCollection[0].Position;
                Vector2 p2 = h2.SpriteCollection[0].Position;

                Vector2 p3 = (p2 + p1) / 2;
                float length = (p2 - p1).Length - DifficultyManager.HitObjectRadius * 1.96f;
				
				if (length > DifficultyManager.HitObjectRadius)
				{
	                pSprite connectingLine = new pSprite(TextureManager.Load(OsuTexture.connectionline),FieldTypes.GamefieldSprites,OriginTypes.Centre,
	                    ClockTypes.Audio, p3, h1.SpriteCollection[0].DrawDepth + 0.001f, true, Color4.White);
	                connectingLine.Scale = new Vector2(length/2, DifficultyManager.HitObjectSizeModifier * 2); //div 2 because texture is 2px long.
	                connectingLine.Rotation = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
	                connectingLine.Transform(h1.SpriteCollection[0].Transformations);
	                h1.SpriteCollection.Add(connectingLine);
	                h1.DimCollection.Add(connectingLine);
					
					h1.connectedObject = h2;
					h2.connectedObject = h1;
					
					h1.connectionSprite = connectingLine;
					h2.connectionSprite = connectingLine;
				}	
		}

        #region IDrawable Members

        public bool Draw()
        {
            spriteManager.Draw();


            return true;
        }

        #endregion

        #region IUpdateable Members
		
		int processedTo;
		
        public void Update()
        {
            spriteManager.Update();


            int lowestActiveObject = -1;
			
			processedTo = hitObjectsCount - 1;
			//initialise to the last object. if we don't find an earlier one below, this wil be used.
			
#if OPTIMISED_PROCESSING
            for (int i = processFrom; i < hitObjectsCount; i++)
#else
			for (int i = 0; i < hitObjectsCount; i++)
#endif
			{
				HitObject h = hitObjects[i];

                if (h.IsVisible || !h.IsHit)
                {
                    h.Update();

                    if (Player.Autoplay && !h.IsHit && Clock.AudioTime >= h.StartTime)
                        TriggerScoreChange(h.Hit(), h);

                    TriggerScoreChange(h.CheckScoring(), h);

                    if (lowestActiveObject < 0)
                        lowestActiveObject = i;
                }
                else
                {
                    Slider s = h as Slider;
                    if (s != null && s.EndTime < Clock.AudioTime)
                        s.DisposePathTexture();
                }
				
#if OPTIMISED_PROCESSING
				if (h.StartTime > Clock.AudioTime + 4000 && !h.IsVisible)
				{
					processedTo = i;
					break; //stop processing after a decent amount of leeway...
				}
#endif
			}

            if (lowestActiveObject >= 0)
                processFrom = lowestActiveObject;

#if DEBUG
            DebugOverlay.AddLine("HitObjectManager: activeObjects[" + processFrom + ".." + processedTo + "]  (total " + hitObjectsCount + ")");
#endif
        }

        #endregion
			
		internal bool AllNotesHit
		{
			get
			{
                return hitObjects[hitObjectsCount - 1].IsHit;
			}
		}

        /// <summary>
        /// Finds an object at the specified window-space location.
        /// </summary>
        /// <returns>Found object, null on no object found.</returns>
        internal HitObject FindObjectAt(TrackingPoint tracking)
        {
#if OPTIMISED_PROCESSING
			for (int i = processFrom; i < processedTo + 1; i++)
#else
			for (int i = 0; i < hitObjectsCount; i++)
#endif
            {
                HitObject h = hitObjects[i];
				
				if (h.HitTestInitial(tracking))
                    return h;
            }
			
            return null;
        }

        internal void HandlePressAt(TrackingPoint point)
        {
            HitObject found = FindObjectAt(point);

            if (found == null) return;
			
            if (found.Index > 0)
            {
                if (Clock.AudioTime < found.StartTime - DifficultyManager.HitWindow300)
				{
					//check last hitObject has been hit already and isn't still active
	                HitObject last = hitObjects[found.Index - 1];
	                if (!last.IsHit && Clock.AudioTime < last.StartTime - DifficultyManager.HitWindow100)
	                {
	                    found.Shake();
	                    return;
	                }
				}
            }

            TriggerScoreChange(found.Hit(), found);
        }

        Dictionary<ScoreChange, int> ComboScoreCounts = new Dictionary<ScoreChange, int>();

        public event ScoreChangeDelegate OnScoreChanged;
        
        /// <summary>
        /// Cached value of the first beat length for the current beatmap. Used for general calculations (circle dimming).
        /// </summary>
        public double FirstBeatLength;

        private void TriggerScoreChange(ScoreChange change, HitObject hitObject)
        {
            if (change == ScoreChange.Ignore) return;

            ScoreChange hitAmount = change & ScoreChange.HitValuesOnly;
            
            if (hitAmount != ScoreChange.Ignore)
            {
                //handle combo additions here
                ComboScoreCounts[hitAmount] += 1;

                //is next hitObject the end of a combo?
                if (hitObject.Index == hitObjectsCount - 1 || hitObjects[hitObject.Index + 1].NewCombo)
                {
                    //apply combo addition
                    if (ComboScoreCounts[ScoreChange.Hit100] == 0 && ComboScoreCounts[ScoreChange.Hit50] == 0 && ComboScoreCounts[ScoreChange.Miss] == 0)
                        change |= ScoreChange.GekiAddition;
                    else if (ComboScoreCounts[ScoreChange.Miss] == 0)
                        change |= ScoreChange.KatuAddition;
                    else
                        change |= ScoreChange.MuAddition;

                    ResetComboCounts();
                }
            }
            

            hitObject.HitAnimation(change);

            if (OnScoreChanged != null)
                OnScoreChanged(change, hitObject);
        }

        private void ResetComboCounts()
        {
            ComboScoreCounts[ScoreChange.Miss] = 0;
            ComboScoreCounts[ScoreChange.Hit50] = 0;
            ComboScoreCounts[ScoreChange.Hit100] = 0;
            ComboScoreCounts[ScoreChange.Hit300] = 0;
        }

        internal double VelocityAt(int time)
        {
            return (100000.0f * beatmap.DifficultySliderMultiplier / beatmap.beatLengthAt(time, true));
        }

        internal double ScoringDistanceAt(int time)
        {
            return ((100 * beatmap.DifficultySliderMultiplier / beatmap.bpmMultiplierAt(time)) / beatmap.DifficultySliderTickRate);
        }
    }

    internal enum FileSection
    {
        Unknown,
        General,
        Colours,
        Editor,
        Metadata,
        TimingPoints,
        Events,
        HitObjects,
        Difficulty,
        Variables
    } ;
}