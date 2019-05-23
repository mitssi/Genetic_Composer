using GA_Composer.Enums;
using GA_Composer.GeneticAlgorithm;
using GA_Composer.Models;
using GA_Composer.ViewModels;
using Midi;
using NAudio.Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GA_Composer.Repositories
{
    public enum SelectedScore
    {
        GA = 1,
        MIDI = 2
    }

    public class ScoreRepository
    {
        #region Field
        
        Random random = new Random(DateTime.Now.Millisecond);
        SelectedScore selectedScore;

        /// <summary>
        /// 노트들이 들어가 있는 리스트
        /// </summary>
        public List<GeneticBar> GABarList = new List<GeneticBar>();

        /// <summary>
        /// 미디 불러오기를 했을 때 노트들이 들어있는 리스트
        /// </summary>
        public List<GeneticBar> MidiBarList { get; set; } = new List<GeneticBar>();

        /// <summary>
        /// 코드들이 들어가 있는 리스트
        /// </summary>
        public List<GeneticChord> ChordList = new List<GeneticChord>();

        /// <summary>
        /// GA에서 사용하는 악보에 있는 Chord List
        /// </summary>
        public ChordTableEnum[][] ChordListUsingGA { get; set; }

        /// <summary>
        /// 반주들의 모음(List<PlaySignal>는 하나의 반주를 의미)
        /// </summary>
        public List<PlaySignal> PlayList { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// 어떤 악보를 다루고 있는가
        /// </summary>
        public SelectedScore SelectedScore
        {
            get
            {
                return selectedScore;
            }
            set
            {
                if (value == SelectedScore.GA)
                {
                    SelectedBarList = GABarList;
                }
                else if (value == SelectedScore.MIDI)
                {
                    SelectedBarList = MidiBarList;
                }
                selectedScore = value;
            }
        }

        public List<GeneticBar> SelectedBarList { get; set; }

        EventDataSet Events { get; set; }

        #endregion

        #region Constructor

        public ScoreRepository()
        {
            SelectedBarList = GABarList;
            Events = new EventDataSet();
            PlayList = new List<PlaySignal>();
        }

        #endregion
        
        #region Common Public Method
        /// <summary>
        /// Chord 를 GeneticChord 로 바꾸어주는 함수
        /// </summary>
        /// <param name="chord">Genetic Chord 로 바꿀 Chord</param>
        /// <returns></returns>
        public ChordTableEnum ChordToGeneticChord(Chord chord)
        {
            return (ChordTableEnum)Enum.Parse(typeof(ChordTableEnum), chord.Name, true);
        }
        #endregion

        #region MIDI Methods
        
        public void importFilesAndLearning(int score)
        {
            #region Create an openDialog to import a midi file

            OpenFileDialog ofd_midiFile = new OpenFileDialog();
            ofd_midiFile.Multiselect = true;
            ofd_midiFile.Filter = "Midi files(*.mid)|*.mid";

            #endregion

            if (ofd_midiFile.ShowDialog() == DialogResult.OK)
            {
                // Showing the file path in the textbox
                foreach(string str in ofd_midiFile.FileNames){
                    try
                    {
                        openMidiFIle(str);
                        StaticVM.TableViewModel.Learning(score, StaticRepo.ScoreRepository.MidiBarList);
                    }
                    catch
                    {
                        MessageBox.Show(str);
                    }
                }
            }
        }

        /// <summary>
        /// 반주 미디파일을 import 하는 함수.
        /// </summary>
        public void importBackMidiFile()
        {
            MidiFile mf_file;
            int iDevideDeltaTime, iTimeSignatureStart, iTimeSignatureEnd, iMicrosecondsPerQuarterNote;
            List<IList<MidiEvent>> ilist;
            string str_filePath, str_fileName;

            #region Create an openDialog to import a midi file

            OpenFileDialog ofd_midiFile = new OpenFileDialog();
            ofd_midiFile.Multiselect = false;
            ofd_midiFile.Filter = "Midi files(*.mid)|*.mid";

            #endregion

            if (ofd_midiFile.ShowDialog() == DialogResult.OK)
            {
                // Showing the file path in the textbox
                str_filePath = ofd_midiFile.FileName;
                int index = str_filePath.LastIndexOf("\\");
                str_fileName = str_filePath.Substring(index + 1);

                // Setting the midifile object path
                mf_file = new MidiFile(str_filePath);
                string sMidiString = mf_file.ToString();

                // Setting the Quarter Note's deltaTick
                iDevideDeltaTime = mf_file.DeltaTicksPerQuarterNote;

                // Setting the time signature value (6/8 -> start = 6, end = 8)
                int iTimeSignatureIndex = sMidiString.IndexOf("TimeSignature");
                string sTempSignature = sMidiString.Substring(iTimeSignatureIndex + "TimeSignature".Length + 1, 3);
                iTimeSignatureStart = Convert.ToInt32(sTempSignature.Substring(0, 1));
                iTimeSignatureEnd = Convert.ToInt32(sTempSignature.Substring(2, 1));

                // Setting the tempo value
                int iTempoIndex = sMidiString.IndexOf("bpm ");
                int iTempoSize = 0;
                while (sMidiString.Substring(iTempoIndex + 5 + iTempoSize, 1) != ")")
                    iTempoSize++;
                if (sMidiString.IndexOf("bpm") != -1)
                {
                    string sTempo = sMidiString.Substring(iTempoIndex + "bpm".Length + 2, iTempoSize);
                    iMicrosecondsPerQuarterNote = Convert.ToInt32(sTempo);
                    StaticVM.ScoreViewModel.Tempo = Math.Round(60000000.0 / iMicrosecondsPerQuarterNote, 4);
                }

                // Making a note collection manager and track combobox
                ilist = new List<IList<MidiEvent>>();
                for (int i = 0; i < mf_file.Events.Tracks; i++)
                {
                    ilist.Add(mf_file.Events.GetTrackEvents(i));
                }

                foreach(var v1 in ilist)
                {
                    foreach(var v2 in v1)
                    {
                        if(v2 is PatchChangeEvent)
                        {
                            StaticRepo.ConfigRepository.MainOutputDevice.SendProgramChange((Channel)(v2.Channel - 1), (Instrument)((v2 as PatchChangeEvent).Patch));
                        }
                        else if(v2 is NoteOnEvent)
                        {
                            var tempList = new List<Signal>();

                            if ((v2 as NoteOnEvent).Velocity != 0)
                            {
                                tempList.Add(new NoteSignal
                                {
                                    Channel = (Channel)(v2.Channel - 1),
                                    NoteOn = true,
                                    Pitch = (Pitch)((v2 as NoteOnEvent).NoteNumber),
                                    Velocity = (v2 as NoteOnEvent).Velocity
                                });
                            }
                            else
                            {
                                tempList.Add(new NoteSignal
                                {
                                    Channel = (Channel)(v2.Channel - 1),
                                    NoteOn = false,
                                    Pitch = (Pitch)((v2 as NoteOnEvent).NoteNumber),
                                    Velocity = (v2 as NoteOnEvent).Velocity
                                });
                            }

                            PlayList.Add(new PlaySignal
                            {
                                Offset = (int)v2.AbsoluteTime*8,
                                SignalList = tempList
                            });
                        }
                    }
                    //PlayList
                }

                StreamWriter sw = new StreamWriter("midievent.txt");
                sw.Write(mf_file.ToString());
                sw.Close();
                StreamReader sr = new StreamReader("midievent.txt");
            }
        }

        private void openMidiFIle(string str_filePath)
        {
            MidiFile mf_file;
            int iDevideDeltaTime, iTimeSignatureStart, iTimeSignatureEnd, iMicrosecondsPerQuarterNote;
            List<IList<MidiEvent>> ilist;

            int index = str_filePath.LastIndexOf("\\");
            string str_fileName = str_filePath.Substring(index + 1);

            // Setting the midifile object path
            mf_file = new MidiFile(str_filePath);
            string sMidiString = mf_file.ToString();

            // Setting the Quarter Note's deltaTick
            iDevideDeltaTime = mf_file.DeltaTicksPerQuarterNote;

            // Setting the time signature value (6/8 -> start = 6, end = 8)
            int iTimeSignatureIndex = sMidiString.IndexOf("TimeSignature");
            string sTempSignature = sMidiString.Substring(iTimeSignatureIndex + "TimeSignature".Length + 1, 3);
            iTimeSignatureStart = Convert.ToInt32(sTempSignature.Substring(0, 1));
            iTimeSignatureEnd = Convert.ToInt32(sTempSignature.Substring(2, 1));

            // Setting the tempo value
            int iTempoIndex = sMidiString.IndexOf("bpm ");
            int iTempoSize = 0;
            while (sMidiString.Substring(iTempoIndex + 5 + iTempoSize, 1) != ")")
                iTempoSize++;
            if (sMidiString.IndexOf("bpm") != -1)
            {
                string sTempo = sMidiString.Substring(iTempoIndex + "bpm".Length + 2, iTempoSize);
                iMicrosecondsPerQuarterNote = Convert.ToInt32(sTempo);
                StaticVM.ScoreViewModel.Tempo = Math.Round(60000000.0 / iMicrosecondsPerQuarterNote, 4);
            }

            // Making a note collection manager and track combobox
            ilist = new List<IList<MidiEvent>>();
            for (int i = 0; i < mf_file.Events.Tracks; i++)
            {
                ilist.Add(mf_file.Events.GetTrackEvents(i));
            }

            //StreamWriter sw = new StreamWriter("midievent.txt");
            //sw.Write(mf_file.ToString());
            //sw.Close();
            //StreamReader sr = new StreamReader("midievent.txt");

            int iEndBar = FindEndBar(ilist);

            ImportMidiEvent(ilist, iDevideDeltaTime);
            ConvertEventDataToBarList();
        }

        /// <summary>
        /// 미디파일을 import 하는 함수. 여기 안에 Openfile 들도 다 있으니 그냥 쓰기만 하면 된다.
        /// MidiBarList 에 미디 데이터가 저장된다.
        /// </summary>
        public void importMidiFile()
        {
            string str_filePath;

            #region Create an openDialog to import a midi file

            OpenFileDialog ofd_midiFile = new OpenFileDialog();
            ofd_midiFile.Multiselect = false;
            ofd_midiFile.Filter = "Midi files(*.mid)|*.mid";

            #endregion

            if (ofd_midiFile.ShowDialog() == DialogResult.OK)
            {
                // Showing the file path in the textbox
                str_filePath = ofd_midiFile.FileName;
                openMidiFIle(str_filePath);
            }

            StaticVM.MainViewModel.ScoreDocument.Refresh();
        }

        private void ConvertEventDataToBarList()
        {
            MidiBarList.Clear();

            var eList = Events.Note.ToList();
            var lastNote = eList.Last();

            // 일단은 마지막 노트의 DeltaTime 을 보고 필요한 만큼 Bar 를 추가시킨다
            for (int i=0; i<int.Parse(lastNote.DeltaTime)/3840; i++)
            {
                MidiBarList.Add(new GeneticBar());
            }
            
            int sumOfNotesDurations = 0;

            foreach (var note in eList)
            {
                if(sumOfNotesDurations != Int32.Parse(note.DeltaTime) - 3840)
                {
                    int restduration = ((int)Math.Round(double.Parse(note.DeltaTime) - 3840 - sumOfNotesDurations) / 80);
                    sumOfNotesDurations += restduration * 80;

                    // 쉼표 마디가 바뀔 때는 나누어서 저장
                    if(sumOfNotesDurations / 3840 != (sumOfNotesDurations - restduration * 80) / 3840)
                    {
                        MidiBarList[(int.Parse(note.DeltaTime) / 3840) - 2].Notes.Add(GeneNoteToGeneticNote(new GeneNote
                        {
                            pitch = 0,
                            duration = (restduration * 80) - (sumOfNotesDurations % 3840)
                        }));

                        if(sumOfNotesDurations % 3840 != 0)
                        {
                            MidiBarList[(int.Parse(note.DeltaTime) / 3840) - 1].Notes.Add(GeneNoteToGeneticNote(new GeneNote
                            {
                                pitch = 0,
                                duration = sumOfNotesDurations % 3840
                            }));
                        }
                    }
                    else
                    {
                        MidiBarList[(int.Parse(note.DeltaTime) / 3840) - 1].Notes.Add(GeneNoteToGeneticNote(new GeneNote
                        {
                            pitch = 0,
                            duration = restduration * 80
                        }));
                    }
                }

                int noteduration = (int)Math.Round(double.Parse(note.Duration) / 80);

                sumOfNotesDurations += noteduration * 80;
                MidiBarList[(int.Parse(note.DeltaTime) / 3840) - 1].Notes.Add(GeneNoteToGeneticNote(new GeneNote
                {
                    pitch = int.Parse(note.Pitch),
                    duration = noteduration * 80
                }));
            }
        }

        private int FindEndBar(List<IList<MidiEvent>> ilist)
        {
            NoteEvent ne;
            int endBarNum = 0;
            int BarNum;

            foreach (IList<MidiEvent> list in ilist)
            {
                foreach (MidiEvent note in list)
                {
                    if (note is NoteOnEvent)
                    {
                        ne = (NoteOnEvent)note;

                        char[] delimiterChars = { ' ' };
                        string[] words = note.ToString().Split(delimiterChars);

                        BarNum = Convert.ToInt32(words[0]);
                        if (endBarNum < BarNum)
                            endBarNum = BarNum;
                    }
                }
            }

            return endBarNum;
        }
        
        private void ImportMidiEvent(List<IList<MidiEvent>> ilist, int iDevideDeltaTime)
        {
            int temp;
            Hashtable ht_trackTable = new Hashtable();
            Events = new EventDataSet();

            // Traveling the midifile's indivisual note
            NoteEvent ne;

            int iNoteIndex = 0;
            
            // Melody Track
            foreach (MidiEvent note in ilist[2])
            {
                if (note is NoteOnEvent)
                {
                    if (note.ToString().IndexOf("Note Off") == -1) // Note On
                    {
                        // Note Setting
                        ne = (NoteOnEvent)note;

                        char[] delimiterChars = { ' ' };
                        string[] words = note.ToString().Split(delimiterChars);

                        if (int.TryParse(words[7], out temp) == false)
                        {
                            continue;
                        } // Length 를 보고 숫자가 아니라는건 노트가 아니라는 것이라 넘긴다
                        
                        Events.Note.Rows.Add(new object[8]{Convert.ToInt32(words[0]), ne.Channel, note.CommandCode,
                            Convert.ToInt32(words[7]), ne.NoteNumber, ne.Velocity, "", 0}); // db에 넣는다
                    }
                    else
                    {
                        char[] delimiterChars = { ' ' };
                        string[] words = note.ToString().Split(delimiterChars);
                        iNoteIndex = (Convert.ToInt32(words[0]) - (iDevideDeltaTime * 4)) / (iDevideDeltaTime * 2);
                    }
                }
            }
            
            #region Chord Track

            //List<Pitch> pitchs = new List<Pitch>();
            //List<Chord> chords;
            //string sPrevPlayed = "";
            //string sPrevNote = "";

            //foreach (MidiEvent note in ilist[3]) // Chord Track
            //{
            //    if (note is NoteOnEvent)
            //    {
            //        if (note.ToString().IndexOf("Note Off") == -1) // Note On
            //        {
            //            // Note Setting
            //            ne = (NoteOnEvent)note;

            //            char[] delimiterChars = { ' ' };
            //            string[] words = note.ToString().Split(delimiterChars);

            //            if (sPrevPlayed != words[0])
            //            {
            //                chords = Chord.FindMatchingChords(pitchs);

            //                if (chords.Count != 0)
            //                    dataSet.Chord.Rows.Add(new object[5] { Convert.ToInt32(sPrevPlayed), sPrevNote, chords[0].Name, "", chords[0].Bass });

            //                pitchs = new List<Pitch>();
            //                pitchs.Add((Pitch)ne.NoteNumber);
            //            }
            //            else
            //            {
            //                pitchs.Add((Pitch)ne.NoteNumber);
            //            }

            //            if (int.TryParse(words[7], out temp) == false)
            //            {
            //                continue;
            //            } // Non note trouble preventation

            //            sPrevPlayed = words[0];
            //            sPrevNote = words[4];
            //        }
            //    }
            //}

            //// Last Chord
            ////chords = Chord.FindMatchingChords(pitchs);
            ////if (chords.Count != 0)
            ////    dataSet.Chord.Rows.Add(new object[5] { Convert.ToInt32(sPrevPlayed), sPrevNote, chords[0].Pattern.Name, "", chords[0].Bass });

            //// Bass Track
            //foreach (MidiEvent note in ilist[4])
            //{
            //    if (note is NoteOnEvent)
            //    {
            //        if (note.ToString().IndexOf("Note Off") == -1) // Note On
            //        {
            //            // Note Setting
            //            ne = (NoteOnEvent)note;

            //            char[] delimiterChars = { ' ' };
            //            string[] words = note.ToString().Split(delimiterChars);

            //            if (int.TryParse(words[7], out temp) == false)
            //            {
            //                continue;
            //            } // Length 를 보고 숫자가 아니라는건 노트가 아니라는 것이라 넘긴다

            //            dataSet.BassNote.Rows.Add(new object[8]{Convert.ToInt32(words[0]), ne.Channel, note.CommandCode,
            //                Convert.ToInt32(words[7]), ne.NoteNumber + plusBassTone, 65, "", 0}); // db에 넣는다
            //        }
            //        else
            //        {
            //            char[] delimiterChars = { ' ' };
            //            string[] words = note.ToString().Split(delimiterChars);
            //            iNoteIndex = (Convert.ToInt32(words[0]) - (iDevideDeltaTime * 4)) / (iDevideDeltaTime * 2);
            //        }
            //    }
            //}

            #endregion
        }

        #endregion

        #region GA Public Methods

        public void MakeChordListUsingGA()
        {
            ChordListUsingGA = new ChordTableEnum[GABarList.Count][];
            int thisOffSet = 0;
            int thisIndex = 0;

            for (int i = 0; i < GABarList.Count; i++)
            {
                ChordListUsingGA[i] = new ChordTableEnum[StaticRepo.ConfigRepository.MaxNumOfNote];
                for (int j = 0; j < StaticRepo.ConfigRepository.MaxNumOfNote; j++)
                {
                    if (ChordList[thisIndex].OffSet <= thisOffSet)
                    {
                        thisIndex++;
                        if (thisIndex >= ChordList.Count)
                        {
                            break;
                        }
                    }

                    ChordListUsingGA[i][j] = ChordToGeneticChord(ChordList[thisIndex - 1].Chord);
                    thisOffSet += StaticRepo.ConfigRepository.TickPerMinNote;
                }
            }
        }

        public void GenerateRandomNotes(int BarNumber)
        {
            if (GABarList.Count - 1 < BarNumber)
            {
                throw new Exception(String.Format("마디 수를 벗어난 {0}에 노트를 만들려고 합니다. (현재 마디 수 : {1})", BarNumber, GABarList.Count));
            }

            while (GABarList[BarNumber].Notes.Sum(note => note.Duration) < 48)
            {
                GABarList[BarNumber].Notes.Add(new GeneticGraphicNote
                {
                    Pitch = (GeneticPitch)random.Next((int)GeneticPitch.A3, (int)GeneticPitch.C6),
                    Duration = random.Next(2, 5) * 3,
                    isSharp = false,
                    isRest = random.Next(0,2) == 0 ? true : false
                });
            }

            StaticVM.ScoreViewModel.ScrollLocker = true;
            CheckNotes(BarNumber);
        }

        public void GenerateMoneyChord()
        {
            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("CMaj7"),
                OffSet = 0
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Bm7"),
                OffSet = 960 * 4
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Em7"),
                OffSet = 960 * 6
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Am7"),
                OffSet = 960 * 8
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Gm7"),
                OffSet = 960 * 12
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("C7"),
                OffSet = 960 * 14
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("F7"),
                OffSet = 960 * 16
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Fm7"),
                OffSet = 960 * 20
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Em7"),
                OffSet = 960 * 24
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Am7"),
                OffSet = 960 * 26
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Dm7"),
                OffSet = 960 * 28
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Gm7"),
                OffSet = 960 * 30
            });


            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("CMaj7"),
                OffSet = 960 * 32
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Bm7"),
                OffSet = 960 * 36
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Em7"),
                OffSet = 960 * 38
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Am7"),
                OffSet = 960 * 40
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Gm7"),
                OffSet = 960 * 44
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("C7"),
                OffSet = 960 * 46
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("F7"),
                OffSet = 960 * 48
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Fm7"),
                OffSet = 960 * 52
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Em7"),
                OffSet = 960 * 56
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Am7"),
                OffSet = 960 * 58
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Dm7"),
                OffSet = 960 * 60
            });

            ChordList.Add(new GeneticChord
            {
                Chord = new Chord("Gm7"),
                OffSet = 960 * 62
            });
        }

        /// <summary>
        /// population 에서 가장 좋은걸 뽑아서 악보에 표기한다
        /// </summary>
        public void MakeNotesUsingGA()
        {
            GABarList.Clear();
            Chromosome bestChromosome;

            for (int i = 0; i < StaticRepo.GARepository.iGA.NumOfBar; i++)
            {
                GABarList.Add(new GeneticBar());
                bestChromosome = StaticRepo.GARepository.iGA.ChromosomeList[i].OrderByDescending(c => c.Fitness).First();

                for (int j = 0; j < bestChromosome.Length; j++)
                {
                    GABarList[i].Notes.Add(GeneNoteToGeneticNote(bestChromosome.Note[j]));
                }
            }
        }

        public void CheckNotes(int BarNumber)
        {
            if (GABarList.Count - 1 < BarNumber)
            {
                throw new Exception(String.Format("마디 수를 벗어난 {0}를 체크하려고 합니다. (현재 마디 수 : {1})", BarNumber, GABarList.Count));
            }

            // Duration 의 합이 48을 넘으면 줄이는 형식으로 마디에 들어가는 음표를 맞춤
            while (GABarList[BarNumber].Notes.Sum(note => note.Duration) > 48)
            {
                if (GABarList[BarNumber].Notes.Last().Duration != 3)
                {
                    GABarList[BarNumber].Notes.Last().Duration-=3;
                }
                else
                {
                    GABarList[BarNumber].Notes.Remove(GABarList[BarNumber].Notes.Last());
                }
            }
        }
        #endregion

        #region Private Methods

        private GeneticGraphicNote GeneNoteToGeneticNote(GeneNote note)
        {
            GeneticGraphicNote resultNote = new GeneticGraphicNote
            {
                Duration = note.duration / 80,
                isRest = note.pitch == 0
            };

            if(((int)note.pitch) == 0)
            {
                resultNote.Pitch = 0;
                resultNote.isSharp = false;
            }
            else
            {
                switch (((int)note.pitch) % 12)
                {
                    case 0: // C
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 1);
                        resultNote.isSharp = false;
                        break;
                    case 1: // C#
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 1);
                        resultNote.isSharp = true;
                        break;
                    case 2: // D
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 2);
                        resultNote.isSharp = false;
                        break;
                    case 3: // D#
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 2);
                        resultNote.isSharp = true;
                        break;
                    case 4: // E
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 3);
                        resultNote.isSharp = false;
                        break;
                    case 5: // F
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 4);
                        resultNote.isSharp = false;
                        break;
                    case 6: // F#
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 4);
                        resultNote.isSharp = true;
                        break;
                    case 7: // G
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 5);
                        resultNote.isSharp = false;
                        break;
                    case 8: // G#
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 5);
                        resultNote.isSharp = true;
                        break;
                    case 9: // A
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 6);
                        resultNote.isSharp = false;
                        break;
                    case 10: // A#
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 6);
                        resultNote.isSharp = true;
                        break;
                    case 11: // B
                        resultNote.Pitch = (GeneticPitch)((((int)note.pitch - 48) / 12) * 7 + 7);
                        resultNote.isSharp = false;
                        break;
                }
            }

            return resultNote;
        }

        #endregion

    }
}
