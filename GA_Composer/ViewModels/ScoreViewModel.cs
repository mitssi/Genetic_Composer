using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using GA_Composer.Repositories;
using GA_Composer.Models;
using GA_Composer.Enums;
using Telerik.WinControls.UI.Docking;
using Telerik.WinControls.UI;
using Midi;

namespace GA_Composer.ViewModels
{
    public class ScoreViewModel
    {
        #region Private Field

        PrivateFontCollection privateFonts;
        Font font;
        Font sharpfont;
        Font chordfont;
        Font geneticfont;
        Font slashfont;
        Image legato;
        Image reverseLegato;
        Pen blackLine = new Pen(Color.DarkSlateGray);
        Pen blackBoldLine = new Pen(Color.DarkSlateGray, 5);
        SolidBrush blackBrush = new SolidBrush(Color.FromArgb(255, 15, 25, 35));
        SolidBrush redBrush = new SolidBrush(Color.Red);
        GeneticGraphicNote selectedNote = null;
        double tempo = 80;
        // 셋잇 단음표의 표현을 위해 만듦
        int tripleMaxY = 0, tripleMinY = 10000, triplePoint = 0;
        int tripleStartX = 0, tripleEndX = 0, tripleStartY = 0, tripleEndY = 0;
        Point tripleStart, tripleEnd;
        // 인자 전달 갯수 간략화
        int noteCount;
        int barWidth;
        Graphics g;
        SolidBrush brush;
        Point thisPoint;
        GeneticGraphicNote note;

        List<PlaySignal> playList;

        #endregion

        #region Properties

        /// <summary>
        /// 노트 사이의 간격을 조정함
        /// </summary>
        /// <remarks>
        /// 1 = Duration 을 기준으로 차지하는 길이를 설정
        /// 0 = 마디 안의 노트 개수를 기준으로 차지하는 길이를 설정
        /// </remarks>
        public double Strength { get; set; } = 0.8;

        /// <summary>
        /// 오선의 가로길이
        /// </summary>
        public int ScoreWidth { get; set; }

        /// <summary>
        /// 오선의 세로길이
        /// </summary>
        public int ScoreHeight { get; set; }

        /// <summary>
        /// 오선의 양 옆 여백
        /// </summary>
        public int ScoreWidthMargin { get; set; } = 20;
        
        /// <summary>
        /// 오선의 위 아래 여백
        /// </summary>
        public int ScoreHeightMargin { get; set; } = 60;
        
        /// <summary>
        /// 코드와 오선지와의 간격
        /// </summary>
        public int BetweenChordAndStaff { get; set; } = 80;

        /// <summary>
        /// 마디 안에서 양 여백
        /// </summary>
        public int BarWidthMargin { get; set; } = 5;

        /// <summary>
        /// 오선지에서 선 간의 간격 
        /// </summary>
        public int BarLineHeightSpace { get; } = 10;
        
        /// <summary>
        /// 오선줄 한 라인에 마디가 몇개가 들어가는지
        /// </summary>
        public int NumberOfBarInLine { get; set; } = 4;

        /// <summary>
        /// 오선지 이미지 표시를 시작할 Y값 (X는 무조건 0)
        /// </summary>
        /// <remarks>
        /// 스크롤링 할 때 0, 0 기준으로 그림을 그리면 스크롤이 의미가 없어져버린다
        /// </remarks>
        public int StartY { get; set; } = 0;

        /// <summary>
        /// 재생 템포
        /// </summary>
        public double Tempo {
            get
            {
                return tempo;
            }
            set
            {
                tempo = value;
                StaticVM.MainViewModel.TempoTextBox.Text = value.ToString();
            }
        }

        public Task PlayScoreTask;

        /// <summary>
        /// 플레이 Task 를 실행하는지 안하는지 true 일경우 계속 재생
        /// </summary>
        public bool isPlayScoreTrack { get; set; } = false;

        #endregion

        #region Constructor

        public ScoreViewModel()
        {
            privateFonts = new PrivateFontCollection();
            privateFonts.AddFontFile("Resources/MusicalFont.ttf");
            font = new Font(privateFonts.Families[0], 30f);
            sharpfont = new Font(privateFonts.Families[0], 28f);
            chordfont = new Font("Segoe UI", 12f);
            geneticfont = new Font("Segoe UI", 10f);
            slashfont = new Font(privateFonts.Families[0], 30f);
            
            legato = Properties.Resources.Legato;
            reverseLegato = Properties.Resources.LegatoRev;
        }

        #endregion

        #region Public Method

        /// <summary>
        /// 악보를 표시하는 컨트롤에서 악보를 지속적으로 표기하도록 악보를 그려주는 함수
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="length"></param>
        public void DrawScore(object sender, PaintEventArgs e)
        {
            #region 변수

            Graphics g = e.Graphics;

            // 안티앨리어싱을 줘서 더 선명한 그림을 만든다
            e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            // 오선의 총 길이 설정
            ScoreWidth = StaticVM.MainViewModel.ScoreDocument.Width - 2 * ScoreWidthMargin;

            // 현재의 위치
            Point thisPoint = new Point(0, StartY);

            Point prevPoint = new Point(0, StartY);

            // 마디 1개의 길이
            int BarWidth = 0;

            // 지금까지 지나간 총 OffSet
            int thisOffSet = 0;

            // 전 음표의 OffSet
            int prevOffSet = 0;
            
            // 현재의 코드
            GeneticChord thisChord = StaticRepo.ScoreRepository.ChordList[0];

            // 현재의 Bar Number
            int barNumber = 0;

            #endregion
            
            foreach (GeneticBar gb in StaticRepo.ScoreRepository.SelectedBarList)
            {
                barNumber++;
                // 개행 맨 처음에는 노트를 그리기 전에 설정해야 해줄것들과 보표 등을 넣는다
                if (StaticRepo.ScoreRepository.SelectedBarList.IndexOf(gb) % NumberOfBarInLine == 0)
                {
                    #region 오선을 그리기 전에 마진을 설정해 놓는다

                    thisPoint.X = ScoreWidthMargin;
                    thisPoint.Y += ScoreHeightMargin;

                    #endregion

                    #region 박자와 조표 들어가는 곳의 오선 그리기
                    for (int i = 0; i <= 4; i++)
                    {
                        g.DrawLine(blackLine, thisPoint.X, thisPoint.Y + i * BarLineHeightSpace, thisPoint.X + 58, thisPoint.Y + i * BarLineHeightSpace);
                    }
                    #endregion

                    #region 높은음자리표 그리기

                    g.DrawString(String.Format("{0}", (char)MusicalFont.Clef_G), font, blackBrush, thisPoint.X + 5, thisPoint.Y - 15);

                    #endregion
                    
                    #region 만일 맨 처음 시작이면 박자를 넣는다

                    if (StaticRepo.ScoreRepository.SelectedBarList.IndexOf(gb) == 0)
                    {
                        thisPoint.X += 38;

                        g.DrawString(String.Format("4"), font, blackBrush, thisPoint.X, thisPoint.Y - 46);
                        g.DrawString(String.Format("4"), font, blackBrush, thisPoint.X, thisPoint.Y - 25);
                    }
                    else
                    {
                        thisPoint.X += 20;
                    }

                    #endregion

                    // 맨 처음 공백
                    thisPoint.X += 20;

                    // 마디 크기 설정
                    BarWidth = (StaticVM.MainViewModel.ScoreDocument.Width - thisPoint.X - ScoreWidthMargin) / NumberOfBarInLine - 30;
                }

                // 마디마다 최초 여백 설정
                thisPoint.X += 20;

                #region 오선 그리기

                for (int i = 0; i <= 4; i++)
                {
                    g.DrawLine(blackLine, thisPoint.X - 20, thisPoint.Y + i * BarLineHeightSpace, thisPoint.X + BarWidth + 9, thisPoint.Y + i * BarLineHeightSpace);

                }
                #endregion

                #region 노트(음표)와 코드 넣기

                #region 임시 변수

                double prevIntervalRate = 0;
                Point prevIntervalPoint;

                #endregion

                // 맨 처음에는 prevPoint 를 thisPoint 로 설정한다
                if(StaticRepo.ScoreRepository.SelectedBarList.IndexOf(gb) == 0)
                {
                    prevPoint = thisPoint;
                }

                int noteCount = 0;

                // 노트의 총 합을 구하는 과정 정음표와 셋잇단음표를 계산해야한다
                foreach (GeneticGraphicNote gn in gb.Notes)
                {
                    if (gn.Duration == 15 || gn.Duration == 27 || gn.Duration == 30 || gn.Duration == 33)
                        noteCount += 2;
                    else
                        noteCount += 1;
                }
                // 셋잇단음표일경우 이음표로 나누어야 하는 경우에 노트가 나누어진다
                int noteSum = 0;
                foreach (GeneticGraphicNote gn in gb.Notes)
                {
                    if (gn.Duration == 8)
                    {
                        if(noteSum == 8) // 만일 셋잇단음표를 넘어간다면 따로 노트가 되어야 한다
                        {
                            noteSum -= 12;
                            noteCount ++;
                        }
                    }
                    else if(gn.Duration == 12)
                    {
                        if(noteSum == 4 || noteSum == 8) // 만일 셋잇단음표를 넘어간다면 따로 노트가 되어야 한다
                        {
                            noteCount++;
                        }
                        noteSum -= 12;
                    }
                    else if (gn.Duration == 16)
                    {
                        noteCount++;
                        noteSum -= 12;
                    }
                    else if(gn.Duration == 20 || gn.Duration == 32 || gn.Duration == 44)
                    {
                        noteCount++;
                        if (noteSum == 4)
                        {
                            noteCount++;
                            noteSum -= 12;
                        }
                        noteSum -= 12;
                    }
                    else if (gn.Duration == 24 || gn.Duration == 36)
                    {
                        if (noteSum != 0)
                        {
                            noteCount+=2;
                            noteSum -= 24;
                        }
                    }
                    else if (gn.Duration == 28 || gn.Duration == 40)
                    {
                        noteCount++;
                        if (noteSum == 4)
                        {
                            noteCount++;
                            noteSum -= 36;
                        }
                        noteSum -= 24;
                    }


                    noteSum += gn.Duration;

                    if (12 == noteSum)
                    {
                        noteSum = 0;
                    }
                }
                
                foreach (GeneticGraphicNote gn in gb.Notes)
                {
                    // 노트의 Offset 을 설정해준다
                    gn.Offset = thisOffSet;


                    // 현재 OffSet 이 0이 아닌경우는 최소 한 음은 지나간 것이기 때문에 Prev를 설정할 수 있다
                    if (thisOffSet != 0)
                    {
                        prevOffSet = thisOffSet;
                    }

                    // OffSet 에 현재 노트 길이를 추가한다
                    thisOffSet += gn.Duration * 320 / 4;

                    // 전 음표의 OffSet 과 현재 음표의 OffSet 사이에 Chord 의 OffSet 이 있으면 적당한 위치에 코드를 넣는다
                    if (prevOffSet <= thisChord.OffSet && thisOffSet > thisChord.OffSet)
                    {
                        prevIntervalRate = (double)(thisChord.OffSet - prevOffSet) / (double)(thisOffSet - prevOffSet);
                        prevIntervalPoint = new Point((int)((thisPoint.X - prevPoint.X) * prevIntervalRate + thisPoint.X), thisPoint.Y);

                        g.DrawString(thisChord.Chord.Name, chordfont, blackBrush, prevIntervalPoint.X - 7, thisPoint.Y - 50);

                        if(StaticRepo.ScoreRepository.SelectedBarList == StaticRepo.ScoreRepository.GABarList)
                        {
                            if (gb.Notes[0] == gn) // Genetic Monitering
                            {
                                g.DrawString("Best Fitness : " + Math.Round(StaticRepo.GARepository.Analytics[StaticRepo.ConfigRepository.Generation - 1].Best[0][barNumber - 1], 3).ToString(), geneticfont, blackBrush, thisPoint.X - 7, thisPoint.Y - 80);
                                g.DrawString("Ave. Fitness : " + Math.Round(StaticRepo.GARepository.Analytics[StaticRepo.ConfigRepository.Generation - 1].Average[0][barNumber - 1], 3).ToString(), geneticfont, blackBrush, thisPoint.X - 7, thisPoint.Y - 65);
                            }
                        }

                        // Chord 의 Last 가 오기전까지 thisChord 를 계속 갱신해줌
                        if (StaticRepo.ScoreRepository.ChordList.Last() != thisChord)
                        {
                            thisChord = StaticRepo.ScoreRepository.ChordList[StaticRepo.ScoreRepository.ChordList.IndexOf(thisChord) + 1];
                        }
                    }
                    prevPoint = thisPoint;
                    // 노트를 화면에 넣는다

                    InsertNoteInScore(g, ref thisPoint, gn, noteCount, BarWidth);
                }

                #endregion

                // 마디 끝 여백 설정
                thisPoint.X += 10;

                #region 마디 끝에 짝대기 넣기

                // 완전히 끝 마디면 끝맺음표를 넣고
                if (StaticRepo.ScoreRepository.SelectedBarList.IndexOf(gb) == StaticRepo.ScoreRepository.SelectedBarList.Count - 1)
                {
                    g.DrawLine(blackLine, thisPoint.X - 6, thisPoint.Y, thisPoint.X - 6, thisPoint.Y + 40);
                    g.DrawLine(blackBoldLine, thisPoint.X + 2, thisPoint.Y, thisPoint.X + 2, thisPoint.Y + 41);

                    // 악보의 높이를 지금의 Y 좌표로 놓고 스크롤을 지금보다 + 100정도 여유공간을 두어서 설정한다
                    ScoreHeight = thisPoint.Y + 100;
                    ScrollingScore();
                }
                // 완전히 끝 마디가 아닐 경우 일반적인 마디를 넣는다
                else
                {
                    g.DrawLine(blackLine, thisPoint.X+1, thisPoint.Y, thisPoint.X+1, thisPoint.Y + 40);
                }

                #endregion

                #region 개행해야 할 조건에서는 개행하기

                if (StaticRepo.ScoreRepository.SelectedBarList.IndexOf(gb) % (NumberOfBarInLine) == 3)
                {
                    thisPoint.Y += ScoreHeightMargin;
                }

                #endregion
            }
        }

        private void PlayBassNote(ref int offset, int betweenOffset, Chord chord)
        {
            Pitch RootPitch;

            if (chord.Root.Letter >= 'C' && chord.Root.Letter <= 'E')
                RootPitch = chord.Root.PitchInOctave(2);
            else
                RootPitch = chord.Root.PitchInOctave(1);
            
            while(betweenOffset / 480 >= 1)
            {
                while (betweenOffset / 960 >= 1)
                {
                    while (betweenOffset / 1920 >= 1)
                    {
                        #region Bass
                        InsertNote(offset, 960, RootPitch, Channel.Channel2, 80);
                        InsertNote(offset + 1440, 440, RootPitch, Channel.Channel2, 80);
                        #endregion

                        #region Piano

                        InsertNote(offset, 920, RootPitch, Channel.Channel3, 80);
                        InsertNote(offset + 960, 920, RootPitch, Channel.Channel3, 75);

                        if (chord.NoteSequence.Length == 3)
                        {
                            InsertControl(offset, Midi.Control.SustainPedal, 127, Channel.Channel3);

                            InsertNote(offset, 920, chord.NoteSequence[1].PitchInOctave(3), Channel.Channel3, 60);
                            InsertNote(offset, 920, chord.NoteSequence[2].PitchInOctave(3), Channel.Channel3, 70);
                            InsertNote(offset, 920, chord.NoteSequence[0].PitchInOctave(4), Channel.Channel3, 60);

                            InsertNote(offset + 960, 920, chord.NoteSequence[1].PitchInOctave(3), Channel.Channel3, 60);
                            InsertNote(offset + 960, 920, chord.NoteSequence[2].PitchInOctave(3), Channel.Channel3, 70);
                            InsertNote(offset + 960,920, chord.NoteSequence[0].PitchInOctave(4), Channel.Channel3, 60);

                            InsertControl(offset+1910, Midi.Control.SustainPedal, 0, Channel.Channel3);
                        }
                        
                        #endregion

                        offset += 1920;
                        betweenOffset -= 1920;
                    }

                    if(betweenOffset / 960 >= 1)
                    {
                        #region Bass
                        InsertNote(offset, 960, RootPitch, Channel.Channel2, 80);
                        #endregion

                        #region Piano
                        if (chord.NoteSequence.Length == 3)
                        {
                            InsertControl(offset, Midi.Control.SustainPedal, 127, Channel.Channel3);

                            InsertNote(offset, 920, chord.NoteSequence[1].PitchInOctave(3), Channel.Channel3, 60);
                            InsertNote(offset, 920, chord.NoteSequence[2].PitchInOctave(3), Channel.Channel3, 70);
                            InsertNote(offset, 920, chord.NoteSequence[0].PitchInOctave(4), Channel.Channel3, 60);
                            
                            InsertControl(offset + 950, Midi.Control.SustainPedal, 0, Channel.Channel3);
                        }
                        #endregion

                        offset += 960;
                        betweenOffset -= 960;
                    }
                }

                if (betweenOffset / 480 >= 1)
                {
                    #region Bass
                    InsertNote(offset, 480, RootPitch, Channel.Channel2, 80);
                    #endregion

                    #region Piano
                    if (chord.NoteSequence.Length == 3)
                    {
                        InsertControl(offset, Midi.Control.SustainPedal, 127, Channel.Channel3);

                        InsertNote(offset, 450, chord.NoteSequence[1].PitchInOctave(3), Channel.Channel3, 60);
                        InsertNote(offset, 450, chord.NoteSequence[2].PitchInOctave(3), Channel.Channel3, 70);
                        InsertNote(offset, 450, chord.NoteSequence[0].PitchInOctave(4), Channel.Channel3, 60);

                        InsertControl(offset + 470, Midi.Control.SustainPedal, 0, Channel.Channel3);
                    }
                    #endregion

                    offset += 480;
                    betweenOffset -= 480;
                }
            }
        }

        /// <summary>
        /// 악보를 Play 한다
        /// </summary>
        public void PlayScore()
        {
            isPlayScoreTrack = true;
            StaticVM.MainViewModel.Stop.Enabled = true;
            StaticVM.MainViewModel.Pause.Enabled = true;
            StaticVM.MainViewModel.Play.Enabled = false;

            foreach (GeneticBar gb in StaticRepo.ScoreRepository.SelectedBarList)
            {
                foreach (GeneticGraphicNote gn in gb.Notes)
                {
                    gn.isSeleted = false;
                    selectedNote = null;
                }
            }

            selectedNote = StaticRepo.ScoreRepository.SelectedBarList[0].Notes[0];
            selectedNote.isSeleted = true;

            playList = new List<PlaySignal>();

            int offset = 0;
            int selectedBarIndex = 0;

            //if(selectedNote != null)
            //{
            //    selectedBarIndex = selectedNote.Offset / 3840;
            //    offset = selectedBarIndex * 3840;
            //}

            #region Channel1 : 메인 음

            foreach (var bar in StaticRepo.ScoreRepository.SelectedBarList)
            {
                if (StaticRepo.ScoreRepository.SelectedBarList.IndexOf(bar) >= selectedBarIndex)
                {
                    foreach (var note in bar.Notes)
                    {
                        // OnNote
                        // 일단은 playList에 해당 offset 이 있는지 찾아본다
                        InsertNote(offset, note.Duration * 80, note.Pitch.GeneticPitchToMidiPitch(note.isSharp), Channel.Channel1, 100);

                        offset += note.Duration * 80;
                    }
                }
            }

            #endregion

            foreach (var v1 in StaticRepo.ScoreRepository.PlayList)
            {
                playList.Add(v1);
            }
            
            #region Channel2 : 베이스 & 코드

            //offset = 0;
            //int betweenOffset = 0;

            //GeneticChord prevChord = StaticRepo.ScoreRepository.ChordList[0];

            //foreach (var chord in StaticRepo.ScoreRepository.ChordList)
            //{
            //    // 첫번째는 건너뛰고~!
            //    if (StaticRepo.ScoreRepository.ChordList.IndexOf(chord) == 0)
            //    {
            //        continue;
            //    }

            //    betweenOffset = chord.OffSet - prevChord.OffSet;

            //    PlayBassNote(ref offset, betweenOffset, prevChord.Chord);
            //    // PlayChord(ref offset, betweenOffset, prevChord.Chord);

            //    if (StaticRepo.ScoreRepository.ChordList.Last() == chord)
            //    {
            //        betweenOffset = 
            //            (3840 * StaticRepo.ScoreRepository.SelectedBarList.Count) 
            //            - chord.OffSet;

            //        PlayBassNote(ref offset, betweenOffset, chord.Chord);
            //    }
                
            //    prevChord = chord;
            //}

            #endregion

            #region Channel 10 : 드럼

            //offset = 0;

            //for (int i=selectedBarIndex; i< StaticRepo.ScoreRepository.SelectedBarList.Count; i++)
            //{
            //    #region Kick

            //    InsertNote(offset + 480 * 0, 240, (Pitch)Percussion.BassDrum1, Channel.Channel10, 70);
            //    InsertNote(offset + 480 * 5, 240, (Pitch)Percussion.BassDrum1, Channel.Channel10, 70);

            //    #endregion

            //    #region Snare

            //    InsertNote(offset + 480 * 2, 240, (Pitch)Percussion.SideStick, Channel.Channel10, 70);
            //    InsertNote(offset + 480 * 6, 240, (Pitch)Percussion.SideStick, Channel.Channel10, 70);

            //    #endregion

            //    #region Hihat

            //    InsertNote(offset + 480 * 0, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 70);
            //    InsertNote(offset + 480 * 1, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 30);
            //    InsertNote(offset + 480 * 2, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 70);
            //    InsertNote(offset + 480 * 3, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 30);
            //    InsertNote(offset + 480 * 4, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 70);
            //    InsertNote(offset + 480 * 5, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 30);
            //    InsertNote(offset + 480 * 6, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 70);
            //    InsertNote(offset + 480 * 7, 240, (Pitch)Percussion.ClosedHiHat, Channel.Channel10, 30);
            //    #endregion

            //    offset += 3840;
            //}

            #endregion
            
            offset = 0;

            PlayScoreTask = new Task(async () =>
            {
                bool start = false;
                PlaySignal prevSignalList = new PlaySignal();
                NoteSignal noteSignal;
                ControlSignal controlSignal;

                StaticVM.MainViewModel.ScoreDocument.Invoke(new MethodInvoker(() => StaticVM.MainViewModel.ScoreDocument.Refresh()));

                foreach (var signalList in playList.OrderBy((signallist) => signallist.Offset))
                {
                    if (start == false)
                    {
                        start = true;
                        prevSignalList = signalList;
                    }
                    else
                    {
                        // 해당 TIme(Offset) Signal List 에있는 신호들을 모두 재생
                        foreach (var signal in prevSignalList.SignalList)
                        {
                            // 만약에 신호가 Note 라면
                            if(signal is NoteSignal)
                            {
                                noteSignal = signal as NoteSignal;

                                if (noteSignal.NoteOn == true)
                                {
                                    if (noteSignal.Pitch != 0)
                                    {
                                        StaticRepo.ConfigRepository.MainOutputDevice.SendNoteOn(
                                            noteSignal.Channel, noteSignal.Pitch, noteSignal.Velocity
                                        );
                                    }
                                    else
                                    {
                                        StaticRepo.ConfigRepository.MainOutputDevice.SendNoteOn(
                                            noteSignal.Channel, noteSignal.Pitch, 0
                                        );
                                    }
                                }
                                else
                                {
                                    StaticRepo.ConfigRepository.MainOutputDevice.SendNoteOff(
                                        noteSignal.Channel, noteSignal.Pitch, noteSignal.Velocity
                                    );

                                    if (noteSignal.Channel == Channel.Channel1)
                                    {
                                        // 메인 멜로디일 경우 오른쪽 노트를 선택한다
                                        SelectWithArrowKey(false);
                                        StaticVM.MainViewModel.ScoreDocument.Invoke(new MethodInvoker(() => StaticVM.MainViewModel.ScoreDocument.Refresh()));
                                    }
                                }
                            }
                            // 만약에 신호가 Control 이라면
                            else if(signal is ControlSignal)
                            {
                                controlSignal = signal as ControlSignal;
                            }
                        }
                        if(signalList.Offset != prevSignalList.Offset)
                        {
                            await Task.Delay((int)((signalList.Offset - prevSignalList.Offset) * (62.5f / Tempo)));
                        }
                        
                        if (isPlayScoreTrack == false)
                            return;

                        prevSignalList = signalList;
                    }
                }

                StaticRepo.ConfigRepository.MainOutputDevice.SilenceAllNotes();
                isPlayScoreTrack = false;

                // Invoke 가 없습니다.. 젠장 이렇게 하기 싫은데
                try
                {
                    StaticVM.MainViewModel.Stop.Enabled = false;
                    StaticVM.MainViewModel.Pause.Enabled = false;
                    StaticVM.MainViewModel.Play.Enabled = true;
                }
                catch { }
            });

            PlayScoreTask.Start();
        }

        /// <summary>
        /// 키를 눌렀을 때 발생하는 이벤트
        /// </summary>
        public void KeyDown(object sender, KeyEventArgs e)
        {
            if(isPlayScoreTrack == true)
            {
                return;
            }

            foreach (GeneticBar gb in StaticRepo.ScoreRepository.SelectedBarList)
            {
                foreach (GeneticGraphicNote gn in gb.Notes)
                {
                    if (gn.isSeleted == true)
                    {
                        if (e.KeyData == Keys.NumPad8)
                        {
                            gn.Pitch += 1;
                        }
                        else if (e.KeyData == Keys.NumPad2)
                        {
                            gn.Pitch -= 1;
                        }
                        else if(e.KeyData == Keys.NumPad4)
                        {
                            SelectWithArrowKey(true);
                        }
                        else if(e.KeyData == Keys.NumPad6)
                        {
                            SelectWithArrowKey(false);
                            StaticVM.MainViewModel.ScoreDocument.Refresh();
                            return;
                        }
                    }
                }
            }

            StaticVM.MainViewModel.ScoreDocument.Refresh();
        }

        /// <summary>
        /// 마우스로 찍었을 때 발생하는 이벤트
        /// </summary>
        public void ClickForm(object sender, MouseEventArgs e)
        {
            if(isPlayScoreTrack == true)
            {
                // 플레이 중일때는 아무런 효과도 없게 한다
                return;
            }

            foreach (GeneticBar gb in StaticRepo.ScoreRepository.SelectedBarList)
            {
                foreach (GeneticGraphicNote gn in gb.Notes)
                {
                    if(e.Location.X < gn.Righthigh.X
                        && e.Location.Y > gn.Righthigh.Y
                        && e.Location.X < gn.Rightbottom.X
                        && e.Location.Y < gn.Rightbottom.Y
                        && e.Location.X > gn.Leftbottom.X
                        && e.Location.Y < gn.Leftbottom.Y
                        && e.Location.X > gn.Lefthigh.X
                        && e.Location.Y > gn.Lefthigh.Y
                        )
                    {
                        gn.isSeleted = true;
                        selectedNote = gn;
                        StaticVM.MainViewModel.ScoreDocument.Refresh();
                        return;
                    }
                    else
                    {
                        gn.isSeleted = false;
                        selectedNote = null;
                    }
                }
            }

            StaticVM.MainViewModel.ScoreDocument.Refresh();
        }
        
        /// <summary>
        /// 노트(음표)를 실질적으로 그래픽상으로 표기해주는 함수
        /// </summary>
        /// <param name="g">노트를 넣는 그래픽스 객체</param>
        /// <param name="thisPoint">화면에서 노트를 넣는 Point</param>
        /// <param name="note">어떤 노트인지</param>
        /// <param name="noteCount">지금 Bar 안에 노트가 몇개 있는지</param>
        public void InsertNoteInScore(Graphics g, ref Point thisPoint, GeneticGraphicNote note, int noteCount, int barWidth)
        {
            bool defaultXplus = true; // 음 2개를 잇지 않은 경우에는 X를 박자에 맞게 알아서 플러스해준다
            this.noteCount = noteCount;
            this.barWidth = barWidth;
            this.g = g;
            this.note = note;
            this.thisPoint = thisPoint;

            if(note.isSeleted == true)
            {
                brush = redBrush;
            }
            else
            {
                brush = blackBrush;
            }
            
            if(note.isRest == false)
            {
                if (note.Pitch <= GeneticPitch.C4)
                {
                    int slash = ((int)GeneticPitch.C4 - (int)note.Pitch + 2) / 2;
                    for (int i = 0; i < slash; i++)
                    {
                        g.DrawLine(blackLine, thisPoint.X - 8, thisPoint.Y + 50 + i * 10, thisPoint.X + 8, thisPoint.Y + 50 + i * 10);
                    }
                }
                else if (note.Pitch >= GeneticPitch.A5)
                {
                    int slash = ((int)note.Pitch - (int)GeneticPitch.A5 + 2) / 2;
                    for (int i = 0; i < slash; i++)
                    {
                        g.DrawLine(blackLine, thisPoint.X - 8, thisPoint.Y - 10 - i * 10, thisPoint.X + 8, thisPoint.Y - 10 - i * 10);
                    }
                }
            }

            note.Leftbottom = new Point(thisPoint.X - 13, thisPoint.Y + 31 + (GeneticPitch.B4 - note.Pitch) * 5);
            note.Lefthigh = new Point(thisPoint.X - 13, thisPoint.Y + 11 + (GeneticPitch.B4 - note.Pitch) * 5);
            note.Righthigh = new Point(thisPoint.X + 7, thisPoint.Y + 11 + (GeneticPitch.B4 - note.Pitch) * 5);
            note.Rightbottom = new Point(thisPoint.X + 7, thisPoint.Y + 31 + (GeneticPitch.B4 - note.Pitch) * 5);
            
            if (note.isSharp == true)
            {
                g.DrawString(String.Format("{0}", (char)MusicalFont.Sharp), sharpfont, brush, thisPoint.X - 21, thisPoint.Y - 32 + (14 - (int)note.Pitch) * 5);
            }

            switch (note.Duration)
            {
                // 16분음표
                case 3:
                    DrawNote(note.isRest, NoteName.Sixteen, 0);
                    break;
                // 8분음표 셋잇단음표
                case 4:
                    DrawNote(note.isRest, NoteName.Eight, 0);
                    DrawTriple(note.Duration);
                    break;
                // 8분음표
                case 6:
                    DrawNote(note.isRest, NoteName.Eight, 0);
                    break;
                // 4분음표 셋잇단음표
                case 8:
                    if(triplePoint != 640) // 만일 앞에 셋잇 8분음표가 있다거나 아무것도 없으면 그냥 넣으면 되는데 앞에 셋잇 4분음표가 있으면 쪼개야 함
                    {
                        DrawNote(note.isRest, NoteName.Four, 0);
                        DrawTriple(note.Duration);
                    }
                    else // 셋잇 8분음표로 쪼개야 하는 경우 (셋잇 8분음표 + 4)
                    {
                        defaultXplus = false;

                        DrawNote(note.isRest, NoteName.Eight, 0);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                        DrawTriple(4);
                        JumpPoint(ref thisPoint, 4);

                        DrawNote(note.isRest, NoteName.Eight, 0);
                        DrawTriple(4);
                        JumpPoint(ref thisPoint, 4);
                    }
                    break;
                // 점 8분음표
                case 9:
                    DrawNote(note.isRest, NoteName.Eight, 1);
                    break;
                // 4분음표
                case 12:
                    // 앞에 셋잇단음표가 없을 때
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Four, 0);
                    }
                    else
                    {
                        defaultXplus = false;

                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if(note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                        }
                        else if(triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Eight, 0);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);
                        }
                        else
                        {
                            throw new Exception("case 12 : trplePoint = " + triplePoint);
                        }
                    }
                    
                    break;
                // 4분음표 + 1
                case 15:
                    defaultXplus = false;

                    DrawNote(note.isRest, NoteName.Four, 0);
                    if (note.isRest == false)
                        DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                    JumpPoint(ref thisPoint, 12);

                    DrawNote(note.isRest, NoteName.Sixteen, 0);
                    JumpPoint(ref thisPoint, 3);
                    break;
                // 4분음표 + 8분셋잇단음표
                case 16:
                    defaultXplus = false;
                    // 앞에 셋잇단음표가 없을 때는 4분음표와 8분 셋잇단음표를 만들고 잇는다
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Four, 0);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                        JumpPoint(ref thisPoint, 12);

                        DrawNote(note.isRest, NoteName.Eight, 0);
                        DrawTriple(4);
                        JumpPoint(ref thisPoint, 4);
                    }
                    else
                    {
                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 4분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                            
                            DrawNote(note.isRest, NoteName.Four, 0);
                            JumpPoint(ref thisPoint, 12);
                        }
                        else
                        {
                            throw new Exception("case 16 : trplePoint = " + triplePoint);
                        }
                    }
                    break;
                // 점 4분음표
                case 18:
                    DrawNote(note.isRest, NoteName.Four, 1);
                    break;
                // 4분음표 + 4분셋잇단음표
                case 20:
                    defaultXplus = false;
                    // 앞에 셋잇단음표가 없을 때는 4분음표와 4분 셋잇단음표를 만들고 잇는다
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Four, 0);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                        JumpPoint(ref thisPoint, 12);

                        DrawNote(note.isRest, NoteName.Four, 0);
                        DrawTriple(8);
                        JumpPoint(ref thisPoint, 8);
                    }
                    else
                    {
                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 4분음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            JumpPoint(ref thisPoint, 12);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                            JumpPoint(ref thisPoint, 12);

                            DrawNote(note.isRest, NoteName.Eight, 0);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                        }
                        else
                        {
                            throw new Exception("case 20 : trplePoint = " + triplePoint);
                        }
                    }

                    break;
                // 점점 4분음표
                case 21:
                    if (note.isRest == true)
                    {
                        Draw4Rest(2);
                    }
                    else
                    {
                        Draw4Note(2);
                    }
                    break;
                // 2분음표
                case 24:
                    // 앞에 셋잇단음표가 없을 때는 4분음표와 4분 셋잇단음표를 만들고 잇는다
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Two, 0);
                    }
                    else
                    {
                        defaultXplus = false;
                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 4분음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                            JumpPoint(ref thisPoint, 12);

                            DrawNote(note.isRest, NoteName.Eight, 0);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                            JumpPoint(ref thisPoint, 12);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);
                        }
                        else
                        {
                            throw new Exception("case 24 : trplePoint = " + triplePoint);
                        }
                    }
                    break;
                // 2분음표 + 1
                case 27:
                    DrawNote(note.isRest, NoteName.Two, 0);
                    if (note.isRest == false)
                        DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                    JumpPoint(ref thisPoint, 24);

                    DrawNote(note.isRest, NoteName.Sixteen, 0);
                    JumpPoint(ref thisPoint, 3);
                    
                    defaultXplus = false;

                    break;
                // 2분음표 + 8분셋잇단음표
                case 28:
                    defaultXplus = false;
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Two, 0);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                        JumpPoint(ref thisPoint, 24);

                        DrawNote(note.isRest, NoteName.Eight, 0);
                        DrawTriple(4);
                        JumpPoint(ref thisPoint, 4);
                    }
                    else
                    {
                        if (triplePoint == 320)
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 12);
                            JumpPoint(ref thisPoint, 12);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Two, 0);
                            JumpPoint(ref thisPoint, 24);
                        }
                        else
                        {
                            throw new Exception("case 28 : trplePoint = " + triplePoint);
                        }
                    }
                    break;
                // 2분음표 + 2
                case 30:
                    DrawNote(note.isRest, NoteName.Two, 0);
                    if (note.isRest == false)
                        DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                    JumpPoint(ref thisPoint, 24);

                    DrawNote(note.isRest, NoteName.Eight, 0);
                    JumpPoint(ref thisPoint, 6);

                    defaultXplus = false;
                    break;
                // 2분음표 + 4분셋잇단음표
                case 32:
                    defaultXplus = false;
                    // 앞에 셋잇단음표가 없을 때는 4분음표와 4분 셋잇단음표를 만들고 잇는다
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Two, 0);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                        JumpPoint(ref thisPoint, 24);

                        DrawNote(note.isRest, NoteName.Four, 0);
                        DrawTriple(8);
                        JumpPoint(ref thisPoint, 8);
                    }
                    else
                    {
                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 4분음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Two, 0);
                            JumpPoint(ref thisPoint, 24);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Two, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                            JumpPoint(ref thisPoint, 24);

                            DrawNote(note.isRest, NoteName.Eight, 0);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                        }
                        else
                        {
                            throw new Exception("case 20 : trplePoint = " + triplePoint);
                        }
                    }

                    break;
                // 2분음표 + 3
                case 33:
                    DrawNote(note.isRest, NoteName.Two, 0);
                    if (note.isRest == false)
                        DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                    JumpPoint(ref thisPoint, 24);

                    DrawNote(note.isRest, NoteName.Eight, 1);
                    JumpPoint(ref thisPoint, 9);

                    defaultXplus = false;
                    break;
                // 점 2분음표
                case 36:
                    // 앞에 셋잇단음표가 없을 때는 4분음표와 4분 셋잇단음표를 만들고 잇는다
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Two, 1);
                    }
                    else
                    {
                        defaultXplus = false;
                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 4분음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Two, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                            JumpPoint(ref thisPoint, 24);

                            DrawNote(note.isRest, NoteName.Eight, 0);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Two, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                            JumpPoint(ref thisPoint, 24);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);
                        }
                        else
                        {
                            throw new Exception("case 36 : trplePoint = " + triplePoint);
                        }
                    }
                    break;
                // 점2분음표 + 1
                case 39:
                    DrawNote(note.isRest, NoteName.Two, 1);
                    if (note.isRest == false)
                        DrawLegato(note.Pitch >= GeneticPitch.B4, 36);
                    JumpPoint(ref thisPoint, 36);

                    DrawNote(note.isRest, NoteName.Sixteen, 0);
                    JumpPoint(ref thisPoint, 3);

                    defaultXplus = false;

                    break;
                // 점2분음표 + 8분셋잇단음표
                case 40:
                    defaultXplus = false;
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Two, 1);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 36);
                        JumpPoint(ref thisPoint, 36);

                        DrawNote(note.isRest, NoteName.Eight, 0);
                        DrawTriple(4);
                        JumpPoint(ref thisPoint, 4);
                    }
                    else
                    {
                        if (triplePoint == 320)
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 24);
                            JumpPoint(ref thisPoint, 24);

                            DrawNote(note.isRest, NoteName.Four, 0);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Two, 1);
                            JumpPoint(ref thisPoint, 36);
                        }
                        else
                        {
                            throw new Exception("case 28 : trplePoint = " + triplePoint);
                        }
                    }
                    break;
                // 점점 2분음표
                case 42:
                    DrawNote(note.isRest, NoteName.Two, 2);
                    break;
                // 점2분음표 + 4분셋잇단음표
                case 44:
                    defaultXplus = false;
                    // 앞에 셋잇단음표가 없을 때는 4분음표와 4분 셋잇단음표를 만들고 잇는다
                    if (triplePoint == 0)
                    {
                        DrawNote(note.isRest, NoteName.Two, 1);
                        if (note.isRest == false)
                            DrawLegato(note.Pitch >= GeneticPitch.B4, 36);
                        JumpPoint(ref thisPoint, 36);

                        DrawNote(note.isRest, NoteName.Four, 0);
                        DrawTriple(8);
                        JumpPoint(ref thisPoint, 8);
                    }
                    else
                    {
                        if (triplePoint == 320) // 앞에 8분 셋잇단음표가 있으면 4분 셋잇단음표와 4분음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Four, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 8);
                            DrawTriple(8);
                            JumpPoint(ref thisPoint, 8);

                            DrawNote(note.isRest, NoteName.Two, 1);
                            JumpPoint(ref thisPoint, 36);
                        }
                        else if (triplePoint == 640) // 앞에 4분 셋잇단음표가 있으면 8분 셋잇단음표와 4분음표와 8분 셋잇단음표를 만들고 잇는다
                        {
                            DrawNote(note.isRest, NoteName.Eight, 0);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 4);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);

                            DrawNote(note.isRest, NoteName.Two, 1);
                            if (note.isRest == false)
                                DrawLegato(note.Pitch >= GeneticPitch.B4, 36);
                            JumpPoint(ref thisPoint, 36);

                            DrawNote(note.isRest, NoteName.Eight, 0);
                            DrawTriple(4);
                            JumpPoint(ref thisPoint, 4);
                        }
                        else
                        {
                            throw new Exception("case 44 : trplePoint = " + triplePoint);
                        }
                    }

                    break;
                // 점점점 2분음표
                case 45:
                    DrawNote(note.isRest, NoteName.Two, 3);
                    break;
                // 온음표
                case 48:
                    DrawNote(note.isRest, NoteName.One, 0);
                    break;
                default:
                    break;
            }
            
            if(defaultXplus == true)
            {
                JumpPoint(ref thisPoint, note.Duration);
            }
            
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 스크롤은 한번만 변경되면 되기 때문에 쓸때없이 여러번 변경되는 것을 막기 위해서 락커를 둔다
        /// </summary>
        public bool ScrollLocker { get; set; } = false;

        /// <summary>
        /// 현재의 악보 크기에 맞는 스크롤러를 생성한다
        /// </summary>
        /// <remarks>
        /// 반드시 이 함수를 호출하기 전에 ScrollLocker 를 true 로 만들어야 한다
        /// </remarks>
        private void ScrollingScore()
        {
            if(ScrollLocker == true)
            {
                StaticVM.MainViewModel.ScrollingLabel.Height = ScoreHeight;
                StaticVM.MainViewModel.ScoreDocument.Refresh();
                ScrollLocker = false;
            }
        }

        /// <summary>
        /// NumPad 4, 6을 눌렀을 때 노트 선택을 다른걸로 할 수 있게 한다
        /// </summary>
        /// <param name="isLeft">왼쪽이면 True, 오른쪽이면 False</param>
        private void SelectWithArrowKey(bool isLeft)
        {
            for(int i=0; i < StaticRepo.ScoreRepository.SelectedBarList.Count; i++)
            {
                for(int j=0; j< StaticRepo.ScoreRepository.SelectedBarList[i].Notes.Count; j++)
                {
                    if (StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j].isSeleted == true)
                    {
                        if(isLeft == true)
                        {
                            // 만약에 맨 처음인데 Left 를 하면 그냥 탈출한다
                            if (i == 0 && j == 0)
                            {
                                return;
                            }

                            StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j].isSeleted = false;

                            // 만약에 Bar 의 맨 앞부분이면 그 전 Bar 가장 나중의 노트를 선택한다
                            if (j == 0)
                            {
                                StaticRepo.ScoreRepository.SelectedBarList[i - 1].Notes.Last().isSeleted = true;
                                selectedNote = StaticRepo.ScoreRepository.SelectedBarList[i - 1].Notes.Last();
                            }
                            else
                            {
                                StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j - 1].isSeleted = true;
                                selectedNote = StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j - 1];
                            }

                        }
                        else
                        {
                            // 만약에 맨 마지막인데 Right 를 하면 그냥 탈출한다
                            if(i == StaticRepo.ScoreRepository.SelectedBarList.Count - 1 && j == StaticRepo.ScoreRepository.SelectedBarList.Last().Notes.Count - 1)
                            {
                                return;
                            }

                            StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j].isSeleted = false;

                            // 만약에 Bar 의 맨 뒷부분이면 그 후 Bar 가장 처음의 노트를 선택한다
                            if (j == StaticRepo.ScoreRepository.SelectedBarList[i].Notes.Count - 1)
                            {
                                StaticRepo.ScoreRepository.SelectedBarList[i + 1].Notes[0].isSeleted = true;
                                selectedNote = StaticRepo.ScoreRepository.SelectedBarList[i + 1].Notes[0];
                            }
                            else
                            {
                                StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j + 1].isSeleted = true;
                                selectedNote = StaticRepo.ScoreRepository.SelectedBarList[i].Notes[j + 1];
                            }
                        }

                        return;
                    }
                }
            }
        }
        
        /// <summary>
        /// Note 를 입력한다(16분음표 = 240)
        /// </summary>
        /// <param name="offset">노트의 위치</param>
        /// <param name="duration">노트의 길이</param>
        /// <param name="pitch">노트의 피치</param>
        /// <param name="channel">노트의 채널</param>
        /// <param name="velocity">노트의 벨로시티</param>
        private void InsertNote(int offset, int duration, Pitch pitch, Channel channel, int velocity)
        {
            int findedIndex = playList.FindIndex((v) => v.Offset == offset);
            if (findedIndex == -1) // 만일 없다면 추가한다
            {
                PlaySignal newNote = new PlaySignal
                {
                    Offset = offset,
                    SignalList = new List<Signal>()
                };

                newNote.SignalList.Add(new NoteSignal
                {
                    NoteOn = true,
                    Pitch = pitch,
                    Velocity = velocity,
                    Channel = channel
                });

                playList.Add(newNote);
            }
            else // 있으면 그곳에 추가한다
            {
                playList[findedIndex].SignalList.Add(new NoteSignal
                {
                    NoteOn = true,
                    Pitch = pitch,
                    Velocity = velocity,
                    Channel = channel
                });
            }

            findedIndex = playList.FindIndex((v) => v.Offset == offset + duration);

            if (findedIndex == -1) // 만일 없다면 추가한다
            {
                PlaySignal newNote = new PlaySignal
                {
                    Offset = offset + duration,
                    SignalList = new List<Signal>()
                };

                newNote.SignalList.Add(new NoteSignal
                {
                    NoteOn = false,
                    Pitch = pitch,
                    Velocity = velocity,
                    Channel = channel
                });

                playList.Add(newNote);
            }
            else // 있으면 그곳에 추가한다
            {
                playList[findedIndex].SignalList.Add(new NoteSignal
                {
                    NoteOn = false,
                    Pitch = pitch,
                    Velocity = velocity,
                    Channel = channel
                });
            }
        }

        /// <summary>
        /// Control 을 입력한다(16분음표 = 240)
        /// </summary>
        /// <param name="offset">컨트롤의 위치</param>
        /// <param name="control">어떤 컨트롤인지</param>
        /// <param name="value">컨트롤의 Value</param>
        /// <param name="channel">컨트롤의 채널</param>
        private void InsertControl(int offset, Midi.Control control, int value, Channel channel)
        {
            int findedIndex = playList.FindIndex((v) => v.Offset == offset);
            if (findedIndex == -1) // 만일 없다면 추가한다
            {
                PlaySignal newNote = new PlaySignal
                {
                    Offset = offset,
                    SignalList = new List<Signal>()
                };

                newNote.SignalList.Add(new ControlSignal
                {
                    Control = control,
                    Value = value,
                    Channel = channel
                });

                playList.Add(newNote);
            }
            else // 있으면 그곳에 추가한다
            {
                playList[findedIndex].SignalList.Add(new ControlSignal
                {
                    Control = control,
                    Value = value,
                    Channel = channel
                });
            }
        }

        #endregion
        
        #region Drawing Notes and Rests Methods


        public enum NoteName
        {
            Sixteen = 16,
            Eight = 8,
            Four = 4,
            Two = 2,
            One = 1
        }

        public void DrawNote(bool isRest, NoteName name, int numofDot)
        {
            if (isRest == true)
            {
                if (name == NoteName.Sixteen)
                {
                    Draw16Rest(numofDot);
                }
                else if (name == NoteName.Eight)
                {
                    Draw8Rest(numofDot);
                }
                else if (name == NoteName.Four)
                {
                    Draw4Rest(numofDot);
                }
                else if (name == NoteName.Two)
                {
                    Draw2Rest(numofDot);
                }
                else if (name == NoteName.One)
                {
                    Draw1Rest();
                }
            }
            else
            {
                if (name == NoteName.Sixteen)
                {
                    Draw16Note(numofDot);
                }
                else if (name == NoteName.Eight)
                {
                    Draw8Note(numofDot);
                }
                else if (name == NoteName.Four)
                {
                    Draw4Note(numofDot);
                }
                else if (name == NoteName.Two)
                {
                    Draw2Note(numofDot);
                }
                else if (name == NoteName.One)
                {
                    Draw1Note();
                }
            }
        }


        public void Draw16Note(int numOfDot)
        {
            if (note.Pitch < GeneticPitch.B4)
            {
                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootBlack), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarUp), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.Note16Up), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
            }
            else
            {
                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootBlack), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarDown), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.Note16Down), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
            }
        }

        public void Draw16Rest(int numOfDot)
        {
            g.DrawString(String.Format("{0}", (char)MusicalFont.Rest16), font, brush, thisPoint.X - 12, thisPoint.Y + 36);
        }

        public void Draw8Note(int numOfDot)
        {
            if (note.Pitch < GeneticPitch.B4)
            {

                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootBlack), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarUp), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.Note8Up), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);

                if (numOfDot == 1)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 37 + (14 - (int)note.Pitch) * 5);
                }
            }
            else
            {

                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootBlack), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarDown), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.Note8Down), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);

                if (numOfDot == 1)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 38 + (14 - (int)note.Pitch) * 5);
                }
            }
        }

        public void Draw8Rest(int numOfDot)
        {
            g.DrawString(String.Format("{0}", (char)MusicalFont.Rest8), font, brush, thisPoint.X - 12, thisPoint.Y - 40);

            if (numOfDot == 1)
            {
                g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 37);
            }
        }

        public void Draw4Note(int numOfDot)
        {
            if (note.Pitch < GeneticPitch.B4)
            {

                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootBlack), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarUp), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);

                if(numOfDot == 1)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 37 + (14 - (int)note.Pitch) * 5);
                }
                else if(numOfDot == 2)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 10, thisPoint.Y - 37 + (14 - (int)note.Pitch) * 5);
                }
            }
            else
            {
                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootBlack), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarDown), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);

                if (numOfDot == 1)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 38 + (14 - (int)note.Pitch) * 5);
                }
                else if (numOfDot == 2)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 10, thisPoint.Y - 37 + (14 - (int)note.Pitch) * 5);
                }
            }
        }

        public void Draw4Rest(int numOfDot)
        {
            g.DrawString(String.Format("{0}", (char)MusicalFont.Rest4), font, brush, thisPoint.X - 12, thisPoint.Y - 40);
            
            if (numOfDot == 1)
            {
                g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 37);
            }
            else if (numOfDot == 2)
            {
                g.DrawString(String.Format("."), font, brush, thisPoint.X + 10, thisPoint.Y - 37);
            }
        }


        public void Draw2Note(int numOfDot)
        {
            if (note.Pitch < GeneticPitch.B4)
            {
                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootHole), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarUp), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);

                if (numOfDot == 1)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 37 + (14 - (int)note.Pitch) * 5);
                }
                else if (numOfDot == 2)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 16, thisPoint.Y - 38 + (14 - (int)note.Pitch) * 5);
                }
                else if (numOfDot == 3)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 16, thisPoint.Y - 38 + (14 - (int)note.Pitch) * 5);
                }
            }
            else
            {

                g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootHole), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
                g.DrawString(String.Format("{0}", (char)MusicalFont.NotePillarDown), font, brush, thisPoint.X - 11, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);

                if (numOfDot == 1)
                {
                    g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 38 + (14 - (int)note.Pitch) * 5);
                }
                else if (numOfDot == 2)
                {

                }
            }
        }

        public void Draw2Rest(int numOfDot)
        {
            g.DrawString(String.Format("{0}", (char)MusicalFont.Rest1), font, brush, thisPoint.X - 12, thisPoint.Y - 40);

            if (numOfDot == 1)
            {
                g.DrawString(String.Format("."), font, brush, thisPoint.X + 4, thisPoint.Y - 37);
            }
            else if (numOfDot == 2)
            {

            }
        }
        
        /// <summary>
        /// 수정 필요
        /// </summary>
        public void Draw1Rest()
        {
            g.DrawString(String.Format("{0}", (char)MusicalFont.Rest1), font, brush, thisPoint.X - 12, thisPoint.Y - 40);
        }

        public void Draw1Note()
        {
            g.DrawString(String.Format("{0}", (char)MusicalFont.NoteRootHole), font, brush, thisPoint.X - 12, thisPoint.Y - 36 + (14 - (int)note.Pitch) * 5);
        }

        public void DrawLegato(bool isReverse, int duration)
        {
            if(isReverse == false)
            {
                g.DrawImage(legato, thisPoint.X - 2, thisPoint.Y + (96 - ((int)note.Pitch * 5)), (int)((((double)duration / 3f * barWidth) / 16f * Strength) + (((double)barWidth / (double)noteCount) * (1f - Strength))) + 2, 10);
            }
            else
            {
                g.DrawImage(reverseLegato, thisPoint.X, thisPoint.Y + (76 - ((int)note.Pitch * 5)), (int)((((double)duration / 3f * barWidth) / 16f * Strength) + (((double)barWidth / (double)noteCount) * (1f - Strength))), 10);
            }
        }



        private void DrawTriple(int duration)
        {
            duration *= 80;
            Point point = new Point();

            if (note.isRest == true)
            {
                point.X = thisPoint.X;
                point.Y = thisPoint.Y - 5;
            }
            else if (note.Pitch < GeneticPitch.B4)
            {
                point.X = thisPoint.X + 3;
                point.Y = thisPoint.Y + (10 - (int)note.Pitch) * 5;
            }
            else
            {
                point.X = thisPoint.X;
                point.Y = thisPoint.Y + (16 - (int)note.Pitch) * 5;
            }


            if (triplePoint == 0)
            {
                tripleStart = point;
                triplePoint += duration;
            }
            else
            {
                if (triplePoint + duration == 960)
                {
                    tripleEnd = point;

                    // 셋잇단음표 표시를 한다
                    g.DrawLine(blackLine, new Point(tripleStart.X, tripleStart.Y), new Point(tripleStart.X, tripleStart.Y - 10));
                    g.DrawLine(blackLine, new Point(tripleEnd.X, tripleEnd.Y), new Point(tripleEnd.X, tripleEnd.Y - 10));
                    // g.DrawLine(blackLine, new Point(tripleStart.X, tripleStart.Y - 10), new Point(tripleEnd.X, tripleEnd.Y - 10));

                    g.DrawLine(blackLine, new Point(tripleStart.X, tripleStart.Y - 10),
                        new Point((int)((double)(tripleEnd.X + tripleStart.X) / 2f - 5f), (int)((double)(tripleEnd.Y + tripleStart.Y - 20f) / 2f - (double)(tripleEnd.Y - tripleStart.Y) / (double)(tripleEnd.X - tripleStart.X) * 5f)));

                    g.DrawLine(blackLine, new Point((int)((double)(tripleEnd.X + tripleStart.X) / 2f + 5f), (int)((double)(tripleEnd.Y + tripleStart.Y - 20f) / 2f + (double)(tripleEnd.Y - tripleStart.Y) / (double)(tripleEnd.X - tripleStart.X) * 5f)),
                        new Point(tripleEnd.X, tripleEnd.Y - 10));

                    g.DrawString("3", geneticfont, blackBrush, ((tripleEnd.X + tripleStart.X) / 2 - 5), (tripleEnd.Y + tripleStart.Y) / 2 - 20);

                    triplePoint = 0;
                }
                else
                {
                    triplePoint += duration;
                }
            }
        }



        public void JumpPoint(ref Point thisPoint, int duration)
        {
            thisPoint.X += (int)((((double)duration / 3f * barWidth) / 16f * Strength) // 박자에 의한 간격
                                        + (((double)barWidth / (double)noteCount) * (1f - Strength)));
            this.thisPoint = thisPoint;
        }

        #endregion
    }
}
