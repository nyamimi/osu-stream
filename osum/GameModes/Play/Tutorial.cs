﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osum.GameModes.SongSelect;
using osum.GameplayElements.Beatmaps;
using osum.GameplayElements;
using osum.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osum.Helpers;
using osum.Graphics.Renderers;
using osum.Support;
using osum.GameplayElements.Scoring;
using osum.GameModes.Play.Components;
using osum.Audio;
using osum.GameplayElements.HitObjects.Osu;
using osum.Graphics.Skins;

namespace osum.GameModes.Play
{
    class Tutorial : Player
    {
        BackButton backButton;
        pText touchToContinueText;

        const int music_offset = 2950;
        const int music_beatlength = 375;

        internal override void Initialize()
        {
            Difficulty = Difficulty.None;
            Beatmap = null;

            MainMenu.InitializeBgm();

            base.Initialize();

            touchToContinueText = new pText("Tap to continue!", 30, new Vector2(0, 20), 1, true, Color4.YellowGreen)
            {
                TextBounds = new Vector2(GameBase.BaseSize.Width * 0.8f, 0),
                Field = FieldTypes.StandardSnapBottomCentre,
                TextAlignment = TextAlignment.Centre,
                TextShadow = true,
                Bold = true,
                Origin = OriginTypes.BottomCentre
            };
            topMostSpriteManager.Add(touchToContinueText);



            backButton = new BackButton(delegate { Director.ChangeMode(OsuMode.MainMenu); });
            backButton.Alpha = 0;
            topMostSpriteManager.Add(backButton);

            loadNextSegment();
        }

        TutorialSegments currentSegment;
        TutorialSegments nextSegment = TutorialSegments.Introduction_1;

        VoidDelegate currentSegmentDelegate;

        bool touchToContinue = true;
        private void showTouchToContinue(bool showBackButton = true)
        {
            if (touchToContinue)
                return;

            touchToContinue = true;

            GameBase.Scheduler.Add(delegate
            {
                if (showBackButton)
                    backButton.FadeIn(1000, 0.3f);

                touchToContinueText.Transformations.Clear();
                touchToContinueText.Transform(new Transformation(TransformationType.Fade, 1, 0, Clock.ModeTime + 600, Clock.ModeTime + 1400, EasingTypes.In) { LoopDelay = 600, Looping = true });
            }, 400);
        }

        protected override void InputManager_OnDown(InputSource source, TrackingPoint point)
        {
            if (touchToContinue && !backButton.IsHovering)
            {
                if (touchToContinueText.Transformations.Count > 0)
                    loadNextSegment();
                return;
            }

            base.InputManager_OnDown(source, point);
        }

        private pText showText(string text, float verticalOffset = 0)
        {
            pText pt = new pText(text, 30, new Vector2(0, verticalOffset), 1, true, Color4.White)
            {
                TextBounds = new Vector2(GameBase.BaseSize.Width * 0.95f, 0),
                Field = FieldTypes.StandardSnapCentre,
                TextAlignment = TextAlignment.Centre,
                TextShadow = true,
                Origin = OriginTypes.Centre
            };

            pt.ScaleScalar = 1.4f;
            pt.FadeIn(300);
            pt.ScaleTo(1, 400, EasingTypes.In);
            tutorialSegmentManager.Add(pt);

            return pt;
        }

        public override bool Draw()
        {
            base.Draw();

            tutorialSegmentManager.Draw();

            return true;
        }

        int lastFrameBeat;
        int currentBeat;
        public override void Update()
        {
            lastFrameBeat = currentBeat;
            currentBeat = (Clock.AudioTime - music_offset) / music_beatlength;

            if (currentSegmentDelegate != null) currentSegmentDelegate();

            base.Update();

            tutorialSegmentManager.Update();
        }

        protected override void initializeUIElements()
        {
            //base.initializeUIElements();
        }

        enum TutorialSegments
        {
            None,
            Introduction_1,
            Introduction_2,
            Introduction_3,
            Introduction_4,
            HitCircle_1,
            HitCircle_2,
            HitCircle_3,
            HitCircle_4,
            HitCircle_5,
            HitCircle_6,
            HitCircle_Interact,
            HitCircle_Judge,
            Hold_1,
            Hold_2,
            Hold_3,
            Hold_Interact,
            Hold_Judge,
            Slider_1,
            Slider_2,
            Slider_3,
            Slider_Interact,
            Spinner_1,
            Spinner_2,
            Spinner_3,
            Spinner_4,
            Spinner_Interact,
            Spinner_Judge,
            Healthbar_1,
            Healthbar_2,
            Healthbar_3,
            Healthbar_4,
            Healthbar_5,
            Healthbar_End,
            Score_1,
            Score_2,
            Score_3,
            Score_4,
            End,
        }

        SpriteManager tutorialSegmentManager = new SpriteManager();

        private HitObject sampleHitObject;

        private void loadNextSegment()
        {
            loadSegment(nextSegment);
        }

        private void loadSegment(TutorialSegments segment)
        {
            currentSegment = segment;

            nextSegment = (TutorialSegments)(currentSegment + 1);

            foreach (pDrawable p in tutorialSegmentManager.Sprites)
            {
                p.AlwaysDraw = false;
                p.FadeOut(100);
                p.ScaleTo(0.9f, 400, EasingTypes.In);
            }

            tutorialSegmentManager.Sprites.Clear();

            currentSegmentDelegate = null;

            touchToContinueText.Transformations.Clear();
            touchToContinueText.Alpha = 0;

            touchToContinue = false;

            backButton.FadeOut(200);

            switch (currentSegment)
            {
                case TutorialSegments.Introduction_1:
                    showText("Welcome to the world of osu!.\nThis tutorial will teach you everything you need to know in order to become a rhythm master.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Introduction_2:
                    showText("osu!stream is a game which requires both rhythmical and positional accuracy.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Introduction_3:
                    showText("You will need to feel the beat, so make sure you are using headphones or playing in quiet surroundings!");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Introduction_4:
                    showText("Let's start by looking at the different kinds of beats.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Healthbar_1:
                    showText("The health bar is located at the top-left of your display.");
                    healthBar = new HealthBar();
                    {
                        pDrawable lastFlash = null;
                        currentSegmentDelegate = delegate
                        {
                            if (lastFlash == null || lastFlash.Alpha == 0)
                                lastFlash = healthBar.s_barBg.AdditiveFlash(1000, 1).ScaleTo(healthBar.s_barBg.ScaleScalar * 1.04f, 1000);
                        };
                    }

                    streamSwitchDisplay = new StreamSwitchDisplay();
                    showTouchToContinue();
                    break;
                case TutorialSegments.Healthbar_2:
                    showText("It will go up or down depending on your performance.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Healthbar_3:
                    showText("In stream mode gameplay, you can jump to the next stream by filling your health bar.", -120);

                    playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_STANDARD, false);

                    healthBar.SetCurrentHp(100);
                    {
                        float increaseRate = 0;
                        currentSegmentDelegate = delegate
                        {
                            if (touchToContinue) return;

                            if (healthBar.CurrentHp == 200)
                            {
                                if (increaseRate > 20)
                                {
                                    streamSwitchDisplay.EndSwitch();
                                    healthBar.SetCurrentHp(100);
                                    playfieldBackground.ChangeColour(Difficulty.Hard);
                                    showTouchToContinue();
                                }
                                else
                                {
                                    increaseRate += 0.2f;
                                    streamSwitchDisplay.BeginSwitch(true);
                                    playfieldBackground.Move(increaseRate);
                                }
                            }
                            else
                            {
                                healthBar.SetCurrentHp(healthBar.CurrentHp + 1);
                            }
                        };
                    }
                    break;
                case TutorialSegments.Healthbar_4:
                    showText("In a similar manner, if it reaches zero, you will drop down a stream.", -120);
                    {
                        float increaseRate = 0;
                        currentSegmentDelegate = delegate
                        {
                            if (touchToContinue) return;

                            if (healthBar.CurrentHp == 0)
                            {
                                if (increaseRate > 20)
                                {
                                    streamSwitchDisplay.EndSwitch();
                                    healthBar.SetCurrentHp(100);
                                    playfieldBackground.ChangeColour(Difficulty.Normal);
                                    showTouchToContinue();
                                }
                                else
                                {
                                    increaseRate += 0.2f;
                                    streamSwitchDisplay.BeginSwitch(false);
                                    playfieldBackground.Move(-increaseRate);
                                }
                            }
                            else
                            {
                                healthBar.SetCurrentHp(healthBar.CurrentHp - 1);
                            }
                        };
                    }
                    break;
                case TutorialSegments.Healthbar_5:
                    showText("If it hits zero on the lowest stream you will fail instantly, so watch out!", -120);

                    playfieldBackground.ChangeColour(Difficulty.Easy, false);

                    currentSegmentDelegate = delegate
                    {
                        if (playfieldBackground.Velocity == 0)
                            healthBar.SetCurrentHp(healthBar.CurrentHp - 0.5f);
                        if (healthBar.CurrentHp == 0)
                        {

                            if (!touchToContinue)
                            {
                                showTouchToContinue();
                                playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO);
                                showFailSprite();
                                AudioEngine.Music.Pause();
                            }
                        }
                        else if (healthBar.CurrentHp < HealthBar.HP_BAR_MAXIMUM / 3)
                            playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_WARNING);
                    };
                    break;
                case TutorialSegments.Healthbar_End:
                    healthBar.SetCurrentHp(100);
                    hideFailSprite();
                    AudioEngine.Music.Play();
                    healthBar.InitialIncrease = true;
                    currentSegmentDelegate = delegate { if (healthBar.DisplayHp > 20) loadNextSegment(); };
                    break;









                case TutorialSegments.Score_1:
                    scoreDisplay = new ScoreDisplay();
                    {
                        pDrawable lastFlash = null;
                        currentSegmentDelegate = delegate
                        {
                            if (lastFlash == null || lastFlash.Alpha == 0)
                                scoreDisplay.spriteManager.Sprites.ForEach(s => lastFlash = s.AdditiveFlash(1000, 1).ScaleTo(s.ScaleScalar * 1.1f, 1000));
                        };
                    }
                    showText("Scoring is based on a your accuracy and combo.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Score_2:
                    showText("You can also get score bonuses from reaching higher streams, and for spinning spinners fast!");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Score_3:
                    showText("Your current combo can be seen in the bottom-left corner of the screen.");
                    {
                        GameBase.Scheduler.Add(delegate
                        {
                            comboCounter = new ComboCounter();
                            comboCounter.SetCombo(35);

                            pDrawable lastFlash = null;
                            currentSegmentDelegate = delegate
                            {
                                if (comboCounter.displayCombo == 35)
                                {
                                    if (!touchToContinue)
                                        showTouchToContinue(false);

                                    if (lastFlash == null || lastFlash.Alpha == 0)
                                        comboCounter.spriteManager.Sprites.ForEach(s => lastFlash = s.AdditiveFlash(1000, 1).ScaleTo(s.ScaleScalar * 1.1f, 1000));
                                }
                            };
                        }, 500);
                    }
                    break;
                case TutorialSegments.Score_4:
                    comboCounter.SetCombo(0);
                    showText("Your combo will only show up when you are on a streak!");
                    GameBase.Scheduler.Add(delegate { showTouchToContinue(); }, 1500);
                    break;







                case TutorialSegments.HitCircle_1:
                    resetScore();
                    playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO, false);

                    showText("\"Hit Circles\" are the most basic beat in osu!.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.HitCircle_2:
                    {
                        showText("They are made up of the main circle...", -50);

                        if (hitObjectManager != null) hitObjectManager.Dispose();
                        hitObjectManager = new HitObjectManager(null);

                        sampleHitObject = new HitCircle(hitObjectManager, new Vector2(256, 197), 0, true, 0, HitObjectSoundType.Normal);
                        sampleHitObject.Colour = Color4.OrangeRed;
                        sampleHitObject.ComboNumber = 1;

                        sampleHitObject.Sprites.ForEach(s =>
                        {
                            s.AlwaysDraw = true;
                            s.Transformations.Clear();
                            s.Clocking = ClockTypes.Mode;
                            s.FadeInFromZero(200);
                        });

                        HitCircle c = sampleHitObject as HitCircle;
                        c.SpriteApproachCircle.Alpha = 0;
                        c.SpriteApproachCircle.Transformations.Clear();

                        hitObjectManager.spriteManager.Add(sampleHitObject.Sprites);

                        showTouchToContinue();
                    }
                    break;
                case TutorialSegments.HitCircle_3:
                    {
                        showText("And the approach circle.", -80);

                        HitCircle c = sampleHitObject as HitCircle;
                        c.SpriteHitCircle1.FadeColour(ColourHelper.Darken(c.Colour, 0.3f), 200);
                        c.SpriteApproachCircle.ScaleScalar = 4;
                        c.SpriteApproachCircle.FadeIn(200);

                        showTouchToContinue();
                    }
                    break;
                case TutorialSegments.HitCircle_4:
                    {
                        showText("When the approach circle reaches the border of the main circle...", -80);

                        HitCircle c = sampleHitObject as HitCircle;

                        c.SpriteApproachCircle.Alpha = 1;
                        c.SpriteApproachCircle.ScaleTo(1, 4000);
                        c.SpriteHitCircle1.FadeColour(ColourHelper.Lighten(c.Colour, 0.7f), 4000);

                        currentSegmentDelegate = delegate
                        {
                            if (c.SpriteApproachCircle.Transformations.Count == 0)
                            {
                                if (!touchToContinue)
                                {
                                    AudioEngine.PlaySample(OsuSamples.HitNormal, SampleSet.Normal);
                                    showText("...you should tap!", 80).Colour = Color4.Yellow;
                                    showTouchToContinue();
                                }

                                pDrawable lastFlash = null;
                                currentSegmentDelegate = delegate
                                {
                                    if (lastFlash == null || lastFlash.Alpha == 0)
                                        lastFlash = c.SpriteApproachCircle.AdditiveFlash(1000, 1).ScaleTo(1.4f, 1000);
                                };
                            }
                        };
                    }
                    break;
                case TutorialSegments.HitCircle_5:
                    {
                        showText("The more accurate your timing, the more points you get!", -90);

                        HitCircle c = sampleHitObject as HitCircle;

                        c.SpriteApproachCircle.FadeOut(200);

                        GameBase.Scheduler.Add(delegate
                        {
                            AudioEngine.PlaySample(OsuSamples.HitNormal, SampleSet.Normal);
                            showText("good...", 80).FadeOut(1000);
                            c.HitAnimation(ScoreChange.Hit50);
                        }, 2000);

                        GameBase.Scheduler.Add(delegate
                        {
                            AudioEngine.PlaySample(OsuSamples.HitNormal, SampleSet.Normal);
                            AudioEngine.PlaySample(OsuSamples.HitWhistle, SampleSet.Normal);
                            showText("great!", 90).FadeOut(1000);
                            c.HitAnimation(ScoreChange.Hit100);
                        }, 3000);

                        GameBase.Scheduler.Add(delegate
                        {
                            AudioEngine.PlaySample(OsuSamples.HitNormal, SampleSet.Normal);
                            AudioEngine.PlaySample(OsuSamples.HitFinish, SampleSet.Normal);
                            showText("Perfect!", 100).FadeOut(2000);
                            c.HitAnimation(ScoreChange.Hit300);
                        }, 4000);

                        GameBase.Scheduler.Add(delegate
                        {
                            loadNextSegment();
                        }, 6500);
                    }
                    break;
                case TutorialSegments.HitCircle_6:
                    showText("Okay. Let's give it a shot!\nTry hitting these 8 hit circles.");
                    showTouchToContinue();
                    break;
                case TutorialSegments.HitCircle_Interact:
                    {
                        resetScore();
                        playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_STANDARD, false);

                        sampleHitObject.Sprites.ForEach(s => s.AlwaysDraw = false);

                        Difficulty = Difficulty.Easy;

                        Beatmap = new Beatmap();
                        Beatmap.ControlPoints.Add(new ControlPoint(music_offset, music_beatlength, TimeSignatures.SimpleQuadruple, SampleSet.Normal, CustomSampleSet.Default, 100, true, false));

                        if (countdown == null) countdown = new CountdownDisplay();

                        firstCountdown = true;
                        AudioEngine.Music.SeekTo(58000);
                        CountdownResume(music_offset + 160 * music_beatlength, 8);

                        loadBeatmap();
                        hitObjectManager.OnScoreChanged += new ScoreChangeDelegate(hitObjectManager_OnScoreChanged);

                        const int x1 = 100;
                        const int x2 = 512 - 100;
                        const int y1 = 80;
                        const int y2 = 384 - 80;

                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x1, y1), music_offset + 160 * music_beatlength, true, 0, HitObjectSoundType.Normal), Difficulty);
                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x2, y1), music_offset + 164 * music_beatlength, false, 0, HitObjectSoundType.Finish), Difficulty);
                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x1, y2), music_offset + 168 * music_beatlength, false, 0, HitObjectSoundType.Normal), Difficulty);
                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x2, y2), music_offset + 172 * music_beatlength, false, 0, HitObjectSoundType.Finish), Difficulty);

                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x2, y1), music_offset + 176 * music_beatlength, true, 0, HitObjectSoundType.Normal), Difficulty);
                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x1, y2), music_offset + 180 * music_beatlength, false, 0, HitObjectSoundType.Finish), Difficulty);
                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x2, y2), music_offset + 184 * music_beatlength, false, 0, HitObjectSoundType.Normal), Difficulty);
                        hitObjectManager.Add(new HitCircle(hitObjectManager, new Vector2(x1, y1), music_offset + 188 * music_beatlength, false, 0, HitObjectSoundType.Finish), Difficulty);

                        hitObjectManager.PostProcessing();

                        hitObjectManager.SetActiveStream(Difficulty.Easy);

                        currentSegmentDelegate = delegate
                        {
                            if (!touchToContinue && hitObjectManager.AllNotesHit)
                                loadNextSegment();
                        };
                    }
                    break;
                case TutorialSegments.HitCircle_Judge:
                    playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO, false);

                    if (currentScore.countMiss > 0)
                    {
                        playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_WARNING, false);
                        showText("Hmm, looks like we need to practise a bit more. Let's go over this again!");
                        nextSegment = TutorialSegments.HitCircle_1;
                    }
                    else if (currentScore.count50 > 0)
                    {
                        showText("Getting there!\nWatch the approaching circle carefully and listen to the beat. Let's try once more!");
                        nextSegment = TutorialSegments.HitCircle_Interact;
                    }
                    else if (currentScore.count100 > 0)
                    {
                        showText("That's right!\nFocus on the beat of the song and try to time your taps to get higher accuracy.");
                    }
                    else
                    {
                        showText("Flawless! Great job!");
                    }

                    showTouchToContinue();
                    break;



                case TutorialSegments.Hold_1:
                    {
                        resetScore();
                        playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO, false);

                        showText("\"Holds\" are like hit circles, but you need to tap and hold them.", -80);
                        showTouchToContinue();


                        if (hitObjectManager != null) hitObjectManager.Dispose();
                        hitObjectManager = new HitObjectManager(null);

                        sampleHitObject = new HoldCircle(hitObjectManager, new Vector2(256, 197), 0, true, 0, HitObjectSoundType.Normal, 1000, 40, null, 800, 10);
                        //arbitrary
                        sampleHitObject.Colour = Color4.White;

                        sampleHitObject.ComboNumber = 1;

                        sampleHitObject.Sprites.ForEach(s =>
                        {
                            s.AlwaysDraw = true;
                            s.Transformations.Clear();
                            s.Clocking = ClockTypes.Mode;
                            s.FadeInFromZero(200);
                        });

                        HoldCircle h = sampleHitObject as HoldCircle;
                        h.HitCircleStart.SpriteApproachCircle.ScaleScalar = 4;

                        hitObjectManager.spriteManager.Add(sampleHitObject.Sprites);
                    }
                    break;

                case TutorialSegments.Hold_2:
                    {
                        HoldCircle c = sampleHitObject as HoldCircle;

                        Player.Autoplay = true;

                        c.HitCircleStart.SpriteApproachCircle.Alpha = 1;
                        c.HitCircleStart.SpriteApproachCircle.ScaleTo(1, 2000);

                        c.HitCircleStart.StartTime = c.StartTime = Clock.ModeTime + 2000;
                        c.EndTime = c.StartTime + 5000;
                        c.Colour = Color4.White;

                        currentSegmentDelegate = delegate
                        {
                            if (c.HitCircleStart.SpriteApproachCircle.AlwaysDraw &&
                                c.HitCircleStart.SpriteApproachCircle.Transformations.Count == 1)
                            {
                                c.HitCircleStart.SpriteApproachCircle.AlwaysDraw = false;
                                showText("You need to hold on until it explodes!", 80);
                            }

                            if (c.progressCurrent == c.RepeatCount && !c.IsHit)
                            {
                                //this is bad but fixing it will be even worse.
                                c.Sprites.ForEach(s => s.FadeOut(100));

                                GameBase.Scheduler.Add(delegate
                                {
                                    loadNextSegment();
                                }, 1000);
                            }

                            c.HitAnimation(c.CheckScoring());

                            c.Update();
                        };
                    }
                    break;
                case TutorialSegments.Hold_3:
                    Player.Autoplay = false;
                    showText("Let's try a few holds!");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Hold_Interact:
                    {
                        resetScore();
                        playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_STANDARD, false);

                        Difficulty = Difficulty.Easy;

                        Beatmap = new Beatmap();
                        Beatmap.ControlPoints.Add(new ControlPoint(music_offset, music_beatlength, TimeSignatures.SimpleQuadruple, SampleSet.Normal, CustomSampleSet.Default, 100, true, false));

                        if (countdown == null) countdown = new CountdownDisplay();

                        firstCountdown = true;
                        AudioEngine.Music.SeekTo(58000);
                        CountdownResume(music_offset + 160 * music_beatlength, 8);

                        loadBeatmap();

                        const int x1 = 100;
                        const int x2 = 512 - 100;
                        const int y1 = 80;
                        const int y2 = 384 - 80;

                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x1, y1), music_offset + 160 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x2, y2), music_offset + 168 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x2, y1), music_offset + 176 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x1, y2), music_offset + 184 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);

                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x1, y1), music_offset + 192 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x2, y2), music_offset + 196 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x2, y1), music_offset + 200 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x1, y2), music_offset + 204 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);

                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x2, y1), music_offset + 208 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x1, y1), music_offset + 212 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x2, y2), music_offset + 216 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);
                        hitObjectManager.Add(new HoldCircle(hitObjectManager, new Vector2(x1, y2), music_offset + 220 * music_beatlength, true, 0, HitObjectSoundType.Normal, (4 * music_beatlength) / 8f / 1000f, 8, null, 1, 1), Difficulty);

                        hitObjectManager.PostProcessing();

                        hitObjectManager.SetActiveStream(Difficulty.Easy);

                        currentSegmentDelegate = delegate
                        {
                            if (!touchToContinue && hitObjectManager.AllNotesHit)
                                loadNextSegment();
                        };
                    }
                    break;
                case TutorialSegments.Hold_Judge:
                    playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO, false);

                    GameBase.Scheduler.Add(delegate
                    {

                        if (currentScore.countMiss + currentScore.count50 > 3)
                        {
                            playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_WARNING, false);
                            showText("Make sure you hold the notes until they explode! Let's go over the basics again.");
                            nextSegment = TutorialSegments.Hold_1;
                        }
                        else if (currentScore.count100 > 0)
                        {
                            showText("Yeah, just like that. Make sure to hold them until they explode!");
                        }
                        else
                        {
                            showText("Perfect.");
                        }

                        showTouchToContinue();
                    }, 500);
                    break;


                case TutorialSegments.Slider_1:
                    {
                        Clock.ResetManual();
                        Player.Autoplay = true;

                        showText("\"Sliders\" are like hit circles,", -80);

                        if (hitObjectManager != null) hitObjectManager.Dispose();
                        hitObjectManager = new HitObjectManager(null);

                        sampleHitObject = new Slider(hitObjectManager, new Vector2(100, 192), 2000, true, 0, HitObjectSoundType.Normal, CurveTypes.Bezier, 0, 300, new List<Vector2>() { new Vector2(100, 192), new Vector2(400, 192) }, null, 200, 40);


                        sampleHitObject.Colour = TextureManager.DefaultColours[0];
                        sampleHitObject.ComboNumber = 1;

                        sampleHitObject.SetClocking(ClockTypes.Manual);

                        currentSegmentDelegate = delegate
                        {
                            if (Clock.ManualTime < 1500)
                                Clock.IncrementManual(0.5f);
                            else
                                showTouchToContinue();
                            sampleHitObject.CheckScoring();
                            sampleHitObject.Update();
                        };

                        hitObjectManager.spriteManager.Add(sampleHitObject.Sprites);



                        GameBase.Scheduler.Add(delegate
                        {
                            showText("but extend into tracks.", 80);
                        }, 1000);

                    }
                    break;
                case TutorialSegments.Slider_2:
                    {
                        showText("Touch it like a circle...", -100);

                        GameBase.Scheduler.Add(delegate
                        {
                            showText("then follow the ball with your finger to the end!", 120);
                        }, 1000);

                        currentSegmentDelegate = delegate
                        {
                            if (Clock.ManualTime < sampleHitObject.EndTime + 500)
                                Clock.IncrementManual(0.5f);
                            else
                                showTouchToContinue();

                            sampleHitObject.HitAnimation(sampleHitObject.CheckScoring());
                            sampleHitObject.Update();
                        };

                    }
                    break;
                case TutorialSegments.Slider_3:
                    showText("Some sliders need to be repeated.", -80);

                    Clock.ResetManual();

                    if (hitObjectManager != null) hitObjectManager.Dispose();
                    hitObjectManager = new HitObjectManager(null);

                    sampleHitObject = new Slider(hitObjectManager, new Vector2(100, 192), 2000, true, 0, HitObjectSoundType.Normal, CurveTypes.Bezier, 2, 300, new List<Vector2>() { new Vector2(100, 192), new Vector2(400, 192) }, null, 200, 40);


                    sampleHitObject.Colour = TextureManager.DefaultColours[0];
                    sampleHitObject.ComboNumber = 1;

                    sampleHitObject.SetClocking(ClockTypes.Manual);

                    currentSegmentDelegate = delegate
                    {
                        if (Clock.ManualTime < 5500)
                            Clock.IncrementManual(0.5f);
                        else
                            showTouchToContinue();
                        sampleHitObject.CheckScoring();
                        sampleHitObject.Update();
                    };

                    hitObjectManager.spriteManager.Add(sampleHitObject.Sprites);

                    GameBase.Scheduler.Add(delegate
                    {
                        showText("This will be indicated by an arrow at the end.", 120);
                    }, 1000);

                    break;
                case TutorialSegments.Slider_Interact:
                    showText("Oh, you wanted to try sliding?", -80);
                    GameBase.Scheduler.Add(delegate
                    {
                        showText("TOO BAD! (not done yet)", 80);
                        showTouchToContinue();
                    }, 1000);
                    break;



                case TutorialSegments.Spinner_1:
                    resetScore();

                    showText("\"Spinners\" are the only beats which are not rhythmical.");
                    showTouchToContinue();
                    break;

                case TutorialSegments.Spinner_2:
                    showText("When a spinner appears...", -140);
                    {
                        Spinner sp = null;

                        GameBase.Scheduler.Add(delegate
                        {

                            if (hitObjectManager != null) hitObjectManager.Dispose();
                            hitObjectManager = new HitObjectManager(null);

                            sampleHitObject = new Spinner(hitObjectManager, 0, 9999999, HitObjectSoundType.Normal);

                            sampleHitObject.Sprites.ForEach(s =>
                            {
                                s.AlwaysDraw = true;
                                s.Transformations.Clear();
                                s.Clocking = ClockTypes.Mode;
                                s.FadeInFromZero(500);
                            });

                            sp = sampleHitObject as Spinner;
                            sp.rotationRequirement = 5 * Spinner.sensitivity_modifier;
                            sp.SpriteClear.Transformations.Clear();
                            sp.SpriteSpin.Transformations.Clear();
                            sp.ApproachCircle.Transformations.Clear();

                            hitObjectManager.spriteManager.Add(sampleHitObject.Sprites);
                        }, 400);

                        GameBase.Scheduler.Add(delegate
                        {
                            showText("..you should spin it with your finger until the bars fill!", 80);

                            currentSegmentDelegate = delegate
                            {
                                if (!sp.Cleared)
                                {
                                    sp.velocityFromInputPerMillisecond = 0.01f;
                                    sp.Update();
                                    sp.CheckScoring();
                                }
                                else
                                {
                                    showTouchToContinue();
                                }


                            };

                        }, 800);
                    }
                    break;
                case TutorialSegments.Spinner_3:
                    showText("Spin faster for a bonus!", -140);
                    {
                        Spinner sp = sampleHitObject as Spinner;

                        currentSegmentDelegate = delegate
                        {
                            if (sp.BonusScore < 1000)
                            {
                                sp.velocityFromInputPerMillisecond = 0.04f;
                                sp.Update();
                                sp.CheckScoring();
                            }
                            else
                            {
                                if (!touchToContinue)
                                {
                                    showText("But make sure you are ready for the beats after the spinner!", 80);
                                    showTouchToContinue();
                                }
                            }
                        };
                    }
                    break;
                case TutorialSegments.Spinner_4:
                    sampleHitObject.Sprites.ForEach(s => { s.Transformations.Clear(); s.AlwaysDraw = false; });
                    showText("Let's try some spinners!");
                    showTouchToContinue();
                    break;
                case TutorialSegments.Spinner_Interact:
                    resetScore();
                    playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_STANDARD, false);

                    Difficulty = Difficulty.Easy;

                    Beatmap = new Beatmap();
                    Beatmap.ControlPoints.Add(new ControlPoint(music_offset, music_beatlength, TimeSignatures.SimpleQuadruple, SampleSet.Normal, CustomSampleSet.Default, 100, true, false));

                    if (countdown == null) countdown = new CountdownDisplay();

                    firstCountdown = true;
                    AudioEngine.Music.SeekTo(58000);
                    CountdownResume(music_offset + 160 * music_beatlength, 8);

                    loadBeatmap();

                    hitObjectManager.Add(new Spinner(hitObjectManager, music_offset + 160 * music_beatlength, music_offset + 164 * music_beatlength, HitObjectSoundType.Normal), Difficulty);
                    hitObjectManager.Add(new Spinner(hitObjectManager, music_offset + 168 * music_beatlength, music_offset + 172 * music_beatlength, HitObjectSoundType.Normal), Difficulty);
                    hitObjectManager.Add(new Spinner(hitObjectManager, music_offset + 176 * music_beatlength, music_offset + 188 * music_beatlength, HitObjectSoundType.Normal), Difficulty);

                    hitObjectManager.PostProcessing();

                    hitObjectManager.SetActiveStream(Difficulty.Easy);

                    currentSegmentDelegate = delegate
                    {
                        if (!touchToContinue && hitObjectManager.AllNotesHit)
                            loadNextSegment();
                    };

                    break;
                case TutorialSegments.Spinner_Judge:
                    playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_INTRO, false);

                    GameBase.Scheduler.Add(delegate
                    {

                        if (currentScore.countMiss > 0)
                        {
                            playfieldBackground.ChangeColour(PlayfieldBackground.COLOUR_WARNING, false);
                            showText("Are you actually trying? All you need to do is make circles with your finger! Let's try again...");
                            nextSegment = TutorialSegments.Spinner_Interact;
                        }
                        else if (currentScore.count50 > 0 || currentScore.count100 > 0)
                        {
                            showText("You're spinning, but a bit slow. Let's try once more!");
                            nextSegment = TutorialSegments.Spinner_Interact;
                        }
                        else
                        {
                            showText("You spin like a TORNADO!");
                        }

                        showTouchToContinue();
                    }, 500);
                    break;
                case TutorialSegments.End:
                    backButton.HandleInput = false;
                    Director.ChangeMode(OsuMode.MainMenu, new FadeTransition(3000, FadeTransition.DEFAULT_FADE_IN));
                    break;
            }
        }

        void hitObjectManager_OnScoreChanged(ScoreChange change, HitObject hitObject)
        {
            switch (change)
            {
                case ScoreChange.Hit300:
                case ScoreChange.Hit300g:
                case ScoreChange.Hit300k:
                case ScoreChange.Hit300m:
                    showText("Perfect!", 0).FadeOut(1000);
                    break;
                case ScoreChange.Hit100:
                case ScoreChange.Hit100m:
                case ScoreChange.Hit100k:
                    if (Clock.AudioTime < hitObject.StartTime)
                        showText("A bit early..", -60).FadeOut(1000);
                    else
                        showText("A bit late..", 60).FadeOut(1000);
                    break;
                case ScoreChange.Hit50:
                case ScoreChange.Hit50m:
                case ScoreChange.Miss:
                    if (Clock.AudioTime < hitObject.StartTime)
                    {
                        pText t = showText("Very early..", -60);
                        t.TextSize *= 1.4f;
                        t.Colour = Color4.OrangeRed;
                        t.FadeOut(1000);
                    }
                    else
                    {
                        pText t = showText("Very late..", 60);
                        t.TextSize *= 1.4f;
                        t.Colour = Color4.OrangeRed;
                        t.FadeOut(1000);
                    }
                    break;
            }
        }

        protected override void UpdateStream()
        {
        }

        protected override void CheckForCompletion()
        {
        }

    }
}
