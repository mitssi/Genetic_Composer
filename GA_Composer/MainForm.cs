using GA_Composer.Models;
using GA_Composer.Repositories;
using GA_Composer.ViewModels;
using Midi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace GA_Composer
{
    public partial class MainForm : Telerik.WinControls.UI.RadRibbonForm
    {
        #region Private Field

        RadLabel[][] ChordTableLabels;
        double[][][] SelectedChordTable;

        #endregion

        public MainForm()
        {
            InitializeComponent();
            Init();
            
            for (int i = 0; i < StaticRepo.ConfigRepository.NumberOfBar; i++)
            {
                StaticRepo.ScoreRepository.GABarList.Add(new GeneticBar());
            }

            //for (int i=0; i<StaticRepo.ScoreRepository.BarList.Count; i++)
            //{
            //    StaticRepo.ScoreRepository.GenerateRandomNotes(i);
            //}

            StaticRepo.ScoreRepository.GenerateMoneyChord();
            StaticRepo.ScoreRepository.MakeChordListUsingGA();
            
            StaticRepo.GARepository.iGA.InitGA(StaticRepo.ConfigRepository.NumberOfBar);
            StaticRepo.ScoreRepository.MakeNotesUsingGA();

            // radLabel_ScoreScolling.Height = StaticVM.ScoreViewModel.ScoreHeight;
            // radLabel_ScoreScolling.Size = new Size(10, StaticVM.ScoreViewModel.ScoreHeight);
        }

        /// <summary>
        /// 초기 설정
        /// </summary>
        private void Init()
        {
            #region Make Chordtable

            SelectedChordTable = StaticRepo.TableRepository.SavedTable.Chordtable;
            ChordTableLabels = new RadLabel[14][];
            
            for (int i=0; i<14; i++)
            {
                ChordTableLabels[i] = new RadLabel[13];

                for(int j=0; j<13; j++)
                {
                    ChordTableLabels[i][j] = new RadLabel
                    {
                        Font = new Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                        BackColor = Color.Transparent,
                        ForeColor = Color.Black,
                        Text = "100",
                        ThemeName = "Office2013Light",
                        TextAlignment = ContentAlignment.MiddleCenter
                    };

                    // Light Gray Color
                    if(i == 3 || i == 5 || i == 8 || i == 10 || i == 12
                        || j == 2 || j == 4 || j == 7 || j == 9 || j == 11)
                    {
                        ChordTableLabels[i][j].BackColor = Color.LightGray;
                    }

                    // Dim Gray Color
                    if ((i == 0 || i == 3 || i == 5 || i == 8 || i == 10 || i == 12) &&
                        (j == 0 || j == 2 || j == 4 || j == 7 || j == 9 || j == 11))
                    {
                        ChordTableLabels[i][j].BackColor = Color.DimGray;
                        ChordTableLabels[i][j].ForeColor = Color.White;
                    }

                    if(i == 1)
                    {
                        ChordTableLabels[i][j].BackColor = Color.AliceBlue;
                    }
                    
                    documentWindow_ChordTable.Controls.Add(ChordTableLabels[i][j]);
                }
            }
            
            ChordTableLabels[0][0].BackColor = Color.Transparent;
            ChordTableLabels[0][0].Text = "";

            ChordTableLabels[0][1].Text = "C";
            ChordTableLabels[0][2].Text = "C#";
            ChordTableLabels[0][3].Text = "D";
            ChordTableLabels[0][4].Text = "D#";
            ChordTableLabels[0][5].Text = "E";
            ChordTableLabels[0][6].Text = "F";
            ChordTableLabels[0][7].Text = "F#";
            ChordTableLabels[0][8].Text = "G";
            ChordTableLabels[0][9].Text = "G#";
            ChordTableLabels[0][10].Text = "A";
            ChordTableLabels[0][11].Text = "A#";
            ChordTableLabels[0][12].Text = "B";

            ChordTableLabels[1][0].Text = "＠";
            ChordTableLabels[2][0].Text = "C";
            ChordTableLabels[3][0].Text = "C#";
            ChordTableLabels[4][0].Text = "D";
            ChordTableLabels[5][0].Text = "D#";
            ChordTableLabels[6][0].Text = "E";
            ChordTableLabels[7][0].Text = "F";
            ChordTableLabels[8][0].Text = "F#";
            ChordTableLabels[9][0].Text = "G";
            ChordTableLabels[10][0].Text = "G#";
            ChordTableLabels[11][0].Text = "A";
            ChordTableLabels[12][0].Text = "A#";
            ChordTableLabels[13][0].Text = "B";

            object locking = new object();

            documentWindow_ChordTable.SizeChanged += async (sender, e) =>
            {
                await Task.Delay(1000);

                int PlusWidth = (documentWindow_ChordTable.Width - 220) / 13;
                int PlusHeight = (documentWindow_ChordTable.Height - 52) / 12;
                
                for (int i = 0; i < 14; i++)
                {
                    for (int j = 0; j < 13; j++)
                    {
                        ChordTableLabels[i][j].Invoke(new MethodInvoker(() => {
                            ChordTableLabels[i][j].Location = new Point(170 + PlusWidth * i, 15 + PlusHeight * j);
                        }));
                    }
                }
            };
            
            for (int i=0; i< Enum.GetNames(typeof(ChordTableEnum)).Length; i++)
            {
                radListView_ChordTableList.Items.Add((ChordTableEnum)i);
            }

            radListView_ChordTableList.SelectedIndex = 0;

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    ChordTableLabels[i + 1][j + 1].Text =
                    Math.Round(SelectedChordTable[0][i][j], 2).ToString();
                }
            }

            radListView_ChordTableList.SelectedIndexChanged += (sender, e) => {
                for(int i=0; i<13; i++)
                {
                    for(int j=0; j<12; j++)
                    {
                        ChordTableLabels[i + 1][j + 1].Text =
                        Math.Round(SelectedChordTable[radListView_ChordTableList.SelectedIndex][i][j],2).ToString();
                    }
                }
            };

            radListView_ChordTableList.SelectedIndex = 0;


            #endregion

            StaticVM.MainViewModel.ScoreDocument = documentWindow_Score;
            StaticVM.MainViewModel.SequenceTableDocument = documentWindow_SequenceTable;
            StaticVM.MainViewModel.ChordTableDocument = documentWindow_ChordTable;
            StaticVM.MainViewModel.ScrollingLabel = radLabel_ScoreScolling;
            StaticVM.ScoreViewModel.ScoreHeightMargin = (int)(radTrackBarElement_StaffInterver.Value / 50 * 60) + 40;
            StaticVM.MainViewModel.Play = radButtonElement_ScorePlay;
            StaticVM.MainViewModel.Pause = radButtonElement_ScorePause;
            StaticVM.MainViewModel.Stop = radButtonElement_ScoreStop;
            StaticVM.MainViewModel.TempoTextBox = this.radTextBoxElement_Tempo;

            StaticVM.MainViewModel.Fitness1 = radTextBoxElement_Fitness1;
            StaticVM.MainViewModel.Fitness2 = radTextBoxElement_Fitness2;
            StaticVM.MainViewModel.Fitness3 = radTextBoxElement_Fitness3;
            StaticVM.MainViewModel.Fitness4 = radTextBoxElement_Fitness4;
            StaticVM.MainViewModel.Fitness5 = radTextBoxElement_Fitness5;
            StaticVM.MainViewModel.Fitness6 = radTextBoxElement_Fitness6;
            StaticVM.MainViewModel.Fitness7 = radTextBoxElement_Fitness7;
            StaticVM.MainViewModel.Fitness8 = radTextBoxElement_Fitness8;
            StaticVM.MainViewModel.Fitness9 = radTextBoxElement_Fitness9;

            StaticVM.MainViewModel.Moniter_generation = radLabel_Moniter_generation;
            StaticVM.MainViewModel.Moniter_bestfitness = radLabel_Moniter_bestfitness;
            StaticVM.MainViewModel.Moniter_mutation = radLabel_Moniter_mutation;
            StaticVM.MainViewModel.Moniter_population = radLabel_Moniter_population;
            StaticVM.MainViewModel.Moniter_rouletteK = radLabel_Moniter_rouletteK;
            StaticVM.MainViewModel.Moniter_averagefitness = radLabel_Moniter_averagefit;
            StaticVM.MainViewModel.Moniter_elitism = radLabel_Moniter_elitism;
            StaticVM.MainViewModel.Moniter_legatopercentage = radLabel_Moniter_legato;

            StaticRepo.ConfigRepository.FitnessConstant_OneNoteChordTable = double.Parse(radTextBoxElement_Fitness1.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TwoNoteChordTable = double.Parse(radTextBoxElement_Fitness2.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNotes = double.Parse(radTextBoxElement_Fitness3.Text);
            StaticRepo.ConfigRepository.FitnessConstant_BetweenHighAndLow = double.Parse(radTextBoxElement_Fitness4.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfChange = double.Parse(radTextBoxElement_Fitness5.Text);
            StaticRepo.ConfigRepository.FitnessConstant_BetweenNotesPitch = double.Parse(radTextBoxElement_Fitness6.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNoteLength = double.Parse(radTextBoxElement_Fitness7.Text);
            StaticRepo.ConfigRepository.FitnessConstant_IsFirstNoteInChord = double.Parse(radTextBoxElement_Fitness8.Text);
            StaticRepo.ConfigRepository.FitnessConstant_JazzRhythmScore = double.Parse(radTextBoxElement_Fitness9.Text);

            StaticRepo.ConfigRepository.TickPerMinNote = StaticRepo.ConfigRepository.TickPerOneBar / StaticRepo.ConfigRepository.MaxNumOfNote;

            documentWindow_Score.MouseWheel += ScrollingEventHandler;

            #region 드롭다운 리스트 설정

            // 악기
            for (int i = 0; i < 128; i++)
            {
                radDropDownListElement_Instrument.Items.Add(((Instrument)i).ToString());
            }
            radDropDownListElement_Instrument.SelectedIndex = 0;

            // 점수
            for (int i=5; i>=-5; i--)
            {
                radDropDownListElement_Score.Items.Add(i.ToString());
            }
            radDropDownListElement_Score.SelectedIndex = 0;

            #endregion

            #region 디바이스 리스트 설정

            if (OutputDevice.InstalledDevices.Count == 0)
            {
                radButtonElement_ScorePlay.Enabled = false;
            }
            else
            {

                for (int i = 0; i < OutputDevice.InstalledDevices.Count; i++)
                {
                    radDropDownListElement_OutputDevice.Items.Add(OutputDevice.InstalledDevices[i].Name);
                }

                radDropDownListElement_OutputDevice.SelectedIndex = 0;
                StaticRepo.ConfigRepository.MainOutputDevice = OutputDevice.InstalledDevices[radDropDownListElement_OutputDevice.SelectedIndex];
                StaticRepo.ConfigRepository.MainOutputDevice.Open();
            }

            #endregion

            #region 악기 세팅

            StaticRepo.ConfigRepository.MainOutputDevice.SendProgramChange(Channel.Channel2, Instrument.ElectricBassPick);

            #endregion

            #region Data Binding

            StaticVM.MainViewModel.SequenceTable_TheNumberOfNotes = radGridView_SequenceTable_TheNumberOfNotes;
            StaticVM.MainViewModel.SequenceTable_BetweenHighAndLow = radGridView_SequenceTable_BetweenHighAndLow;
            StaticVM.MainViewModel.SequenceTable_TheNumberOfChanges = radGridView_SequenceTable_TheNumberOfChanges;
            StaticVM.MainViewModel.SequenceTable_BetweenPitchofNotes = radGridView_SequenceTable_BetweenPitchofNotes;
            StaticVM.MainViewModel.SequenceTable_TheNumberOfNoteLengths = radGridView_SequenceTable_NumofLength;
            StaticVM.MainViewModel.SequenceTable_Patterns = radGridView_SequenceTable_Patterns;
            StaticVM.MainViewModel.SqeuenceTableDataBinding();

            #endregion

            radTextBoxElement_Tempo.TextBoxItem.LostFocus += TempoCheck;
        }

        #region Event Handler

        /// <summary>
        /// 오선지에 악보를 그린다
        /// </summary>
        /// <remarks>
        /// 악보의 현재 상태를 항상 확인하여 갱신할 수 있게 해줘야 함
        /// </remarks>
        private void documentWindow_Score_Paint(object sender, PaintEventArgs e)
        {
            StaticVM.ScoreViewModel.DrawScore(sender, e);
        }

        private void documentWindow_Score_MouseClick(object sender, MouseEventArgs e)
        {
            StaticVM.ScoreViewModel.ClickForm(sender, e);
        }

        private void radTrackBarElement_NoteSPlitter_ValueChanged(object sender, EventArgs e)
        {
            StaticVM.ScoreViewModel.Strength = (double)(radTrackBarElement_NoteSPlitter.Value / 50);
            this.Refresh();
        }

        private void ScrollingEventHandler(object sender, EventArgs e)
        {
            StaticVM.ScoreViewModel.StartY = -documentWindow_Score.VerticalScroll.Value;
            this.Refresh();
        }

        private void documentWindow_Score_KeyDown_1(object sender, KeyEventArgs e)
        {
            StaticVM.ScoreViewModel.KeyDown(sender, e);

            if (e.KeyData == Keys.F5)
            {
                if(StaticVM.ScoreViewModel.isPlayScoreTrack == false)
                {
                    StaticVM.ScoreViewModel.PlayScore();
                }
                else
                {    
                    StaticVM.ScoreViewModel.isPlayScoreTrack = false;
                    StaticRepo.ConfigRepository.MainOutputDevice.SilenceAllNotes();
                    radButtonElement_ScorePlay.Enabled = true;
                    radButtonElement_ScoreStop.Enabled = false;
                    radButtonElement_ScorePause.Enabled = false;
                }
            }
        }

        private void radTrackBarElement_StaffInterver_ValueChanged(object sender, EventArgs e)
        {
            StaticVM.ScoreViewModel.ScoreHeightMargin = (int)(radTrackBarElement_StaffInterver.Value / 50 * 60) + 40;
            this.Refresh();
        }

        private void radButtonElement_ScorePlay_Click(object sender, EventArgs e)
        {
            StaticVM.ScoreViewModel.PlayScore();
        }

        private void TempoCheck(object sender, EventArgs e)
        {
            double newTempo;
            if (double.TryParse(radTextBoxElement_Tempo.Text, out newTempo) == true)
            {
                if (newTempo < 50 || newTempo > 300)
                {
                    MessageBox.Show("Input the correct tempo value (50 ~ 300)");
                    radTextBoxElement_Tempo.Text = StaticVM.ScoreViewModel.Tempo.ToString();
                }
                else
                {
                    StaticVM.ScoreViewModel.Tempo = newTempo;
                }
            }
            else
            {
                MessageBox.Show("Input the correct tempo value");
                radTextBoxElement_Tempo.Text = StaticVM.ScoreViewModel.Tempo.ToString();
            }
        }

        private void radDropDownListElement_OutputDevice_ValueChanged(object sender, EventArgs e)
        {
            if(StaticRepo.ConfigRepository.MainOutputDevice == null)
            {
                return;
            }

            if (StaticRepo.ConfigRepository.MainOutputDevice.IsOpen == true)
            {
                StaticRepo.ConfigRepository.MainOutputDevice.Close();
            }

            if (OutputDevice.InstalledDevices.Count != 0)
            {
                StaticRepo.ConfigRepository.MainOutputDevice = OutputDevice.InstalledDevices[radDropDownListElement_OutputDevice.SelectedIndex];
            }

            // StaticRepo.DeviceRepository.MainOutputDevice.Close();
        }


        private void radButtonElement_ScoreStop_Click(object sender, EventArgs e)
        {
            StaticVM.ScoreViewModel.isPlayScoreTrack = false;
            StaticRepo.ConfigRepository.MainOutputDevice.SilenceAllNotes();
            radButtonElement_ScorePlay.Enabled = true;
            radButtonElement_ScoreStop.Enabled = false;
            radButtonElement_ScorePause.Enabled = false;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void radButtonElement_GA_NextGeneration_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < int.Parse(radTextBoxElement_SkipGeneration.Text); i++)
            {
                StaticRepo.GARepository.iGA.NextGeneration();
            }

            this.Refresh();
        }

        private void radButtonElement_SaveTables_Click(object sender, EventArgs e)
        {
            StaticVM.TableViewModel.SaveTables();
        }

        private void radButtonElement_ImportMidi_Click(object sender, EventArgs e)
        {
            StaticRepo.ScoreRepository.importMidiFile();
        }

        private void radCheckBoxElement_GA_CheckStateChanged(object sender, EventArgs e)
        {
            if (radCheckBoxElement_GA.Checked == true)
            {
                StaticRepo.ScoreRepository.SelectedScore = SelectedScore.GA;
                radCheckBoxElement_MIDI.Checked = false;
            }
            else
            {
                StaticRepo.ScoreRepository.SelectedScore = SelectedScore.MIDI;
                radCheckBoxElement_MIDI.Checked = true;
            }
        }

        private void radCheckBoxElement_MIDI_CheckStateChanged(object sender, EventArgs e)
        {
            if (radCheckBoxElement_MIDI.Checked == true)
            {
                StaticRepo.ScoreRepository.SelectedScore = SelectedScore.MIDI;
                radCheckBoxElement_GA.Checked = false;
                this.Refresh();
            }
            else
            {
                StaticRepo.ScoreRepository.SelectedScore = SelectedScore.GA;
                radCheckBoxElement_GA.Checked = true;
                this.Refresh();
            }
        }

        private void radButtonElement_GiveScoreFromMIDI_Click(object sender, EventArgs e)
        {
            StaticVM.TableViewModel.Learning(int.Parse(radDropDownListElement_Score.SelectedItem.Text), StaticRepo.ScoreRepository.MidiBarList);
        }

        private void radButtonElement_GiveScoreFromGA_Click(object sender, EventArgs e)
        {
            StaticVM.TableViewModel.Learning(int.Parse(radDropDownListElement_Score.SelectedItem.Text), StaticRepo.ScoreRepository.GABarList);
        }

        private void radTextBoxElement_Fitness_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_OneNoteChordTable = double.Parse(radTextBoxElement_Fitness1.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TwoNoteChordTable = double.Parse(radTextBoxElement_Fitness2.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNotes = double.Parse(radTextBoxElement_Fitness3.Text);
            StaticRepo.ConfigRepository.FitnessConstant_BetweenHighAndLow = double.Parse(radTextBoxElement_Fitness4.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfChange = double.Parse(radTextBoxElement_Fitness5.Text);
            StaticRepo.ConfigRepository.FitnessConstant_BetweenNotesPitch = double.Parse(radTextBoxElement_Fitness6.Text);
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNoteLength = double.Parse(radTextBoxElement_Fitness7.Text);
            StaticRepo.ConfigRepository.FitnessConstant_IsFirstNoteInChord = double.Parse(radTextBoxElement_Fitness8.Text);
            StaticRepo.ConfigRepository.FitnessConstant_JazzRhythmScore = double.Parse(radTextBoxElement_Fitness9.Text);

        }

        private void radButtonElement_ImportAll_Click(object sender, EventArgs e)
        {

        }

        private void radButtonElement_ResetTables_Click(object sender, EventArgs e)
        {
            StaticRepo.TableRepository.SavedTable.SequenceTable.SetAll();
            StaticVM.MainViewModel.SqeuenceTableDataBinding();
            StaticVM.MainViewModel.SequenceTableDocument.Refresh();
        }

        private void radButtonElement_Next100_Click(object sender, EventArgs e)
        {
            for (int j = 0; j < 10; j++)
            {
                StaticRepo.GARepository.iGA.NextGeneration();
            }
            this.Refresh();
        }

        private void radCheckBox_RealScore_CheckStateChanged(object sender, EventArgs e)
        {
            if (radCheckBox_RealScore.Checked == true)
            {
                radCheckBox_ProcessedScore.Checked = false;
                SelectedChordTable = StaticRepo.TableRepository.SavedTable.Chordtable;
            }
            else
            {
                radCheckBox_ProcessedScore.Checked = true;
                SelectedChordTable = StaticRepo.TableRepository.ConvertedChordTable;
            }

            documentWindow_ChordTable.Refresh();
        }

        private void radCheckBox_ProcessedScore_CheckStateChanged(object sender, EventArgs e)
        {
            if (radCheckBox_ProcessedScore.Checked == true)
            {
                radCheckBox_RealScore.Checked = false;
                SelectedChordTable = StaticRepo.TableRepository.ConvertedChordTable;
            }
            else
            {
                radCheckBox_RealScore.Checked = true;
                SelectedChordTable = StaticRepo.TableRepository.SavedTable.Chordtable;
            }

            documentWindow_ChordTable.Refresh();
        }

        private void radTextBoxElement_Mutation_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.Mutation = double.Parse(radTextBoxElement_Mutation.Text);
        }

        private void radTextBoxElement_RouletteK_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.RouletteK = int.Parse(radTextBoxElement_RouletteK.Text);
        }

        private void radTextBoxElement_Elitism_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.Elitism = int.Parse(radTextBoxElement_Elitism.Text);
        }

        private void radTextBoxElement_Legato_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.Legatopercentage = double.Parse(radTextBoxElement_Legato.Text);
        }

        private void radButtonElement_GA_Initialize_Click(object sender, EventArgs e)
        {
            StaticRepo.GARepository.iGA.InitGA(StaticRepo.ConfigRepository.NumberOfBar);
            StaticRepo.ScoreRepository.MakeNotesUsingGA();
            documentWindow_Score.Refresh();
        }

        private void radButtonElement_Music_ImportMIDI_Click(object sender, EventArgs e)
        {
            StaticRepo.ScoreRepository.importBackMidiFile();
        }

        private void radButtonElement_ImportFolder_Click(object sender, EventArgs e)
        {
            StaticRepo.ScoreRepository.importFilesAndLearning(int.Parse(radDropDownListElement_Score.SelectedItem.Text));
        }

        private void radTextBoxElement_Fitness1_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_OneNoteChordTable = double.Parse(radTextBoxElement_Fitness1.Text);
        }

        private void radTextBoxElement_Fitness2_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_TwoNoteChordTable = double.Parse(radTextBoxElement_Fitness2.Text);
        }

        private void radTextBoxElement_Fitness3_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNotes = double.Parse(radTextBoxElement_Fitness3.Text);
        }

        private void radTextBoxElement_Fitness4_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_BetweenHighAndLow = double.Parse(radTextBoxElement_Fitness4.Text);
        }

        private void radTextBoxElement_Fitness5_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfChange = double.Parse(radTextBoxElement_Fitness5.Text);
        }

        private void radTextBoxElement_Fitness6_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_BetweenNotesPitch = double.Parse(radTextBoxElement_Fitness6.Text);
        }

        private void radTextBoxElement_Fitness7_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNoteLength = double.Parse(radTextBoxElement_Fitness7.Text);
        }

        private void radTextBoxElement_Fitness8_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_IsFirstNoteInChord = double.Parse(radTextBoxElement_Fitness8.Text);
        }

        private void radTextBoxElement_Fitness9_TextChanged(object sender, EventArgs e)
        {
            StaticRepo.ConfigRepository.FitnessConstant_JazzRhythmScore = double.Parse(radTextBoxElement_Fitness9.Text);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (StaticRepo.ConfigRepository.MainOutputDevice.IsOpen == true)
            {
                StaticRepo.ConfigRepository.MainOutputDevice.Close();
            }
        }


        #endregion

    }
}
