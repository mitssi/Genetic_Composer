using GA_Composer.Models;
using GA_Composer.Repositories;
using GA_Composer.Utils;
using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.ViewModels
{
    public class TableViewModel
    {
        public void SaveTables()
        {
            Serializer.SerializeObject(StaticRepo.TableRepository.SavedTable, "table.xml");
        }

        /// <summary>
        /// MIDI 악보를 불러온 상태에서 MIDI 를 학습함
        /// </summary>
        /// <param name="score">부여할 점수</param>
        public void Learning(int score, List<GeneticBar> BarList)
        {
            #region Sequence Table

            foreach (var bar in BarList)
            {
                Sequence thisSequence = StaticRepo.TableRepository.SavedTable.SequenceTable;
                var notzeronote = bar.Notes.Where(note => note.isRest == false);

                #region Patterns

                int offset = 0;
                int noteoffset = 0;
                int[] bit = new int[6];

                foreach(var v in bar.Notes)
                {
                    noteoffset = 0;

                    while (noteoffset * 4 < v.Duration) // v.Duration 은 한 박자에 12 라서 8분 셋잇단음표는 4
                    {
                        // bit[(offset + noteoffset) % 6] = v.pitch == 0 ? 0 : 1;
                        bit[offset % 6] = 1;
                        noteoffset += 1;

                        if((offset + noteoffset) % 6 == 0) // bit 다 채웠을 때
                        {
                            thisSequence.patterns[32 * bit[0] + 16 * bit[1] + 8 * bit[2] + 4 * bit[3] + 2 * bit[4] + 1 * bit[5]] += score;

                            for (int i = 0; i < 6; i++)
                            {
                                bit[i] = 0;
                            }
                        }
                    }

                    offset += noteoffset;
                }

                #endregion

                // theNumberOfNotes
                thisSequence.theNumberOfNotes[bar.Notes.Count()-1] += score;

                // theNumberOfChange, betweenNotesPitch, Length
                int prevUpanddown = 0; // + 면 상승 - 면 하강
                int allChange = 0;
                GeneticNote prevNote = new GeneticNote();
                foreach (var note in notzeronote)
                {
                    if (bar.Notes.IndexOf(note) != 0)
                    {
                        int thisUpanddown = (int)note.ToPitch - (int)prevNote.ToPitch;

                        if (Math.Abs(thisUpanddown) < 13)
                        {
                            thisSequence.betweenNotesPitch[Math.Abs(thisUpanddown)] += score;
                        }
                        else
                        {
                            thisSequence.betweenNotesPitch[13] += score;
                        }

                        if (thisUpanddown * prevUpanddown < 0)
                        {
                            allChange++;
                        }

                        prevNote = note;
                        prevUpanddown = thisUpanddown;
                    }
                    else
                    {
                        prevNote = note;
                    }

                    #region Length

                    thisSequence.theNumberOfNoteLength[note.Duration / 4 - 1] +=score;

                    #endregion

                }

                thisSequence.theNumberOfChange[allChange] += score;
            
                // betweenHighAndLow
                int high = notzeronote.Max(note => (int)note.ToPitch);
                int low = notzeronote.Min(note => (int)note.ToPitch);

                if (high - low < StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow)
                {
                    thisSequence.betweenHighAndLow[high - low] += score;
                }
                else
                {
                    thisSequence.betweenHighAndLow[StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow-1] += score;
                }

                thisSequence.SetAll();
            }

            StaticVM.MainViewModel.SqeuenceTableDataBinding();
            StaticVM.MainViewModel.SequenceTableDocument.Refresh();

            #endregion

            #region Chord Table
            int barNumber = 0;

            foreach (var bar in BarList)
            {
                // 12개로 쪼갬
                GeneticNote[] gn = new GeneticNote[12];

                int index = 0;
                foreach (var note in bar.Notes)
                {
                    int offset = 0;
                    while (offset < note.Duration)
                    {
                        gn[index] = new GeneticNote();
                        gn[index].Pitch = note.Pitch;
                        gn[index].Duration = note.Duration;
                        gn[index].isSharp = note.isSharp;
                        gn[index].isRest = note.isRest;
                        index++;
                        offset += 4;
                    }
                }

                #region Chordtable[0] - 음 하나

                for (int i = 0; i < index; i++)
                {
                    StaticRepo.TableRepository.SavedTable.Chordtable
                        [StaticRepo.ScoreRepository.ChordListUsingGA[barNumber][i].Index()]
                        [0][(int)gn[i].ToPitch % 12] += score;
                }

                #endregion

                #region Chordtable[1~] - 음 여러개

                // 코드에서 다른 코드로 이동할 때 변경되는 것은 점수를 주지 않는다

                GeneticNote prevNote = new GeneticNote();
                int prevOffset = 0;
                int thisOffset = 0;
                
                foreach (var note in bar.Notes)
                {
                    if (bar.Notes.IndexOf(note) != 0)
                    {
                        thisOffset = prevOffset + prevNote.Duration;
                        // 만일 이 노트의 코드가 전 노트의 코드와 같다면
                        if (gn[StaticRepo.ScoreRepository.ChordListUsingGA[barNumber][prevOffset/4].Index()] 
                            == gn[StaticRepo.ScoreRepository.ChordListUsingGA[barNumber][thisOffset/4].Index()])
                        {
                            StaticRepo.TableRepository.SavedTable.Chordtable
                                [StaticRepo.ScoreRepository.ChordListUsingGA[barNumber][prevOffset/4].Index()]
                                [((int)prevNote.ToPitch % 12) + 1]
                                [(int)note.ToPitch % 12] += score;
                        }
                        prevOffset = thisOffset;
                        prevNote = note;
                    }
                    else
                    {
                        prevNote = note;
                    }
                }

                StaticVM.MainViewModel.ScoreDocument.Refresh();
                #endregion

                barNumber++;
            }

            StaticRepo.TableRepository.ConvertChordTable();
            #endregion
        }
    }
}
