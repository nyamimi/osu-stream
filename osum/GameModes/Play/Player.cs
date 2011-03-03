//  Play.cs
//  Author: Dean Herbert <pe@ppy.sh>
//  Copyright (c) 2010 2010 Dean Herbert
using System;
using osum.GameplayElements;
using osum.GameplayElements.Beatmaps;
using osum.Helpers;
//using osu.Graphics.Renderers;
using osum.Graphics.Primitives;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using System.Drawing;
using osum.Audio;
using osum.Graphics.Renderers;
using osum.GameplayElements.Scoring;
using osum.GameModes.Play.Components;
using osum.Graphics.Sprites;
using osum.Graphics.Skins;

namespace osum.GameModes
{
    public class Player : GameMode
    {
        HitObjectManager hitObjectManager;

        HealthBar healthBar;
        ScoreDisplay scoreDisplay;
        ComboCounter comboCounter;

        Score currentScore;

        static Beatmap Beatmap;
        public static bool Autoplay;
        private PlayfieldBackground s_Playfield;


        public Player()
            : base()
        {
        }

        void InputManager_OnDown(InputSource source, TrackingPoint point)
        {
            //pass on the event to hitObjectManager for handling.
            hitObjectManager.HandlePressAt(point);

            if (InputManager.TrackingPoints.Count == 2)
            {
                Vector2 p1 = InputManager.TrackingPoints[0].WindowPosition;
                Vector2 p2 = InputManager.TrackingPoints[1].WindowPosition;

                if (Math.Max(p1.X, p2.X) > (GameBase.BaseSize.Width - 40) &&
                    Math.Min(p1.X, p2.X) < 40 &&
                    p1.Y + p2.Y < 80)
                {
                    Director.ChangeMode(OsuMode.SongSelect);
                }
                else if (Math.Max(p1.X, p2.X) > (GameBase.BaseSize.Width - 40) &&
                    Math.Min(p1.X, p2.X) < 40 &&
                    p1.Y + p2.Y > (GameBase.BaseSize.Height * 2) - 40)
                {
                    Player.Autoplay = !Autoplay;
                }

            }
        }

        int lastSeek;
        void InputManager_OnMove(InputSource source, TrackingPoint point)
        {
            // fast forward for iphone
            if (InputManager.TrackingPoints.Count >= 4 && Clock.Time - lastSeek > 250)
            {
                lastSeek = Clock.Time;
                AudioEngine.Music.SeekTo(Clock.AudioTime + 2000);
            }
        }

        internal override void Initialize()
        {
            TextureManager.PopulateSurfaces();

            InputManager.OnDown += InputManager_OnDown;
            InputManager.OnMove += InputManager_OnMove;

            TextureManager.RequireSurfaces = true;

            hitObjectManager = new HitObjectManager(Beatmap);
            hitObjectManager.OnScoreChanged += hitObjectManager_OnScoreChanged;

            hitObjectManager.LoadFile();

            hitObjectManager.ActiveStream = Difficulty.Normal;

            healthBar = new HealthBar();

            scoreDisplay = new ScoreDisplay();

            comboCounter = new ComboCounter();

            currentScore = new Score();

            s_Playfield = new PlayfieldBackground();
            spriteManager.Add(s_Playfield);

            AudioEngine.Music.Load(Beatmap.GetFileBytes(Beatmap.AudioFilename), false);
            Director.OnTransitionEnded += new VoidDelegate(Director_OnTransitionEnded);

            if (fpsTotalCount != null)
            {
                fpsTotalCount.AlwaysDraw = false;
                fpsTotalCount = null;
            }

            gcAtStart = GC.CollectionCount(0);
        }

        void Director_OnTransitionEnded()
        {
            AudioEngine.Music.Play();
        }

        void hitObjectManager_OnScoreChanged(ScoreChange change, HitObject hitObject)
        {
            if (currentScore.totalHits == 0 && change > 0)
                s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_STANDARD);

            //handle the score addition
            switch (change & ~ScoreChange.ComboAddition)
            {
                case ScoreChange.SpinnerBonus:
                    currentScore.totalScore += 1000;
                    break;
                case ScoreChange.SpinnerSpinPoints:
                    currentScore.totalScore += 500;
                    break;
                case ScoreChange.SpinnerSpin:
                    break;
                case ScoreChange.SliderRepeat:
                case ScoreChange.SliderEnd:
                    currentScore.totalScore += 30;
                    comboCounter.IncreaseCombo();
                    break;
                case ScoreChange.SliderTick:
                    currentScore.totalScore += 10;
                    comboCounter.IncreaseCombo();
                    break;
                case ScoreChange.Hit50:
                    currentScore.totalScore += 50;
                    currentScore.count50++;
                    comboCounter.IncreaseCombo();
                    break;
                case ScoreChange.Hit100:
                    currentScore.totalScore += 100;
                    currentScore.count100++;
                    comboCounter.IncreaseCombo();
                    break;
                case ScoreChange.Hit300:
                    currentScore.totalScore += 300;
                    currentScore.count300++;
                    comboCounter.IncreaseCombo();
                    break;
                case ScoreChange.Miss:
                    currentScore.countMiss++;
                    comboCounter.SetCombo(0);
                    break;
            }

            //then handle the hp addition
            switch (change)
            {
                case ScoreChange.Miss:
                    healthBar.ReduceCurrentHp(20);
                    break;
                default:
                    healthBar.IncreaseCurrentHp(5);
                    break;
            }

            scoreDisplay.SetScore(currentScore.totalScore);
            scoreDisplay.SetAccuracy(currentScore.accuracy * 100);
        }

        static pSprite fpsTotalCount;
        int gcAtStart;

        public override void Dispose()
        {
            InputManager.OnDown -= InputManager_OnDown;

            TextureManager.RequireSurfaces = false;

            hitObjectManager.Dispose();

            healthBar.Dispose();
            scoreDisplay.Dispose();

            base.Dispose();

            //Performance testing code.
            fpsTotalCount = new pText("Total Player.cs frames: " + frameCount + " of " + Math.Round(msCount / 16.666667f) + " (GC: " + (GC.CollectionCount(0) - gcAtStart) + ")", 16, new Vector2(0, 100), new Vector2(512, 256), 0, false, Color4.White, false);
            fpsTotalCount.FadeOutFromOne(15000);
            GameBase.Instance.MainSpriteManager.Add(fpsTotalCount);
        }

        int frameCount = 0;
        double msCount = 0;

        public override bool Draw()
        {
            base.Draw();

            hitObjectManager.Draw();

            scoreDisplay.Draw();
            healthBar.Draw();
            comboCounter.Draw();

            frameCount++;

            msCount += GameBase.ElapsedMilliseconds;

            return true;
        }

        bool stateCompleted; //todo: make this an enum state

        public override void Update()
        {
            //check whether the map is finished
            if (hitObjectManager.AllNotesHit && !Director.IsTransitioning && !stateCompleted)
            {
                stateCompleted = true;
                GameBase.Scheduler.Add(delegate
                {
                    Ranking.RankableScore = currentScore;
                    Director.ChangeMode(OsuMode.Ranking);
                }, 2000);
            }

            hitObjectManager.Update();

            healthBar.Update();

            if (hitObjectManager.ActiveStream == Difficulty.Easy && healthBar.CurrentHp < HealthBar.HP_BAR_MAXIMUM)
            {
                if (healthBar.CurrentHp == 0)
                {
                    s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_INTRO);

                    if (!stateCompleted)
                    {
                        stateCompleted = true;

                        AudioEngine.Music.Pause();
                        GameBase.Scheduler.Add(delegate
                        {
                            Ranking.RankableScore = currentScore;
                            Director.ChangeMode(OsuMode.SongSelect);
                        }, 2000);
                    }
                } 
                else if (healthBar.CurrentHp < HealthBar.HP_BAR_MAXIMUM / 3)
                    s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_WARNING);
                else
                    s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_EASY);

                
            }
            else if (healthBar.CurrentHp == HealthBar.HP_BAR_MAXIMUM)
            {
                switch (hitObjectManager.ActiveStream)
                {
                    case Difficulty.Easy:
                        hitObjectManager.ActiveStream = Difficulty.Normal;
                        s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_STANDARD);
                        healthBar.SetCurrentHp(HealthBar.HP_BAR_MAXIMUM / 2);
                        break;
                    case Difficulty.Normal:
                        hitObjectManager.ActiveStream = Difficulty.Hard;
                        s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_HARD);
                        healthBar.SetCurrentHp(HealthBar.HP_BAR_MAXIMUM / 2);
                        break;
                }
            }
            else if (healthBar.CurrentHp == 0)
            {
                switch (hitObjectManager.ActiveStream)
                {
                    case Difficulty.Hard:
                        hitObjectManager.ActiveStream = Difficulty.Normal;
                        s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_STANDARD);
                        healthBar.SetCurrentHp(HealthBar.HP_BAR_MAXIMUM / 2);
                        break;
                    case Difficulty.Normal:
                        hitObjectManager.ActiveStream = Difficulty.Easy;
                        s_Playfield.ChangeColour(PlayfieldBackground.COLOUR_EASY);
                        healthBar.SetCurrentHp(HealthBar.HP_BAR_MAXIMUM / 2);
                        break;
                }
            }


            scoreDisplay.Update();
            comboCounter.Update();

            base.Update();

            //playfield.Alpha = hitObjectManager.AllowSpinnerOptimisation ? 0 : 1;
        }

        internal static void SetBeatmap(Beatmap beatmap)
        {
            Beatmap = beatmap;
        }
    }
}

